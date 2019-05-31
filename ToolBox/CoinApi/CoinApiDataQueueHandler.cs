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
using System.Threading;
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

        private List<string> _subscribedExchanges = new List<string>();

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
                _subscribedSymbols = symbolsToSubscribe.Union(_subscribedSymbols).ToHashSet();

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
            if (markets.Count == 0) return;

            Log.Trace($"CoinApiDataQueueHandler.SubscribeMarkets(): {string.Join(",", markets)}");

            _subscribedExchanges = markets.ToList();

            var message = JsonConvert.SerializeObject(new HelloMessage
            {
                ApiKey = _apiKey,
                Heartbeat = true,
                SubscribeDataType = new[] { "trade", "quote" },
                SubscribeFilterSymbolId = markets.Select(x => _symbolMapper.GetExchangeId(x)).ToArray()
            });

            _webSocket.Send(message);

            _connectionHandler.EnableMonitoring(true);
        }

        private void ProcessSubscriptionRequest()
        {
            if (_subscriptionsPending) return;

            _lastSubscribeRequestUtcTime = DateTime.UtcNow;
            _subscriptionsPending = true;

            Task.Run(() =>
            {
                while (true)
                {
                    DateTime requestTime;
                    List<Symbol> symbolsToSubscribe;
                    lock (_lockerSubscriptions)
                    {
                        requestTime = _lastSubscribeRequestUtcTime.Add(_subscribeDelay);
                        symbolsToSubscribe = _subscribedSymbols.ToList();
                    }

                    if (DateTime.UtcNow > requestTime)
                    {
                        // minimum delay has passed since last subscribe request, send the subscribe request
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
                    }

                    Thread.Sleep(200);
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
            if (symbolsToSubscribe.Count == 0) return;

            Log.Trace($"CoinApiDataQueueHandler.SubscribeSymbols(): {string.Join(",", symbolsToSubscribe)}");

            var message = JsonConvert.SerializeObject(new HelloMessage
            {
                ApiKey = _apiKey,
                Heartbeat = true,
                SubscribeDataType = new[] { "trade", "quote" },
                SubscribeFilterSymbolId = _subscribedSymbols.Select(_symbolMapper.GetBrokerageSymbol).ToArray()
            });

            _webSocket.Send(message);

            _connectionHandler.EnableMonitoring(true);
        }

        private void OnMessage(object sender, WebSocketMessage e)
        {
            //Log.Trace(e.Message);

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

                            _connectionHandler.KeepAlive();
                            break;
                        }

                    case "quote":
                        {
                            var quote = jObject.ToObject<QuoteMessage>();

                            lock (_locker)
                            {
                                _ticks.Add(new Tick
                                {
                                    Symbol = _symbolMapper.GetLeanSymbol(quote.SymbolId, SecurityType.Crypto, string.Empty),
                                    Time = quote.TimeExchange,
                                    AskPrice = quote.AskPrice,
                                    AskSize = quote.AskSize,
                                    BidPrice = quote.BidPrice,
                                    BidSize = quote.BidSize,
                                    TickType = TickType.Quote
                                });
                            }

                            _connectionHandler.KeepAlive();
                            break;
                        }

                    // not a typo :)
                    case "hearbeat":
                        _connectionHandler.KeepAlive();
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
