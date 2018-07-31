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

using Newtonsoft.Json;
using QuantConnect.Orders;
using QuantConnect.Securities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using System.Collections.Concurrent;
using QuantConnect.Util;
using Newtonsoft.Json.Linq;
using com.sun.corba.se.impl.protocol.giopmsgheaders;
using System.Globalization;
using QuantConnect.Data.Market;

namespace QuantConnect.Brokerages.Bitfinex
{
    public partial class BitfinexBrokerage
    {
        private const string ApiVersion = "v1";
        private readonly IAlgorithm _algorithm;
        private readonly ConcurrentQueue<WebSocketMessage> _messageBuffer = new ConcurrentQueue<WebSocketMessage>();
        private readonly object channelLocker = new object();
        private volatile bool _streamLocked;
        internal enum BitfinexEndpointType { Public, Private }
        private readonly RateGate _restRateLimiter = new RateGate(8, TimeSpan.FromMinutes(1));
        private readonly ConcurrentDictionary<Symbol, OrderBook> _orderBooks = new ConcurrentDictionary<Symbol, OrderBook>();
        /// <summary>
        /// Rest client used to call missing conversion rates
        /// </summary>
        public IRestClient RateClient { get; set; }

        /// <summary>
        /// Locking object for the Ticks list in the data queue handler
        /// </summary>
        protected readonly object TickLocker = new object();

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="wssUrl">websockets url</param>
        /// <param name="websocket">instance of websockets client</param>
        /// <param name="restClient">instance of rest client</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="algorithm">the algorithm instance is required to retreive account type</param>
        public BitfinexBrokerage(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, IAlgorithm algorithm)
            : base(wssUrl, websocket, restClient, apiKey, apiSecret, Market.Bitfinex, "Bitfinex")
        {
            _algorithm = algorithm;
            RateClient = new RestClient("http://data.fixer.io/api/latest?base=usd&access_key=26a2eb9f13db3f14b6df6ec2379f9261");

            WebSocket.Open += (sender, args) =>
            {
                //var tickers = new[]
                //{
                //    "LTCUSD", "LTCEUR", "LTCBTC",
                //    "BTCUSD", "BTCEUR", "BTCGBP",
                //    "ETHBTC", "ETHUSD", "ETHEUR",
                //    "BCHBTC", "BCHUSD", "BCHEUR"
                //};
                //Subscribe(tickers.Select(ticker => Symbol.Create(ticker, SecurityType.Crypto, Market.Bitfinex)));
            };
        }


        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void OnMessage(object sender, WebSocketMessage e)
        {
            // Verify if we're allowed to handle the streaming packet yet; while we're placing an order we delay the
            // stream processing a touch.
            try
            {
                if (_streamLocked)
                {
                    _messageBuffer.Enqueue(e);
                    return;
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            OnMessageImpl(sender, e);
        }

        public override void Subscribe(IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                if (symbol.Value.Contains("UNIVERSE") ||
                    symbol.SecurityType != SecurityType.Forex && symbol.SecurityType != SecurityType.Crypto)
                {
                    continue;
                }

                WebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "subscribe",
                    channel = "book",
                    pair = symbol.Value
                }));
            }

