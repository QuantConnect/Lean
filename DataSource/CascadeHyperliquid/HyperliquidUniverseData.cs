/*
 * Cascade Labs - Hyperliquid Universe Data
 * Data class representing a Hyperliquid contract for universe selection
 */

using Newtonsoft.Json.Linq;
using QuantConnect.Data;

namespace QuantConnect.Lean.DataSource.CascadeHyperliquid
{
    /// <summary>
    /// Data class representing a Hyperliquid contract for universe selection.
    /// Contains market metadata for filtering and analysis.
    /// </summary>
    public class HyperliquidUniverseData : BaseData
    {
        /// <summary>
        /// Return the URL source for this data. Not used for API-based universe data.
        /// </summary>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource(string.Empty, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Reader - not used for API-based universe data
        /// </summary>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            return null;
        }

        /// <summary>Hyperliquid coin name (e.g., "BTC" for perps, "UBTC" for spot)</summary>
        public string Coin { get; set; } = string.Empty;

        /// <summary>Security type (CryptoFuture for perps, Crypto for spot)</summary>
        public SecurityType SecurityType { get; set; }

        /// <summary>Current mark price</summary>
        public decimal MarkPrice { get; set; }

        /// <summary>Current mid price</summary>
        public decimal MidPrice { get; set; }

        /// <summary>Oracle price</summary>
        public decimal OraclePrice { get; set; }

        /// <summary>Current funding rate</summary>
        public decimal FundingRate { get; set; }

        /// <summary>Open interest</summary>
        public decimal OpenInterest { get; set; }

        /// <summary>24-hour notional volume</summary>
        public decimal DayNotionalVolume { get; set; }

        /// <summary>24-hour base volume</summary>
        public decimal DayBaseVolume { get; set; }

        /// <summary>Maximum leverage</summary>
        public int MaxLeverage { get; set; }

        /// <summary>Premium</summary>
        public decimal Premium { get; set; }

        /// <summary>Previous day price</summary>
        public decimal PrevDayPrice { get; set; }

        /// <summary>Size decimals (precision for order sizes)</summary>
        public int SzDecimals { get; set; }

        /// <summary>
        /// Create from Hyperliquid API data
        /// </summary>
        /// <param name="coin">Coin name from meta.universe[i].name</param>
        /// <param name="securityType">CryptoFuture or Crypto</param>
        /// <param name="metaEntry">meta.universe[i] JSON object</param>
        /// <param name="assetCtx">assetCtxs[i] JSON object</param>
        /// <param name="symbol">LEAN symbol</param>
        /// <param name="time">Timestamp</param>
        public static HyperliquidUniverseData FromApiData(
            string coin,
            SecurityType securityType,
            JToken metaEntry,
            JToken assetCtx,
            Symbol symbol,
            DateTime time)
        {
            return new HyperliquidUniverseData
            {
                Symbol = symbol,
                Time = time,
                Coin = coin,
                SecurityType = securityType,
                MarkPrice = assetCtx["markPx"]?.Value<decimal>() ?? 0m,
                MidPrice = assetCtx["midPx"]?.Value<decimal>() ?? 0m,
                OraclePrice = assetCtx["oraclePx"]?.Value<decimal>() ?? 0m,
                FundingRate = assetCtx["funding"]?.Value<decimal>() ?? 0m,
                OpenInterest = assetCtx["openInterest"]?.Value<decimal>() ?? 0m,
                DayNotionalVolume = assetCtx["dayNtlVlm"]?.Value<decimal>() ?? 0m,
                DayBaseVolume = assetCtx["dayBaseVlm"]?.Value<decimal>() ?? 0m,
                MaxLeverage = metaEntry["maxLeverage"]?.Value<int>() ?? 0,
                Premium = assetCtx["premium"]?.Value<decimal>() ?? 0m,
                PrevDayPrice = assetCtx["prevDayPx"]?.Value<decimal>() ?? 0m,
                SzDecimals = metaEntry["szDecimals"]?.Value<int>() ?? 0
            };
        }
    }
}
