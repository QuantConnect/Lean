/*
 * Cascade Labs - TradeAlert Base Provider
 * Base class for TradeAlert data providers with shared S3 and parquet functionality
 */

using Parquet;
using Parquet.Data;
using QuantConnect.Logging;
using QuantConnect.Configuration;
using QuantConnect.Lean.DataSource.CascadeCommon;

namespace QuantConnect.Lean.DataSource.CascadeTradeAlert
{
    /// <summary>
    /// Base class for TradeAlert data providers
    /// </summary>
    public abstract class TradeAlertBaseProvider : IDisposable
    {
        /// <summary>
        /// Shared OCI S3 client
        /// </summary>
        protected readonly CascadeS3Client S3Client;

        /// <summary>
        /// TradeAlert S3 bucket name
        /// </summary>
        protected readonly string S3Bucket;

        /// <summary>
        /// Local data path for cached parquet files (uses LEAN's standard alternative data directory)
        /// </summary>
        protected readonly string LocalDataPath;

        /// <summary>
        /// Data type this provider handles
        /// </summary>
        protected abstract TradeAlertDataType DataType { get; }

        /// <summary>
        /// Default symbol for aggregated data
        /// </summary>
        protected const string DefaultSymbol = "_ALL";

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the TradeAlertBaseProvider
        /// </summary>
        protected TradeAlertBaseProvider()
        {
            S3Client = new CascadeS3Client();
            S3Bucket = Config.Get("tradealert-s3-bucket", "");
            // Use LEAN's standard alternative data directory: {DataFolder}/alternative/tradealert
            LocalDataPath = Path.Combine(Globals.DataFolder, "alternative", "tradealert");
            Log.Trace($"TradeAlertBaseProvider: Local cache path: {LocalDataPath}");
        }

        /// <summary>
        /// Whether the provider is properly configured and available (local path or S3)
        /// </summary>
        public bool IsConfigured => (S3Client.IsConfigured && !string.IsNullOrEmpty(S3Bucket)) || Directory.Exists(LocalDataPath);

        /// <summary>
        /// Gets data for a specific timestamp
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="symbol">Symbol (default _ALL)</param>
        /// <returns>List of data records as dictionaries</returns>
        public virtual List<Dictionary<string, object?>> GetData(DateTime timestamp, string symbol = DefaultSymbol)
        {
            if (!IsConfigured)
            {
                Log.Trace($"{GetType().Name}: S3 not configured");
                return new List<Dictionary<string, object?>>();
            }

            // Note: LEAN Algorithm.Time is already in exchange local time (Eastern for US equities)
            // The ConvertToEastern would double-convert, so we use the timestamp directly
            // TODO: Consider adding a flag for UTC vs local input
            var easternTime = timestamp;

            // Round to 5-minute interval for non-snapshot data
            if (DataType != TradeAlertDataType.Snapshot)
            {
                easternTime = TradeAlertPathUtils.RoundTo5Min(easternTime);
                // TradeAlert files use end-of-bar timestamps (e.g., 0935 for 9:30-9:35 bar)
                // At 9:35, we access 0935.parquet which contains the just-completed 9:30-9:35 bar
                // NO AddMinutes(5) - that would cause look-ahead bias by accessing future data
            }

            var s3Path = TradeAlertPathUtils.GetS3Path(DataType, symbol, easternTime);
            Log.Debug($"{GetType().Name}: GetData for {timestamp:HH:mm} -> {easternTime:HH:mm} -> {s3Path}");
            return DownloadAndParseParquet(s3Path);
        }

        /// <summary>
        /// Gets data for a date range
        /// </summary>
        /// <param name="startUtc">Start time in UTC</param>
        /// <param name="endUtc">End time in UTC</param>
        /// <param name="symbol">Symbol (default _ALL)</param>
        /// <returns>List of data records as dictionaries</returns>
        public virtual List<Dictionary<string, object?>> GetDataRange(DateTime startUtc, DateTime endUtc, string symbol = DefaultSymbol)
        {
            if (!IsConfigured)
            {
                Log.Trace($"{GetType().Name}: S3 not configured");
                return new List<Dictionary<string, object?>>();
            }

            var allRecords = new List<Dictionary<string, object?>>();
            var startEastern = TradeAlertPathUtils.ConvertToEastern(startUtc);
            var endEastern = TradeAlertPathUtils.ConvertToEastern(endUtc);

            // Iterate through each day in the range
            for (var date = startEastern.Date; date <= endEastern.Date; date = date.AddDays(1))
            {
                var dayRecords = GetDataForDate(date, symbol, startEastern, endEastern);
                allRecords.AddRange(dayRecords);
            }

            return allRecords;
        }

