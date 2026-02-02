/*
 * Cascade Labs - TradeAlert Most Active Provider
 * Provides access to most active underlying stocks by options volume from S3
 *
 * Most active data includes comprehensive metrics for underlyings with
 * the highest options volume, available in 5-minute intervals.
 */

using QuantConnect.Data;
using QuantConnect.Logging;

namespace QuantConnect.Lean.DataSource.CascadeTradeAlert
{
    /// <summary>
    /// Provider for TradeAlert most_active data (most active underlyings by options volume)
    ///
    /// Most active data includes:
    /// - Volume metrics: option_volume, put_volume, call_volume, equity_volume
    /// - Greeks aggregates: net_delta, net_vega, rolling_net_delta
    /// - IV metrics: atm_ivol, ivol_chg, skew data (d25_30_skew, norm_d25_30_skew)
    /// - Premium data: put_prem, call_prem, bullish/bearish premium
    /// - Open interest: option_open_int, put_open_int, call_open_int
    /// - Derived fields: vrp, norm_edge, norm_delta, steepness
    ///
    /// Data is available in 5-minute intervals during market hours.
    /// </summary>
    public class TradeAlertMostActiveProvider : TradeAlertBaseProvider
    {
        /// <inheritdoc/>
        protected override TradeAlertDataType DataType => TradeAlertDataType.MostActive;

        /// <summary>
        /// Initializes a new instance of the TradeAlertMostActiveProvider
        /// </summary>
        public TradeAlertMostActiveProvider() : base()
        {
            Log.Trace("TradeAlertMostActiveProvider: Initialized");
        }

