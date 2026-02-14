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
    /// - CryptoFuture (perpetuals) and Crypto (spot) security types
    /// </remarks>
    public class HyperliquidHistoryProvider : SynchronizingHistoryProvider
    {
        private HyperliquidRestClient? _restClient;
        private HyperliquidSymbolMapper? _symbolMapper;
        private HyperliquidS3Client? _s3Client;
        private bool _initialized;

        /// <summary>
        /// Number of days before today where we switch from S3 to REST API.
        /// S3 data is updated ~monthly, so data from the last ~30 days may not be on S3 yet.
        /// REST API is used as a fallback for this recent tail.
        /// </summary>
        private const int S3CutoffDays = 30;

        // Warning flags to avoid log spam
        private volatile bool _invalidSecurityTypeWarningFired;
        private volatile bool _invalidResolutionWarningFired;
        private volatile bool _quoteDataWarningFired;

        // Spot token -> pair name cache for API symbol transformation
        // Maps token name (e.g., "UBTC") to pair API symbol (e.g., "@142")
        private Dictionary<string, string>? _spotTokenToPairCache;
        private readonly object _spotCacheLock = new();
        private volatile bool _spotCacheInitialized;

        /// <summary>
        /// Earliest date with Hyperliquid S3 data available.
        /// node_trades/hourly starts March 2025, node_fills_by_block starts July 2025.
        /// </summary>
        private static readonly DateTime MinimumHistoryDate = new DateTime(2025, 3, 22, 0, 0, 0, DateTimeKind.Utc);

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
            _s3Client = new HyperliquidS3Client();
            _initialized = true;

            Log.Trace($"HyperliquidHistoryProvider: Initialized (testnet: {useTestnet}, s3: {_s3Client.IsConfigured})");
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
        public IEnumerable<BaseData>? GetHistory(HistoryRequest request)
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
                    Log.Trace($"HyperliquidHistoryProvider: Unsupported security type '{request.Symbol.SecurityType}'. Only CryptoFuture and Crypto are supported.");
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

            // Skip requests entirely before Hyperliquid existed
            if (request.EndTimeUtc <= MinimumHistoryDate)
            {
                return null;
            }

            // Route to appropriate handler based on tick type and resolution
            if (request.TickType == TickType.Trade && request.Resolution == Resolution.Tick)
            {
                return GetTickHistory(coin, request);
            }

            if (request.TickType == TickType.Quote)
            {
                if (!_quoteDataWarningFired)
                {
                    _quoteDataWarningFired = true;
                    Log.Trace($"HyperliquidHistoryProvider: Quote data not supported. Skipping quote requests.");
                }
                return null;
            }

            // Get candle/TradeBar data
            Log.Trace($"HyperliquidHistoryProvider: Requesting candle data for {coin}, resolution {request.Resolution}, from {request.StartTimeUtc} to {request.EndTimeUtc}");
            return GetCandleHistory(coin, request);
        }

        /// <summary>
        /// Ensures the spot token to pair cache is initialized
        /// </summary>
        private void EnsureSpotCacheInitialized()
        {
            if (_spotCacheInitialized) return;

            lock (_spotCacheLock)
            {
                if (_spotCacheInitialized) return;

                try
                {
                    var spotMetaTask = _restClient!.GetSpotMetaAsync();
                    spotMetaTask.Wait();

                    var spotMeta = spotMetaTask.Result;
                    if (spotMeta == null)
                    {
                        Log.Error("HyperliquidHistoryProvider: Failed to fetch spotMeta");
                        _spotTokenToPairCache = new Dictionary<string, string>();
                        _spotCacheInitialized = true;
                        return;
                    }

                    _spotTokenToPairCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    // Build tokenIndex -> tokenName mapping
                    var tokenIndexToName = new Dictionary<int, string>();
                    var tokens = spotMeta["tokens"] as JArray;
                    if (tokens != null)
                    {
                        foreach (var token in tokens)
                        {
                            var name = token["name"]?.Value<string>();
                            var index = token["index"]?.Value<int>() ?? -1;

                            if (!string.IsNullOrEmpty(name) && index >= 0)
                            {
                                tokenIndexToName[index] = name;
                            }
                        }
                    }

                    // Parse universe array to get pair names and map base token to pair
                    // Each pair has "name" (e.g., "@142") and "tokens" array [baseTokenIndex, quoteTokenIndex]
                    var universe = spotMeta["universe"] as JArray;
                    if (universe != null)
                    {
                        foreach (var pair in universe)
                        {
                            var pairName = pair["name"]?.Value<string>();
                            var pairTokens = pair["tokens"] as JArray;

                            if (string.IsNullOrEmpty(pairName) || pairTokens == null || pairTokens.Count < 2)
                                continue;

                            // First token is base, second is quote (usually USDC=0)
                            var baseTokenIndex = pairTokens[0]?.Value<int>() ?? -1;
                            var quoteTokenIndex = pairTokens[1]?.Value<int>() ?? -1;

                            // Only cache pairs quoted in USDC (index 0)
                            if (quoteTokenIndex != 0)
                                continue;

                            if (baseTokenIndex >= 0 && tokenIndexToName.TryGetValue(baseTokenIndex, out var baseTokenName))
                            {
                                _spotTokenToPairCache[baseTokenName] = pairName;
                            }
                        }
                    }

                    Log.Trace($"HyperliquidHistoryProvider: Initialized spot cache with {_spotTokenToPairCache.Count} token->pair mappings");

                    // Log some examples for debugging
                    var examples = _spotTokenToPairCache.Take(5).Select(kvp => $"{kvp.Key}={kvp.Value}");
                    Log.Trace($"HyperliquidHistoryProvider: Sample mappings: {string.Join(", ", examples)}");
                }
                catch (Exception ex)
                {
                    Log.Error($"HyperliquidHistoryProvider: Error initializing spot cache: {ex.Message}");
                    _spotTokenToPairCache = new Dictionary<string, string>();
                }

                _spotCacheInitialized = true;
            }
        }

        /// <summary>
        /// Gets the API symbol format for candle requests
        /// </summary>
        /// <param name="symbol">LEAN symbol</param>
        /// <param name="coin">Coin name from symbol mapper (e.g., "BTC", "UBTC")</param>
        /// <returns>API symbol format: coin name for perps, pair name (e.g., @142) for spot</returns>
        private string? GetCandleApiSymbol(Symbol symbol, string coin)
        {
            if (symbol.SecurityType == SecurityType.CryptoFuture)
            {
                // Perps use coin name directly
                return coin;
            }

            if (symbol.SecurityType == SecurityType.Crypto)
            {
                // Spot uses pair name format (e.g., "@142" for UBTC/USDC)
                EnsureSpotCacheInitialized();

                if (_spotTokenToPairCache == null || !_spotTokenToPairCache.TryGetValue(coin, out var pairName))
                {
                    var availableTokens = _spotTokenToPairCache != null
                        ? string.Join(", ", _spotTokenToPairCache.Keys.Take(20))
                        : "none";
                    Log.Error($"HyperliquidHistoryProvider: Spot token '{coin}' not found in spotMeta. Available tokens: {availableTokens}");
                    return null;
                }

                Log.Trace($"HyperliquidHistoryProvider: Mapped spot token '{coin}' to pair '{pairName}'");
                return pairName;
            }

            return coin;
        }

        /// <summary>
        /// Gets tick (trade) history for a symbol.
        /// Uses S3 for historical data, REST for recent data.
        /// </summary>
        private IEnumerable<BaseData>? GetTickHistory(string coin, HistoryRequest request)
        {
            var results = new List<BaseData>();
            var s3CutoffUtc = DateTime.UtcNow.AddDays(-S3CutoffDays);

            // S3 portion: historical tick data from S3 fills
            if (_s3Client != null && _s3Client.IsConfigured && request.StartTimeUtc < s3CutoffUtc)
            {
                var s3EndUtc = request.EndTimeUtc < s3CutoffUtc ? request.EndTimeUtc : s3CutoffUtc;

                // For spot, S3 uses pair names like @151. For perps, coin name directly.
                var s3Coin = GetS3CoinFilter(request.Symbol, coin);

                var s3Ticks = GetS3TickHistory(s3Coin, request.Symbol, request.StartTimeUtc, s3EndUtc);
                results.AddRange(s3Ticks);

                Log.Trace($"HyperliquidHistoryProvider: Got {results.Count} ticks from S3 for {coin}");

                // If request is entirely within S3 range, return now
                if (request.EndTimeUtc <= s3CutoffUtc)
                {
                    return results;
                }
            }

            // REST portion: recent tick data
            var apiSymbol = GetCandleApiSymbol(request.Symbol, coin);
            if (apiSymbol == null)
            {
                return results;
            }

            try
            {
                var tradesTask = _restClient!.GetRecentTradesAsync(apiSymbol);
                tradesTask.Wait();

                var trades = tradesTask.Result;
                if (trades == null || !trades.Any())
                {
                    return results;
                }

                // REST start is either after S3 cutoff or the original start
                var restStart = (_s3Client?.IsConfigured == true && request.StartTimeUtc < s3CutoffUtc)
                    ? s3CutoffUtc : request.StartTimeUtc;

                foreach (var trade in trades)
                {
                    var time = trade["time"]?.Value<long>() ?? 0;
                    var price = decimal.Parse(trade["px"]?.Value<string>() ?? "0");
                    var size = decimal.Parse(trade["sz"]?.Value<string>() ?? "0");
                    var side = trade["side"]?.Value<string>() ?? "";

                    var timeUtc = DateTimeOffset.FromUnixTimeMilliseconds(time).UtcDateTime;

                    if (timeUtc < restStart || timeUtc >= request.EndTimeUtc)
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
        /// Gets candle (TradeBar) history for a symbol.
        /// Uses S3 for historical data (beyond cutoff), REST API for recent data.
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
            var s3CutoffUtc = DateTime.UtcNow.AddDays(-S3CutoffDays);

            // S3 portion: historical candle data from S3 fills
            if (_s3Client != null && _s3Client.IsConfigured && request.StartTimeUtc < s3CutoffUtc)
            {
                var s3EndUtc = request.EndTimeUtc < s3CutoffUtc ? request.EndTimeUtc : s3CutoffUtc;

                // For spot, S3 uses pair names like @151. For perps, coin name directly.
                var s3Coin = GetS3CoinFilter(request.Symbol, coin);

                var s3Bars = GetS3CandleHistory(s3Coin, request.Symbol, request.Resolution, request.StartTimeUtc, s3EndUtc);
                results.AddRange(s3Bars);

                Log.Trace($"HyperliquidHistoryProvider: Got {results.Count} bars from S3 for {coin}");

                // If request is entirely within S3 range, return now
                if (request.EndTimeUtc <= s3CutoffUtc)
                {
                    return results;
                }
            }

            // REST portion: recent candle data
            var apiSymbol = GetCandleApiSymbol(request.Symbol, coin);
            if (apiSymbol == null)
            {
                return results;
            }

            try
            {
                // REST start is either after S3 cutoff or the original start
                var restStartUtc = (_s3Client?.IsConfigured == true && request.StartTimeUtc < s3CutoffUtc)
                    ? s3CutoffUtc : request.StartTimeUtc;

                var startMs = new DateTimeOffset(restStartUtc).ToUnixTimeMilliseconds();
                var endMs = new DateTimeOffset(request.EndTimeUtc).ToUnixTimeMilliseconds();

                Log.Trace($"HyperliquidHistoryProvider: Fetching candles for apiSymbol '{apiSymbol}' (coin: {coin}) from REST");
                var candlesTask = _restClient!.GetCandleSnapshotAsync(apiSymbol, interval, startMs, endMs);
                candlesTask.Wait();

                var candles = candlesTask.Result;
                Log.Trace($"HyperliquidHistoryProvider: Received {candles?.Count() ?? 0} candles from REST API");
                if (candles == null || !candles.Any())
                {
                    Log.Trace($"HyperliquidHistoryProvider: No candles returned from REST for {apiSymbol}");
                    return results;
                }

                foreach (var candle in candles)
                {
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
        /// Gets the coin filter string for S3 data.
        /// For spot tokens, S3 uses the pair name (e.g., "@151") not the token name.
        /// For perps, the coin name is used directly.
        /// </summary>
        private string GetS3CoinFilter(Symbol symbol, string coin)
        {
            if (symbol.SecurityType == SecurityType.Crypto)
            {
                // Spot: need to resolve to pair name for S3 filtering
                var apiSymbol = GetCandleApiSymbol(symbol, coin);
                return apiSymbol ?? coin;
            }

            // Perps and others use coin name directly
            return coin;
        }

        /// <summary>
        /// Downloads S3 fills for a date/hour range and aggregates into TradeBars
        /// </summary>
        private IEnumerable<BaseData> GetS3CandleHistory(
            string coin, Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            var fills = DownloadS3Fills(coin, startUtc, endUtc);
            return HyperliquidFillParser.AggregateToTradeBars(fills, symbol, resolution, startUtc, endUtc);
        }

        /// <summary>
        /// Downloads S3 fills for a date/hour range and converts to Ticks
        /// </summary>
        private IEnumerable<BaseData> GetS3TickHistory(
            string coin, Symbol symbol, DateTime startUtc, DateTime endUtc)
        {
            var fills = DownloadS3Fills(coin, startUtc, endUtc);
            return HyperliquidFillParser.ToTicks(fills, symbol, startUtc, endUtc);
        }

        /// <summary>
        /// Downloads and parses S3 fill data for a date/hour range, filtering by coin.
        /// Iterates through each hour in the range, downloads the hourly file, and parses fills.
        /// </summary>
        private List<HyperliquidFill> DownloadS3Fills(string coin, DateTime startUtc, DateTime endUtc)
        {
            var allFills = new List<HyperliquidFill>();
            if (_s3Client == null || !_s3Client.IsConfigured) return allFills;

            // Clamp to Hyperliquid launch date — no data exists before this
            if (startUtc < MinimumHistoryDate)
            {
                startUtc = MinimumHistoryDate;
            }

            if (startUtc >= endUtc) return allFills;

            // Iterate through each hour in the range
            var currentHour = new DateTime(startUtc.Year, startUtc.Month, startUtc.Day, startUtc.Hour, 0, 0, DateTimeKind.Utc);
            var lastHour = new DateTime(endUtc.Year, endUtc.Month, endUtc.Day, endUtc.Hour, 0, 0, DateTimeKind.Utc);

            // Cache prefix lookups per date to avoid repeated S3 list calls
            var prefixCache = new Dictionary<string, string?>();

            while (currentHour <= lastHour)
            {
                var dateStr = currentHour.ToString("yyyyMMdd");

                if (!prefixCache.TryGetValue(dateStr, out var prefix))
                {
                    prefix = _s3Client.GetPrefixForDate(dateStr);
                    prefixCache[dateStr] = prefix;

                    if (prefix == null)
                    {
                        Log.Trace($"HyperliquidHistoryProvider: No S3 data available for {dateStr}");
                    }
                }

                if (prefix != null)
                {
                    var hour = currentHour.Hour;
                    using var stream = _s3Client.DownloadAndDecompress(prefix, dateStr, hour);
                    if (stream != null)
                    {
                        IEnumerable<HyperliquidFill> fills;
                        if (prefix == HyperliquidS3Client.NodeFillsByBlockPrefixPath)
                        {
                            fills = HyperliquidFillParser.ParseNodeFillsByBlock(stream, coin);
                        }
                        else
                        {
                            fills = HyperliquidFillParser.ParseNodeTrades(stream, coin);
                        }

                        allFills.AddRange(fills);
                    }
                }

                currentHour = currentHour.AddHours(1);
            }

            Log.Trace($"HyperliquidHistoryProvider: Downloaded {allFills.Count} fills from S3 for {coin} ({startUtc:yyyy-MM-dd HH:mm} to {endUtc:yyyy-MM-dd HH:mm})");
            return allFills;
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
            _s3Client?.Dispose();
        }
    }
}
