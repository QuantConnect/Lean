/*
 * Cascade Labs - S3 TradeAlert Client
 * Downloads TradeAlert parquet data from S3-compatible storage (OCI Object Storage)
 */

using Amazon.S3;
using Amazon.S3.Model;
using QuantConnect.Logging;
using QuantConnect.Configuration;

namespace QuantConnect.Lean.DataSource.CascadeTradeAlert
{
    /// <summary>
    /// S3 client for downloading TradeAlert parquet data from OCI Object Storage
    /// </summary>
    public class S3TradeAlertClient : IDisposable
    {
        private readonly string _endpoint;
        private readonly string _bucket;
        private readonly string _region;
        private readonly AmazonS3Client? _client;
        private bool _disposed;

        /// <summary>
        /// Whether S3 is configured and available
        /// </summary>
        public bool IsConfigured { get; }

        /// <summary>
        /// Initializes a new instance of the S3TradeAlertClient
        /// </summary>
        public S3TradeAlertClient()
        {
            _endpoint = Config.Get("tradealert-s3-endpoint", "");
            _bucket = Config.Get("tradealert-s3-bucket", "");
            _region = Config.Get("tradealert-s3-region", "us-ashburn-1");
            var accessKey = Config.Get("tradealert-s3-access-key", "");
            var secretKey = Config.Get("tradealert-s3-secret-key", "");

            if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_bucket) ||
                string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            {
                Log.Trace("S3TradeAlertClient: S3 not configured, TradeAlert data will not be available");
                IsConfigured = false;
                return;
            }

            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{_endpoint}",
                ForcePathStyle = true,
                SignatureVersion = "4",
                AuthenticationRegion = _region
            };

            _client = new AmazonS3Client(accessKey, secretKey, config);
            IsConfigured = true;

            Log.Trace($"S3TradeAlertClient: Initialized with endpoint {_endpoint}, bucket {_bucket}");
        }

        /// <summary>
        /// Downloads a file from S3
        /// </summary>
        /// <param name="key">S3 object key</param>
        /// <returns>File contents as byte array, or null if not found</returns>
        public async Task<byte[]?> DownloadAsync(string key)
        {
            if (!IsConfigured || _client == null)
            {
                return null;
            }

            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _bucket,
                    Key = key
                };

                using var response = await _client.GetObjectAsync(request);
                using var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);

                Log.Debug($"S3TradeAlertClient: Downloaded {memoryStream.Length} bytes from {key}");
                return memoryStream.ToArray();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Log.Debug($"S3TradeAlertClient: File not found: {key}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"S3TradeAlertClient: Error downloading {key}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Downloads a file from S3 (synchronous)
        /// </summary>
        /// <param name="key">S3 object key</param>
        /// <returns>File contents as byte array, or null if not found</returns>
        public byte[]? Download(string key)
        {
            return DownloadAsync(key).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Lists files in S3 with a given prefix
        /// </summary>
        /// <param name="prefix">S3 key prefix</param>
        /// <param name="limit">Maximum number of files to return (0 for all)</param>
        /// <returns>List of S3 keys</returns>
        public async Task<List<string>> ListFilesAsync(string prefix, int limit = 0)
        {
            if (!IsConfigured || _client == null)
            {
                return new List<string>();
            }

            var keys = new List<string>();

            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = _bucket,
                    Prefix = prefix
                };

                ListObjectsV2Response response;
                do
                {
                    response = await _client.ListObjectsV2Async(request);

                    foreach (var obj in response.S3Objects)
                    {
                        keys.Add(obj.Key);
                        if (limit > 0 && keys.Count >= limit)
                        {
                            return keys;
                        }
                    }

                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated);

                Log.Debug($"S3TradeAlertClient: Listed {keys.Count} files with prefix {prefix}");
                return keys;
            }
            catch (Exception ex)
            {
                Log.Error($"S3TradeAlertClient: Error listing files with prefix {prefix}: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Lists files in S3 with a given prefix (synchronous)
        /// </summary>
        public List<string> ListFiles(string prefix, int limit = 0)
        {
            return ListFilesAsync(prefix, limit).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Checks if a file exists in S3
        /// </summary>
        public async Task<bool> FileExistsAsync(string key)
        {
            if (!IsConfigured || _client == null)
            {
                return false;
            }

            try
            {
                await _client.GetObjectMetadataAsync(_bucket, key);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Disposes of the S3 client
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of managed resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _client?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