        /// <summary>
        /// Gets data for a specific date
        /// </summary>
        protected virtual List<Dictionary<string, object?>> GetDataForDate(
            DateTime date,
            string symbol,
            DateTime startEastern,
            DateTime endEastern)
        {
            var prefix = TradeAlertPathUtils.GetS3PrefixForDate(DataType, symbol, date);
            var files = ListFilesWithLocalFallback(prefix);

            if (files.Count == 0)
            {
                return new List<Dictionary<string, object?>>();
            }

            var allRecords = new List<Dictionary<string, object?>>();

            foreach (var file in files.OrderBy(f => f))
            {
                var timestamp = TradeAlertPathUtils.ParseTimestampFromPath(file);
                if (timestamp == null) continue;

                // Check if within time range
                if (timestamp < startEastern || timestamp > endEastern) continue;

                var records = DownloadAndParseParquet(file);
                allRecords.AddRange(records);
            }

            return allRecords;
        }

        /// <summary>
        /// Lists files, checking local directory first then S3
        /// </summary>
        /// <param name="s3Prefix">S3 prefix to search</param>
        /// <returns>List of S3-style paths (even for local files)</returns>
        protected List<string> ListFilesWithLocalFallback(string s3Prefix)
        {
            // Try local directory first
            var localDir = GetLocalPath(s3Prefix);
            if (Directory.Exists(localDir))
            {
                var localFiles = Directory.GetFiles(localDir, "*.parquet")
                    .Select(f => ConvertLocalPathToS3Path(f))
                    .ToList();

                if (localFiles.Count > 0)
                {
                    Log.Debug($"{GetType().Name}: Found {localFiles.Count} local files for prefix {s3Prefix}");
                    return localFiles;
                }
            }

            // Fall back to S3
            if (!S3Client.IsConfigured)
            {
                return new List<string>();
            }

            return S3Client.ListFiles(S3Bucket, s3Prefix);
        }

        /// <summary>
        /// Converts a local file path back to S3-style path for consistent handling
        /// </summary>
        protected string ConvertLocalPathToS3Path(string localPath)
        {
            // Get path relative to LocalDataPath
            var relativePath = localPath.Substring(LocalDataPath.Length).TrimStart(Path.DirectorySeparatorChar);
            // Convert to S3-style path with forward slashes and tradealert prefix
            return "tradealert/" + relativePath.Replace(Path.DirectorySeparatorChar, '/');
        }

        /// <summary>
        /// Gets the latest available data
        /// </summary>
        /// <param name="symbol">Symbol (default _ALL)</param>
        /// <returns>Tuple of (records, timestamp) or empty list with null timestamp if not found</returns>
        public virtual (List<Dictionary<string, object?>> Records, DateTime? Timestamp) GetLatestData(string symbol = DefaultSymbol)
        {
            if (!IsConfigured)
            {
                Log.Trace($"{GetType().Name}: Not configured (no local path or S3)");
                return (new List<Dictionary<string, object?>>(), null);
            }

            var prefix = TradeAlertPathUtils.GetS3Prefix(DataType, symbol);
            var files = ListFilesWithLocalFallback(prefix);

            if (files.Count == 0)
            {
                Log.Debug($"{GetType().Name}: No files found with prefix {prefix}");
                return (new List<Dictionary<string, object?>>(), null);
            }

            // Sort and get the latest file
            files.Sort();
            var latestFile = files[^1];

            var timestamp = TradeAlertPathUtils.ParseTimestampFromPath(latestFile);
            var records = DownloadAndParseParquet(latestFile);

            Log.Debug($"{GetType().Name}: Retrieved {records.Count} records from latest file {latestFile}");
            return (records, timestamp);
        }

