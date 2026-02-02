/*
 * Cascade Labs - TradeAlert Base Provider
 * Base class for TradeAlert data providers with shared S3 and parquet functionality
 */

using Parquet;
using Parquet.Data;
using QuantConnect.Logging;

namespace QuantConnect.Lean.DataSource.CascadeTradeAlert
{
    /// <summary>
    /// Base class for TradeAlert data providers
    /// </summary>
    public abstract class TradeAlertBaseProvider : IDisposable
    {
        /// <summary>
        /// S3 client for downloading parquet files
        /// </summary>
        protected readonly S3TradeAlertClient S3Client;

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
            S3Client = new S3TradeAlertClient();
        }

        /// <summary>
        /// Whether the provider is properly configured and available
        /// </summary>
        public bool IsConfigured => S3Client.IsConfigured;

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

            var easternTime = TradeAlertPathUtils.ConvertToEastern(timestamp);

            // Round to 5-minute interval for non-snapshot data
            if (DataType != TradeAlertDataType.Snapshot)
            {
                easternTime = TradeAlertPathUtils.RoundTo5Min(easternTime);
            }

            var s3Path = TradeAlertPathUtils.GetS3Path(DataType, symbol, easternTime);
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
            var files = S3Client.ListFiles(prefix);

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
        /// Gets the latest available data
        /// </summary>
        /// <param name="symbol">Symbol (default _ALL)</param>
        /// <returns>Tuple of (records, timestamp) or empty list with null timestamp if not found</returns>
        public virtual (List<Dictionary<string, object?>> Records, DateTime? Timestamp) GetLatestData(string symbol = DefaultSymbol)
        {
            if (!IsConfigured)
            {
                Log.Trace($"{GetType().Name}: S3 not configured");
                return (new List<Dictionary<string, object?>>(), null);
            }

            var prefix = TradeAlertPathUtils.GetS3Prefix(DataType, symbol);
            var files = S3Client.ListFiles(prefix);

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
        /// Downloads and parses a parquet file from S3
        /// </summary>
        protected List<Dictionary<string, object?>> DownloadAndParseParquet(string s3Path)
        {
            try
            {
                var bytes = S3Client.Download(s3Path);
                if (bytes == null)
                {
                    Log.Debug($"{GetType().Name}: File not found: {s3Path}");
                    return new List<Dictionary<string, object?>>();
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
