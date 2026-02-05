/*
 * Cascade Labs - Hyperliquid Fill Parser
 *
 * Parses S3 trade/fill data and aggregates into TradeBars or Ticks.
 * Supports two S3 formats:
 * - node_trades: one JSON line per trade (March-June 2025)
 * - node_fills_by_block: block-based fills (July 2025+)
 */

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Lean.DataSource.CascadeHyperliquid
{
    /// <summary>
    /// Represents a single parsed fill from S3 data
    /// </summary>
    public readonly struct HyperliquidFill
    {
        public string Coin { get; init; }
        public decimal Price { get; init; }
        public decimal Size { get; init; }
        public string Side { get; init; }
        public DateTime TimeUtc { get; init; }
    }

    /// <summary>
    /// Parses Hyperliquid S3 trade data and aggregates into LEAN market data types
    /// </summary>
    public static class HyperliquidFillParser
    {
        /// <summary>
        /// Parses node_trades format (one JSON line per trade).
        /// Each line: {"coin":"BTC","side":"A","time":"2025-03-22T10:48:33.216798262","px":"42500.5","sz":"0.1",...}
        /// </summary>
        /// <param name="stream">Decompressed data stream</param>
        /// <param name="coinFilter">Only return fills for this coin (case-sensitive). Null for all.</param>
        /// <returns>Sequence of parsed fills</returns>
        public static IEnumerable<HyperliquidFill> ParseNodeTrades(Stream stream, string? coinFilter = null)
        {
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                HyperliquidFill fill;
                try
                {
                    var obj = JObject.Parse(line);

                    var coin = obj["coin"]?.Value<string>();
                    if (coin == null) continue;
                    if (coinFilter != null && coin != coinFilter) continue;

                    var timeStr = obj["time"]?.Value<string>();
                    if (timeStr == null) continue;

                    // Time format: ISO 8601 with nanosecond precision (2025-03-22T10:48:33.216798262)
                    // DateTime only supports up to 7 fractional digits, truncate if needed
                    if (!TryParseIsoTime(timeStr, out var timeUtc)) continue;

                    fill = new HyperliquidFill
                    {
                        Coin = coin,
                        Price = ParseDecimal(obj["px"]?.Value<string>()),
                        Size = ParseDecimal(obj["sz"]?.Value<string>()),
                        Side = obj["side"]?.Value<string>() ?? "",
                        TimeUtc = timeUtc
                    };
                }
                catch (JsonReaderException)
                {
                    continue;
                }

                yield return fill;
            }
        }

        /// <summary>
        /// Parses node_fills_by_block format (one JSON line per block with events array).
        /// Each line: {"local_time":"...","block_number":N,"events":[[addr,{fill}],[addr,{fill}],...]}
        /// Each fill has: coin, px, sz, side, time (ms epoch), dir, oid, tid, etc.
        /// Note: each trade appears twice (buyer + seller). We deduplicate by tid.
        /// </summary>
        /// <param name="stream">Decompressed data stream</param>
        /// <param name="coinFilter">Only return fills for this coin (case-sensitive). Null for all.</param>
        /// <returns>Sequence of parsed fills (deduplicated by tid)</returns>
        public static IEnumerable<HyperliquidFill> ParseNodeFillsByBlock(Stream stream, string? coinFilter = null)
        {
            var seenTids = new HashSet<long>();
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                JObject block;
                try
                {
                    block = JObject.Parse(line);
                }
                catch (JsonReaderException)
                {
                    continue;
                }

                var events = block["events"] as JArray;
                if (events == null || events.Count == 0) continue;

                foreach (var evt in events)
                {
                    if (evt is not JArray pair || pair.Count < 2) continue;

                    var fillObj = pair[1] as JObject;
                    if (fillObj == null) continue;

                    var coin = fillObj["coin"]?.Value<string>();
                    if (coin == null) continue;
                    if (coinFilter != null && coin != coinFilter) continue;

                    // Deduplicate by tid (each trade has two fills: buyer + seller)
                    var tid = fillObj["tid"]?.Value<long>() ?? 0;
                    if (tid != 0 && !seenTids.Add(tid)) continue;

                    var timeMs = fillObj["time"]?.Value<long>() ?? 0;
                    if (timeMs == 0) continue;

                    yield return new HyperliquidFill
                    {
                        Coin = coin,
                        Price = ParseDecimal(fillObj["px"]?.Value<string>()),
                        Size = ParseDecimal(fillObj["sz"]?.Value<string>()),
                        Side = fillObj["side"]?.Value<string>() ?? "",
                        TimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(timeMs).UtcDateTime
                    };
                }
            }
        }

        /// <summary>
        /// Aggregates fills into TradeBars at the requested resolution
        /// </summary>
        /// <param name="fills">Sequence of fills (should be for a single coin)</param>
        /// <param name="symbol">LEAN symbol for the TradeBars</param>
        /// <param name="resolution">Target resolution</param>
        /// <param name="startUtc">Start of requested range (inclusive)</param>
        /// <param name="endUtc">End of requested range (exclusive)</param>
        /// <returns>TradeBars sorted by time</returns>
        public static IEnumerable<TradeBar> AggregateToTradeBars(
            IEnumerable<HyperliquidFill> fills,
            Symbol symbol,
            Resolution resolution,
            DateTime startUtc,
            DateTime endUtc)
        {
            var period = resolution.ToTimeSpan();
            var bars = new SortedDictionary<DateTime, TradeBar>();

            foreach (var fill in fills)
            {
                if (fill.TimeUtc < startUtc || fill.TimeUtc >= endUtc) continue;
                if (fill.Price <= 0 || fill.Size <= 0) continue;

                // Truncate to resolution period
                var barTime = TruncateToResolution(fill.TimeUtc, period);

                if (!bars.TryGetValue(barTime, out var bar))
                {
                    bar = new TradeBar
                    {
                        Symbol = symbol,
                        Time = barTime,
                        Open = fill.Price,
                        High = fill.Price,
                        Low = fill.Price,
                        Close = fill.Price,
                        Volume = 0,
                        Period = period,
                        DataType = MarketDataType.TradeBar
                    };
                    bars[barTime] = bar;
                }

                bar.High = Math.Max(bar.High, fill.Price);
                bar.Low = Math.Min(bar.Low, fill.Price);
                bar.Close = fill.Price;
                bar.Volume += fill.Size;
            }

            return bars.Values;
        }

        /// <summary>
        /// Converts fills into Tick data
        /// </summary>
        /// <param name="fills">Sequence of fills</param>
        /// <param name="symbol">LEAN symbol</param>
        /// <param name="startUtc">Start of requested range (inclusive)</param>
        /// <param name="endUtc">End of requested range (exclusive)</param>
        /// <returns>Ticks sorted by time</returns>
        public static IEnumerable<Tick> ToTicks(
            IEnumerable<HyperliquidFill> fills,
            Symbol symbol,
            DateTime startUtc,
            DateTime endUtc)
        {
            foreach (var fill in fills)
            {
                if (fill.TimeUtc < startUtc || fill.TimeUtc >= endUtc) continue;
                if (fill.Price <= 0) continue;

                yield return new Tick
                {
                    Symbol = symbol,
                    Time = fill.TimeUtc,
                    Value = fill.Price,
                    Quantity = fill.Size,
                    TickType = TickType.Trade,
                    BidPrice = fill.Side == "B" ? fill.Price : 0,
                    AskPrice = fill.Side == "A" ? fill.Price : 0
                };
            }
        }

        /// <summary>
        /// Truncates a datetime to the start of a resolution period
        /// </summary>
        private static DateTime TruncateToResolution(DateTime time, TimeSpan period)
        {
            if (period == TimeSpan.FromDays(1))
            {
                return time.Date;
            }

            var ticks = time.Ticks - (time.Ticks % period.Ticks);
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        /// <summary>
        /// Parses an ISO time string with potential nanosecond precision
        /// </summary>
        private static bool TryParseIsoTime(string timeStr, out DateTime result)
        {
            // DateTime.TryParse handles up to 7 fractional digits.
            // Hyperliquid sends 9 (nanoseconds). Truncate to 7 if needed.
            var dotIndex = timeStr.IndexOf('.');
            if (dotIndex >= 0)
            {
                var fractionalLength = timeStr.Length - dotIndex - 1;
                if (fractionalLength > 7)
                {
                    timeStr = timeStr.Substring(0, dotIndex + 8); // dot + 7 digits
                }
            }

            return DateTime.TryParse(timeStr, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result);
        }

        /// <summary>
        /// Parses a decimal string, returning 0 on failure
        /// </summary>
        private static decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
        }
    }
}