        /// <summary>
        /// Downloads and parses a parquet file, checking local cache first then S3.
        /// On S3 cache miss, writes to local cache for future use.
        /// </summary>
        protected List<Dictionary<string, object?>> DownloadAndParseParquet(string s3Path)
        {
            try
            {
                // Try local cache first
                var localPath = GetLocalPath(s3Path);
                if (File.Exists(localPath))
                {
                    Log.Debug($"{GetType().Name}: Cache hit: {localPath}");
                    var localBytes = File.ReadAllBytes(localPath);
                    return ParseParquetBytes(localBytes);
                }

                // Fall back to S3
                if (!S3Client.IsConfigured)
                {
                    Log.Debug($"{GetType().Name}: Cache miss and S3 not configured: {s3Path}");
                    return new List<Dictionary<string, object?>>();
                }

                var bytes = S3Client.Download(S3Bucket, s3Path);
                if (bytes == null)
                {
                    Log.Debug($"{GetType().Name}: File not found in S3: {s3Path}");
                    return new List<Dictionary<string, object?>>();
                }

                // Write-through cache: save to local for future use
                try
                {
                    var directory = Path.GetDirectoryName(localPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.WriteAllBytes(localPath, bytes);
                    Log.Debug($"{GetType().Name}: Cached to: {localPath}");
                }
                catch (Exception cacheEx)
                {
                    Log.Error($"{GetType().Name}: Failed to cache {localPath}: {cacheEx.Message}");
                }

                return ParseParquetBytes(bytes);
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}: Error downloading/parsing {s3Path}: {ex.Message}");
                return new List<Dictionary<string, object?>>();
            }
        }

        /// <summary>
        /// Converts S3 path to local file path
        /// </summary>
        /// <param name="s3Path">S3 path (e.g., tradealert/sweeps/_ALL/5min/2022/01/03/0935.parquet)</param>
        /// <returns>Local file path in {DataFolder}/alternative/tradealert/...</returns>
        protected string GetLocalPath(string s3Path)
        {
            // S3 path format: tradealert/sweeps/_ALL/5min/2022/01/03/0935.parquet
            // Local path: {DataFolder}/alternative/tradealert/sweeps/_ALL/5min/2022/01/03/0935.parquet
            // Strip the "tradealert/" prefix since LocalDataPath already includes it
            var relativePath = s3Path;
            if (relativePath.StartsWith("tradealert/"))
            {
                relativePath = relativePath.Substring("tradealert/".Length);
            }
            return Path.Combine(LocalDataPath, relativePath);
        }

        /// <summary>
        /// Parses parquet bytes into a list of dictionaries
        /// </summary>
        protected List<Dictionary<string, object?>> ParseParquetBytes(byte[] bytes)
        {
            var records = new List<Dictionary<string, object?>>();

            using var stream = new MemoryStream(bytes);
            using var reader = ParquetReader.CreateAsync(stream).GetAwaiter().GetResult();

            for (var rowGroupIndex = 0; rowGroupIndex < reader.RowGroupCount; rowGroupIndex++)
            {
                using var rowGroupReader = reader.OpenRowGroupReader(rowGroupIndex);

                // Read all columns
                var columns = new Dictionary<string, DataColumn>();
                foreach (var field in reader.Schema.DataFields)
                {
                    var column = rowGroupReader.ReadColumnAsync(field).GetAwaiter().GetResult();
                    columns[field.Name] = column;
                }

                // Determine row count
                var rowCount = columns.Values.FirstOrDefault()?.Data.Length ?? 0;

                // Convert to row-based dictionary format
                for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    var record = new Dictionary<string, object?>();
                    foreach (var (fieldName, column) in columns)
                    {
                        record[fieldName] = column.Data.GetValue(rowIndex);
                    }
                    records.Add(record);
                }
            }

            Log.Debug($"{GetType().Name}: Parsed {records.Count} records from parquet");
            return records;
        }

        /// <summary>
        /// Disposes of the provider
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
                    S3Client.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
