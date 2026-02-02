/*
 * Cascade Labs - Kalshi API Response Models
 * Models for Kalshi API responses
 */

using Newtonsoft.Json;

namespace QuantConnect.Lean.DataSource.CascadeKalshiData.Models
{
    /// <summary>
    /// Response wrapper for candlestick API endpoint
    /// </summary>
    public class KalshiCandlestickResponse
    {
        /// <summary>
        /// List of candlestick data points
        /// </summary>
        [JsonProperty("candlesticks")]
        public List<KalshiCandlestick> Candlesticks { get; set; } = new();

        /// <summary>
        /// Cursor for pagination (if present)
        /// </summary>
        [JsonProperty("cursor")]
        public string? Cursor { get; set; }
    }

    /// <summary>
    /// Single candlestick data point from Kalshi API
    /// </summary>
    public class KalshiCandlestick
    {
        /// <summary>
        /// End of period timestamp in Unix seconds
        /// </summary>
        [JsonProperty("end_period_ts")]
        public long EndPeriodTs { get; set; }

        /// <summary>
        /// Yes bid OHLC bar (prices in cents 0-100)
        /// </summary>
        [JsonProperty("yes_bid")]
        public KalshiOhlcBar? YesBid { get; set; }

        /// <summary>
        /// Yes ask OHLC bar (prices in cents 0-100)
        /// </summary>
        [JsonProperty("yes_ask")]
        public KalshiOhlcBar? YesAsk { get; set; }

        /// <summary>
        /// Trade price OHLC bar (prices in cents 0-100, may be null)
        /// </summary>
        [JsonProperty("price")]
        public KalshiOhlcBar? Price { get; set; }

        /// <summary>
        /// Volume for this period
        /// </summary>
        [JsonProperty("volume")]
        public long Volume { get; set; }

        /// <summary>
        /// Open interest at end of period
        /// </summary>
        [JsonProperty("open_interest")]
        public long OpenInterest { get; set; }
    }

    /// <summary>
    /// OHLC bar with prices in cents (0-100)
    /// </summary>
    public class KalshiOhlcBar
    {
        /// <summary>
        /// Open price in cents (0-100)
        /// </summary>
        [JsonProperty("open")]
        public int Open { get; set; }

        /// <summary>
        /// High price in cents (0-100)
        /// </summary>
        [JsonProperty("high")]
        public int High { get; set; }

        /// <summary>
        /// Low price in cents (0-100)
        /// </summary>
        [JsonProperty("low")]
        public int Low { get; set; }

        /// <summary>
        /// Close price in cents (0-100)
        /// </summary>
        [JsonProperty("close")]
        public int Close { get; set; }

        /// <summary>
        /// Check if bar has valid data (all OHLC values > 0)
        /// </summary>
        public bool IsValid => Open > 0 && High > 0 && Low > 0 && Close > 0;
    }

    /// <summary>
    /// Response wrapper for markets API endpoint
    /// </summary>
    public class KalshiMarketsResponse
    {
        /// <summary>
        /// List of markets
        /// </summary>
        [JsonProperty("markets")]
        public List<KalshiMarket> Markets { get; set; } = new();

        /// <summary>
        /// Cursor for pagination
        /// </summary>
        [JsonProperty("cursor")]
        public string? Cursor { get; set; }
    }

    /// <summary>
    /// Response wrapper for single market lookup
    /// </summary>
    public class KalshiMarketResponse
    {
        /// <summary>
        /// The market data
        /// </summary>
        [JsonProperty("market")]
        public KalshiMarket? Market { get; set; }
    }

    /// <summary>
    /// Market information from Kalshi API
    /// </summary>
    public class KalshiMarket
    {
        /// <summary>
        /// Market ticker (e.g., KXHIGHNY-26JAN20-T62, INXD-25FEB07-B5350)
        /// </summary>
        [JsonProperty("ticker")]
        public string Ticker { get; set; } = string.Empty;

        /// <summary>
        /// Event ticker this market belongs to
        /// </summary>
        [JsonProperty("event_ticker")]
        public string EventTicker { get; set; } = string.Empty;

        /// <summary>
        /// Series ticker (e.g., KXHIGHNY, INXD)
        /// </summary>
        [JsonProperty("series_ticker")]
        public string SeriesTicker { get; set; } = string.Empty;

        /// <summary>
        /// Market title/description
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Market subtitle with more details
        /// </summary>
        [JsonProperty("subtitle")]
        public string Subtitle { get; set; } = string.Empty;

        /// <summary>
        /// Yes subtitle (what YES means)
        /// </summary>
        [JsonProperty("yes_sub_title")]
        public string YesSubTitle { get; set; } = string.Empty;

        /// <summary>
        /// No subtitle (what NO means)
        /// </summary>
        [JsonProperty("no_sub_title")]
        public string NoSubTitle { get; set; } = string.Empty;

        /// <summary>
        /// Open timestamp in ISO format
        /// </summary>
        [JsonProperty("open_time")]
        public string? OpenTime { get; set; }

        /// <summary>
        /// Close timestamp in ISO format
        /// </summary>
        [JsonProperty("close_time")]
        public string? CloseTime { get; set; }