            Log.Trace("BitfinexBrokerage.Subscribe: Sent subscribe.");
        }

        /// <summary>
        /// Ends current subscriptions
        /// </summary>
        public void Unsubscribe(IEnumerable<Symbol> symbols)
        {
            if (WebSocket.IsOpen)
            {
                var map = ChannelList.ToDictionary(k => k.Value.Symbol.ToUpper(), k => k.Key, StringComparer.InvariantCultureIgnoreCase);
                foreach (var symbol in symbols)
                {
                    if (map.ContainsKey(symbol.Value))
                    {
                        WebSocket.Send(JsonConvert.SerializeObject(new
                        {
                            @event = "unsubscribe",
                            channelId = map[symbol.Value]
                        }));
                    }
                }
            }
        }


        /// <summary>
        /// Implementation of the OnMessage event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageImpl(object sender, WebSocketMessage e)
        {
            try
            {
                var token = JToken.Parse(e.Message);

                if (token is JArray)
                {
                    if (token[1].Type != JTokenType.String)
                    {
                        if (token.Count() == 2)
                        {
                            OnSnapshot(
                                token[0].ToObject<string>(),
                                token[1].ToObject<string[][]>()
                            );
                        }
                        else
                        {
                            OnUpdate(
                                token[0].ToObject<string>(),
                                token.ToObject<string[]>().Skip(1).ToArray()
                            );
                        }
                    }
                }
                else if (token is JObject)
                {
                    Messages.BaseMessage raw = token.ToObject<Messages.BaseMessage>();
                    switch (raw.Event.ToLower())
                    {
                        case "subscribed":
                            OnSubscribe(token.ToObject<Messages.OrderBookSubscription>());
                            return;
                        case "unsubscribed":
                            OnUnsubscribe(token.ToObject<Messages.OrderBookUnsubscribing>());
                            return;
                        case "info":
                        case "ping":
                            return;
                        case "error":
                            Log.Trace($"BitfinexWebsocketsBrokerage.OnMessage: Error: {e.Message}");
                            return;
                        default:
                            Log.Trace($"BitfinexWebsocketsBrokerage.OnMessage: Unexpected message format: {e.Message}");
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }

        private void OnSubscribe(Messages.OrderBookSubscription data)
        {
            try
            {
                Channel existing = null;
                lock (channelLocker)
                {
                    if (!ChannelList.TryGetValue(data.ChannelId, out existing))
                    {
                        ChannelList[data.ChannelId] = new Channel() { Name = data.ChannelId, Symbol = data.Symbol }; ;
                    }
                    else
                    {
                        existing.Name = data.ChannelId;
                        existing.Symbol = data.Symbol;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnUnsubscribe(Messages.OrderBookUnsubscribing data)
        {
            try
            {
                lock (channelLocker)
                {
                    ChannelList.Remove(data.ChannelId);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnSnapshot(string channelId, string[][] entries)
        {
            try
            {
                Channel channel = ChannelList[channelId];
                var symbol = Symbol.Create(channel.Symbol, SecurityType.Crypto, Market.Bitfinex);

                OrderBook orderBook;
                if (!_orderBooks.TryGetValue(symbol, out orderBook))
                {
                    orderBook = new OrderBook(symbol);
                    _orderBooks[symbol] = orderBook;
                }
                else
                {
                    orderBook.BestBidAskUpdated -= OnBestBidAskUpdated;
                    orderBook.Clear();
                }

                foreach (var entry in entries)
                {
                    var price = decimal.Parse(entry[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                    var amount = decimal.Parse(entry[2], NumberStyles.Float, CultureInfo.InvariantCulture);

                    if (amount > 0)
                        orderBook.UpdateBidRow(price, amount);
                    else
                        orderBook.UpdateAskRow(price, amount);
                }

                orderBook.BestBidAskUpdated += OnBestBidAskUpdated;

                EmitQuoteTick(symbol, orderBook.BestBidPrice, orderBook.BestBidSize, orderBook.BestAskPrice, orderBook.BestAskSize);
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnUpdate(string channelId, string[] entries)
        {
            try
            {
                Channel channel = ChannelList[channelId];
                var symbol = Symbol.Create(channel.Symbol, SecurityType.Crypto, Market.Bitfinex);
                var orderBook = _orderBooks[symbol];

                var price = decimal.Parse(entries[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                var count = int.Parse(entries[1]);
                var amount = decimal.Parse(entries[2], NumberStyles.Float, CultureInfo.InvariantCulture);

                if (count == 0)
                {
                    orderBook.RemovePriceLevel(price);
                }
                else
                {
                    if (amount > 0)
                    {
                        orderBook.UpdateBidRow(price, amount);
                    }
                    else if (amount < 0)
                    {
                        orderBook.UpdateAskRow(price, amount);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnBestBidAskUpdated(object sender, BestBidAskUpdatedEventArgs e)
        {
            EmitQuoteTick(e.Symbol, e.BestBidPrice, e.BestBidSize, e.BestAskPrice, e.BestAskSize);
        }

        private void EmitQuoteTick(Symbol symbol, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            lock (TickLocker)
            {
                Ticks.Add(new Tick
                {
                    AskPrice = askPrice,
                    BidPrice = bidPrice,
                    Value = (askPrice + bidPrice) / 2m,
                    Time = DateTime.UtcNow,
                    Symbol = symbol,
                    TickType = TickType.Quote,
                    AskSize = askSize,
                    BidSize = bidSize
                });
            }
        }
    }
}
