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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using RestSharp;
using System.Text.RegularExpressions;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.GDAX
{
    public partial class GDAXBrokerage
    {
        #region Declarations
        private readonly object _tickLocker = new object();
        /// <summary>
        /// Collection of partial split messages
        /// </summary>
        public ConcurrentDictionary<long, GDAXFill> FillSplit { get; set; }
        private readonly string _passPhrase;
        private const string SymbolMatching = "ETH|LTC|BTC|BCH";
        private readonly IAlgorithm _algorithm;
        private readonly CancellationTokenSource _canceller = new CancellationTokenSource();
        private readonly ConcurrentQueue<WebSocketMessage> _messageBuffer = new ConcurrentQueue<WebSocketMessage>();
        private volatile bool _streamLocked;
        private readonly ConcurrentDictionary<int, decimal> _orderFees = new ConcurrentDictionary<int, decimal>();
        private readonly ConcurrentDictionary<Symbol, OrderBook> _orderBooks = new ConcurrentDictionary<Symbol, OrderBook>();
        private readonly bool _isDataQueueHandler;

        /// <summary>
        /// Rest client used to call missing conversion rates
        /// </summary>
        public IRestClient RateClient { get; set; }
        #endregion

        /// <summary>
        /// The list of websocket channels to subscribe
        /// </summary>
        protected virtual string[] ChannelNames { get; } = { "heartbeat", "user", "matches" };

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
                var tickers = new[]
                {
                    "LTCUSD", "LTCEUR", "LTCBTC",
                    "BTCUSD", "BTCEUR", "BTCGBP",
                    "ETHBTC", "ETHUSD", "ETHEUR",
                    "BCHUSD"
                };
                Subscribe(tickers.Select(ticker => Symbol.Create(ticker, SecurityType.Crypto, Market.GDAX)));
            };

            _isDataQueueHandler = this is GDAXDataQueueHandler;
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
                else if (raw.Type == "snapshot")
                {
                    OnSnapshot(e.Message);
                    return;
                }
                else if (raw.Type == "l2update")
                {
                    OnL2Update(e.Message);
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
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }

        private void OnSnapshot(string data)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<Messages.Snapshot>(data);

                var symbol = ConvertProductId(message.ProductId);

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

                foreach (var row in message.Bids)
                {
                    var price = decimal.Parse(row[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                    var size = decimal.Parse(row[1], NumberStyles.Float, CultureInfo.InvariantCulture);
                    orderBook.UpdateBidRow(price, size);
                }
                foreach (var row in message.Asks)
                {
                    var price = decimal.Parse(row[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                    var size = decimal.Parse(row[1], NumberStyles.Float, CultureInfo.InvariantCulture);
                    orderBook.UpdateAskRow(price, size);
                }

                orderBook.BestBidAskUpdated += OnBestBidAskUpdated;

                if (_isDataQueueHandler)
                {
                    EmitQuoteTick(symbol, orderBook.BestBidPrice, orderBook.BestBidSize, orderBook.BestAskPrice, orderBook.BestAskSize);
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
            if (_isDataQueueHandler)
            {
                EmitQuoteTick(e.Symbol, e.BestBidPrice, e.BestBidSize, e.BestAskPrice, e.BestAskSize);
            }
        }

        private void OnL2Update(string data)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<Messages.L2Update>(data);

                var symbol = ConvertProductId(message.ProductId);

                var orderBook = _orderBooks[symbol];

                foreach (var row in message.Changes)
                {
                    var side = row[0];
                    var price = Convert.ToDecimal(row[1], CultureInfo.InvariantCulture);
                    var size = decimal.Parse(row[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                    if (side == "buy")
                    {
                        if (size == 0)
                        {
                            orderBook.RemoveBidRow(price);
                        }
                        else
                        {
                            orderBook.UpdateBidRow(price, size);
                        }
                    }
                    else if (side == "sell")
                    {
                        if (size == 0)
                        {
                            orderBook.RemoveAskRow(price);
                        }
                        else
                        {
                            orderBook.UpdateAskRow(price, size);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Data: " + data);
                throw;
            }
        }

        private void OrderMatch(string data)
        {
            var message = JsonConvert.DeserializeObject<Messages.Matched>(data, JsonSettings);

            if (_isDataQueueHandler)
            {
                EmitTradeTick(message);
            }

            var cached = CachedOrderIDs
                .Where(o => o.Value.BrokerId.Contains(message.MakerOrderId) || o.Value.BrokerId.Contains(message.TakerOrderId))
                .ToList();

            var symbol = ConvertProductId(message.ProductId);

            if (cached.Count == 0)
            {
                return;
            }

            Log.Trace($"GDAXBrokerage.OrderMatch(): Match: {message.ProductId} {data}");
            var orderId = cached[0].Key;
            var order = cached[0].Value;

            if (!FillSplit.ContainsKey(orderId))
            {
                FillSplit[orderId] = new GDAXFill(order);
            }

            var split = FillSplit[orderId];
            split.Add(message);

            //is this the total order at once? Is this the last split fill?
            var status = Math.Abs(message.Size) == Math.Abs(order.Quantity) || Math.Abs(split.OrderQuantity) == Math.Abs(split.TotalQuantity())
                ? OrderStatus.Filled : OrderStatus.PartiallyFilled;

            OrderDirection direction;
            // Messages are always from the perspective of the market maker. Flip direction if executed as a taker.
            if (order.BrokerId[0] == message.TakerOrderId)
            {
                direction = message.Side == "sell" ? OrderDirection.Buy : OrderDirection.Sell;
            }
            else
            {
                direction = message.Side == "sell" ? OrderDirection.Sell : OrderDirection.Buy;
            }

            decimal totalOrderFee;
            if (!_orderFees.TryGetValue(orderId, out totalOrderFee))
            {
                totalOrderFee = GetFee(order);
                _orderFees[orderId] = totalOrderFee;
            }

            // apply order fee on a pro rata basis
            var orderFee = totalOrderFee * Math.Abs(message.Size) / Math.Abs(order.Quantity);

            var orderEvent = new OrderEvent
            (
                orderId, symbol, message.Time, status,
                direction,
                message.Price, direction == OrderDirection.Sell ? -message.Size : message.Size,
                orderFee, $"GDAX Match Event {direction}"
            );

            //if we're filled we won't wait for done event
            if (orderEvent.Status == OrderStatus.Filled)
            {
                Order outOrder;
                CachedOrderIDs.TryRemove(orderId, out outOrder);

                decimal outOrderFee;
                _orderFees.TryRemove(orderId, out outOrderFee);
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
            var cached = CachedOrderIDs.Where(o => o.Value.BrokerId.Contains(message.OrderId)).ToList();

            if (cached.Count == 0 || cached[0].Value.Status == OrderStatus.Filled)
            {
                Log.Trace($"GDAXBrokerage.Messaging.OrderDone(): Order could not locate order in cache with order id {message.OrderId}");
                return;
            }

            var orderId = cached[0].Key;
            var order = cached[0].Value;

            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, -1,
                $"GDAXWebsocketsBrokerage.OrderDone: Encountered done message prior to match filling order brokerId: {message.OrderId} orderId: {orderId}"));

            var split = FillSplit[orderId];

            //should have already been filled but match message may have been missed. Let's say we've filled now
            var orderEvent = new OrderEvent
            (
                orderId, ConvertProductId(message.ProductId), message.Time, OrderStatus.Filled,
                message.Side == "sell" ? OrderDirection.Sell : OrderDirection.Buy,
                message.Price, message.Side == "sell" ? -split.TotalQuantity() : split.TotalQuantity(),
                GetFee(order), "GDAX Fill Event"
            );

            Order outOrder;
            CachedOrderIDs.TryRemove(orderId, out outOrder);

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
                throw new Exception($"GDAXBrokerage.GetTick: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var tick = JsonConvert.DeserializeObject<Messages.Tick>(response.Content);
            return new Tick(tick.Time, symbol, tick.Bid, tick.Ask) { Quantity = tick.Volume };
        }

        /// <summary>
        /// Emits a new quote tick
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="bidPrice">The bid price</param>
        /// <param name="bidSize">The bid size</param>
        /// <param name="askPrice">The ask price</param>
        /// <param name="askSize">The ask price</param>
        private void EmitQuoteTick(Symbol symbol, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            lock (_tickLocker)
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

        /// <summary>
        /// Emits a new trade tick from a match message
        /// </summary>
        private void EmitTradeTick(Messages.Matched message)
        {
            var symbol = ConvertProductId(message.ProductId);

            lock (_tickLocker)
            {
                Ticks.Add(new Tick
                {
                    Value = message.Price,
                    Time = DateTime.UtcNow,
                    Symbol = symbol,
                    TickType = TickType.Trade,
                    Quantity = message.Size
                });
            }
        }

        /// <summary>
        /// Creates websocket message subscriptions for the supplied symbols
        /// </summary>
        public override void Subscribe(IEnumerable<Symbol> symbols)
        {
            foreach (var item in symbols)
            {
                if (item.Value.Contains("UNIVERSE") ||
                    item.SecurityType != SecurityType.Forex && item.SecurityType != SecurityType.Crypto)
                {
                    continue;
                }

                if (!IsSubscribeAvailable(item))
                {
                    //todo: refactor this outside brokerage
                    //alternative service: http://openexchangerates.org/latest.json
                    PollTick(item);
                }
                else
                {
                    this.ChannelList[item.Value] = new Channel { Name = item.Value, Symbol = item.Value };
                }
            }

            var products = ChannelList.Select(s => s.Value.Symbol.Substring(0, 3) + "-" + s.Value.Symbol.Substring(3)).ToArray();

            var payload = new
            {
                type = "subscribe",
                product_ids = products,
                channels = ChannelNames
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


            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, -1, "GDAXBrokerage.Subscribe: Sent subscribe."));

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
            return Regex.IsMatch(symbol.Value, SymbolMatching);
        }

        /// <summary>
        /// Ends current subscriptions
        /// </summary>
        public void Unsubscribe(IEnumerable<Symbol> symbols)
        {
            if (WebSocket.IsOpen)
            {
                WebSocket.Send(JsonConvert.SerializeObject(new {type = "unsubscribe", channels = ChannelNames}));
            }
        }

    }
}