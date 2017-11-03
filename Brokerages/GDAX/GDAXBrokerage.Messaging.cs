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
using Newtonsoft.Json.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuantConnect.Packets;
using System.Threading;
using RestSharp;
using System.Text.RegularExpressions;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.GDAX
{
    public partial class GDAXBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
    {

        #region Declarations
        private object _tickLocker = new object();
        /// <summary>
        /// Collection of partial split messages
        /// </summary>
        public ConcurrentDictionary<long, GDAXFill> FillSplit { get; set; }
        private string _passPhrase;
        private const string _symbolMatching = "ETH|LTC|BTC";
        private IAlgorithm _algorithm;
        private static string[] _channelNames = new string[] { "heartbeat", "ticker", "user", "matches" };
        private CancellationTokenSource _canceller = new CancellationTokenSource();
        private ConcurrentQueue<WebSocketMessage> _messageBuffer = new ConcurrentQueue<WebSocketMessage>();
        private volatile bool _streamLocked;

        /// <summary>
        /// Rest client used to call missing conversion rates
        /// </summary>
        public IRestClient RateClient { get; set; }
        #endregion

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="wssUrl">websockets url</param>
        /// <param name="websocket">instance of websockets client</param>
        /// <param name="restClient">instance of rest client</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="passPhrase">pass phrase</param>
        /// <param name="algorithm">the algorithm instance is required to retreive account type</param>
        public GDAXBrokerage(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, string passPhrase, IAlgorithm algorithm)
            : base(wssUrl, websocket, restClient, apiKey, apiSecret, Market.GDAX, "GDAX")
        {
            FillSplit = new ConcurrentDictionary<long, GDAXFill>();
            _passPhrase = passPhrase;
            _algorithm = algorithm;
            RateClient = new RestClient("http://api.fixer.io/latest?base=usd");

            WebSocket.Open += (sender, args) =>
            {
                var tickers = new[] {"LTCUSD", "LTCEUR", "LTCBTC", "BTCUSD", "BTCEUR", "BTCGBP", "ETHBTC", "ETHUSD", "ETHEUR"};
                Subscribe(null, tickers.Select(ticker => Symbol.Create(ticker, SecurityType.Crypto, Market.GDAX)));
            };
        }

        /// <summary>
        /// Lock the streaming processing while we're sending orders as sometimes they fill before the REST call returns.
        /// </summary>
        public void LockStream()
        {
            Log.Trace("GDAXBrokerage.Messaging.LockStream(): Locking Stream");
            _streamLocked = true;
        }

        /// <summary>
        /// Unlock stream and process all backed up messages.
        /// </summary>
        public void UnlockStream()
        {
            Log.Trace("GDAXBrokerage.Messaging.UnlockStream(): Processing Backlog...");
            while (_messageBuffer.Any())
            {
                WebSocketMessage e;
                _messageBuffer.TryDequeue(out e);
                OnMessageImpl(this, e);
            }
            Log.Trace("GDAXBrokerage.Messaging.UnlockStream(): Stream Unlocked.");
            // Once dequeued in order; unlock stream.
            _streamLocked = false;
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

        /// <summary>
        /// Implementation of the OnMessage event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageImpl(object sender, WebSocketMessage e)
        {
            try
            {
                var raw = JsonConvert.DeserializeObject<Messages.BaseMessage>(e.Message, JsonSettings);

                LastHeartbeatUtcTime = DateTime.UtcNow;

                if (raw.Type == "heartbeat")
                {
                    return;
                }
                else if (raw.Type == "ticker")
                {
                    EmitTick(e.Message);
                    return;
                }
                else if (raw.Type == "error")
                {
                    Log.Error($"GDAXBrokerage.OnMessage.error(): Data: {Environment.NewLine}{e.Message}");
                    var error = JsonConvert.DeserializeObject<Messages.Error>(e.Message, JsonSettings);
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"GDAXBrokerage.OnMessage: {error.Message} {error.Reason}"));
                    return;
                }
                else if (raw.Type == "done")
                {
                    OrderDone(e.Message);
                    return;
                }
                else if (raw.Type == "match")
                {
                    OrderMatch(e.Message);
                    return;
                }
                else if (raw.Type == "open" || raw.Type == "change" || raw.Type == "received" || raw.Type == "subscriptions" || raw.Type == "last_match")
                {
                    //known messages we don't need to handle or log
                    return;
                }

                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, -1, ("GDAXWebsocketsBrokerage.OnMessage: Unexpected message format: " + e.Message)));
            }
            catch (Exception ex)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {ex.ToString()}"));
                throw;
            }
        }

        private void OrderMatch(string data)
        {
            var message = JsonConvert.DeserializeObject<Messages.Matched>(data, JsonSettings);
            var cached = CachedOrderIDs.Where(o => o.Value.BrokerId.Contains(message.MakerOrderId) || o.Value.BrokerId.Contains(message.TakerOrderId));

            var symbol = ConvertProductId(message.ProductId);

            if (!cached.Any())
            {
                return;
            }

            Log.Trace($"GDAXBrokerage.OrderMatch(): Match: {message.ProductId} {data}");
            var orderId = cached.First().Key;
            var orderObj = cached.First().Value;

            if (!FillSplit.ContainsKey(orderId))
            {
                FillSplit[orderId] = new GDAXFill(orderObj);
            }

            var split = FillSplit[orderId];
            split.Add(message);

            //is this the total order at once? Is this the last split fill?
            var status = Math.Abs(message.Size) == Math.Abs(cached.Single().Value.Quantity) || Math.Abs(split.OrderQuantity) == Math.Abs(split.TotalQuantity())
                ? OrderStatus.Filled : OrderStatus.PartiallyFilled;

            OrderDirection direction;
            // Messages are always from the perspective of the market maker. Flip it in cases of a market order.
            if (orderObj.Type == OrderType.Market)
            {
                direction = message.Side == "sell" ? OrderDirection.Buy : OrderDirection.Sell;
            }
            else
            {
                direction = message.Side == "sell" ? OrderDirection.Sell : OrderDirection.Buy;
            }

            var orderEvent = new OrderEvent
            (
                cached.First().Key, symbol, message.Time, status,
                direction,
                message.Price, direction == OrderDirection.Sell ? -message.Size : message.Size,
                GetFee(cached.First().Value), $"GDAX Match Event {direction}"
            );

            //if we're filled we won't wait for done event
            if (orderEvent.Status == OrderStatus.Filled)
            {
                Orders.Order outOrder = null;
                CachedOrderIDs.TryRemove(cached.First().Key, out outOrder);
            }

            OnOrderEvent(orderEvent);
        }

        private void OrderDone(string data)
        {
            Log.Trace($"GDAXBrokerage.Messaging.OrderDone(): Order completed with data {data}");
            var message = JsonConvert.DeserializeObject<Messages.Done>(data, JsonSettings);

            //if we don't exit now, will result in fill message
            if (message.Reason == "canceled" || message.RemainingSize > 0)
            {
                Log.Trace($"GDAXBrokerage.Messaging.OrderDone(): Order cancelled. Remaining {message.RemainingSize}");
                return;
            }

            //is this our order?
            var cached = CachedOrderIDs.Where(o => o.Value.BrokerId.Contains(message.OrderId));

            if (!cached.Any() || cached.Single().Value.Status == OrderStatus.Filled)
            {
                Log.Trace($"GDAXBrokerage.Messaging.OrderDone(): Order could not locate order in cache with order id {message.OrderId}");
                return;
            }

            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, -1,
                $"GDAXWebsocketsBrokerage.OrderDone: Encountered done message prior to match filling order brokerId: {message.OrderId} orderId: {cached.FirstOrDefault().Key}"));

            var split = this.FillSplit[cached.First().Key];

            //should have already been filled but match message may have been missed. Let's say we've filled now
            var orderEvent = new OrderEvent
            (
                cached.First().Key, ConvertProductId(message.ProductId), message.Time, OrderStatus.Filled,
                message.Side == "sell" ? OrderDirection.Sell : OrderDirection.Buy,
                message.Price, message.Side == "sell" ? -split.TotalQuantity() : split.TotalQuantity(),
                GetFee(cached.First().Value), "GDAX Fill Event"
            );

            Orders.Order outOrder = null;
            CachedOrderIDs.TryRemove(cached.First().Key, out outOrder);

            OnOrderEvent(orderEvent);
        }

        /// <summary>
        /// Retrieves a price tick for a given symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public Tick GetTick(Symbol symbol)
        {
            var req = new RestRequest($"/products/{ConvertSymbol(symbol)}/ticker", Method.GET);
            var response = RestClient.Execute(req);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"GDAXBrokerage.GetFee: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var tick = JsonConvert.DeserializeObject<Messages.Tick>(response.Content);
            return new Tick(tick.Time, symbol, tick.Bid, tick.Ask) { Quantity = tick.Volume };
        }

        /// <summary>
        /// Converts a ticker message and emits data as a new tick
        /// </summary>
        /// <param name="data"></param>
        private void EmitTick(string data)
        {
            var message = JsonConvert.DeserializeObject<Messages.Ticker>(data, JsonSettings);

            var symbol = ConvertProductId(message.ProductId);

            lock (_tickLocker)
            {
                Ticks.Add(new Tick
                {
                    AskPrice = message.BestAsk,
                    BidPrice = message.BestBid,
                    Value = (message.BestAsk + message.BestBid) / 2m,
                    Time = DateTime.UtcNow,
                    Symbol = symbol,
                    TickType = TickType.Quote
                    //todo: tick volume
                });

                Ticks.Add(new Tick
                {
                    Value = message.Price,
                    Time = DateTime.UtcNow,
                    Symbol = symbol,
                    TickType = TickType.Trade,
                    Quantity = message.LastSize
                });
            }
        }

        #region IDataQueueHandler
        /// <summary>
        /// Creates websocket message subscriptions for the supplied symbols
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public override void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {

            foreach (var item in symbols)
            {
                if (item.Value.Contains("UNIVERSE")) continue;

                if (!IsSubscribeAvailable(item))
                {
                    //todo: refactor this outside brokerage
                    //alternative service: http://openexchangerates.org/latest.json
                    PollTick(item);
                }
                else
                {
                    this.ChannelList[item.Value] = new Channel { Name = item.Value, Symbol = item.Value };

                    //emit baseline tick
                    var message = GetTick(item);

                    lock (_tickLocker)
                    {
                        Tick updating = new Tick
                        {
                            AskPrice = message.AskPrice,
                            BidPrice = message.BidPrice,
                            Value = (message.AskPrice + message.BidPrice) / 2m,
                            Time = DateTime.UtcNow,
                            Symbol = item,
                            TickType = TickType.Quote
                            //todo: tick volume
                        };

                        this.Ticks.Add(updating);
                    }
                }
            }

            var products = ChannelList.Select(s => s.Value.Symbol.Substring(0, 3) + "-" + s.Value.Symbol.Substring(3)).ToArray();

            var payload = new
            {
                type = "subscribe",
                product_ids = products,
                channels = _channelNames
            };

            if (payload.product_ids.Length == 0)
            {
                return;
            }

            var token = GetAuthenticationToken(JsonConvert.SerializeObject(payload), "GET", "/users/self/verify");

            var json = JsonConvert.SerializeObject(new
            {
                type = payload.type,
                channels = payload.channels,
                product_ids = payload.product_ids,
                SignHeader = token.Signature,
                KeyHeader = ApiKey,
                PassHeader = _passPhrase,
                TimeHeader = token.Timestamp
            });

            WebSocket.Send(json);


            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, -1, "GDAXBrokerage.Subscribe: Sent subcribe."));

        }

        /// <summary>
        /// Poll for new tick to refresh conversion rate of non-USD denomination
        /// </summary>
        /// <param name="symbol"></param>
        public void PollTick(Symbol symbol)
        {

            int delay = 36000000;
            var token = _canceller.Token;
            var listener = Task.Factory.StartNew(() =>
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, -1, $"GDAXBrokerage.PollLatestTick: started polling for ticks: {symbol.Value.ToString()}"));
                while (true)
                {
                    var rate = GetConversionRate(symbol.Value.Replace("USD", ""));

                    lock (_tickLocker)
                    {
                        var latest = new Tick
                        {
                            Value = rate,
                            Time = DateTime.UtcNow,
                            Symbol = symbol
                        };
                        Ticks.Add(latest);
                    }

                    Thread.Sleep(delay);
                    if (token.IsCancellationRequested) break;
                }
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, -1, $"PollLatestTick: stopped polling for ticks: {symbol.Value.ToString()}"));
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private decimal GetConversionRate(string currency)
        {
            var response = RateClient.Execute(new RestSharp.RestRequest(Method.GET));
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, (int)response.StatusCode, "GetConversionRate: error returned from conversion rate service."));
                return 0;
            }

            var raw = JsonConvert.DeserializeObject<JObject>(response.Content);
            var rate = raw.SelectToken("rates." + currency).Value<decimal>();
            if (rate == 0)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, (int)response.StatusCode, "GetConversionRate: zero value returned from conversion rate service."));
                return 0;
            }

            return 1m / rate;
        }

        private bool IsSubscribeAvailable(Symbol symbol)
        {
            return Regex.IsMatch(symbol.Value, _symbolMatching);
        }

        /// <summary>
        /// Ends current subscriptions
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            if (WebSocket.IsOpen)
            {
                WebSocket.Send(JsonConvert.SerializeObject(new {type = "unsubscribe", channels = _channelNames}));
            }
        }
        #endregion

    }
}