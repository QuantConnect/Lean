/*
 * Cascade Labs - Hyperliquid S3 Client
 *
 * Downloads historical trade data from Hyperliquid's public S3 bucket.
 * Handles LZ4 decompression and temporary file caching.
 * Bucket is requester-pays (hl-mainnet-node-data).
 */

using System.Collections.Concurrent;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using K4os.Compression.LZ4.Streams;
using QuantConnect.Logging;
using QuantConnect.Configuration;

namespace QuantConnect.Lean.DataSource.CascadeHyperliquid
{
    /// <summary>
    /// S3 client for downloading Hyperliquid historical trade data
    /// </summary>
    /// <remarks>
    /// Data sources on S3 (bucket: hl-mainnet-node-data):
    /// - node_trades/hourly/YYYYMMDD/HH.lz4 - Trade fills (March-June 2025)
    /// - node_fills_by_block/hourly/YYYYMMDD/HH.lz4 - Block-based fills (July 2025+)
    ///
    /// All files are LZ4 compressed. Each hourly file contains fills for ALL coins.
    /// The bucket uses requester-pays, so AWS credentials with S3 read access are required.
    ///
    /// Downloaded files are cached in the user-configured data folder under hyperliquid/s3_cache/.
    /// This cache persists across runs to avoid re-downloading the same hourly files.
    /// </remarks>
    public class HyperliquidS3Client : IDisposable
    {
        private const string BucketName = "hl-mainnet-node-data";
        private const string NodeTradesPrefix = "node_trades/hourly";
        private const string NodeFillsByBlockPrefix = "node_fills_by_block/hourly";
        private const int MaxRetries = 3;

        private readonly AmazonS3Client? _client;
        private readonly string _cacheDir;
        private readonly ConcurrentDictionary<string, Lazy<string?>> _downloadCache = new();
        private readonly Random _jitterRandom = new();
        private bool _disposed;

        /// <summary>
        /// Whether S3 credentials are configured and the client is available
        /// </summary>
        public bool IsConfigured { get; }

        /// <summary>
        /// Initializes a new instance of the HyperliquidS3Client
        /// </summary>
        public HyperliquidS3Client()
        {
            var accessKey = Config.Get("hyperliquid-aws-access-key-id", "");
            var secretKey = Config.Get("hyperliquid-aws-secret-access-key", "");
            var region = Config.Get("hyperliquid-aws-region", "ap-northeast-1");

            // Cache in user-configured data folder, not temp — persists across runs
            var dataFolder = Config.Get("data-folder", "../../../Data/");
            _cacheDir = Path.Combine(dataFolder, "hyperliquid", "s3_cache");

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            {
                Log.Trace("HyperliquidS3Client: AWS credentials not configured, S3 data will not be available");
                IsConfigured = false;
                return;
            }

            try
            {
                var config = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(region)
                };

                _client = new AmazonS3Client(accessKey, secretKey, config);
                IsConfigured = true;

                Directory.CreateDirectory(_cacheDir);
                Log.Trace($"HyperliquidS3Client: Initialized with region {region}, temp cache: {_cacheDir}");
            }
            catch (Exception ex)
            {
                Log.Error($"HyperliquidS3Client: Failed to initialize: {ex.Message}");
                IsConfigured = false;
            }
        }

        /// <summary>
        /// Downloads and decompresses an hourly file from S3, using a temp disk cache.
        /// Concurrent requests for the same key share a single download via Lazy.
        /// </summary>
        /// <param name="prefix">S3 prefix (e.g., "node_trades/hourly")</param>
        /// <param name="date">Date string YYYYMMDD</param>
        /// <param name="hour">Hour string (0-23)</param>
        /// <returns>Decompressed data as a stream, or null if not found</returns>
        public Stream? DownloadAndDecompress(string prefix, string date, int hour)
        {
            if (!IsConfigured || _client == null) return null;

            var key = $"{prefix}/{date}/{hour}.lz4";

            // GetOrAdd with Lazy ensures only one thread downloads a given key;
            // all other concurrent callers block on the same Lazy until it completes.
            var lazy = _downloadCache.GetOrAdd(key, k => new Lazy<string?>(() => DownloadToTempFile(k, prefix, date, hour)));
            var tempFile = lazy.Value;

            if (tempFile == null)
            {
                // Download failed or file not found — remove so next caller can retry
                _downloadCache.TryRemove(key, out _);
                return null;
            }

            return File.OpenRead(tempFile);
        }

