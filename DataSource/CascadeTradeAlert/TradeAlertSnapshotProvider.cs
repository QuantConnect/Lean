/*
 * Cascade Labs - TradeAlert Snapshot Provider
 * Provides access to end-of-day (EOD) underlying fields snapshot data from S3
 *
 * Snapshot data provides comprehensive daily metrics for all underlyings,
 * captured at market close (4:00 PM Eastern).
 */

using QuantConnect.Data;
using QuantConnect.Logging;

namespace QuantConnect.Lean.DataSource.CascadeTradeAlert
{
    /// <summary>
    /// Provider for TradeAlert snapshot data (end-of-day underlying fields)
    ///
    /// Snapshot data includes all fields from most_active but captured at market close:
    /// - Daily volume metrics: option_volume, put_volume, call_volume
    /// - Daily Greeks: net_delta, net_vega
    /// - IV metrics: atm_ivol, historical IV levels, term structure
    /// - Premium totals: put_prem, call_prem, bullish/bearish premium
    /// - Open interest: option_open_int, next_day_oi, oi changes
    /// - Percentile rankings: atm_ivol_pctl, close_pctl, pc_ratio_pctl
    /// - Technical indicators: ema10-200, sma10-200, macd, rsi
    ///
    /// Data is available once daily at market close (16:00 Eastern).
    /// </summary>
    public class TradeAlertSnapshotProvider : TradeAlertBaseProvider
    {
        /// <inheritdoc/>
        protected override TradeAlertDataType DataType => TradeAlertDataType.Snapshot;

        /// <summary>
        /// Standard EOD time (4:00 PM Eastern)
        /// </summary>
        private static readonly TimeSpan EodTime = new TimeSpan(16, 0, 0);

        /// <summary>
        /// Initializes a new instance of the TradeAlertSnapshotProvider
        /// </summary>
        public TradeAlertSnapshotProvider() : base()
        {
            Log.Trace("TradeAlertSnapshotProvider: Initialized");
        }

