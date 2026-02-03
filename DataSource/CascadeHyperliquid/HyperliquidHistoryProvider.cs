/*
 * Cascade Labs - Hyperliquid History Provider
 *
 * Provides historical data for Hyperliquid perpetual futures
 * Implements IHistoryProvider for LEAN integration
 */

using NodaTime;
using Newtonsoft.Json.Linq;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Lean.Engine.DataFeeds;

using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Lean.DataSource.CascadeHyperliquid
{
    /// <summary>
    /// History provider for Hyperliquid perpetual futures
    /// </summary>
    /// <remarks>
    /// Supports:
    /// - OHLCV candles (Minute, Hour, Daily resolutions)
    /// - Tick data (trades)
    /// - CryptoFuture security type only
    /// </remarks>
    public class HyperliquidHistoryProvider : SynchronizingHistoryProvider
    {
        private HyperliquidRestClient? _restClient;
        private HyperliquidSymbolMapper? _symbolMapper;
        private bool _initialized;

        // Warning flags to avoid log spam
        private volatile bool _invalidSecurityTypeWarningFired;
        private volatile bool _invalidResolutionWarningFired;

        /// <summary>
        /// Mapping of LEAN Resolution to Hyperliquid interval strings
        /// </summary>
        private static readonly Dictionary<Resolution, string> ResolutionToInterval = new()
        {
            { Resolution.Minute, "1m" },
            { Resolution.Hour, "1h" },
            { Resolution.Daily, "1d" }
        };

        /// <summary>
        /// Initializes a new instance of the Hyperliquid history provider
        /// </summary>
        public HyperliquidHistoryProvider()
        {
            var useTestnet = Config.GetBool("hyperliquid-use-testnet", false);
            _restClient = new HyperliquidRestClient(useTestnet);
            _symbolMapper = new HyperliquidSymbolMapper();
            _initialized = true;

            Log.Trace($"HyperliquidHistoryProvider: Initialized (testnet: {useTestnet})");
        }

        /// <summary>
        /// Initializes the history provider
        /// </summary>
        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
            // No additional initialization needed
        }

        /// <summary>
        /// Gets historical data for multiple history requests
        /// </summary>
        public override IEnumerable<Slice>? GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            var subscriptions = new List<Subscription>();

            foreach (var request in requests)
            {
                var history = GetHistory(request);
                if (history == null) continue;

                var subscription = CreateSubscription(request, history);
                if (!subscription.MoveNext()) continue;

                subscriptions.Add(subscription);
            }

            if (subscriptions.Count == 0) return null;

            return CreateSliceEnumerableFromSubscriptions(subscriptions, sliceTimeZone);
        }

        /// <summary>
        /// Gets historical data for a single history request
        /// </summary>
        private IEnumerable<BaseData>? GetHistory(HistoryRequest request)
        {
            if (!_initialized || _restClient == null || _symbolMapper == null)
            {
                Log.Error("HyperliquidHistoryProvider: Not initialized");
                return null;
            }

            // Validate symbol
            if (!CanSubscribe(request.Symbol))
            {
                if (!_invalidSecurityTypeWarningFired)
                {
                    _invalidSecurityTypeWarningFired = true;
                    Log.Trace($"HyperliquidHistoryProvider: Unsupported security type '{request.Symbol.SecurityType}'. Only CryptoFuture is supported.");
                }
                return null;
            }

            // Get Hyperliquid coin symbol
            string coin;
            try
            {
                coin = _symbolMapper.GetBrokerageSymbol(request.Symbol);
            }
            catch (Exception ex)
            {
                Log.Error($"HyperliquidHistoryProvider: Failed to map symbol {request.Symbol}: {ex.Message}");
                return null;
            }

            // Route to appropriate handler based on tick type and resolution
            if (request.TickType == TickType.Trade && request.Resolution == Resolution.Tick)
            {
                return GetTickHistory(coin, request);
            }

            if (request.TickType == TickType.Quote)
            {
                // Hyperliquid doesn't provide historical quote data
                Log.Trace($"HyperliquidHistoryProvider: Quote data not supported for {request.Symbol}");
                return null;
            }

            // Get candle/TradeBar data
            return GetCandleHistory(coin, request);
        }

        /// <summary>
        /// Gets tick (trade) history for a symbol
        /// </summary>
        private IEnumerable<BaseData>? GetTickHistory(string coin, HistoryRequest request)
        {
            // Note: Hyperliquid's recentTrades endpoint only returns the most recent trades
            // It does not support historical ranges. For production use, you'd need to
            // either use websocket subscriptions or accept limited historical tick data.

            var results = new List<BaseData>();

            try
            {
                var tradesTask = _restClient!.GetRecentTradesAsync(coin);
                tradesTask.Wait();

                var trades = tradesTask.Result;
                if (trades == null || !trades.Any())
                {
                    return results;
                }

                foreach (var trade in trades)
                {
                    var time = trade["time"]?.Value<long>() ?? 0;
                    var price = decimal.Parse(trade["px"]?.Value<string>() ?? "0");
                    var size = decimal.Parse(trade["sz"]?.Value<string>() ?? "0");
                    var side = trade["side"]?.Value<string>() ?? "";

                    var timeUtc = DateTimeOffset.FromUnixTimeMilliseconds(time).UtcDateTime;

                    // Only return trades within requested time range
                    if (timeUtc < request.StartTimeUtc || timeUtc >= request.EndTimeUtc)
                    {
                        continue;
                    }

                    results.Add(new Tick
                    {
                        Symbol = request.Symbol,
                        Time = timeUtc,
                        Value = price,
                        Quantity = size,
                        TickType = TickType.Trade,
                        BidPrice = side == "B" ? price : 0,
                        AskPrice = side == "A" ? price : 0
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"HyperliquidHistoryProvider: Error fetching tick history for {coin}: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Gets candle (TradeBar) history for a symbol
        /// </summary>
        private IEnumerable<BaseData>? GetCandleHistory(string coin, HistoryRequest request)
        {
            // Validate resolution
            if (!ResolutionToInterval.TryGetValue(request.Resolution, out var interval))
            {
                if (!_invalidResolutionWarningFired)
                {
                    _invalidResolutionWarningFired = true;
                    Log.Trace($"HyperliquidHistoryProvider: Unsupported resolution '{request.Resolution}'. Supported: Minute, Hour, Daily.");
                }
                return null;
            }

            var results = new List<BaseData>();

            try
            {
                var startMs = new DateTimeOffset(request.StartTimeUtc).ToUnixTimeMilliseconds();
                var endMs = new DateTimeOffset(request.EndTimeUtc).ToUnixTimeMilliseconds();

                var candlesTask = _restClient!.GetCandleSnapshotAsync(coin, interval, startMs, endMs);
                candlesTask.Wait();

                var candles = candlesTask.Result;
                if (candles == null || !candles.Any())
                {
                    return results;
                }

                foreach (var candle in candles)
                {
                    // Hyperliquid candle format:
                    // T: close timestamp (ms)
                    // t: open timestamp (ms)
                    // o: open price
                    // h: high price
                    // l: low price
                    // c: close price
                    // v: volume
                    // n: number of trades

                    var openTime = candle["t"]?.Value<long>() ?? 0;
                    var closeTime = candle["T"]?.Value<long>() ?? 0;
                    var open = decimal.Parse(candle["o"]?.Value<string>() ?? "0");
                    var high = decimal.Parse(candle["h"]?.Value<string>() ?? "0");
                    var low = decimal.Parse(candle["l"]?.Value<string>() ?? "0");
                    var close = decimal.Parse(candle["c"]?.Value<string>() ?? "0");
                    var volume = decimal.Parse(candle["v"]?.Value<string>() ?? "0");

                    var time = DateTimeOffset.FromUnixTimeMilliseconds(openTime).UtcDateTime;
                    var period = TimeSpan.FromMilliseconds(closeTime - openTime);

                    results.Add(new TradeBar
                    {
                        Symbol = request.Symbol,
                        Time = time,
                        Open = open,
                        High = high,
                        Low = low,
                        Close = close,
                        Volume = volume,
                        Period = period,
                        DataType = MarketDataType.TradeBar
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"HyperliquidHistoryProvider: Error fetching candle history for {coin}: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Checks if a symbol can be subscribed to
        /// </summary>
        private bool CanSubscribe(Symbol symbol)
        {
            if (symbol == null || _symbolMapper == null)
            {
                return false;
            }

            return _symbolMapper.IsSymbolSupported(symbol);
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            _restClient?.Dispose();
        }
    }
}
