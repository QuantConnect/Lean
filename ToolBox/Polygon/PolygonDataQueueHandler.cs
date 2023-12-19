/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.Forex;
using QuantConnect.ToolBox.Polygon.WebSocket;
using QuantConnect.ToolBox.Polygon.History;
using QuantConnect.Util;
using HistoryRequest = QuantConnect.Data.HistoryRequest;
using static QuantConnect.StringExtensions;

namespace QuantConnect.ToolBox.Polygon
{
    /// <summary>
    /// An implementation of <see cref="IDataQueueHandler"/> and <see cref="IHistoryProvider"/> for Polygon.io
    /// </summary>
    public class PolygonDataQueueHandler : SynchronizingHistoryProvider, IDataQueueHandler
    {
        private const string HistoryBaseUrl = "https://api.polygon.io";
        private const int ResponseSizeLimitAggregateData = 50000;
        private const int ResponseSizeLimitEquities = 50000;
        private const int ResponseSizeLimitCurrencies = 10000;
        private readonly string _apiKey = Config.Get("polygon-api-key");

        private readonly IDataAggregator _dataAggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(
            Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"));

        private readonly DataQueueHandlerSubscriptionManager _subscriptionManager;

        private readonly ManualResetEvent _successfulAuthentication = new(false);
        private readonly ManualResetEvent _failedAuthentication = new(false);

        private readonly Dictionary<SecurityType, PolygonWebSocketClientWrapper> _webSocketClientWrappers = new();
        private readonly PolygonSymbolMapper _symbolMapper = new PolygonSymbolMapper();
        private readonly MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
        private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();

        // exchange time zones by symbol
        private readonly Dictionary<Symbol, DateTimeZone> _symbolExchangeTimeZones = new();

        // map Polygon exchange -> Lean market
        // Crypto exchanges from: https://api.polygon.io/v1/meta/crypto-exchanges?apiKey=xxx
        private readonly Dictionary<int, string> _cryptoExchangeMap = new()
        {
            { 1, Market.Coinbase },
            { 2, Market.Bitfinex },
            { 6, Market.Bitstamp },
            { 10, Market.HitBTC },
            { 23, Market.Kraken }
        };

        private int _dataPointCount;

        /// <summary>
        /// Static constructor for the <see cref="PolygonDataQueueHandler"/> class
        /// </summary>
        static PolygonDataQueueHandler()
        {
            // Polygon.io requires TLS 1.2
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonDataQueueHandler"/> class
        /// </summary>
        public PolygonDataQueueHandler() : this(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonDataQueueHandler"/> class
        /// </summary>
        public PolygonDataQueueHandler(bool streamingEnabled)
        {
            if (streamingEnabled)
            {
                var securityTypes = new[] { SecurityType.Equity, SecurityType.Forex, SecurityType.Crypto };

                foreach (var securityType in securityTypes)
                {
                    _failedAuthentication.Reset();
                    _successfulAuthentication.Reset();

                    var websocket = new PolygonWebSocketClientWrapper(_apiKey, _symbolMapper, securityType, OnMessage);

                    var timedout = WaitHandle.WaitAny(new WaitHandle[] { _failedAuthentication, _successfulAuthentication }, TimeSpan.FromMinutes(2));
                    if (timedout == WaitHandle.WaitTimeout)
                    {
                        // Close current websocket connection
                        websocket.Close();
                        // Close all connections that have been successful so far
                        ShutdownWebSockets();
                        throw new TimeoutException($"Timeout waiting for websocket to connect for {securityType}");
                    }

                    // If it hasn't timed out, it could still have failed.
                    // For example, the API keys do not have rights to subscribe to the current security type
                    // In this case, we close this connect and move on
                    if (_failedAuthentication.WaitOne(0))
                    {
                        websocket.Close();
                        continue;
                    }

                    _webSocketClientWrappers[securityType] = websocket;
                }

                // If we could not connect to any websocket because of the API rights,
                // we exit this data queue handler
                if (_webSocketClientWrappers.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Websocket authentication failed for all security types: {string.Join(", ", securityTypes)}." +
                        "Please confirm whether the subscription plan associated with your API keys includes support to websockets.");
                }
            }

            var subscriber = new EventBasedDataQueueHandlerSubscriptionManager(t => t.ToString());
            subscriber.SubscribeImpl += Subscribe;
            subscriber.UnsubscribeImpl += Unsubscribe;

            _subscriptionManager = subscriber;
        }

        #region IDataQueueHandler implementation

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        /// <summary>
        /// Indicates the connection is live.
        /// </summary>
        public bool IsConnected => _webSocketClientWrappers.Count > 0 && _webSocketClientWrappers.Values.All(client => client.IsOpen);

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            if (!CanSubscribe(dataConfig.Symbol))
            {
                return null;
            }

            var enumerator = _dataAggregator.Add(dataConfig, newDataAvailableHandler);
            _subscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            _subscriptionManager.Unsubscribe(dataConfig);
            _dataAggregator.Remove(dataConfig);
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be added</param>
        /// <param name="tickType">Type of tick data</param>
        private bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            foreach (var symbol in symbols)
            {
                var webSocket = GetWebSocket(symbol.SecurityType);
                webSocket.Subscribe(symbol, tickType);
            }

            return true;
        }

        /// <summary>
        /// Removes the specified symbols from the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be removed</param>
        /// <param name="tickType">Type of tick data</param> 
        private bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            foreach (var symbol in symbols)
            {
                var webSocket = GetWebSocket(symbol.SecurityType);
                webSocket.Unsubscribe(symbol, tickType);
            }

            return true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            ShutdownWebSockets();
            _dataAggregator.DisposeSafely();
        }

