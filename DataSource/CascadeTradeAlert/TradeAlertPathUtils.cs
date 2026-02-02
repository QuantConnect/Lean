/*
 * Cascade Labs - TradeAlert Path Utilities
 * Utility methods for constructing S3 paths for TradeAlert data
 */

using NodaTime;

namespace QuantConnect.Lean.DataSource.CascadeTradeAlert
{
    /// <summary>
    /// Utility class for TradeAlert S3 path operations
    /// </summary>
    public static class TradeAlertPathUtils
    {
        /// <summary>
        /// Eastern Time zone used for TradeAlert data
        /// </summary>
        public static readonly DateTimeZone EasternTimeZone = DateTimeZoneProviders.Tzdb["America/New_York"];

        /// <summary>
        /// Builds S3 path for TradeAlert data
        /// </summary>
        /// <param name="dataType">Data type: sweeps, most_active, or underlying_fields_eod</param>
        /// <param name="symbol">Symbol (typically _ALL for aggregated data)</param>
        /// <param name="timestamp">Timestamp in Eastern time</param>
        /// <returns>S3 path string</returns>
        public static string GetS3Path(TradeAlertDataType dataType, string symbol, DateTime timestamp)
        {
            var symbolSafe = symbol.Replace("/", "_").Replace("@", "_");
            var year = timestamp.Year;
            var month = timestamp.Month;
            var day = timestamp.Day;
            var hour = timestamp.Hour;
            var minute = timestamp.Minute;

            var dataTypeName = GetDataTypeName(dataType);
            var frequency = dataType == TradeAlertDataType.Snapshot ? "daily" : "5min";

            return $"tradealert/{dataTypeName}/{symbolSafe}/{frequency}/{year}/{month:D2}/{day:D2}/{hour:D2}{minute:D2}.parquet";
        }

        /// <summary>
        /// Gets S3 prefix for listing files
        /// </summary>
        /// <param name="dataType">Data type</param>
        /// <param name="symbol">Symbol</param>
        /// <returns>S3 prefix string</returns>
        public static string GetS3Prefix(TradeAlertDataType dataType, string symbol)
        {
            var symbolSafe = symbol.Replace("/", "_").Replace("@", "_");
            var dataTypeName = GetDataTypeName(dataType);
            var frequency = dataType == TradeAlertDataType.Snapshot ? "daily" : "5min";

            return $"tradealert/{dataTypeName}/{symbolSafe}/{frequency}/";
        }

        /// <summary>
        /// Gets S3 prefix for a specific date
        /// </summary>
        public static string GetS3PrefixForDate(TradeAlertDataType dataType, string symbol, DateTime date)
        {
            var symbolSafe = symbol.Replace("/", "_").Replace("@", "_");
            var dataTypeName = GetDataTypeName(dataType);
            var frequency = dataType == TradeAlertDataType.Snapshot ? "daily" : "5min";

            return $"tradealert/{dataTypeName}/{symbolSafe}/{frequency}/{date.Year}/{date.Month:D2}/{date.Day:D2}/";
        }

        /// <summary>
        /// Parses timestamp from S3 path
        /// </summary>
        /// <param name="path">S3 path</param>
        /// <returns>Parsed DateTime or null</returns>
        public static DateTime? ParseTimestampFromPath(string path)
        {
            try
            {
                // Format: tradealert/{data_type}/{symbol}/{freq}/{year}/{month}/{day}/{hhmm}.parquet
                var parts = path.Split('/');
                if (parts.Length < 4)
                {
                    return null;
                }

                var filename = parts[^1]; // Last element
                if (!filename.EndsWith(".parquet"))
                {
                    return null;
                }

                // Parse date from path
                var yearStr = parts[^4];
                var monthStr = parts[^3];
                var dayStr = parts[^2];
                var timeStr = filename.Replace(".parquet", "");

                if (!int.TryParse(yearStr, out var year) ||
                    !int.TryParse(monthStr, out var month) ||
                    !int.TryParse(dayStr, out var day) ||
                    timeStr.Length != 4)
                {
                    return null;
                }

                var hour = int.Parse(timeStr[..2]);
                var minute = int.Parse(timeStr[2..]);

                return new DateTime(year, month, day, hour, minute, 0);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Rounds datetime down to nearest 5-minute interval
        /// </summary>
        public static DateTime RoundTo5Min(DateTime dt)
        {
            var minutes = (dt.Minute / 5) * 5;
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, minutes, 0);
        }

        /// <summary>
        /// Converts UTC time to Eastern time
        /// </summary>
        public static DateTime ConvertToEastern(DateTime utcTime)
        {
            var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc));
            var easternTime = instant.InZone(EasternTimeZone);
            return easternTime.ToDateTimeUnspecified();
        }

        /// <summary>
        /// Converts Eastern time to UTC
        /// </summary>
        public static DateTime ConvertToUtc(DateTime easternTime)
        {
            var localDateTime = LocalDateTime.FromDateTime(easternTime);
            var zonedDateTime = localDateTime.InZoneLeniently(EasternTimeZone);
            return zonedDateTime.ToDateTimeUtc();
        }

        /// <summary>
        /// Checks if a time is within market hours (9:30 AM - 4:00 PM Eastern)
        /// </summary>
        public static bool IsMarketHours(DateTime easternTime)
        {
            var marketOpen = new TimeSpan(9, 30, 0);
            var marketClose = new TimeSpan(16, 0, 0);
            var time = easternTime.TimeOfDay;
            return time >= marketOpen && time < marketClose;
        }

        /// <summary>
        /// Gets market open time for a given date in Eastern
        /// </summary>
        public static DateTime GetMarketOpen(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, 9, 30, 0);
        }

        /// <summary>
        /// Gets market close time for a given date in Eastern
        /// </summary>
        public static DateTime GetMarketClose(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, 16, 0, 0);
        }

        private static string GetDataTypeName(TradeAlertDataType dataType)
        {
            return dataType switch
            {
                TradeAlertDataType.Sweeps => "sweeps",
                TradeAlertDataType.MostActive => "most_active",
                TradeAlertDataType.Snapshot => "underlying_fields_eod",
                _ => throw new ArgumentException($"Unknown data type: {dataType}")
            };
        }
    }

    /// <summary>
    /// TradeAlert data types
    /// </summary>
    public enum TradeAlertDataType
    {
        /// <summary>
        /// Option sweeps/block trades (5-minute intervals)
        /// </summary>
        Sweeps,

        /// <summary>
        /// Most active underlyings by options volume (5-minute intervals)
        /// </summary>
        MostActive,

        /// <summary>
        /// End-of-day snapshot of underlying fields (daily)
        /// </summary>
        Snapshot
    }
}