        /// <summary>
        /// Downloads and LZ4-decompresses a single S3 object to a temp file with retries.
        /// Returns the temp file path, or null if the file doesn't exist or all retries are exhausted.
        /// </summary>
        private string? DownloadToTempFile(string key, string prefix, string date, int hour)
        {
            var cacheFile = Path.Combine(_cacheDir, prefix.Replace('/', '_'), $"{date}_{hour:D2}.json");

            // Check temp cache first (survives across Lazy recreations)
            if (File.Exists(cacheFile))
            {
                Log.Trace($"HyperliquidS3Client: Temp cache hit for {key}");
                return cacheFile;
            }

            var retryCount = 0;
            while (true)
            {
                try
                {
                    var request = new GetObjectRequest
                    {
                        BucketName = BucketName,
                        Key = key,
                        RequestPayer = RequestPayer.Requester
                    };

                    var response = _client!.GetObjectAsync(request).GetAwaiter().GetResult();

                    var cacheDir = Path.GetDirectoryName(cacheFile)!;
                    Directory.CreateDirectory(cacheDir);

                    using (var lz4Stream = LZ4Stream.Decode(response.ResponseStream))
                    using (var fileStream = File.Create(cacheFile))
                    {
                        lz4Stream.CopyTo(fileStream);
                    }

                    var fileInfo = new FileInfo(cacheFile);
                    Log.Trace($"HyperliquidS3Client: Downloaded and cached {key} -> {cacheFile} ({fileInfo.Length:N0} bytes)");
                    return cacheFile;
                }
                catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Log.Trace($"HyperliquidS3Client: File not found: {key}");
                    return null;
                }
                catch (Exception ex)
                {
                    if (retryCount >= MaxRetries)
                    {
                        Log.Error($"HyperliquidS3Client: Max retries exceeded for {key}: {ex.Message}");
                        return null;
                    }

                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount)) +
                                TimeSpan.FromMilliseconds(_jitterRandom.Next(0, 1000));

                    Log.Trace($"HyperliquidS3Client: Retry {retryCount + 1}/{MaxRetries} for {key} after {delay.TotalSeconds:F1}s: {ex.Message}");

                    Thread.Sleep(delay);
                    retryCount++;
                }
            }
        }

        /// <summary>
        /// Gets the appropriate S3 prefix for a given date, trying node_fills_by_block first (newer),
        /// then falling back to node_trades
        /// </summary>
        /// <param name="date">Date string YYYYMMDD</param>
        /// <returns>S3 prefix to use, or null if no data is available</returns>
        public string? GetPrefixForDate(string date)
        {
            if (!IsConfigured || _client == null) return null;

            // Try node_fills_by_block first (newer, more comprehensive)
            if (PrefixHasData(NodeFillsByBlockPrefix, date))
            {
                return NodeFillsByBlockPrefix;
            }

            // Fall back to node_trades
            if (PrefixHasData(NodeTradesPrefix, date))
            {
                return NodeTradesPrefix;
            }

            return null;
        }

        /// <summary>
        /// Checks whether S3 has data for a specific date under the given prefix
        /// </summary>
        private bool PrefixHasData(string prefix, string date)
        {
            if (!IsConfigured || _client == null) return false;

            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = BucketName,
                    Prefix = $"{prefix}/{date}/",
                    MaxKeys = 1,
                    RequestPayer = RequestPayer.Requester
                };

                var response = _client.ListObjectsV2Async(request).GetAwaiter().GetResult();
                return response.S3Objects.Count > 0;
            }
            catch (Exception ex)
            {
                Log.Trace($"HyperliquidS3Client: Error checking prefix {prefix}/{date}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// The node_trades S3 prefix
        /// </summary>
        public static string NodeTradesPrefixPath => NodeTradesPrefix;

        /// <summary>
        /// The node_fills_by_block S3 prefix
        /// </summary>
        public static string NodeFillsByBlockPrefixPath => NodeFillsByBlockPrefix;

        /// <summary>
        /// Disposes of the S3 client. The cache directory is NOT deleted — it persists across runs.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _downloadCache.Clear();
                _client?.Dispose();
                _disposed = true;
            }
        }
    }
}
