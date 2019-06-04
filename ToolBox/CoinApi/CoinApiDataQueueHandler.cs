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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.ToolBox.CoinApi.Messages;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.CoinApi
{
    /// <summary>
    /// An implementation of <see cref="IDataQueueHandler"/> for CoinAPI
    /// </summary>
    public class CoinApiDataQueueHandler : IDataQueueHandler, IDisposable
    {
        private const string WebSocketUrl = "wss://ws.coinapi.io/v1/";

        private readonly string _apiKey = Config.Get("coinapi-api-key");
        private readonly WebSocketWrapper _webSocket = new WebSocketWrapper();
        private readonly object _locker = new object();
        private readonly List<Tick> _ticks = new List<Tick>();
        private readonly DefaultConnectionHandler _connectionHandler = new DefaultConnectionHandler();
        private readonly CoinApiSymbolMapper _symbolMapper = new CoinApiSymbolMapper();

        private readonly TimeSpan _subscribeDelay = TimeSpan.FromMilliseconds(250);
        private readonly object _lockerSubscriptions = new object();
        private HashSet<Symbol> _subscribedSymbols = new HashSet<Symbol>();
        private DateTime _lastSubscribeRequestUtcTime = DateTime.MinValue;
        private bool _subscriptionsPending;

        private readonly TimeSpan _minimumTimeBetweenHelloMessages = TimeSpan.FromSeconds(5);
        private DateTime _nextHelloMessageUtcTime = DateTime.MinValue;

        private List<string> _subscribedExchanges = new List<string>();

        private readonly Dictionary<Symbol, Tick> _previousQuotes = new Dictionary<Symbol, Tick>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CoinApiDataQueueHandler"/> class
        /// </summary>
        public CoinApiDataQueueHandler()
        {
            _connectionHandler.ConnectionLost += OnConnectionLost;
            _connectionHandler.ConnectionRestored += OnConnectionRestored;
            _connectionHandler.ReconnectRequested += OnReconnectRequested;

            _connectionHandler.Initialize();

            _webSocket.Initialize(WebSocketUrl);

            _webSocket.Message += OnMessage;

            _webSocket.Connect();
        }

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
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            lock (_lockerSubscriptions)
            {
                var symbolsToSubscribe = (from symbol in symbols
                                          where !_subscribedSymbols.Contains(symbol) && CanSubscribe(symbol)
                                          select symbol).ToList();
                if (symbolsToSubscribe.Count == 0)
                    return;

                Log.Trace($"CoinApiDataQueueHandler.Subscribe(): {string.Join(",", symbolsToSubscribe.Select(x => x.Value))}");

                // CoinAPI requires at least 5 seconds between subscription requests so we need to batch them
                _subscribedSymbols = symbolsToSubscribe.Concat(_subscribedSymbols).ToHashSet();

                ProcessSubscriptionRequest();
            }
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            lock (_lockerSubscriptions)
            {
                var symbolsToUnsubscribe = (from symbol in symbols
                                            where _subscribedSymbols.Contains(symbol)
                                            select symbol).ToList();
                if (symbolsToUnsubscribe.Count == 0)
                    return;

                Log.Trace($"CoinApiDataQueueHandler.Unsubscribe(): {string.Join(",", symbolsToUnsubscribe.Select(x => x.Value))}");

                // CoinAPI requires at least 5 seconds between subscription requests so we need to batch them
                _subscribedSymbols = _subscribedSymbols.Where(x => !symbolsToUnsubscribe.Contains(x)).ToHashSet();

                ProcessSubscriptionRequest();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _connectionHandler.DisposeSafely();

            if (_webSocket.IsOpen)
            {
                _webSocket.Close();
            }
        }

        /// <summary>
        /// Helper method used in QC backend
        /// </summary>
        /// <param name="markets">List of LEAN markets (exchanges) to subscribe</param>
        public void SubscribeMarkets(List<string> markets)
        {
            Log.Trace($"CoinApiDataQueueHandler.SubscribeMarkets(): {string.Join(",", markets)}");

            _subscribedExchanges = markets.ToList();

            SendHelloMessage(markets.Select(x => _symbolMapper.GetExchangeId(x)));

            _connectionHandler.EnableMonitoring(true);
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

                        symbolsToSubscribe = _subscribedSymbols.ToList();
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
                            if (_subscribedSymbols.Count == symbolsToSubscribe.Count)
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

            SendHelloMessage(_subscribedSymbols.Select(_symbolMapper.GetBrokerageSymbol));

            _connectionHandler.EnableMonitoring(true);
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

            var message = JsonConvert.SerializeObject(new HelloMessage
            {
                ApiKey = _apiKey,
                Heartbeat = true,
                SubscribeDataType = new[] { "trade", "quote" },
                SubscribeFilterSymbolId = list.ToArray()
            });

            _webSocket.Send(message);

            _nextHelloMessageUtcTime = DateTime.UtcNow.Add(_minimumTimeBetweenHelloMessages);
        }

        private void OnMessage(object sender, WebSocketMessage e)
        {
            try
            {
                var jObject = JObject.Parse(e.Message);

                var type = jObject["type"].ToString();
                switch (type)
                {
                    case "trade":
                        {
                            var trade = jObject.ToObject<TradeMessage>();

                            lock (_locker)
                            {
                                _ticks.Add(new Tick
                                {
                                    Symbol = _symbolMapper.GetLeanSymbol(trade.SymbolId, SecurityType.Crypto, string.Empty),
                                    Time = trade.TimeExchange,
                                    Value = trade.Price,
                                    Quantity = trade.Size,
                                    TickType = TickType.Trade
                                });
                            }

                            _connectionHandler.KeepAlive(trade.TimeExchange);
                            break;
                        }

                    case "quote":
                        {
                            var quote = jObject.ToObject<QuoteMessage>();

                            lock (_locker)
                            {
                                var tick = new Tick
                                {
                                    Symbol = _symbolMapper.GetLeanSymbol(quote.SymbolId, SecurityType.Crypto, string.Empty),
                                    Time = quote.TimeExchange,
                                    AskPrice = quote.AskPrice,
                                    AskSize = quote.AskSize,
                                    BidPrice = quote.BidPrice,
                                    BidSize = quote.BidSize,
                                    TickType = TickType.Quote
                                };

                                // only emit quote ticks if bid price or ask price changed
                                Tick previousQuote;
                                if (!_previousQuotes.TryGetValue(tick.Symbol, out previousQuote) ||
                                    tick.AskPrice != previousQuote.AskPrice ||
                                    tick.BidPrice != previousQuote.BidPrice)
                                {
                                    _previousQuotes[tick.Symbol] = tick;
                                    _ticks.Add(tick);
                                }
                            }

                            _connectionHandler.KeepAlive(quote.TimeExchange);
                            break;
                        }

                    // not a typo :)
                    case "hearbeat":
                    // just in case the typo will be fixed in the future
                    case "heartbeat":
                        _connectionHandler.KeepAlive(DateTime.UtcNow);
                        break;

                    case "error":
                        {
                            var error = jObject.ToObject<ErrorMessage>();
                            Log.Error(error.Message);
                            break;
                        }

                    default:
                        Log.Trace(e.Message);
                        break;
                }
            }
            catch (Exception exception)
            {
                Log.Error($"Error processing message: {e.Message} - Error: {exception}");
            }
        }

        private void OnConnectionLost(object sender, EventArgs e)
        {
            Log.Error("CoinApiDataQueueHandler.OnConnectionLost(): CoinAPI connection lost.");
        }

        private void OnConnectionRestored(object sender, EventArgs e)
        {
            Log.Trace("CoinApiDataQueueHandler.OnConnectionRestored(): CoinAPI connection restored.");
        }

        private void OnReconnectRequested(object sender, EventArgs e)
        {
            if (!_webSocket.IsOpen)
            {
                _webSocket.Connect();
            }

            if (!_webSocket.IsOpen)
            {
                return;
            }

            if (_subscribedExchanges.Count > 0)
            {
                SubscribeMarkets(_subscribedExchanges);
            }
            else if (_subscribedSymbols.Count > 0)
            {
                SubscribeSymbols(_subscribedSymbols.ToList());
            }
        }
    }
}