        /// <summary>
        /// Gets most active data for a specific 5-minute interval
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC (will be rounded to 5-minute interval)</param>
        /// <param name="symbol">Symbol filter (default _ALL for all symbols)</param>
        /// <returns>List of most active records</returns>
        public new List<Dictionary<string, object?>> GetData(DateTime timestamp, string symbol = DefaultSymbol)
        {
            var records = base.GetData(timestamp, symbol);

            // Filter by symbol if not _ALL
            if (symbol != DefaultSymbol && symbol != "@ALL" && records.Count > 0)
            {
                records = records.Where(r =>
                    r.TryGetValue("usymbol", out var usym) &&
                    string.Equals(usym?.ToString(), symbol, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            Log.Debug($"TradeAlertMostActiveProvider: Retrieved {records.Count} records for {timestamp:yyyy-MM-dd HH:mm}");
            return records;
        }

        /// <summary>
        /// Gets most active stocks by minimum options volume
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="minVolume">Minimum options volume</param>
        /// <returns>List of most active records</returns>
        public List<Dictionary<string, object?>> GetByMinVolume(DateTime timestamp, long minVolume)
        {
            var records = GetData(timestamp);

            return records.Where(r =>
            {
                if (r.TryGetValue("option_volume", out var volObj) && volObj != null)
                {
                    if (long.TryParse(volObj.ToString(), out var volume))
                    {
                        return volume >= minVolume;
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets most active stocks by minimum ADV (average daily volume)
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="minAdv">Minimum ADV</param>
        /// <returns>List of most active records</returns>
        public List<Dictionary<string, object?>> GetByMinAdv(DateTime timestamp, long minAdv)
        {
            var records = GetData(timestamp);

            return records.Where(r =>
            {
                if (r.TryGetValue("adv", out var advObj) && advObj != null)
                {
                    if (long.TryParse(advObj.ToString(), out var adv))
                    {
                        return adv >= minAdv;
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets most active stocks filtered by IV percentile
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="minIvPctl">Minimum IV percentile (0-100)</param>
        /// <param name="maxIvPctl">Maximum IV percentile (0-100)</param>
        /// <returns>List of most active records</returns>
        public List<Dictionary<string, object?>> GetByIvPercentile(DateTime timestamp, double minIvPctl, double maxIvPctl)
        {
            var records = GetData(timestamp);

            return records.Where(r =>
            {
                if (r.TryGetValue("atm_ivol_pctl", out var ivPctlObj) && ivPctlObj != null)
                {
                    if (double.TryParse(ivPctlObj.ToString(), out var ivPctl))
                    {
                        return ivPctl >= minIvPctl && ivPctl <= maxIvPctl;
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets most active stocks filtered by put/call ratio
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="minPcRatio">Minimum put/call ratio</param>
        /// <param name="maxPcRatio">Maximum put/call ratio</param>
        /// <returns>List of most active records</returns>
        public List<Dictionary<string, object?>> GetByPutCallRatio(DateTime timestamp, double minPcRatio, double maxPcRatio)
        {
            var records = GetData(timestamp);

            return records.Where(r =>
            {
                if (r.TryGetValue("put_volume", out var putVolObj) &&
                    r.TryGetValue("call_volume", out var callVolObj) &&
                    putVolObj != null && callVolObj != null)
                {
                    if (double.TryParse(putVolObj.ToString(), out var putVol) &&
                        double.TryParse(callVolObj.ToString(), out var callVol) &&
                        callVol > 0)
                    {
                        var pcRatio = putVol / callVol;
                        return pcRatio >= minPcRatio && pcRatio <= maxPcRatio;
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets most active stocks filtered by VRP (volatility risk premium)
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="minVrp">Minimum VRP</param>
        /// <returns>List of most active records</returns>
        public List<Dictionary<string, object?>> GetByMinVrp(DateTime timestamp, double minVrp)
        {
            var records = GetData(timestamp);

            return records.Where(r =>
            {
                if (r.TryGetValue("vrp", out var vrpObj) && vrpObj != null)
                {
                    if (double.TryParse(vrpObj.ToString(), out var vrp))
                    {
                        return vrp >= minVrp;
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets most active stocks filtered by normalized skew
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="minSkew">Minimum normalized skew</param>
        /// <param name="maxSkew">Maximum normalized skew</param>
        /// <returns>List of most active records</returns>
        public List<Dictionary<string, object?>> GetByNormalizedSkew(DateTime timestamp, double minSkew, double maxSkew)
        {
            var records = GetData(timestamp);

            return records.Where(r =>
            {
                if (r.TryGetValue("norm_d25_30_skew", out var skewObj) && skewObj != null)
                {
                    if (double.TryParse(skewObj.ToString(), out var skew))
                    {
                        return skew >= minSkew && skew <= maxSkew;
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets most active stocks with upcoming earnings
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="maxDaysToEarnings">Maximum days to earnings</param>
        /// <returns>List of most active records with upcoming earnings</returns>
        public List<Dictionary<string, object?>> GetWithUpcomingEarnings(DateTime timestamp, int maxDaysToEarnings)
        {
            var records = GetData(timestamp);

            return records.Where(r =>
            {
                if (r.TryGetValue("days_to_earnings", out var dteObj) && dteObj != null)
                {
                    if (int.TryParse(dteObj.ToString(), out var dte))
                    {
                        return dte > 0 && dte <= maxDaysToEarnings;
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets top N most active stocks by options volume
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="topN">Number of top stocks to return</param>
        /// <returns>List of top N most active records</returns>
        public List<Dictionary<string, object?>> GetTopN(DateTime timestamp, int topN)
        {
            var records = GetData(timestamp);

            return records
                .OrderByDescending(r =>
                {
                    if (r.TryGetValue("option_volume", out var volObj) && volObj != null)
                    {
                        if (long.TryParse(volObj.ToString(), out var volume))
                        {
                            return volume;
                        }
                    }
                    return 0L;
                })
                .Take(topN)
                .ToList();
        }

        /// <summary>
        /// Gets most active ETFs only
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <returns>List of most active ETF records</returns>
        public List<Dictionary<string, object?>> GetEtfsOnly(DateTime timestamp)
        {
            var records = GetData(timestamp);

            return records.Where(r =>
                r.TryGetValue("etf", out var etfObj) &&
                etfObj?.ToString() == "1"
            ).ToList();
        }

        /// <summary>
        /// Gets most active stocks only (excluding ETFs)
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <returns>List of most active stock records</returns>
        public List<Dictionary<string, object?>> GetStocksOnly(DateTime timestamp)
        {
            var records = GetData(timestamp);

            return records.Where(r =>
                r.TryGetValue("stock", out var stockObj) &&
                stockObj?.ToString() == "1"
            ).ToList();
        }
    }
}
