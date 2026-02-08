/*
 * Cascade Labs - Hyperliquid S3 Client
 *
 * Downloads historical trade data from Hyperliquid's public S3 bucket.
 * Handles LZ4 decompression and caches decompressed data to OCI S3
 * via the shared CascadeS3Client.
 * Source bucket is requester-pays (hl-mainnet-node-data).
 */

using System.Collections.Concurrent;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using K4os.Compression.LZ4.Streams;
using QuantConnect.Logging;
using QuantConnect.Configuration;
using QuantConnect.Lean.DataSource.CascadeCommon;

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
    /// Decompressed files are cached to an OCI S3 bucket (via CascadeS3Client with
    /// hyperliquid-s3-bucket) to avoid re-downloading from AWS across runs.
    /// </remarks>
    public class HyperliquidS3Client : IDisposable
    {
        private const string BucketName = "hl-mainnet-node-data";
        private const string NodeTradesPrefix = "node_trades/hourly";
        private const string NodeFillsByBlockPrefix = "node_fills_by_block/hourly";
        private const int MaxRetries = 3;

        private readonly AmazonS3Client? _client;
        private readonly CascadeS3Client _cacheClient;
        private readonly string _cacheBucket;
        private readonly ConcurrentDictionary<string, Lazy<byte[]?>> _downloadCache = new();
        private readonly Random _jitterRandom = new();
        private bool _disposed;

        /// <summary>
        /// Whether AWS S3 credentials are configured and the client is available
        /// </summary>
        public bool IsConfigured { get; }

        /// <summary>
        /// Whether the OCI S3 cache bucket is configured
        /// </summary>
        public bool IsCacheConfigured { get; }

        /// <summary>
        /// Initializes a new instance of the HyperliquidS3Client
        /// </summary>
        public HyperliquidS3Client()
        {
            var accessKey = Config.Get("hyperliquid-aws-access-key-id", "");
            var secretKey = Config.Get("hyperliquid-aws-secret-access-key", "");
            var region = Config.Get("hyperliquid-aws-region", "ap-northeast-1");

            _cacheBucket = Config.Get("hyperliquid-s3-bucket", "");
            _cacheClient = new CascadeS3Client();
            IsCacheConfigured = _cacheClient.IsConfigured && !string.IsNullOrEmpty(_cacheBucket);

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            {
                Log.Trace("HyperliquidS3Client: AWS credentials not configured, S3 data will not be available");
                IsConfigured = false;
                return;
            }

            try
            {
                var awsConfig = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(region)
                };
                _client = new AmazonS3Client(accessKey, secretKey, awsConfig);
                IsConfigured = true;

                if (IsCacheConfigured)
                {
                    Log.Trace($"HyperliquidS3Client: S3 cache configured with bucket {_cacheBucket}");
                }
                else
                {
                    Log.Trace("HyperliquidS3Client: S3 cache not configured, downloads will not be cached");
                }

                Log.Trace($"HyperliquidS3Client: Initialized with AWS region {region}");
            }
            catch (Exception ex)
            {
                Log.Error($"HyperliquidS3Client: Failed to initialize: {ex.Message}");
                IsConfigured = false;
            }
        }

        /// <summary>
        /// Gets the cache key for a given AWS source key (strip .lz4 since cached data is decompressed)
        /// </summary>
        private static string GetCacheKey(string key) => key.Replace(".lz4", ".json");

        /// <summary>
        /// Downloads and decompresses an hourly file from S3. Checks OCI S3 cache first,
        /// falls back to AWS download with LZ4 decompression, and uploads the result to cache.
        /// Concurrent requests for the same key share a single download via Lazy.
        /// </summary>
        /// <param name="prefix">S3 prefix (e.g., "node_trades/hourly")</param>
        /// <param name="date">Date string YYYYMMDD</param>
        /// <param name="hour">Hour (0-23)</param>
        /// <returns>Decompressed data as a stream, or null if not found</returns>
        public Stream? DownloadAndDecompress(string prefix, string date, int hour)
        {
            if (!IsConfigured || _client == null) return null;

            var key = $"{prefix}/{date}/{hour}.lz4";

            // GetOrAdd with Lazy ensures only one thread downloads a given key;
            // all other concurrent callers block on the same Lazy until it completes.
            var lazy = _downloadCache.GetOrAdd(key, k => new Lazy<byte[]?>(() => FetchData(k)));
            var data = lazy.Value;

            if (data == null)
            {
                // Download failed or file not found — remove so next caller can retry
                _downloadCache.TryRemove(key, out _);
                return null;
            }

            return new MemoryStream(data);
        }

        /// <summary>
        /// Fetches decompressed data: checks OCI S3 cache first, then downloads from AWS,
        /// decompresses, and uploads to cache.
        /// </summary>
        private byte[]? FetchData(string key)
        {
            var cacheKey = GetCacheKey(key);

            // Try OCI S3 cache first
            if (IsCacheConfigured)
            {
                try
                {
                    var cached = _cacheClient.Download(_cacheBucket, cacheKey);
                    if (cached != null)
                    {
                        Log.Trace($"HyperliquidS3Client: S3 cache hit for {cacheKey} ({cached.Length:N0} bytes)");
                        return cached;
                    }
                }
                catch (Exception ex)
                {
                    Log.Trace($"HyperliquidS3Client: S3 cache read error for {cacheKey}: {ex.Message}");
                }
            }

            // Download from AWS and decompress
            return DownloadFromAwsAndCache(key, cacheKey);
        }

        /// <summary>
        /// Downloads an LZ4-compressed file from AWS S3, decompresses it, uploads the
        /// decompressed data to the OCI S3 cache, and returns the data as a byte array.
        /// </summary>
        private byte[]? DownloadFromAwsAndCache(string key, string cacheKey)
        {
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

                    byte[] decompressedData;
                    using (var lz4Stream = LZ4Stream.Decode(response.ResponseStream))
                    using (var memoryStream = new MemoryStream())
                    {
                        lz4Stream.CopyTo(memoryStream);
                        decompressedData = memoryStream.ToArray();
                    }

                    Log.Trace($"HyperliquidS3Client: Downloaded and decompressed {key} ({decompressedData.Length:N0} bytes)");

                    // Upload decompressed data to OCI S3 cache
                    if (IsCacheConfigured)
                    {
                        try
                        {
                            _cacheClient.Upload(_cacheBucket, cacheKey, decompressedData);
                            Log.Trace($"HyperliquidS3Client: Cached to S3: {cacheKey}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"HyperliquidS3Client: Failed to cache {cacheKey} to S3: {ex.Message}");
                        }
                    }

                    return decompressedData;
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
        /// Disposes of the S3 clients
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _downloadCache.Clear();
                _client?.Dispose();
                _cacheClient.Dispose();
                _disposed = true;
            }
        }
    }
}
