/*
 * Cascade Labs - Shared OCI S3 Client
 * Single AmazonS3Client configured for OCI Object Storage (S3-compatible API).
 * Used by all Cascade data sources that need OCI S3 access.
 */

using Amazon.S3;
using Amazon.S3.Model;
using QuantConnect.Logging;
using QuantConnect.Configuration;

namespace QuantConnect.Lean.DataSource.CascadeCommon
{
    /// <summary>
    /// Shared S3 client for OCI Object Storage. Configured once from s3-* config keys.
    /// Callers pass the bucket name per operation.
    /// </summary>
    public class CascadeS3Client : IDisposable
    {
        private readonly AmazonS3Client? _client;
        private bool _disposed;

        /// <summary>
        /// Whether OCI S3 credentials are configured
        /// </summary>
        public bool IsConfigured { get; }

        /// <summary>
        /// Initializes the shared OCI S3 client from config
        /// </summary>
        public CascadeS3Client()
        {
            var endpoint = Config.Get("s3-endpoint", "");
            var accessKey = Config.Get("s3-access-key", "");
            var secretKey = Config.Get("s3-secret-key", "");
            var region = Config.Get("s3-region", "us-ashburn-1");

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(accessKey) ||
                string.IsNullOrEmpty(secretKey))
            {
                Log.Trace("CascadeS3Client: OCI S3 not configured");
                IsConfigured = false;
                return;
            }

            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{endpoint}",
                ForcePathStyle = true,
                SignatureVersion = "4",
                AuthenticationRegion = region
            };

            _client = new AmazonS3Client(accessKey, secretKey, config);
            IsConfigured = true;
            Log.Trace($"CascadeS3Client: Initialized with endpoint {endpoint}");
        }

        /// <summary>
        /// Downloads a file from S3
        /// </summary>
        public async Task<byte[]?> DownloadAsync(string bucket, string key)
        {
            if (!IsConfigured || _client == null) return null;

            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = bucket,
                    Key = key
                };

                using var response = await _client.GetObjectAsync(request);
                using var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);

                Log.Debug($"CascadeS3Client: Downloaded {memoryStream.Length} bytes from {bucket}/{key}");
                return memoryStream.ToArray();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"CascadeS3Client: Error downloading {bucket}/{key}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Downloads a file from S3 (synchronous)
        /// </summary>
        public byte[]? Download(string bucket, string key)
        {
            return DownloadAsync(bucket, key).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Uploads data to S3
        /// </summary>
        public async Task UploadAsync(string bucket, string key, byte[] data)
        {
            if (!IsConfigured || _client == null) return;

            var putRequest = new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = new MemoryStream(data)
            };
            await _client.PutObjectAsync(putRequest);
            Log.Debug($"CascadeS3Client: Uploaded {data.Length} bytes to {bucket}/{key}");
        }

        /// <summary>
        /// Uploads data to S3 (synchronous)
        /// </summary>
        public void Upload(string bucket, string key, byte[] data)
        {
            UploadAsync(bucket, key, data).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Lists files in S3 with a given prefix
        /// </summary>
        public async Task<List<string>> ListFilesAsync(string bucket, string prefix, int limit = 0)
        {
            if (!IsConfigured || _client == null) return new List<string>();

            var keys = new List<string>();

            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = bucket,
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

                Log.Debug($"CascadeS3Client: Listed {keys.Count} files in {bucket} with prefix {prefix}");
                return keys;
            }
            catch (Exception ex)
            {
                Log.Error($"CascadeS3Client: Error listing {bucket}/{prefix}: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Lists files in S3 with a given prefix (synchronous)
        /// </summary>
        public List<string> ListFiles(string bucket, string prefix, int limit = 0)
        {
            return ListFilesAsync(bucket, prefix, limit).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Checks if a file exists in S3
        /// </summary>
        public async Task<bool> FileExistsAsync(string bucket, string key)
        {
            if (!IsConfigured || _client == null) return false;

            try
            {
                await _client.GetObjectMetadataAsync(bucket, key);
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
            if (!_disposed)
            {
                _client?.Dispose();
                _disposed = true;
            }
        }
    }
}
