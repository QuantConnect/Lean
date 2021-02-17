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
using System.Threading.Tasks;
using CoinAPI.WebSocket.V1;
using CoinAPI.WebSocket.V1.DataModels;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.CoinApi
{
    /// <summary>
    /// An implementation of <see cref="IDataQueueHandler"/> for CoinAPI
    /// </summary>
    public class CoinApiDataQueueHandler : IDataQueueHandler
    {
        private readonly string _apiKey = Config.Get("coinapi-api-key");
        private readonly string[] _streamingDataType;
        private readonly CoinApiWsClient _client;
        private readonly object _locker = new object();
        private readonly CoinApiSymbolMapper _symbolMapper = new CoinApiSymbolMapper();
        private readonly IDataAggregator _dataAggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(
            Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"));
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;

        private readonly TimeSpan _subscribeDelay = TimeSpan.FromMilliseconds(250);
        private readonly object _lockerSubscriptions = new object();
        private DateTime _lastSubscribeRequestUtcTime = DateTime.MinValue;
        private bool _subscriptionsPending;

        private readonly TimeSpan _minimumTimeBetweenHelloMessages = TimeSpan.FromSeconds(5);
        private DateTime _nextHelloMessageUtcTime = DateTime.MinValue;

        private readonly Dictionary<Symbol, Tick> _previousQuotes = new Dictionary<Symbol, Tick>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CoinApiDataQueueHandler"/> class
        /// </summary>
        public CoinApiDataQueueHandler()
        {
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
                return Enumerable.Empty<BaseData>().GetEnumerator();
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

            SendHelloMessage(markets.Select(x => _symbolMapper.GetExchangeId(x)));
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
            if (symbol.ID.SecurityType != SecurityType.Crypto)
                return false;

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

            SendHelloMessage(symbolsToSubscribe.Select(_symbolMapper.GetBrokerageSymbol));
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
                var tick = new Tick
                {
                    Symbol = _symbolMapper.GetLeanSymbol(trade.symbol_id, SecurityType.Crypto, string.Empty),
                    Time = trade.time_exchange,
                    Value = trade.price,
                    Quantity = trade.size,
                    TickType = TickType.Trade
                };

                lock (_locker)
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
                var tick = new Tick
                {
                    Symbol = _symbolMapper.GetLeanSymbol(quote.symbol_id, SecurityType.Crypto, string.Empty),
                    Time = quote.time_exchange,
                    AskPrice = quote.ask_price,
                    AskSize = quote.ask_size,
                    BidPrice = quote.bid_price,
                    BidSize = quote.bid_size,
                    TickType = TickType.Quote
                };

                lock (_locker)
                {
                    // only emit quote ticks if bid price or ask price changed
                    Tick previousQuote;
                    if (!_previousQuotes.TryGetValue(tick.Symbol, out previousQuote) ||
                        tick.AskPrice != previousQuote.AskPrice ||
                        tick.BidPrice != previousQuote.BidPrice)
                    {
                        _previousQuotes[tick.Symbol] = tick;
                        _dataAggregator.Update(tick);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void OnError(object sender, Exception e)
        {
            Log.Error(e);
        }
    }
}