        #endregion

        #region IHistoryProvider implementation

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public override int DataPointCount => _dataPointCount;

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
        }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            var subscriptions = new List<Subscription>();
            foreach (var request in requests)
            {
                var history = GetHistory(request);
                var subscription = CreateSubscription(request, history);

                subscriptions.Add(subscription);
            }

            return CreateSliceEnumerableFromSubscriptions(subscriptions, sliceTimeZone);
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of BaseData points</returns>
        public IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            return ProcessHistoryRequest(request);
        }

        #endregion

        private IEnumerable<BaseData> ProcessHistoryRequest(HistoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Log.Error("PolygonDataQueueHandler.GetHistory(): History calls for Polygon.io require an API key.");
                yield break;
            }

            // check security type
            if (request.Symbol.SecurityType != SecurityType.Equity &&
                request.Symbol.SecurityType != SecurityType.Forex &&
                request.Symbol.SecurityType != SecurityType.Crypto)
            {
                Log.Error($"PolygonDataQueueHandler.ProcessHistoryRequests(): Unsupported security type: {request.Symbol.SecurityType}.");
                yield break;
            }

            // check tick type
            if (request.TickType != TickType.Trade && request.TickType != TickType.Quote)
            {
                Log.Error($"PolygonDataQueueHandler.ProcessHistoryRequests(): Unsupported tick type: {request.TickType}.");
                yield break;
            }

            // check unsupported security type/tick type combinations
            if (request.Symbol.SecurityType == SecurityType.Forex && request.TickType != TickType.Quote ||
                request.Symbol.SecurityType == SecurityType.Crypto && request.TickType != TickType.Trade)
            {
                Log.Error($"PolygonDataQueueHandler.ProcessHistoryRequests(): Unsupported history request: {request.Symbol.SecurityType}/{request.TickType}.");
                yield break;
            }

            Log.Trace("PolygonDataQueueHandler.ProcessHistoryRequests(): Submitting request: " +
                      Invariant($"{request.Symbol.SecurityType}-{request.TickType}-{request.Symbol.Value}: {request.Resolution} {request.StartTimeUtc}->{request.EndTimeUtc}"));

            switch (request.Resolution)
            {
                case Resolution.Tick:
                    if (request.TickType == TickType.Trade)
                    {
                        foreach (var tick in GetTradeTicks(request))
                        {
                            Interlocked.Increment(ref _dataPointCount);

                            yield return tick;
                        }
                    }
                    else if (request.TickType == TickType.Quote)
                    {
                        foreach (var tick in GetQuoteTicks(request))
                        {
                            Interlocked.Increment(ref _dataPointCount);

                            yield return tick;
                        }
                    }

                    break;

                case Resolution.Second:
                    if (request.TickType == TickType.Trade)
                    {
                        var ticks = GetTradeTicks(request);

                        foreach (var tradeBar in AggregateTradeTicks(request.Symbol, ticks, request.Resolution.ToTimeSpan()))
                        {
                            Interlocked.Increment(ref _dataPointCount);

                            yield return tradeBar;
                        }
                    }
                    else if (request.TickType == TickType.Quote)
                    {
                        var ticks = GetQuoteTicks(request);

                        foreach (var quoteBar in AggregateQuoteTicks(request.Symbol, ticks, request.Resolution.ToTimeSpan()))
                        {
                            Interlocked.Increment(ref _dataPointCount);

                            yield return quoteBar;
                        }
                    }

                    break;

                case Resolution.Minute:
                case Resolution.Hour:
                case Resolution.Daily:
                    if (request.TickType == TickType.Trade)
                    {
                        foreach (var tradeBar in GetTradeBars(request))
                        {
                            Interlocked.Increment(ref _dataPointCount);

                            yield return tradeBar;
                        }
                    }
                    else if (request.TickType == TickType.Quote)
                    {
                        var ticks = GetQuoteTicks(request);

                        foreach (var quoteBar in AggregateQuoteTicks(request.Symbol, ticks, request.Resolution.ToTimeSpan()))
                        {
                            Interlocked.Increment(ref _dataPointCount);

                            yield return quoteBar;
                        }
                    }

                    break;
            }
        }

        private IEnumerable<Tick> GetQuoteTicks(HistoryRequest request)
        {
            switch (request.Symbol.SecurityType)
            {
                case SecurityType.Equity:
                    return GetEquityQuoteTicks(request);

                case SecurityType.Forex:
                    return GetForexQuoteTicks(request);

                default:
                    return Enumerable.Empty<Tick>();
            }
        }

        private IEnumerable<Tick> GetTradeTicks(HistoryRequest request)
        {
            switch (request.Symbol.SecurityType)
            {
                case SecurityType.Equity:
                    return GetEquityTradeTicks(request);

                case SecurityType.Crypto:
                    return GetCryptoTradeTicks(request);

                default:
                    return Enumerable.Empty<Tick>();
            }
        }

        private IEnumerable<Tick> GetForexQuoteTicks(HistoryRequest request)
        {
            // https://api.polygon.io/v1/historic/forex/EUR/USD/2020-08-24?apiKey=

            var start = request.StartTimeUtc;
            var end = request.EndTimeUtc;
            var currentDate = start.Date;

            while (currentDate <= end.Date)
            {
                Log.Debug($"GetForexQuoteTicks(): Downloading ticks for the date {currentDate:yyyy-MM-dd}; symbol: {request.Symbol.ID.Symbol}");

                // If this is a very first iteration set offset exactly as request's start time.
                // Otherwise use date start as an offset. (!) Make sure to cast to Int64.
                var offset = currentDate == start.Date ? (long)Time.DateTimeToUnixTimeStampMilliseconds(start)
                    : (long)Time.DateTimeToUnixTimeStampMilliseconds(currentDate);

                var counter = 0;
                long lastTickTimestamp = 0;

                while (true)
                {
                    counter++;

                    string baseCurrency;
                    string quoteCurrency;
                    Forex.DecomposeCurrencyPair(request.Symbol.Value, out baseCurrency, out quoteCurrency);

                    var url = $"{HistoryBaseUrl}/v1/historic/forex/{baseCurrency}/{quoteCurrency}/{currentDate:yyyy-MM-dd}?" +
                              $"limit={ResponseSizeLimitCurrencies}&apiKey={_apiKey}&offset={offset}";

                    var response = DownloadAndParseData(typeof(ForexQuoteTickResponse[]), url, "ticks") as ForexQuoteTickResponse[];

                    // The first results of the next page will coincide with last of the previous page, lets clear from repeating values
                    var quoteTicksList = response?.Where(x => x.Timestamp != lastTickTimestamp).ToList();
                    if (quoteTicksList.IsNullOrEmpty())
                    {
                        break;
                    }

                    Log.Debug($"GetForexQuoteTicks(): Page # {counter}; " +
                              $"first: {Time.UnixMillisecondTimeStampToDateTime(quoteTicksList.First().Timestamp)}; " +
                              $"last: {Time.UnixMillisecondTimeStampToDateTime(quoteTicksList.Last().Timestamp)}");

                    foreach (var row in quoteTicksList)
                    {
                        var utcTime = Time.UnixMillisecondTimeStampToDateTime(row.Timestamp);

                        if (utcTime < start)
                        {
                            continue;
                        }

                        if (utcTime > end)
                        {
                            yield break;
                        }

                        var time = GetTickTime(request.Symbol, utcTime);
                        yield return new Tick(time, request.Symbol, row.Bid, row.Ask);

                        lastTickTimestamp = row.Timestamp;
                    }

                    offset = lastTickTimestamp;
                    _dataPointCount += quoteTicksList.Count;
                }

                // Jump to the next iteration
                currentDate = currentDate.AddDays(1);
            }
        }

        private IEnumerable<Tick> GetCryptoTradeTicks(HistoryRequest request)
        {
            // https://api.polygon.io/v1/historic/crypto/BTC/USD/2020-08-24?apiKey=

            var start = request.StartTimeUtc;
            var end = request.EndTimeUtc;
            var currentDate = start.Date;

            while (currentDate <= end.Date)
            {
                Log.Debug(
                    $"GetCryptoTradeTicks(): Downloading ticks for the date {currentDate:yyyy-MM-dd}; symbol: {request.Symbol.ID.Symbol}");

                var offset = currentDate == start.Date ? (long)Time.DateTimeToUnixTimeStampMilliseconds(start)
                    : (long)Time.DateTimeToUnixTimeStampMilliseconds(currentDate);

                var counter = 0;
                long lastTickTimestamp = 0;
                while (true)
                {
                    counter++;

                    var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(
                        request.Symbol.ID.Market,
                        request.Symbol,
                        request.Symbol.SecurityType,
                        Currencies.USD);

                    string baseCurrency;
                    string quoteCurrency;
                    Crypto.DecomposeCurrencyPair(request.Symbol, symbolProperties, out baseCurrency, out quoteCurrency);

                    var url = $"{HistoryBaseUrl}/v1/historic/crypto/{baseCurrency}/{quoteCurrency}/{currentDate:yyyy-MM-dd}?" +
                              $"limit={ResponseSizeLimitCurrencies}&apiKey={_apiKey}&offset={offset}";

                    var response = DownloadAndParseData(typeof(CryptoTradeTickResponse[]), url, "ticks") as CryptoTradeTickResponse[];

                    // The first results of the next page will coincide with last of the previous page, lets clear from repeating values
                    var tradeTicksList = response?.Where(x => x.Timestamp != lastTickTimestamp).ToList();

                    if (tradeTicksList.IsNullOrEmpty())
                    {
                        break;
                    }

                    Log.Debug($"GetCryptoTradeTicks(): Page # {counter}; " +
                              $"first: {Time.UnixMillisecondTimeStampToDateTime(tradeTicksList.First().Timestamp)}; " +
                              $"last: {Time.UnixMillisecondTimeStampToDateTime(tradeTicksList.Last().Timestamp)}");

                    foreach (var row in tradeTicksList)
                    {
                        var utcTime = Time.UnixMillisecondTimeStampToDateTime(row.Timestamp);
                        if (utcTime < start)
                        {
                            continue;
                        }

                        if (utcTime > end)
                        {
                            yield break;
                        }

                        // Another oddity of coin api. The final tick of the dat may be a tick from another exchange
                        // If we are geting such a tick, we need to store the last tick value in due course.
                        var market = GetMarketFromCryptoExchangeId(row.Exchange);
                        if (market != request.Symbol.ID.Market)
                        {
                            lastTickTimestamp = row.Timestamp;
                            continue;
                        }

                        var time = GetTickTime(request.Symbol, utcTime);
                        yield return new Tick(time, request.Symbol, string.Empty, string.Empty, row.Size, row.Price);

                        lastTickTimestamp = row.Timestamp;
                    }

                    offset = lastTickTimestamp;
                    _dataPointCount += tradeTicksList.Count;
                }
                // Jump to the next iteration
                currentDate = currentDate.AddDays(1);
            }
        }

        private IEnumerable<Tick> GetEquityQuoteTicks(HistoryRequest request)
        {
            // https://api.polygon.io/v2/ticks/stocks/nbbo/SPY/2020-08-24?apiKey=

            var start = request.StartTimeUtc;
            var end = request.EndTimeUtc;
            var currentDate = start.Date;

            while (currentDate <= end.Date)
            {
                Log.Debug($"GetEquityQuoteTicks(): Downloading ticks for the date {currentDate:yyyy-MM-dd}; symbol: {request.Symbol.ID.Symbol}");

                // If this is a very first iteration set offset exactly as request's start time. Otherwise use date start as an offset.
                var offset = currentDate == start.Date
                    ? Time.DateTimeToUnixTimeStampNanoseconds(start)
                    : Time.DateTimeToUnixTimeStampNanoseconds(currentDate);

                var counter = 0;
                long lastTickSipTimeStamp = 0;

                while (true)
                {
                    counter++;

                    var url = $"{HistoryBaseUrl}/v2/ticks/stocks/nbbo/{request.Symbol.Value}/{currentDate.Date:yyyy-MM-dd}?" +
                              $"apiKey={_apiKey}&timestamp={offset}&limit={ResponseSizeLimitEquities}";
                    var response = DownloadAndParseData(typeof(EquityQuoteTickResponse[]), url, "results") as EquityQuoteTickResponse[];

                    // The first results of the next page will coincide with last of the previous page
                    // We distinguish the results by the timestamp, lets clear from repeating values
                    var quoteTicksList = response?.Where(x => x.SipTimestamp != lastTickSipTimeStamp).ToList();

                    // API will send at the end only such repeating ticks that coincide with last results of previous page
                    // If there are no other ticks other than these then we break
                    if (quoteTicksList.IsNullOrEmpty())
                    {
                        break;
                    }

                    Log.Debug($"GetEquityQuoteTicks(): Page # {counter}; " +
                              $"first: {Time.UnixNanosecondTimeStampToDateTime(quoteTicksList.First().SipTimestamp)}; " +
                              $"last: {Time.UnixNanosecondTimeStampToDateTime(quoteTicksList.Last().SipTimestamp)}");

                    foreach (var row in quoteTicksList)
                    {
                        var utcTime = Time.UnixNanosecondTimeStampToDateTime(row.SipTimestamp);
                        if (utcTime < start)
                        {
                            continue;
                        }

                        if (utcTime > end)
                        {
                            yield break;
                        }

                        var time = GetTickTime(request.Symbol, utcTime);
                        yield return new Tick(time, request.Symbol, string.Empty, string.Empty, row.BidSize, row.BidPrice, row.AskSize, row.AskPrice);

                        // Save the values before to jump to the next iteration
                        lastTickSipTimeStamp = row.SipTimestamp;
                    }

                    offset = lastTickSipTimeStamp;
                    _dataPointCount += quoteTicksList.Count;
                }

                // Jump to the next iteration
                currentDate = currentDate.AddDays(1);
            }
        }

        private IEnumerable<Tick> GetEquityTradeTicks(HistoryRequest request)
        {
            // https://api.polygon.io/v2/ticks/stocks/trades/SPY/2020-08-24?apiKey=

            var start = request.StartTimeUtc;
            var end = request.EndTimeUtc;
            var currentDate = start.Date;

            while (currentDate <= end.Date)
            {
                Log.Debug($"GetEquityTradeTicks(): Downloading ticks for the date {currentDate:yyyy-MM-dd}; symbol: {request.Symbol.ID.Symbol}");

                // If this is a very first iteration set offset exactly as request's start time. Otherwise use date start as an offset.
                var offset = currentDate == start.Date
                    ? Time.DateTimeToUnixTimeStampNanoseconds(start)
                    : Time.DateTimeToUnixTimeStampNanoseconds(currentDate);

                var counter = 0;
                long lastTickSipTimeStamp = 0;

                while (true)
                {
                    counter++;

                    var url = $"{HistoryBaseUrl}/v2/ticks/stocks/trades/{request.Symbol.ID.Symbol}/{currentDate:yyyy-MM-dd}?" +
                              $"apiKey={_apiKey}&timestamp={offset}&limit={ResponseSizeLimitEquities}";

                    var response = DownloadAndParseData(typeof(EquityTradeTickResponse[]), url, "results") as EquityTradeTickResponse[];

                    // The first results of the next page will coincide with last of the previous page
                    // We distinguish the results by the timestamp, lets clear from repeating values
                    var tradeTicksList = response?.Where(x => x.SipTimestamp != lastTickSipTimeStamp).ToList();

                    // API will send at the end only such repeating ticks that coincide with last results of previous page
                    // If there are no other ticks other than these then we break
                    if (tradeTicksList.IsNullOrEmpty())
                    {
                        break;
                    }

                    Log.Debug($"GetEquityTradeTicks(): Page # {counter}; " +
                              $"first: {Time.UnixNanosecondTimeStampToDateTime(tradeTicksList.First().SipTimestamp)}; " +
                              $"last: {Time.UnixNanosecondTimeStampToDateTime(tradeTicksList.Last().SipTimestamp)}");

                    foreach (var row in tradeTicksList)
                    {
                        var utcTime = Time.UnixNanosecondTimeStampToDateTime(row.SipTimestamp);
                        if (utcTime < start)
                        {
                            continue;
                        }

                        if (utcTime > end)
                        {
                            yield break;
                        }

                        var time = GetTickTime(request.Symbol, utcTime);
                        yield return new Tick(time, request.Symbol, string.Empty, string.Empty, row.Size, row.Price);

                        // Save the values before to jump to the next iteration
                        lastTickSipTimeStamp = row.SipTimestamp;
                    }

                    offset = lastTickSipTimeStamp;
                    _dataPointCount += tradeTicksList.Count;
                }

                // Jump to the next iteration
                currentDate = currentDate.AddDays(1);
            }
        }

        private IEnumerable<TradeBar> GetTradeBars(HistoryRequest request)
        {
            var historyTimespan = GetHistoryTimespan(request.Resolution);

            string tickerPrefix;
            switch (request.Symbol.SecurityType)
            {
                case SecurityType.Forex:
                    tickerPrefix = "C:";
                    break;

                case SecurityType.Crypto:
                    tickerPrefix = "X:";
                    break;

                default:
                    tickerPrefix = string.Empty;
                    break;
            }

            var resolutionTimeSpan = request.Resolution.ToTimeSpan();
            var lastRequestedBarStartTime = request.EndTimeUtc.RoundDown(resolutionTimeSpan);
            var start = request.StartTimeUtc.Date;
            var end = lastRequestedBarStartTime;

            // Perform a check of the number of bars requested, this must not exceed a static limit
            var aggregatesCountPerResolution = GetAggregatesCountPerReselection(request.Resolution);
            var dataRequestedCount = (end - start).Ticks / resolutionTimeSpan.Ticks / aggregatesCountPerResolution;

            if (dataRequestedCount > ResponseSizeLimitAggregateData)
            {
                end = start + TimeSpan.FromTicks(resolutionTimeSpan.Ticks * ResponseSizeLimitAggregateData / aggregatesCountPerResolution);
                end = end.Date;
            }

            while (start < lastRequestedBarStartTime)
            {
                var url = $"{HistoryBaseUrl}/v2/aggs/ticker/{tickerPrefix}{request.Symbol.Value}/range/1/{historyTimespan}/{start.Date:yyyy-MM-dd}/{end.Date:yyyy-MM-dd}" +
                          $"?apiKey={_apiKey}&limit={ResponseSizeLimitAggregateData}";

                var aggregatesResponse = DownloadAndParseData(typeof(AggregatesResponse), url) as AggregatesResponse;
                var rows = aggregatesResponse?.Results;

                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        var utcTime = Time.UnixMillisecondTimeStampToDateTime(row.Timestamp);
                        if (utcTime < request.StartTimeUtc)
                        {
                            continue;
                        }

                        if (utcTime > request.EndTimeUtc.Add(request.Resolution.ToTimeSpan()))
                        {
                            yield break;
                        }

                        var time = GetTickTime(request.Symbol, utcTime);

                        yield return new TradeBar(time, request.Symbol, row.Open, row.High, row.Low, row.Close, row.Volume);
                    }
                }

                start = end.AddDays(1);
                end += TimeSpan.FromTicks(resolutionTimeSpan.Ticks * ResponseSizeLimitAggregateData / aggregatesCountPerResolution);

                if (end > lastRequestedBarStartTime)
                {
                    end = lastRequestedBarStartTime;
                }

                end = end.Date;
            }
        }

        private static IEnumerable<TradeBar> AggregateTradeTicks(Symbol symbol, IEnumerable<Tick> ticks, TimeSpan period)
        {
            return
                from t in ticks
                group t by t.Time.RoundDown(period)
                into g
                select new TradeBar
                {
                    Symbol = symbol,
                    Time = g.Key,
                    Open = g.First().LastPrice,
                    High = g.Max(t => t.LastPrice),
                    Low = g.Min(t => t.LastPrice),
                    Close = g.Last().LastPrice,
                    Volume = g.Sum(t => t.Quantity),
                    Period = period
                };
        }

        private static IEnumerable<QuoteBar> AggregateQuoteTicks(Symbol symbol, IEnumerable<Tick> ticks, TimeSpan period)
        {
            return
                from t in ticks
                group t by t.Time.RoundDown(period)
                into g
                select new QuoteBar
                {
                    Symbol = symbol,
                    Time = g.Key,
                    Bid = new Bar
                    {
                        Open = g.First().BidPrice,
                        High = g.Max(b => b.BidPrice),
                        Low = g.Min(b => b.BidPrice),
                        Close = g.Last().BidPrice
                    },
                    Ask = new Bar
                    {
                        Open = g.First().AskPrice,
                        High = g.Max(b => b.AskPrice),
                        Low = g.Min(b => b.AskPrice),
                        Close = g.Last().AskPrice
                    },
                    Period = period
                };
        }

        private static string GetHistoryTimespan(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Daily:
                    return "day";

                case Resolution.Hour:
                    return "hour";

                case Resolution.Minute:
                    return "minute";

                default:
                    throw new Exception($"PolygonDataQueueHandler.GetHistoryTimespan(): unsupported resolution: {resolution}.");
            }
        }

        private PolygonWebSocketClientWrapper GetWebSocket(SecurityType securityType)
        {
            PolygonWebSocketClientWrapper client;
            if (!_webSocketClientWrappers.TryGetValue(securityType, out client))
            {
                throw new InvalidOperationException($"Unsupported security type: {securityType}");
            }

            return client;
        }

        private static bool CanSubscribe(Symbol symbol)
        {
            var securityType = symbol.ID.SecurityType;

            if (symbol.Value.IndexOfInvariant("universe", true) != -1) return false;

            return
                securityType == SecurityType.Equity ||
                securityType == SecurityType.Forex ||
                securityType == SecurityType.Crypto;
        }

        private void OnMessage(string message)
        {
            foreach (var obj in JArray.Parse(message))
            {
                var eventType = obj["ev"].ToString();

                switch (eventType)
                {
                    case "T":
                        ProcessEquityTrade(obj.ToObject<EquityTradeMessage>());
                        break;

                    case "Q":
                        ProcessEquityQuote(obj.ToObject<EquityQuoteMessage>());
                        break;

                    case "C":
                        ProcessForexQuote(obj.ToObject<ForexQuoteMessage>());
                        break;

                    case "XT":
                        ProcessCryptoTrade(obj.ToObject<CryptoTradeMessage>());
                        break;

                    case "XQ":
                        ProcessCryptoQuote(obj.ToObject<CryptoQuoteMessage>());
                        break;

                    case "status":
                        var jstatus = obj["status"];
                        if (jstatus != null && jstatus.Type == JTokenType.String)
                        {
                            var status = jstatus.ToString();
                            if (status.Contains("auth_failed", StringComparison.InvariantCultureIgnoreCase))
                            {
                                var errorMessage = string.Empty;
                                var jmessage = obj["message"];
                                if (jmessage != null)
                                {
                                    errorMessage = jmessage.ToString();
                                }
                                Log.Error($"PolygonDataQueueHandler(): authentication failed: '{errorMessage}'.");
                                _failedAuthentication.Set();
                            }
                            else if (status.Contains("auth_success", StringComparison.InvariantCultureIgnoreCase))
                            {
                                Log.Trace($"PolygonDataQueueHandler(): successful authentication.");
                                _successfulAuthentication.Set();
                            }
                        }
                        break;
                }
            }
        }

        private void ShutdownWebSockets()
        {
            foreach (var websocket in _webSocketClientWrappers)
            {
                websocket.Value.Close();
            }
            _webSocketClientWrappers.Clear();
        }

        private void ProcessEquityTrade(EquityTradeMessage trade)
        {
            var symbol = _symbolMapper.GetLeanSymbol(trade.Symbol, SecurityType.Equity, Market.USA);
            var time = GetTickTime(symbol, trade.Timestamp);

            var tick = new Tick
            {
                TickType = TickType.Trade,
                Symbol = symbol,
                Time = time,
                Value = trade.Price,
                Quantity = trade.Size
            };

            _dataAggregator.Update(tick);
        }

        private void ProcessEquityQuote(EquityQuoteMessage quote)
        {
            var symbol = _symbolMapper.GetLeanSymbol(quote.Symbol, SecurityType.Equity, Market.USA);
            var time = GetTickTime(symbol, quote.Timestamp);

            var tick = new Tick
            {
                TickType = TickType.Quote,
                Symbol = symbol,
                Time = time,
                AskPrice = quote.AskPrice,
                BidPrice = quote.BidPrice,
                AskSize = quote.AskSize,
                BidSize = quote.BidSize,
                Value = (quote.AskPrice + quote.BidPrice) / 2m
            };

            _dataAggregator.Update(tick);
        }

        private void ProcessForexQuote(ForexQuoteMessage quote)
        {
            var symbol = _symbolMapper.GetLeanSymbol(quote.Symbol, SecurityType.Forex, Market.FXCM);
            var time = GetTickTime(symbol, quote.Timestamp);

            var tick = new Tick
            {
                TickType = TickType.Quote,
                Symbol = symbol,
                Time = time,
                AskPrice = quote.AskPrice,
                BidPrice = quote.BidPrice,
                Value = (quote.AskPrice + quote.BidPrice) / 2m
            };

            _dataAggregator.Update(tick);
        }

        private void ProcessCryptoTrade(CryptoTradeMessage trade)
        {
            var market = GetMarketFromCryptoExchangeId(trade.ExchangeId);
            if (string.IsNullOrWhiteSpace(market))
            {
                return;
            }

            var symbol = _symbolMapper.GetLeanSymbol(trade.Symbol, SecurityType.Crypto, market);
            var time = GetTickTime(symbol, trade.Timestamp);

            var tick = new Tick
            {
                TickType = TickType.Trade,
                Symbol = symbol,
                Time = time,
                Value = trade.Price,
                Quantity = trade.Size
            };

            _dataAggregator.Update(tick);
        }

        private void ProcessCryptoQuote(CryptoQuoteMessage quote)
        {
            var market = GetMarketFromCryptoExchangeId(quote.ExchangeId);
            if (string.IsNullOrWhiteSpace(market))
            {
                return;
            }

            var symbol = _symbolMapper.GetLeanSymbol(quote.Symbol, SecurityType.Crypto, market);
            var time = GetTickTime(symbol, quote.Timestamp);

            var tick = new Tick
            {
                TickType = TickType.Quote,
                Symbol = symbol,
                Time = time,
                AskPrice = quote.AskPrice,
                BidPrice = quote.BidPrice,
                AskSize = quote.AskSize,
                BidSize = quote.BidSize,
                Value = (quote.AskPrice + quote.BidPrice) / 2m
            };

            _dataAggregator.Update(tick);
        }

        private DateTime GetTickTime(Symbol symbol, long timestamp)
        {
            var utcTime = Time.UnixMillisecondTimeStampToDateTime(timestamp);

            return GetTickTime(symbol, utcTime);
        }

        private DateTime GetTickTime(Symbol symbol, DateTime utcTime)
        {
            if (!_symbolExchangeTimeZones.TryGetValue(symbol, out var exchangeTimeZone))
            {
                // read the exchange time zone from market-hours-database
                if (_marketHoursDatabase.TryGetEntry(symbol.ID.Market, symbol, symbol.SecurityType, out var entry))
                {
                    exchangeTimeZone = entry.ExchangeHours.TimeZone;
                }
                // If there is no entry for the given Symbol, default to New York
                else
                {
                    exchangeTimeZone = TimeZones.NewYork;
                }

                _symbolExchangeTimeZones.Add(symbol, exchangeTimeZone);
            }

            return utcTime.ConvertFromUtc(exchangeTimeZone);
        }

        private string GetMarketFromCryptoExchangeId(int exchangeId)
        {
            string market;
            return _cryptoExchangeMap.TryGetValue(exchangeId, out market) ? market : string.Empty;
        }

        private static int GetAggregatesCountPerReselection(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Minute:
                    return 1;
                case Resolution.Hour:
                    return 60;
                case Resolution.Daily:
                    return 1;
                default:
                    throw new NotSupportedException($"No data aggregation for {resolution} resolution");
            }
        }

        private static object DownloadAndParseData(Type type, string url, string jsonPropertyName = null)
        {
            var result = url.DownloadData();
            if (result == null)
            {
                return null;
            }

            // If the data download was not successful, log the reason
            var parsedResult = JObject.Parse(result);
            var success = parsedResult["success"]?.Value<bool>() ?? false;
            if (!success)
            {
                success = parsedResult["status"]?.ToString().ToUpperInvariant() == "OK";
            }

            if (!success)
            {
                Log.Debug($"No data for {url}. Reason: {result}");
                return null;
            }

            if (jsonPropertyName != null)
            {
                result = parsedResult[jsonPropertyName]?.ToString();
            }

            return result == null ? null : JsonConvert.DeserializeObject(result, type);
        }
    }
}
