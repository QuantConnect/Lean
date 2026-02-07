/*
 * Cascade Labs - Kalshi Universe Data
 * Data class representing a Kalshi market for universe selection
 */

using System.Globalization;
using System.IO;
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
        /// Return the CSV file path for this universe data.
        /// </summary>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var path = Path.Combine(Globals.DataFolder, "alternative", "kalshi", "universe",
                config.MappedSymbol ?? "_all", $"{date:yyyyMMdd}.csv");
            return new SubscriptionDataSource(path, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Reader — parse a CSV line back into KalshiUniverseData
        /// </summary>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            return FromCsvLine(line, date)!;
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

        /// <summary>Market open time</summary>
        public DateTime? OpenTime { get; set; }

        /// <summary>Market close time</summary>
        public DateTime? CloseTime { get; set; }

        /// <summary>Days until expiration</summary>
        public int? DaysToExpiry => CloseTime.HasValue
            ? (int)(CloseTime.Value - Time).TotalDays
            : null;

        /// <summary>Settlement result (yes, no, or null if not settled)</summary>
        public string? Result { get; set; }

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
                OpenTime = !string.IsNullOrEmpty(market.OpenTime)
                    ? DateTime.Parse(market.OpenTime, null, DateTimeStyles.RoundtripKind)
                    : null,
                CloseTime = !string.IsNullOrEmpty(market.CloseTime)
                    ? DateTime.Parse(market.CloseTime, null, DateTimeStyles.RoundtripKind)
                    : null,
                Result = market.Result,
                StrikeType = market.StrikeType,
                FloorStrike = market.FloorStrike,
                CapStrike = market.CapStrike
            };
        }

        /// <summary>
        /// Serialize to a CSV line for disk caching.
        /// Columns: ticker,event_ticker,series_ticker,category,status,open_time,close_time,result,
        ///          yes_bid,yes_ask,last_price,volume,volume_24h,open_interest,liquidity,
        ///          strike_type,floor_strike,cap_strike
        /// </summary>
        public string ToCsvLine()
        {
            return string.Join(",",
                Ticker,
                EventTicker,
                SeriesTicker,
                Category,
                Status,
                OpenTime?.ToString("o") ?? "",
                CloseTime?.ToString("o") ?? "",
                Result ?? "",
                YesBid.ToString(CultureInfo.InvariantCulture),
                YesAsk.ToString(CultureInfo.InvariantCulture),
                LastPrice.ToString(CultureInfo.InvariantCulture),
                Volume.ToString(CultureInfo.InvariantCulture),
                Volume24h.ToString(CultureInfo.InvariantCulture),
                OpenInterest.ToString(CultureInfo.InvariantCulture),
                Liquidity.ToString(CultureInfo.InvariantCulture),
                StrikeType ?? "",
                FloorStrike?.ToString(CultureInfo.InvariantCulture) ?? "",
                CapStrike?.ToString(CultureInfo.InvariantCulture) ?? "");
        }

        /// <summary>
        /// Parse a CSV line back into KalshiUniverseData
        /// </summary>
        public static KalshiUniverseData? FromCsvLine(string line, DateTime date)
        {
            var csv = line.Split(',');
            if (csv.Length < 18) return null;

            var ticker = csv[0];
            var data = new KalshiUniverseData
            {
                Symbol = Symbol.Create(ticker, SecurityType.PredictionMarket, QuantConnect.Market.Kalshi),
                Time = date,
                Ticker = ticker,
                EventTicker = csv[1],
                SeriesTicker = csv[2],
                Category = csv[3],
                Status = csv[4],
                OpenTime = string.IsNullOrEmpty(csv[5]) ? null : DateTime.Parse(csv[5], null, DateTimeStyles.RoundtripKind),
                CloseTime = string.IsNullOrEmpty(csv[6]) ? null : DateTime.Parse(csv[6], null, DateTimeStyles.RoundtripKind),
                Result = string.IsNullOrEmpty(csv[7]) ? null : csv[7],
                YesBid = decimal.Parse(csv[8], CultureInfo.InvariantCulture),
                YesAsk = decimal.Parse(csv[9], CultureInfo.InvariantCulture),
                LastPrice = decimal.Parse(csv[10], CultureInfo.InvariantCulture),
                Volume = long.Parse(csv[11], CultureInfo.InvariantCulture),
                Volume24h = long.Parse(csv[12], CultureInfo.InvariantCulture),
                OpenInterest = long.Parse(csv[13], CultureInfo.InvariantCulture),
                Liquidity = long.Parse(csv[14], CultureInfo.InvariantCulture),
                StrikeType = string.IsNullOrEmpty(csv[15]) ? null : csv[15],
                FloorStrike = string.IsNullOrEmpty(csv[16]) ? null : decimal.Parse(csv[16], CultureInfo.InvariantCulture),
                CapStrike = string.IsNullOrEmpty(csv[17]) ? null : decimal.Parse(csv[17], CultureInfo.InvariantCulture)
            };

            return data;
        }

        /// <summary>
        /// Reconstruct a KalshiMarket from cached universe data.
        /// Needed for backward compatibility with _marketFilter.
        /// </summary>
        public KalshiMarket ToKalshiMarket()
        {
            return new KalshiMarket
            {
                Ticker = Ticker,
                EventTicker = EventTicker,
                SeriesTicker = SeriesTicker,
                Category = Category,
                Status = Status,
                OpenTime = OpenTime?.ToString("o") ?? "",
                CloseTime = CloseTime?.ToString("o") ?? "",
                Result = Result,
                YesBid = (int)(YesBid * 100m),
                YesAsk = (int)(YesAsk * 100m),
                LastPrice = (int)(LastPrice * 100m),
                Volume = Volume,
                Volume24h = Volume24h,
                OpenInterest = OpenInterest,
                Liquidity = Liquidity,
                StrikeType = StrikeType,
                FloorStrike = FloorStrike,
                CapStrike = CapStrike
            };
        }
    }
}
