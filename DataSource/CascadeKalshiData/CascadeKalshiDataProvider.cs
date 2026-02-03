/*
 * Cascade Labs - Kalshi Data Provider
 * IHistoryProvider and IDataQueueHandler for Kalshi prediction markets
 */

using NodaTime;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Lean.DataSource.CascadeKalshiData.Models;

using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Lean.DataSource.CascadeKalshiData
{
    /// <summary>
    /// Kalshi data provider for prediction market candlestick data.
    /// Supports all Kalshi markets (weather, politics, finance, etc.)
    /// </summary>
    public class CascadeKalshiDataProvider : SynchronizingHistoryProvider, IDataQueueHandler
    {
        private static readonly DateTimeZone KalshiTimeZone = TimeZones.NewYork;

        private IDataAggregator? _dataAggregator;
        private KalshiSymbolMapper? _symbolMapper;
        private CascadeKalshiDataRestClient? _restApiClient;
        private RateGate? _rateGate;
        private bool _initialized;

        // Warning flags to avoid log spam
        private volatile bool _invalidSecurityTypeWarningFired;
        private volatile bool _invalidResolutionWarningFired;
        private volatile bool _invalidTickTypeWarningFired;

        /// <summary>
        /// REST-only provider, always "connected"
        /// </summary>
        public bool IsConnected => true;

        /// <summary>
        /// Supported security types
        /// </summary>
        public HashSet<SecurityType> SupportedSecurityTypes => new() { SecurityType.Base };

        /// <summary>
        /// Supported resolutions
        /// </summary>
        public HashSet<Resolution> SupportedResolutions => new() { Resolution.Minute };

        /// <summary>
        /// Access to the REST client for universe operations
        /// </summary>
        public CascadeKalshiDataRestClient? RestClient => _restApiClient;

        public CascadeKalshiDataProvider()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;

            Log.Trace("CascadeKalshiDataProvider: Initializing...");

            _dataAggregator = Composer.Instance.GetPart<IDataAggregator>();
            if (_dataAggregator == null)
            {
                _dataAggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(
                    Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"),
                    forceTypeNameOnExisting: false);
            }

            // 10 requests per second rate limit
            _rateGate = new RateGate(10, TimeSpan.FromSeconds(1));
            _restApiClient = new CascadeKalshiDataRestClient(_rateGate);
            _symbolMapper = new KalshiSymbolMapper();

            _initialized = true;

            Log.Trace("CascadeKalshiDataProvider: Initialized successfully");
        }

        /// <summary>
        /// Set job configuration for live trading
        /// </summary>
        public void SetJob(LiveNodePacket job)
        {
            // Configuration is read from config file, not job
            if (!_initialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Initialize history provider
        /// </summary>
        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
            // No additional initialization needed
        }

        /// <summary>
        /// Get history for multiple requests
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
        /// Get history for a single request
        /// </summary>
        public IEnumerable<BaseData>? GetHistory(HistoryRequest historyRequest)
        {
            if (_restApiClient == null || _symbolMapper == null)
            {
                Log.Error("CascadeKalshiDataProvider: Not initialized");
                return null;
            }

            var symbol = historyRequest.Symbol;

            // Validate security type FIRST to avoid log spam for unsupported types
            if (!SupportedSecurityTypes.Contains(symbol.SecurityType))
            {
                if (!_invalidSecurityTypeWarningFired)
                {
                    _invalidSecurityTypeWarningFired = true;
                    Log.Trace($"CascadeKalshiDataProvider: Unsupported security type '{symbol.SecurityType}'. Use SecurityType.Base.");
                }
                return null;
            }

            // Validate resolution
            if (!SupportedResolutions.Contains(historyRequest.Resolution))
            {
                if (!_invalidResolutionWarningFired)
                {
                    _invalidResolutionWarningFired = true;
                    Log.Trace($"CascadeKalshiDataProvider: Unsupported resolution '{historyRequest.Resolution}'. Only Minute is supported.");
                }
                return null;
            }

            // Accept both Quote and Trade tick types - Kalshi data is quote-based (bid/ask)
            // but LEAN may request Trade by default, so we handle both
            if (historyRequest.TickType != TickType.Quote && historyRequest.TickType != TickType.Trade)
            {
                if (!_invalidTickTypeWarningFired)
                {
                    _invalidTickTypeWarningFired = true;
                    Log.Trace($"CascadeKalshiDataProvider: Unsupported tick type '{historyRequest.TickType}'. Only Quote/Trade supported.");
                }
                return null;
            }

            // Validate symbol
            if (!_symbolMapper.IsKalshiSymbol(symbol))
            {
                Log.Error($"CascadeKalshiDataProvider: Invalid Kalshi symbol '{symbol.Value}'");
                return null;
            }

            var ticker = _symbolMapper.GetKalshiTicker(symbol);

            // Convert times to Eastern
            var startTimeLocal = historyRequest.StartTimeUtc.ConvertFromUtc(KalshiTimeZone);
            var endTimeLocal = historyRequest.EndTimeUtc.ConvertFromUtc(KalshiTimeZone);

            Log.Trace($"CascadeKalshiDataProvider.GetHistory: Symbol={symbol.Value}, Ticker={ticker}, Resolution={historyRequest.Resolution}");
            Log.Trace($"CascadeKalshiDataProvider: Fetching {ticker} from {startTimeLocal:yyyy-MM-dd HH:mm} to {endTimeLocal:yyyy-MM-dd HH:mm} ET");

            // Fetch data
            var history = GetCandlestickHistory(
                ticker,
                symbol,
                startTimeLocal,
                endTimeLocal,
                historyRequest.Resolution);

            var result = FilterHistory(history, historyRequest, startTimeLocal, endTimeLocal).ToList();
            Log.Trace($"CascadeKalshiDataProvider: Returning {result.Count} bars for {ticker}");
            return result;
        }

        private IEnumerable<QuoteBar> GetCandlestickHistory(
            string marketTicker,
            Symbol symbol,
            DateTime startTimeLocal,
            DateTime endTimeLocal,
            Resolution resolution)
        {
            var period = resolution.ToTimeSpan();

            foreach (var candle in _restApiClient!.GetCandlesticks(marketTicker, startTimeLocal, endTimeLocal))
            {
                // Skip candles without valid bid/ask data
                if (candle.YesBid?.IsValid != true && candle.YesAsk?.IsValid != true)
                {
                    continue;
                }

                var quoteBar = candle.ToQuoteBar(symbol, period, KalshiTimeZone);
                yield return quoteBar;
            }
        }

        private IEnumerable<BaseData> FilterHistory(
            IEnumerable<QuoteBar> history,
            HistoryRequest request,
            DateTime startTimeLocal,
            DateTime endTimeLocal)
        {
            foreach (var bar in history)
            {
                // Filter to requested time range
                if (bar.Time >= startTimeLocal && bar.EndTime <= endTimeLocal)
                {
                    // Check exchange hours if available
                    if (request.ExchangeHours.IsOpen(bar.Time, bar.EndTime, request.IncludeExtendedMarketHours))
                    {
                        yield return bar;
                    }
                }
            }
        }

        /// <summary>
        /// Check if we can subscribe to a symbol
        /// </summary>
        public bool CanSubscribe(Symbol symbol)
        {
            if (symbol.Value.IndexOfInvariant("universe", true) != -1)
            {
                return false;
            }

            if (symbol.IsCanonical())
            {
                return false;
            }

            return _symbolMapper?.IsKalshiSymbol(symbol) ?? false;
        }

        #region Universe Support

        /// <summary>
        /// Get all available markets from Kalshi.
        /// Use this for building custom universes.
        /// </summary>
        /// <param name="status">Filter by status: open, closed, settled (null for all)</param>
        /// <param name="seriesTicker">Filter by series (e.g., KXHIGHNY, INXD)</param>
        /// <returns>List of Kalshi markets</returns>
        public List<KalshiMarket> GetAvailableMarkets(string? status = null, string? seriesTicker = null)
        {
            if (_restApiClient == null)
            {
                Log.Error("CascadeKalshiDataProvider: Not initialized");
                return new List<KalshiMarket>();
            }

            return _restApiClient.GetMarkets(status: status, seriesTicker: seriesTicker);
        }

        /// <summary>
        /// Get markets that were active (open or recently closed) during a specific date range.
        /// Useful for backtesting universe selection.
        /// </summary>
        /// <param name="startDate">Start of date range</param>
        /// <param name="endDate">End of date range</param>
        /// <param name="seriesTicker">Optional series filter</param>
        /// <returns>List of markets active during the period</returns>
        public List<KalshiMarket> GetMarketsForDateRange(DateTime startDate, DateTime endDate, string? seriesTicker = null)
        {
            if (_restApiClient == null)
            {
                Log.Error("CascadeKalshiDataProvider: Not initialized");
                return new List<KalshiMarket>();
            }

            var startTs = startDate.ToUnixSeconds(KalshiTimeZone);
            var endTs = endDate.ToUnixSeconds(KalshiTimeZone);

            // Get markets that close within or after our date range
            return _restApiClient.GetMarkets(
                seriesTicker: seriesTicker,
                minCloseTs: startTs);
        }

        /// <summary>
        /// Get market info for a specific ticker
        /// </summary>
        public KalshiMarket? GetMarketInfo(string ticker)
        {
            return _restApiClient?.GetMarket(ticker);
        }

        /// <summary>
        /// Get all series from Kalshi
        /// </summary>
        public List<KalshiSeries> GetAllSeries()
        {
            if (_restApiClient == null)
            {
                Log.Error("CascadeKalshiDataProvider: Not initialized");
                return new List<KalshiSeries>();
            }

            return _restApiClient.GetSeriesAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Convert a Kalshi market ticker to a LEAN Symbol
        /// </summary>
        public Symbol CreateSymbol(string ticker)
        {
            if (_symbolMapper == null)
            {
                throw new InvalidOperationException("Provider not initialized");
            }

            return _symbolMapper.GetLeanSymbol(ticker);
        }

        #endregion

        // IDataQueueHandler implementation (minimal, REST-only for now)

        /// <summary>
        /// Subscribe to live data (not implemented - REST only)
        /// </summary>
        public IEnumerator<BaseData>? Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            // REST-only provider, no streaming support yet
            Log.Trace($"CascadeKalshiDataProvider: Live subscription not supported for {dataConfig.Symbol}");
            return null;
        }

        /// <summary>
        /// Unsubscribe from live data
        /// </summary>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            // Nothing to do for REST-only
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _restApiClient?.Dispose();
            _dataAggregator?.DisposeSafely();
            _rateGate?.Dispose();
        }
    }
}