        /// <summary>
        /// Gets EOD snapshot data for a specific date
        /// </summary>
        /// <param name="date">Date in UTC</param>
        /// <param name="symbol">Symbol filter (default _ALL for all symbols)</param>
        /// <returns>List of EOD snapshot records</returns>
        public new List<Dictionary<string, object?>> GetData(DateTime date, string symbol = DefaultSymbol)
        {
            // Convert to Eastern and use market close time
            var easternDate = TradeAlertPathUtils.ConvertToEastern(date).Date;
            var eodTimestamp = easternDate.Add(EodTime);

            var s3Path = TradeAlertPathUtils.GetS3Path(DataType, symbol, eodTimestamp);
            var records = DownloadAndParseParquet(s3Path);

            // Filter by symbol if not _ALL
            if (symbol != DefaultSymbol && symbol != "@ALL" && records.Count > 0)
            {
                records = records.Where(r =>
                    r.TryGetValue("usymbol", out var usym) &&
                    string.Equals(usym?.ToString(), symbol, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            Log.Debug($"TradeAlertSnapshotProvider: Retrieved {records.Count} records for {easternDate:yyyy-MM-dd}");
            return records;
        }

        /// <summary>
        /// Gets EOD snapshot data for a date range
        /// </summary>
        /// <param name="startDate">Start date in UTC</param>
        /// <param name="endDate">End date in UTC</param>
        /// <param name="symbol">Symbol filter (default _ALL)</param>
        /// <returns>List of EOD snapshot records with date information</returns>
        public List<Dictionary<string, object?>> GetDataRange(DateTime startDate, DateTime endDate, string symbol = DefaultSymbol)
        {
            var allRecords = new List<Dictionary<string, object?>>();
            var startEastern = TradeAlertPathUtils.ConvertToEastern(startDate).Date;
            var endEastern = TradeAlertPathUtils.ConvertToEastern(endDate).Date;

            for (var date = startEastern; date <= endEastern; date = date.AddDays(1))
            {
                // Skip weekends
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                var records = GetData(date, symbol);
                allRecords.AddRange(records);
            }

            return allRecords;
        }

        /// <summary>
        /// Gets EOD data filtered by market cap
        /// </summary>
        /// <param name="date">Date in UTC</param>
        /// <param name="capType">Cap type: "largecap", "midcap", or "smallcap"</param>
        /// <returns>List of EOD snapshot records</returns>
        public List<Dictionary<string, object?>> GetByMarketCap(DateTime date, string capType)
        {
            var records = GetData(date);

            return records.Where(r =>
                r.TryGetValue(capType, out var capObj) &&
                capObj?.ToString() == "1"
            ).ToList();
        }

        /// <summary>
        /// Gets EOD data filtered by sector
        /// </summary>
        /// <param name="date">Date in UTC</param>
        /// <param name="sector">Sector name</param>
        /// <returns>List of EOD snapshot records</returns>
        public List<Dictionary<string, object?>> GetBySector(DateTime date, string sector)
        {
            var records = GetData(date);

            return records.Where(r =>
                r.TryGetValue("sector", out var sectorObj) &&
                string.Equals(sectorObj?.ToString(), sector, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        /// <summary>
        /// Gets EOD data with significant OI changes
        /// </summary>
        /// <param name="date">Date in UTC</param>
        /// <param name="minOiChangePct">Minimum OI change percentage</param>
        /// <returns>List of EOD snapshot records</returns>
        public List<Dictionary<string, object?>> GetWithOiChanges(DateTime date, double minOiChangePct)
        {
            var records = GetData(date);

            return records.Where(r =>
            {
                if (r.TryGetValue("option_oi_chg", out var oiChgObj) &&
                    r.TryGetValue("option_open_int", out var oiObj) &&
                    oiChgObj != null && oiObj != null)
                {
                    if (double.TryParse(oiChgObj.ToString(), out var oiChg) &&
                        double.TryParse(oiObj.ToString(), out var oi) && oi > 0)
                    {
                        var oiChgPct = Math.Abs(oiChg / oi) * 100;
                        return oiChgPct >= minOiChangePct;
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets EOD data at IV extremes (52-week high or low)
        /// </summary>
        /// <param name="date">Date in UTC</param>
        /// <param name="nearHighOrLow">true for near 52-week high, false for low</param>
        /// <param name="thresholdPct">Percentage threshold from high/low (default 5%)</param>
        /// <returns>List of EOD snapshot records</returns>
        public List<Dictionary<string, object?>> GetAtIvExtremes(DateTime date, bool nearHighOrLow, double thresholdPct = 5)
        {
            var records = GetData(date);

            return records.Where(r =>
            {
                if (r.TryGetValue("atm_ivol", out var ivObj) &&
                    r.TryGetValue(nearHighOrLow ? "w52_iv_high" : "w52_iv_low", out var extremeObj) &&
                    ivObj != null && extremeObj != null)
                {
                    if (double.TryParse(ivObj.ToString(), out var iv) &&
                        double.TryParse(extremeObj.ToString(), out var extreme) && extreme > 0)
                    {
                        var distancePct = Math.Abs((iv - extreme) / extreme) * 100;
                        return distancePct <= thresholdPct;
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets EOD data with extreme skew
        /// </summary>
        /// <param name="date">Date in UTC</param>
        /// <param name="minSkewPctl">Minimum skew percentile (0-100)</param>
        /// <returns>List of EOD snapshot records</returns>
        public List<Dictionary<string, object?>> GetWithExtremeSkew(DateTime date, double minSkewPctl)
        {
            var records = GetData(date);

            return records.Where(r =>
            {
                if (r.TryGetValue("d25_30_skew_pctl", out var skewPctlObj) && skewPctlObj != null)
                {
                    if (double.TryParse(skewPctlObj.ToString(), out var skewPctl))
                    {
                        return skewPctl >= minSkewPctl || skewPctl <= (100 - minSkewPctl);
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets EOD data with unusual volume (high volume relative to average)
        /// </summary>
        /// <param name="date">Date in UTC</param>
        /// <param name="minVolumeMult">Minimum volume multiplier vs average</param>
        /// <returns>List of EOD snapshot records</returns>
        public List<Dictionary<string, object?>> GetWithUnusualVolume(DateTime date, double minVolumeMult)
        {
            var records = GetData(date);

            return records.Where(r =>
            {
                if (r.TryGetValue("option_tw_mult", out var multObj) && multObj != null)
                {
                    if (double.TryParse(multObj.ToString(), out var mult))
                    {
                        return mult >= minVolumeMult;
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets EOD data for symbols with hard-to-borrow stock
        /// </summary>
        /// <param name="date">Date in UTC</param>
        /// <param name="minBorrowRate">Minimum borrow rate percentage</param>
        /// <returns>List of EOD snapshot records</returns>
        public List<Dictionary<string, object?>> GetHardToBorrow(DateTime date, double minBorrowRate = 5)
        {
            var records = GetData(date);

            return records.Where(r =>
            {
                if (r.TryGetValue("borrow_rate", out var rateObj) && rateObj != null)
                {
                    if (double.TryParse(rateObj.ToString(), out var rate))
                    {
                        return rate >= minBorrowRate;
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets symbols ranked by a specific metric
        /// </summary>
        /// <param name="date">Date in UTC</param>
        /// <param name="metricField">Field name to rank by</param>
        /// <param name="topN">Number of top records to return</param>
        /// <param name="ascending">Sort ascending if true, descending if false</param>
        /// <returns>List of top N EOD snapshot records</returns>
        public List<Dictionary<string, object?>> GetTopByMetric(
            DateTime date,
            string metricField,
            int topN,
            bool ascending = false)
        {
            var records = GetData(date);

            var sorted = records
                .Select(r => new
                {
                    Record = r,
                    Value = r.TryGetValue(metricField, out var valObj) && valObj != null &&
                            double.TryParse(valObj.ToString(), out var val) ? val : double.NaN
                })
                .Where(x => !double.IsNaN(x.Value));

            if (ascending)
            {
                sorted = sorted.OrderBy(x => x.Value);
            }
            else
            {
                sorted = sorted.OrderByDescending(x => x.Value);
            }

            return sorted.Take(topN).Select(x => x.Record).ToList();
        }
    }
}
