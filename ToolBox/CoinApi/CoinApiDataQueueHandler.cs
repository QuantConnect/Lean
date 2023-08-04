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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoinAPI.WebSocket.V1;
using CoinAPI.WebSocket.V1.DataModels;
using Newtonsoft.Json;
using NodaTime;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.ToolBox.CoinApi.Messages;
using QuantConnect.Util;
using RestSharp;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.ToolBox.CoinApi
{
    /// <summary>
    /// An implementation of <see cref="IDataQueueHandler"/> for CoinAPI
    /// </summary>
    public class CoinApiDataQueueHandler : SynchronizingHistoryProvider, IDataQueueHandler
    {
        protected int HistoricalDataPerRequestLimit = 10000;
        private static readonly Dictionary<Resolution, string> _ResolutionToCoinApiPeriodMappings = new Dictionary<Resolution, string>
        {
            { Resolution.Second, "1SEC"},
            { Resolution.Minute, "1MIN" },
            { Resolution.Hour, "1HRS" },
            { Resolution.Daily, "1DAY" },
        };

        private readonly string _apiKey = Config.Get("coinapi-api-key");
        private readonly string[] _streamingDataType;
        private readonly CoinApiWsClient _client;
        private readonly object _locker = new object();
        private ConcurrentDictionary<string, Symbol> _symbolCache = new ConcurrentDictionary<string, Symbol>();
        private readonly CoinApiSymbolMapper _symbolMapper = new CoinApiSymbolMapper();
        private readonly IDataAggregator _dataAggregator;
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;

        private readonly TimeSpan _subscribeDelay = TimeSpan.FromMilliseconds(250);
        private readonly object _lockerSubscriptions = new object();
        private DateTime _lastSubscribeRequestUtcTime = DateTime.MinValue;
        private bool _subscriptionsPending;

        private readonly TimeSpan _minimumTimeBetweenHelloMessages = TimeSpan.FromSeconds(5);
        private DateTime _nextHelloMessageUtcTime = DateTime.MinValue;

        private readonly ConcurrentDictionary<string, Tick> _previousQuotes = new ConcurrentDictionary<string, Tick>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CoinApiDataQueueHandler"/> class
        /// </summary>
        public CoinApiDataQueueHandler()
        {
            _dataAggregator = Composer.Instance.GetPart<IDataAggregator>();
            if (_dataAggregator == null)
            {
                _dataAggregator =
                    Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"));
            }
            var product = Config.GetValue<CoinApiProduct>("coinapi-product");
            _streamingDataType = product < CoinApiProduct.Streamer
                ? new[] { "trade" }
                : new[] { "trade", "quote" };

            Log.Trace($"CoinApiDataQueueHandler(): using plan '{product}'. Available data types: '{string.Join(",", _streamingDataType)}'");

            _client = new CoinApiWsClient();
            _client.TradeEvent += OnTrade;
            _client.QuoteEvent += OnQuote;
            _client.Error += OnError;
            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            _subscriptionManager.SubscribeImpl += (s, t) => Subscribe(s);
            _subscriptionManager.UnsubscribeImpl += (s, t) => Unsubscribe(s);
        }

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
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        private bool Subscribe(IEnumerable<Symbol> symbols)
        {
            ProcessSubscriptionRequest();
            return true;
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
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        private bool Unsubscribe(IEnumerable<Symbol> symbols)
        {
            ProcessSubscriptionRequest();
            return true;
        }

        /// <summary>
        /// Returns whether the data provider is connected
        /// </summary>
        /// <returns>true if the data provider is connected</returns>
        public bool IsConnected => true;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _client.TradeEvent -= OnTrade;
            _client.QuoteEvent -= OnQuote;
            _client.Error -= OnError;
            _client.Dispose();
            _dataAggregator.DisposeSafely();
        }

        /// <summary>
        /// Helper method used in QC backend
        /// </summary>
        /// <param name="markets">List of LEAN markets (exchanges) to subscribe</param>
        public void SubscribeMarkets(List<string> markets)
        {
            Log.Trace($"CoinApiDataQueueHandler.SubscribeMarkets(): {string.Join(",", markets)}");

            // we add '_' to be more precise, for example requesting 'BINANCE' doesn't match 'BINANCEUS'
            SendHelloMessage(markets.Select(x => string.Concat(_symbolMapper.GetExchangeId(x.ToLowerInvariant()), "_")));
        }

        private void ProcessSubscriptionRequest()
        {
            if (_subscriptionsPending) return;

            _lastSubscribeRequestUtcTime = DateTime.UtcNow;
            _subscriptionsPending = true;

            Task.Run(async () =>
            {
                while (true)
                {
                    DateTime requestTime;
                    List<Symbol> symbolsToSubscribe;
                    lock (_lockerSubscriptions)
                    {
                        requestTime = _lastSubscribeRequestUtcTime.Add(_subscribeDelay);

                        // CoinAPI requires at least 5 seconds between hello messages
                        if (_nextHelloMessageUtcTime != DateTime.MinValue && requestTime < _nextHelloMessageUtcTime)
                        {
                            requestTime = _nextHelloMessageUtcTime;
                        }

                        symbolsToSubscribe = _subscriptionManager.GetSubscribedSymbols().ToList();
                    }

                    var timeToWait = requestTime - DateTime.UtcNow;

                    int delayMilliseconds;
                    if (timeToWait <= TimeSpan.Zero)
                    {
                        // minimum delay has passed since last subscribe request, send the Hello message
                        SubscribeSymbols(symbolsToSubscribe);

                        lock (_lockerSubscriptions)
                        {
                            _lastSubscribeRequestUtcTime = DateTime.UtcNow;
                            if (_subscriptionManager.GetSubscribedSymbols().Count() == symbolsToSubscribe.Count)
                            {
                                // no more subscriptions pending, task finished
                                _subscriptionsPending = false;
                                break;
                            }
                        }

                        delayMilliseconds = _subscribeDelay.Milliseconds;
                    }
                    else
                    {
                        delayMilliseconds = timeToWait.Milliseconds;
                    }

                    await Task.Delay(delayMilliseconds).ConfigureAwait(false);
                }
            });
        }

        /// <summary>
        /// Returns true if we can subscribe to the specified symbol
        /// </summary>
        private static bool CanSubscribe(Symbol symbol)
        {
            // ignore unsupported security types
            if (symbol.ID.SecurityType != SecurityType.Crypto && symbol.ID.SecurityType != SecurityType.CryptoFuture)
            {
                return false;
            }

            // ignore universe symbols
            return !symbol.Value.Contains("-UNIVERSE-");
        }

        /// <summary>
        /// Subscribes to a list of symbols
        /// </summary>
        /// <param name="symbolsToSubscribe">The list of symbols to subscribe</param>
        private void SubscribeSymbols(List<Symbol> symbolsToSubscribe)
        {
            Log.Trace($"CoinApiDataQueueHandler.SubscribeSymbols(): {string.Join(",", symbolsToSubscribe)}");

            // subscribe to symbols using exact match
            SendHelloMessage(symbolsToSubscribe.Select(x => {
                try
                {
                    var result = string.Concat(_symbolMapper.GetBrokerageSymbol(x), "$");
                    return result;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    return null;
                }
                }).Where(x => x != null));
        }

        private void SendHelloMessage(IEnumerable<string> subscribeFilter)
        {
            var list = subscribeFilter.ToList();
            if (list.Count == 0)
            {
                // If we use a null or empty filter in the CoinAPI hello message
                // we will be subscribing to all symbols for all active exchanges!
                // Only option is requesting an invalid symbol as filter.
                list.Add("$no_symbol_requested$");
            }

            _client.SendHelloMessage(new Hello
            {
                apikey = Guid.Parse(_apiKey),
                heartbeat = true,
                subscribe_data_type = _streamingDataType,
                subscribe_filter_symbol_id = list.ToArray()
            });

            _nextHelloMessageUtcTime = DateTime.UtcNow.Add(_minimumTimeBetweenHelloMessages);
        }

        private void OnTrade(object sender, Trade trade)
        {
            try
            {
                var symbol = GetSymbolUsingCache(trade.symbol_id);
                if(symbol == null)
                {
                    return;
                }

                var tick = new Tick(trade.time_exchange, symbol, string.Empty, string.Empty, quantity: trade.size, price: trade.price);

                lock (symbol)
                {
                    _dataAggregator.Update(tick);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void OnQuote(object sender, Quote quote)
        {
            try
            {
                // only emit quote ticks if bid price or ask price changed
                Tick previousQuote;
                if (!_previousQuotes.TryGetValue(quote.symbol_id, out previousQuote)
                    || quote.ask_price != previousQuote.AskPrice
                    || quote.bid_price != previousQuote.BidPrice)
                {
                    var symbol = GetSymbolUsingCache(quote.symbol_id);
                    if (symbol == null)
                    {
                        return;
                    }

                    var tick = new Tick(quote.time_exchange, symbol, string.Empty, string.Empty,
                        bidSize: quote.bid_size, bidPrice: quote.bid_price,
                        askSize: quote.ask_size, askPrice: quote.ask_price);

                    _previousQuotes[quote.symbol_id] = tick;
                    lock (symbol)
                    {
                        _dataAggregator.Update(tick);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private Symbol GetSymbolUsingCache(string ticker)
        {
            if(!_symbolCache.TryGetValue(ticker, out Symbol result))
            {
                try
                {
                    var securityType = ticker.IndexOf("_PERP_") > 0 ? SecurityType.CryptoFuture : SecurityType.Crypto;
                    result = _symbolMapper.GetLeanSymbol(ticker, securityType, string.Empty);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    // we store the null so we don't keep going into the same mapping error
                    result = null;
                }
                _symbolCache[ticker] = result;
            }
            return result;
        }

        private void OnError(object sender, Exception e)
        {
            Log.Error(e);
        }

        #region SynchronizingHistoryProvider

        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
            // NOP
        }

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

        public IEnumerable<BaseData> GetHistory(HistoryRequest historyRequest)
        {
            if (historyRequest.Symbol.SecurityType != SecurityType.Crypto && historyRequest.Symbol.SecurityType != SecurityType.CryptoFuture)
            {
                Log.Error($"CoinApiDataQueueHandler.GetHistory(): Invalid security type {historyRequest.Symbol.SecurityType}");
                yield break;
            }

            if (historyRequest.Resolution == Resolution.Tick)
            {
                Log.Error("CoinApiDataQueueHandler.GetHistory(): No historical ticks, only OHLCV timeseries");
                yield break;
            }

            if (historyRequest.DataType == typeof(QuoteBar))
            {
                Log.Error("CoinApiDataQueueHandler.GetHistory(): No historical QuoteBars , only TradeBars");
                yield break;
            }

            var resolutionTimeSpan = historyRequest.Resolution.ToTimeSpan();
            var lastRequestedBarStartTime = historyRequest.EndTimeUtc.RoundDown(resolutionTimeSpan);
            var currentStartTime = historyRequest.StartTimeUtc.RoundUp(resolutionTimeSpan);
            var currentEndTime = lastRequestedBarStartTime;

            // Perform a check of the number of bars requested, this must not exceed a static limit
            var dataRequestedCount = (currentEndTime - currentStartTime).Ticks 
                                     / resolutionTimeSpan.Ticks;

            if (dataRequestedCount > HistoricalDataPerRequestLimit)
            {
                currentEndTime = currentStartTime 
                                 + TimeSpan.FromTicks(resolutionTimeSpan.Ticks * HistoricalDataPerRequestLimit);
            }

            while (currentStartTime < lastRequestedBarStartTime)
            {
                var coinApiSymbol = _symbolMapper.GetBrokerageSymbol(historyRequest.Symbol);
                var coinApiPeriod = _ResolutionToCoinApiPeriodMappings[historyRequest.Resolution];

                // Time must be in ISO 8601 format
                var coinApiStartTime = currentStartTime.ToStringInvariant("s");
                var coinApiEndTime = currentEndTime.ToStringInvariant("s");

                // Construct URL for rest request
                var baseUrl =
                    "https://rest.coinapi.io/v1/ohlcv/" +
                    $"{coinApiSymbol}/history?period_id={coinApiPeriod}&limit={HistoricalDataPerRequestLimit}" +
                    $"&time_start={coinApiStartTime}&time_end={coinApiEndTime}";

                // Execute
                var client = new RestClient(baseUrl);
                var restRequest = new RestRequest(Method.GET);
                restRequest.AddHeader("X-CoinAPI-Key", _apiKey);
                var response = client.Execute(restRequest);

                // Log the information associated with the API Key's rest call limits.
                TraceRestUsage(response);

                // Deserialize to array
                var coinApiHistoryBars = JsonConvert.DeserializeObject<HistoricalDataMessage[]>(response.Content);

                // Can be no historical data for a short period interval
                if (!coinApiHistoryBars.Any())
                {
                    Log.Error($"CoinApiDataQueueHandler.GetHistory(): API returned no data for the requested period [{coinApiStartTime} - {coinApiEndTime}] for symbol [{historyRequest.Symbol}]");
                    continue;
                }

                foreach (var ohlcv in coinApiHistoryBars)
                {
                    yield return
                        new TradeBar(ohlcv.TimePeriodStart, historyRequest.Symbol, ohlcv.PriceOpen, ohlcv.PriceHigh,
                            ohlcv.PriceLow, ohlcv.PriceClose, ohlcv.VolumeTraded, historyRequest.Resolution.ToTimeSpan());
                }

                currentStartTime = currentEndTime;
                currentEndTime += TimeSpan.FromTicks(resolutionTimeSpan.Ticks * HistoricalDataPerRequestLimit);
            } 
        }

        #endregion

        private void TraceRestUsage(IRestResponse response)
        {
            var total = GetHttpHeaderValue(response, "x-ratelimit-limit");
            var used = GetHttpHeaderValue(response, "x-ratelimit-used");
            var remaining = GetHttpHeaderValue(response, "x-ratelimit-remaining");

            Log.Trace($"CoinApiDataQueueHandler.TraceRestUsage(): Used {used}, Remaining {remaining}, Total {total}");
        }

        private static string GetHttpHeaderValue(IRestResponse response, string propertyName)
        {
            return response.Headers
                .FirstOrDefault(x => x.Name == propertyName)?
                .Value.ToString();
        }

        // WARNING: here to be called from tests to reduce explicitly the amount of request's output 
        protected void SetUpHistDataLimit(int limit)
        {
            HistoricalDataPerRequestLimit = limit;
        }
    }
}
