/*
 * Cascade Labs - Kalshi Extension Methods
 * Helpers for Kalshi data conversion
 */

using NodaTime;
using QuantConnect.Data.Market;
using QuantConnect.Lean.DataSource.CascadeKalshiData.Models;

namespace QuantConnect.Lean.DataSource.CascadeKalshiData
{
    /// <summary>
    /// Extension methods for Kalshi data conversion
    /// </summary>
    public static class KalshiExtensions
    {
        /// <summary>
        /// Eastern Time zone for Kalshi markets
        /// </summary>
        public static readonly DateTimeZone KalshiTimeZone = TimeZones.NewYork;

        /// <summary>
        /// Convert cents (0-100) to decimal probability (0.00-1.00)
        /// </summary>
        public static decimal CentsToDecimal(this int cents)
        {
            return cents / 100m;
        }

        /// <summary>
        /// Convert nullable cents (0-100) to decimal probability (0.00-1.00)
        /// </summary>
        public static decimal CentsToDecimal(this int? cents)
        {
            return (cents ?? 0) / 100m;
        }

        /// <summary>
        /// Convert Unix timestamp to DateTime
        /// </summary>
        public static DateTime UnixSecondsToDateTime(this long unixSeconds)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).DateTime;
        }

        /// <summary>
        /// Convert Unix timestamp to DateTime in a specific timezone
        /// </summary>
        public static DateTime UnixSecondsToDateTime(this long unixSeconds, DateTimeZone targetZone)
        {
            var utcDateTime = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            return utcDateTime.ConvertFromUtc(targetZone);
        }

        /// <summary>
        /// Convert DateTime to Unix timestamp in seconds
        /// </summary>
        public static long ToUnixSeconds(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime, TimeSpan.Zero).ToUnixTimeSeconds();
        }

        /// <summary>
        /// Convert DateTime to Unix timestamp, assuming it's in the specified timezone
        /// </summary>
        public static long ToUnixSeconds(this DateTime dateTime, DateTimeZone sourceZone)
        {
            var utcDateTime = dateTime.ConvertToUtc(sourceZone);
            return new DateTimeOffset(utcDateTime, TimeSpan.Zero).ToUnixTimeSeconds();
        }

        /// <summary>
        /// Convert a Kalshi candlestick to a LEAN QuoteBar
        /// </summary>
        /// <remarks>
        /// Handles data quality issues common in illiquid prediction markets:
        /// - Skips illiquid sides (all values at 1 cent or 99+ cents)
        /// - Detects and handles crossed quotes (bid > ask)
        /// </remarks>
        public static QuoteBar ToQuoteBar(this KalshiCandlestick candle, Symbol symbol, TimeSpan period, DateTimeZone exchangeTimeZone)
        {
            var endTime = candle.EndPeriodTs.UnixSecondsToDateTime(exchangeTimeZone);
            var startTime = endTime - period;

            var quoteBar = new QuoteBar
            {
                Symbol = symbol,
                Time = startTime,
                Period = period
            };

            // Check validity: must have data and not be illiquid (placeholder values)
            var hasBid = candle.YesBid?.IsValid == true && !candle.YesBid.IsIlliquid;
            var hasAsk = candle.YesAsk?.IsValid == true && !candle.YesAsk.IsIlliquid;

            // Detect crossed quotes (bid > ask) using close prices
            // This is physically impossible and indicates data quality issues
            if (hasBid && hasAsk)
            {
                var bidClose = candle.YesBid!.Close!.Value;
                var askClose = candle.YesAsk!.Close!.Value;

                if (bidClose > askClose)
                {
                    // Crossed quote detected - the ask is likely stale/invalid
                    // Use only the bid side
                    hasAsk = false;
                }
            }

            // Convert bid OHLC (cents to decimal)
            if (hasBid)
            {
                quoteBar.Bid = new Bar(
                    candle.YesBid!.Open.CentsToDecimal(),
                    candle.YesBid.High.CentsToDecimal(),
                    candle.YesBid.Low.CentsToDecimal(),
                    candle.YesBid.Close.CentsToDecimal()
                );
                quoteBar.LastBidSize = candle.Volume;
            }

            // Convert ask OHLC (cents to decimal)
            if (hasAsk)
            {
                quoteBar.Ask = new Bar(
                    candle.YesAsk!.Open.CentsToDecimal(),
                    candle.YesAsk.High.CentsToDecimal(),
                    candle.YesAsk.Low.CentsToDecimal(),
                    candle.YesAsk.Close.CentsToDecimal()
                );
                quoteBar.LastAskSize = candle.Volume;
            }

            return quoteBar;
        }

        /// <summary>
        /// Convert a Kalshi candlestick to a LEAN TradeBar (using trade price if available)
        /// </summary>
        public static TradeBar? ToTradeBar(this KalshiCandlestick candle, Symbol symbol, TimeSpan period, DateTimeZone exchangeTimeZone)
        {
            if (candle.Price?.IsValid != true)
            {
                return null;
            }

            var endTime = candle.EndPeriodTs.UnixSecondsToDateTime(exchangeTimeZone);
            var startTime = endTime - period;

            return new TradeBar(
                startTime,
                symbol,
                candle.Price.Open.CentsToDecimal(),
                candle.Price.High.CentsToDecimal(),
                candle.Price.Low.CentsToDecimal(),
                candle.Price.Close.CentsToDecimal(),
                candle.Volume,
                period
            );
        }

        /// <summary>
        /// Generate date ranges for chunked API requests
        /// </summary>
        public static IEnumerable<(DateTime startDate, DateTime endDate)> GenerateDateRanges(
            DateTime startDate,
            DateTime endDate,
            int intervalDays = 3)
        {
            var current = startDate;
            while (current < endDate)
            {
                var rangeEnd = current.AddDays(intervalDays);
                if (rangeEnd > endDate)
                {
                    rangeEnd = endDate;
                }
                yield return (current, rangeEnd);
                current = rangeEnd;
            }
        }
    }
}
