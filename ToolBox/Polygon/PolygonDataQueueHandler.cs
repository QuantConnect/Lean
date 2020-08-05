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
using System.Globalization;
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
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.ToolBox.Polygon.Messages;
using QuantConnect.ToolBox.Polygon.Responses;
using HistoryRequest = QuantConnect.Data.HistoryRequest;
using static QuantConnect.StringExtensions;

namespace QuantConnect.ToolBox.Polygon
{
    /// <summary>
    /// An implementation of <see cref="IDataQueueHandler"/> and <see cref="IHistoryProvider"/> for Polygon.io
    /// </summary>
    public class PolygonDataQueueHandler : HistoryProviderBase, IDataQueueHandler
    {
        private const string HistoryBaseUrl = "https://api.polygon.io/v2";

        private readonly string _apiKey = Config.Get("polygon-api-key");

        private readonly HashSet<Symbol> _subscribedSymbols = new HashSet<Symbol>();

        private readonly List<Tick> _ticks = new List<Tick>();
        private readonly object _locker = new object();

        private readonly Dictionary<SecurityType, PolygonWebSocketClientWrapper> _webSocketClientWrappers = new Dictionary<SecurityType, PolygonWebSocketClientWrapper>();
        private readonly PolygonSymbolMapper _symbolMapper = new PolygonSymbolMapper();
        private readonly MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