        /// <summary>
        /// Expiration timestamp in ISO format
        /// </summary>
        [JsonProperty("expiration_time")]
        public string? ExpirationTime { get; set; }

        /// <summary>
        /// Market status: open, closed, settled
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Result if settled: yes, no, or null if not settled
        /// </summary>
        [JsonProperty("result")]
        public string? Result { get; set; }

        /// <summary>
        /// Current yes bid in cents (0-100)
        /// </summary>
        [JsonProperty("yes_bid")]
        public int YesBid { get; set; }

        /// <summary>
        /// Current yes ask in cents (0-100)
        /// </summary>
        [JsonProperty("yes_ask")]
        public int YesAsk { get; set; }

        /// <summary>
        /// Current no bid in cents (0-100)
        /// </summary>
        [JsonProperty("no_bid")]
        public int NoBid { get; set; }

        /// <summary>
        /// Current no ask in cents (0-100)
        /// </summary>
        [JsonProperty("no_ask")]
        public int NoAsk { get; set; }

        /// <summary>
        /// Last traded price in cents
        /// </summary>
        [JsonProperty("last_price")]
        public int LastPrice { get; set; }

        /// <summary>
        /// Previous yes bid (for calculating change)
        /// </summary>
        [JsonProperty("previous_yes_bid")]
        public int PreviousYesBid { get; set; }

        /// <summary>
        /// Previous yes ask (for calculating change)
        /// </summary>
        [JsonProperty("previous_yes_ask")]
        public int PreviousYesAsk { get; set; }

        /// <summary>
        /// Previous last price
        /// </summary>
        [JsonProperty("previous_price")]
        public int PreviousPrice { get; set; }

        /// <summary>
        /// Total volume traded (number of contracts)
        /// </summary>
        [JsonProperty("volume")]
        public long Volume { get; set; }

        /// <summary>
        /// 24-hour volume
        /// </summary>
        [JsonProperty("volume_24h")]
        public long Volume24h { get; set; }

        /// <summary>
        /// Current open interest
        /// </summary>
        [JsonProperty("open_interest")]
        public long OpenInterest { get; set; }

        /// <summary>
        /// Liquidity (dollar value available at best bid/ask)
        /// </summary>
        [JsonProperty("liquidity")]
        public long Liquidity { get; set; }

        /// <summary>
        /// Category of the market
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Risk limit in cents
        /// </summary>
        [JsonProperty("risk_limit_cents")]
        public long RiskLimitCents { get; set; }

        /// <summary>
        /// Strike type if applicable (e.g., "greater", "less", "between")
        /// </summary>
        [JsonProperty("strike_type")]
        public string? StrikeType { get; set; }

        /// <summary>
        /// Floor strike value if applicable
        /// </summary>
        [JsonProperty("floor_strike")]
        public decimal? FloorStrike { get; set; }

        /// <summary>
        /// Cap strike value if applicable
        /// </summary>
        [JsonProperty("cap_strike")]
        public decimal? CapStrike { get; set; }
    }

    /// <summary>
    /// Response wrapper for events API endpoint
    /// </summary>
    public class KalshiEventsResponse
    {
        /// <summary>
        /// List of events
        /// </summary>
        [JsonProperty("events")]
        public List<KalshiEvent> Events { get; set; } = new();

        /// <summary>
        /// Cursor for pagination
        /// </summary>
        [JsonProperty("cursor")]
        public string? Cursor { get; set; }
    }

    /// <summary>
    /// Event information from Kalshi API
    /// </summary>
    public class KalshiEvent
    {
        /// <summary>
        /// Event ticker
        /// </summary>
        [JsonProperty("event_ticker")]
        public string EventTicker { get; set; } = string.Empty;

        /// <summary>
        /// Series ticker
        /// </summary>
        [JsonProperty("series_ticker")]
        public string SeriesTicker { get; set; } = string.Empty;

        /// <summary>
        /// Event title
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Category
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Number of markets in this event
        /// </summary>
        [JsonProperty("mutually_exclusive")]
        public bool MutuallyExclusive { get; set; }

        /// <summary>
        /// List of market tickers in this event
        /// </summary>
        [JsonProperty("markets")]
        public List<string> Markets { get; set; } = new();
    }

    /// <summary>
    /// Response wrapper for series API endpoint
    /// </summary>
    public class KalshiSeriesResponse
    {
        /// <summary>
        /// List of series
        /// </summary>
        [JsonProperty("series")]
        public List<KalshiSeries> Series { get; set; } = new();

        /// <summary>
        /// Cursor for pagination
        /// </summary>
        [JsonProperty("cursor")]
        public string? Cursor { get; set; }
    }

    /// <summary>
    /// Series information from Kalshi API
    /// </summary>
    public class KalshiSeries
    {
        /// <summary>
        /// Series ticker
        /// </summary>
        [JsonProperty("ticker")]
        public string Ticker { get; set; } = string.Empty;

        /// <summary>
        /// Series title
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Category
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Tags for filtering
        /// </summary>
        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new();
    }
}
