/*
 * Cascade Labs - Kalshi Universe Data
 * Data class representing a Kalshi market for universe selection
 */

using QuantConnect.Data;
using QuantConnect.Lean.DataSource.CascadeKalshiData.Models;

namespace QuantConnect.Lean.DataSource.CascadeKalshiData
{
    /// <summary>
    /// Data class representing a Kalshi market for universe selection.
    /// Contains market metadata for filtering and analysis.
    /// </summary>
    public class KalshiUniverseData : BaseData
    {
        /// <summary>
        /// Return the URL source for this data. Not used for API-based universe data.
        /// </summary>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            // Universe data is provided via API, not file-based
            return new SubscriptionDataSource(string.Empty, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Reader - not used for API-based universe data
        /// </summary>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            // Universe data is populated directly, not read from files
            return null;
        }

        /// <summary>Market ticker</summary>
        public string Ticker { get; set; } = string.Empty;

        /// <summary>Series ticker (e.g., KXHIGHNY, INXD)</summary>
        public string SeriesTicker { get; set; } = string.Empty;

        /// <summary>Event ticker</summary>
        public string EventTicker { get; set; } = string.Empty;

        /// <summary>Market category (Weather, Finance, Politics, etc.)</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Market status (open, closed, settled)</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Current yes bid price (0.00-1.00)</summary>
        public decimal YesBid { get; set; }

        /// <summary>Current yes ask price (0.00-1.00)</summary>
        public decimal YesAsk { get; set; }

        /// <summary>Bid-ask spread</summary>
        public decimal Spread => YesAsk - YesBid;

        /// <summary>Mid price</summary>
        public decimal MidPrice => (YesBid + YesAsk) / 2;

        /// <summary>Last traded price</summary>
        public decimal LastPrice { get; set; }

        /// <summary>Total volume (contracts)</summary>
        public long Volume { get; set; }

        /// <summary>24-hour volume</summary>
        public long Volume24h { get; set; }

        /// <summary>Open interest</summary>
        public long OpenInterest { get; set; }

        /// <summary>Liquidity (dollar value)</summary>
        public long Liquidity { get; set; }

        /// <summary>Market close time</summary>
        public DateTime? CloseTime { get; set; }

        /// <summary>Days until expiration</summary>
        public int? DaysToExpiry => CloseTime.HasValue
            ? (int)(CloseTime.Value - Time).TotalDays
            : null;

        /// <summary>Strike type (greater, less, between)</summary>
        public string? StrikeType { get; set; }

        /// <summary>Floor strike value</summary>
        public decimal? FloorStrike { get; set; }

        /// <summary>Cap strike value</summary>
        public decimal? CapStrike { get; set; }

        /// <summary>
        /// Create from KalshiMarket API response
        /// </summary>
        public static KalshiUniverseData FromMarket(KalshiMarket market, DateTime time)
        {
            return new KalshiUniverseData
            {
                Symbol = Symbol.Create(market.Ticker, SecurityType.PredictionMarket, QuantConnect.Market.Kalshi),
                Time = time,
                Ticker = market.Ticker,
                SeriesTicker = market.SeriesTicker,
                EventTicker = market.EventTicker,
                Category = market.Category,
                Status = market.Status,
                YesBid = market.YesBid / 100m,  // Convert cents to decimal
                YesAsk = market.YesAsk / 100m,
                LastPrice = market.LastPrice / 100m,
                Volume = market.Volume,
                Volume24h = market.Volume24h,
                OpenInterest = market.OpenInterest,
                Liquidity = market.Liquidity,
                CloseTime = !string.IsNullOrEmpty(market.CloseTime)
                    ? DateTime.Parse(market.CloseTime, null, System.Globalization.DateTimeStyles.RoundtripKind)
                    : null,
                StrikeType = market.StrikeType,
                FloorStrike = market.FloorStrike,
                CapStrike = market.CapStrike
            };
        }
    }
}