        // exchange time zones by symbol
        private readonly Dictionary<Symbol, DateTimeZone> _symbolExchangeTimeZones = new Dictionary<Symbol, DateTimeZone>();

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
                foreach (var securityType in new[] { SecurityType.Equity, SecurityType.Forex, SecurityType.Crypto })
                {
                    var client = new PolygonWebSocketClientWrapper(_apiKey, _symbolMapper, securityType, OnMessage);
                    _webSocketClientWrappers.Add(securityType, client);
                }
            }
        }

        #region IDataQueueHandler implementation

        /// <summary>
        /// Indicates the connection is live.
        /// </summary>
        public bool IsConnected => _webSocketClientWrappers.Values.All(client => client.IsOpen);

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            lock (_locker)
            {
                var copy = _ticks.ToArray();
                _ticks.Clear();
                return copy;
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                if (CanSubscribe(symbol) && !_subscribedSymbols.Contains(symbol))
                {
                    var webSocket = GetWebSocket(symbol.SecurityType);
                    webSocket.Subscribe(symbol, true);

                    _subscribedSymbols.Add(symbol);
                }
            }
        }

        /// <summary>
        /// Removes the specified symbols from the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                if (CanSubscribe(symbol) && _subscribedSymbols.Contains(symbol))
                {
                    var webSocket = GetWebSocket(symbol.SecurityType);
                    webSocket.Subscribe(symbol, false);

                    _subscribedSymbols.Remove(symbol);
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
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
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Log.Error("PolygonDataQueueHandler.GetHistory(): History calls for Polygon.io require an API key.");
                yield break;
            }

            foreach (var request in requests)
            {
                foreach (var slice in ProcessHistoryRequests(request))
                {
                    yield return slice;
                }
            }
        }

        #endregion

        private IEnumerable<Slice> ProcessHistoryRequests(HistoryRequest request)
        {
            // check resolution
            if (request.Resolution != Resolution.Minute &&
                request.Resolution != Resolution.Hour &&
                request.Resolution != Resolution.Daily)
            {
                Log.Error($"PolygonDataQueueHandler.ProcessHistoryRequests(): unsupported resolution: {request.Resolution}.");
                yield break;
            }

            // check security type
            if (request.Symbol.SecurityType != SecurityType.Equity)
            {
                Log.Error($"PolygonDataQueueHandler.ProcessHistoryRequests(): unsupported security type: {request.Symbol.SecurityType}.");
                yield break;
            }

            // check tick type
            if (request.TickType != TickType.Trade)
            {
                Log.Error("PolygonDataQueueHandler.ProcessHistoryRequests(): Only history requests for trade bars are supported.");
                yield break;
            }

            var ticker = request.Symbol.ID.Symbol;
            var start = request.StartTimeUtc.ConvertFromUtc(TimeZones.NewYork);
            var end = request.EndTimeUtc.ConvertFromUtc(TimeZones.NewYork);

            Log.Trace("PolygonDataQueueHandler.ProcessHistoryRequests(): Submitting request: " +
                      Invariant($"{request.Symbol.SecurityType}-{ticker}: {request.Resolution} {start}->{end}")
            );

            var historyTimespan = GetHistoryTimespan(request.Resolution);

            var startDate = start.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var endDate = end.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // Download and parse data
            using (var client = new WebClient())
            {
                var url = $"{HistoryBaseUrl}/aggs/ticker/{ticker}/range/1/{historyTimespan}/{startDate}/{endDate}?apiKey={_apiKey}";

                var response = client.DownloadString(url);

                var result = JsonConvert.DeserializeObject<AggregatesResponse>(response);
                if (result.Results == null)
                {
                    yield break;
                }

                foreach (var row in result.Results)
                {
                    var utcTime = Time.UnixMillisecondTimeStampToDateTime(row.Timestamp);

                    if (utcTime < request.StartTimeUtc ||
                        utcTime > request.EndTimeUtc.Add(request.Resolution.ToTimeSpan()))
                    {
                        continue;
                    }

                    var time = utcTime.ConvertFromUtc(TimeZones.NewYork);

                    Interlocked.Increment(ref _dataPointCount);

                    var tradeBar = new TradeBar(time, request.Symbol, row.Open, row.High, row.Low, row.Close, row.Volume);

                    yield return new Slice(tradeBar.EndTime, new[] { tradeBar });
                }
            }
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
                throw new Exception($"Unsupported security type: {securityType}");
            }

            return client;
        }

        private bool CanSubscribe(Symbol symbol)
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
                }
            }
        }

        private void ProcessEquityTrade(EquityTradeMessage trade)
        {
            var symbol = _symbolMapper.GetLeanSymbol(trade.Symbol, SecurityType.Equity, Market.USA);
            var time = GetTickTime(symbol, trade.Timestamp);

            lock (_locker)
            {
                _ticks.Add(new Tick
                {
                    TickType = TickType.Trade,
                    Symbol = symbol,
                    Time = time,
                    Value = trade.Price,
                    Quantity = trade.Size
                });
            }
        }

        private void ProcessEquityQuote(EquityQuoteMessage quote)
        {
            var symbol = _symbolMapper.GetLeanSymbol(quote.Symbol, SecurityType.Equity, Market.USA);
            var time = GetTickTime(symbol, quote.Timestamp);

            lock (_locker)
            {
                _ticks.Add(new Tick
                {
                    TickType = TickType.Quote,
                    Symbol = symbol,
                    Time = time,
                    AskPrice = quote.AskPrice,
                    BidPrice = quote.BidPrice,
                    AskSize = quote.AskSize,
                    BidSize = quote.BidSize,
                    Value = (quote.AskPrice + quote.BidPrice) / 2m
                });
            }
        }

        private void ProcessForexQuote(ForexQuoteMessage quote)
        {
            var symbol = _symbolMapper.GetLeanSymbol(quote.Symbol, SecurityType.Forex, Market.FXCM);
            var time = GetTickTime(symbol, quote.Timestamp);

            lock (_locker)
            {
                _ticks.Add(new Tick
                {
                    TickType = TickType.Quote,
                    Symbol = symbol,
                    Time = time,
                    AskPrice = quote.AskPrice,
                    BidPrice = quote.BidPrice,
                    Value = (quote.AskPrice + quote.BidPrice) / 2m
                });
            }
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

            lock (_locker)
            {
                _ticks.Add(new Tick
                {
                    TickType = TickType.Trade,
                    Symbol = symbol,
                    Time = time,
                    Value = trade.Price,
                    Quantity = trade.Size
                });
            }
        }

        private void ProcessCryptoQuote(CryptoQuoteMessage quote)
        {
            var market = GetMarketFromCryptoExchangeId(quote.ExchangeId);
            if (string.IsNullOrWhiteSpace(market))
            {
                return;
            }

            var symbol = _symbolMapper.GetLeanSymbol(quote.Symbol, SecurityType.Crypto, market);
            var time = GetTickTime(symbol, quote.ExchangeTimestamp);

            lock (_locker)
            {
                _ticks.Add(new Tick
                {
                    TickType = TickType.Quote,
                    Symbol = symbol,
                    Time = time,
                    AskPrice = quote.AskPrice,
                    BidPrice = quote.BidPrice,
                    AskSize = quote.AskSize,
                    BidSize = quote.BidSize,
                    Value = (quote.AskPrice + quote.BidPrice) / 2m
                });
            }
        }

        private DateTime GetTickTime(Symbol symbol, long timestamp)
        {
            var utcTime = Time.UnixMillisecondTimeStampToDateTime(timestamp);

            DateTimeZone exchangeTimeZone;
            if (!_symbolExchangeTimeZones.TryGetValue(symbol, out exchangeTimeZone))
            {
                // read the exchange time zone from market-hours-database
                exchangeTimeZone = _marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;
                _symbolExchangeTimeZones.Add(symbol, exchangeTimeZone);
            }

            return utcTime.ConvertFromUtc(exchangeTimeZone);
        }

        private string GetMarketFromCryptoExchangeId(int exchangeId)
        {
            // Crypto exchanges from: https://api.polygon.io/v1/meta/crypto-exchanges?apiKey=xxx
            switch (exchangeId)
            {
                case 1:
                    return Market.GDAX;

                case 2:
                    return Market.Bitfinex;

                default:
                    return string.Empty;
            }
        }
    }
}
