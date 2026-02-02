/*
 * Cascade Labs - TradeAlert Sweeps Provider
 * Provides access to option sweeps/block trades data from S3
 *
 * Sweeps data includes trade-level details with Greeks and pricing
 * for option sweep and block trades, available in 5-minute intervals.
 */

using QuantConnect.Data;
using QuantConnect.Logging;

namespace QuantConnect.Lean.DataSource.CascadeTradeAlert
{
    /// <summary>
    /// Provider for TradeAlert sweeps data (option sweep/block trades)
    ///
    /// Sweeps data includes:
    /// - Trade details: type, side, size, price, exchange
    /// - Greeks: delta, vega, theta, gamma, ivol
    /// - Pricing: bid, ask, theo, mid, edge
    /// - Metadata: usymbol, expiry, strike, put_call
    ///
    /// Data is available in 5-minute intervals during market hours.
    /// </summary>
    public class TradeAlertSweepsProvider : TradeAlertBaseProvider
    {
        /// <inheritdoc/>
        protected override TradeAlertDataType DataType => TradeAlertDataType.Sweeps;

        /// <summary>
        /// Initializes a new instance of the TradeAlertSweepsProvider
        /// </summary>
        public TradeAlertSweepsProvider() : base()
        {
            Log.Trace("TradeAlertSweepsProvider: Initialized");
        }

        /// <summary>
        /// Gets sweeps data for a specific 5-minute interval
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC (will be rounded to 5-minute interval)</param>
        /// <param name="symbol">Underlying symbol filter (default _ALL for all symbols)</param>
        /// <returns>List of sweep records</returns>
        public new List<Dictionary<string, object?>> GetData(DateTime timestamp, string symbol = DefaultSymbol)
        {
            var records = base.GetData(timestamp, symbol);

            // Filter by underlying symbol if not _ALL
            if (symbol != DefaultSymbol && symbol != "@ALL" && records.Count > 0)
            {
                records = records.Where(r =>
                    r.TryGetValue("usymbol", out var usym) &&
                    string.Equals(usym?.ToString(), symbol, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            Log.Debug($"TradeAlertSweepsProvider: Retrieved {records.Count} sweeps for {timestamp:yyyy-MM-dd HH:mm}");
            return records;
        }

        /// <summary>
        /// Gets sweeps data filtered by minimum contract size
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="minSize">Minimum contract size</param>
        /// <param name="symbol">Underlying symbol filter</param>
        /// <returns>List of sweep records</returns>
        public List<Dictionary<string, object?>> GetDataBySize(DateTime timestamp, int minSize, string symbol = DefaultSymbol)
        {
            var records = GetData(timestamp, symbol);

            return records.Where(r =>
            {
                if (r.TryGetValue("size", out var sizeObj) && sizeObj != null)
                {
                    if (int.TryParse(sizeObj.ToString(), out var size))
                    {
                        return size >= minSize;
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets sweeps data filtered by delta range
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="minDelta">Minimum absolute delta</param>
        /// <param name="maxDelta">Maximum absolute delta</param>
        /// <param name="symbol">Underlying symbol filter</param>
        /// <returns>List of sweep records</returns>
        public List<Dictionary<string, object?>> GetDataByDelta(
            DateTime timestamp,
            double minDelta,
            double maxDelta,
            string symbol = DefaultSymbol)
        {
            var records = GetData(timestamp, symbol);

            return records.Where(r =>
            {
                if (r.TryGetValue("delta", out var deltaObj) && deltaObj != null)
                {
                    if (double.TryParse(deltaObj.ToString(), out var delta))
                    {
                        var absDelta = Math.Abs(delta);
                        return absDelta >= minDelta && absDelta <= maxDelta;
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets call sweeps only
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="symbol">Underlying symbol filter</param>
        /// <returns>List of call sweep records</returns>
        public List<Dictionary<string, object?>> GetCallSweeps(DateTime timestamp, string symbol = DefaultSymbol)
        {
            var records = GetData(timestamp, symbol);

            return records.Where(r =>
                r.TryGetValue("put_call", out var pcObj) &&
                string.Equals(pcObj?.ToString(), "Call", StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        /// <summary>
        /// Gets put sweeps only
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="symbol">Underlying symbol filter</param>
        /// <returns>List of put sweep records</returns>
        public List<Dictionary<string, object?>> GetPutSweeps(DateTime timestamp, string symbol = DefaultSymbol)
        {
            var records = GetData(timestamp, symbol);

            return records.Where(r =>
                r.TryGetValue("put_call", out var pcObj) &&
                string.Equals(pcObj?.ToString(), "Put", StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        /// <summary>
        /// Gets sweeps filtered by days to expiration
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        /// <param name="minDte">Minimum days to expiration</param>
        /// <param name="maxDte">Maximum days to expiration</param>
        /// <param name="symbol">Underlying symbol filter</param>
        /// <returns>List of sweep records</returns>
        public List<Dictionary<string, object?>> GetDataByDte(
            DateTime timestamp,
            int minDte,
            int maxDte,
            string symbol = DefaultSymbol)
        {
            var records = GetData(timestamp, symbol);

            return records.Where(r =>
            {
                if (r.TryGetValue("dtx", out var dteObj) && dteObj != null)
                {
                    if (int.TryParse(dteObj.ToString(), out var dte))
                    {
                        return dte >= minDte && dte <= maxDte;
                    }
                }
                return false;
            }).ToList();
        }
    }
}
