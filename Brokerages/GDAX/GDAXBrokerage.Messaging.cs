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
using QuantConnect.Logging;
using QuantConnect.Orders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Data;
using QuantConnect.Packets;
using System.Threading;
using RestSharp;
using WebSocket4Net;
using System.Text.RegularExpressions;

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
        private string _wssUrl;
        private const string _symbolMatching = "ETH|LTC|BTC";
        private IAlgorithm _algorithm;
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
        public GDAXBrokerage(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, string passPhrase, IAlgorithm algorithm)
            : base(wssUrl, websocket, restClient, apiKey, apiSecret, Market.GDAX, "GDAX")
        {
            FillSplit = new ConcurrentDictionary<long, GDAXFill>();
            _passPhrase = passPhrase;
            _wssUrl = wssUrl;
            _algorithm = algorithm;
        }

        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void OnMessage(object sender, MessageReceivedEventArgs e)
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
                    var error = JsonConvert.DeserializeObject<Messages.Error>(e.Message, JsonSettings);
                    Log.Error("GDAXBrokerage.OnMessage(): " + error.Message + " " + error.Reason);
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
                else if (raw.Type == "open" || raw.Type == "change" | raw.Type == "received" | raw.Type == "subscriptions")
                {
                    //known messages we don't need to handle or log 
                    return;
                }

                Log.Trace("GDAXWebsocketsBrokerage.OnMessage: " + e.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, string.Format("Parsing wss message failed. Data: {0}", e.Message));
                throw;
            }
        }

        private void OrderMatch(string data)
        {
            var message = JsonConvert.DeserializeObject<Messages.Matched>(data, JsonSettings);
            var cached = CachedOrderIDs.Where(o => o.Value.BrokerId.Contains(message.MakerOrderId) || o.Value.BrokerId.Contains(message.TakerOrderId));
            //todo: update volume quotes

            var symbol = ConvertProductId(message.ProductId);

            if (!cached.Any())
            {
                return;
            }

            var split = this.FillSplit[cached.First().Key];
            split.Add(message);

            //is this the total order at once? Is this the last split fill?
            var status = Math.Abs(message.Size) == Math.Abs(cached.Single().Value.Quantity) || Math.Abs(split.OrderQuantity) == Math.Abs(split.TotalQuantity())
                ? OrderStatus.Filled : OrderStatus.PartiallyFilled;

            var orderEvent = new OrderEvent
            (
                cached.First().Key, symbol, message.Time, status,
                message.Side == "sell" ? OrderDirection.Sell : OrderDirection.Buy,
                message.Price, message.Size,
                GetFee(cached.First().Value), "GDAX Match Event"
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
            var message = JsonConvert.DeserializeObject<Messages.Done>(data, JsonSettings);

            //first process impact on order book
            var symbol = ConvertProductId(message.ProductId);

            //if we don't exit now, will result in fill message
            if (message.Reason == "canceled" || message.RemainingSize > 0)
            {
                return;
            }

            //is this our order?
            var cached = CachedOrderIDs.Where(o => o.Value.BrokerId.Contains(message.OrderId));

            if (!cached.Any())
            {
                return;
            }

            Log.Error("GDAXWebsocketsBrokerage.OrderDone: Encountered done message prior to match filling order brokerId:" + message.OrderId + ", orderId:" + cached.FirstOrDefault().Key);

            var split = this.FillSplit[cached.First().Key];

            //should have already been filled but match message may have been missed. Let's say we've filled now
            var orderEvent = new OrderEvent
            (
                cached.First().Key, ConvertProductId(message.ProductId), message.Time, OrderStatus.Filled,
                message.Side == "sell" ? OrderDirection.Sell : OrderDirection.Buy,
                message.Price, split.TotalQuantity(),
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
            var req = new RestRequest(string.Format("/products/{0}/ticker", ConvertSymbol(symbol)), Method.GET);
            var response = RestClient.Execute(req);
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
                Tick updating = new Tick
                {
                    AskPrice = message.BestAsk,
                    BidPrice = message.BestBid,
                    Value = (message.BestAsk + message.BestBid) / 2m,
                    Time = DateTime.UtcNow,
                    Symbol = symbol,
                    TickType = TickType.Quote,
                    //todo: tick volume                          
                };

                this.Ticks.Add(updating);

                lock (_tickLocker)
                {
                    Tick last = new Tick
                    {
                        Value = message.Price,
                        Time = DateTime.UtcNow,
                        Symbol = symbol,
                        TickType = TickType.Trade,
                        Quantity = message.Side == "sell" ? -message.LastSize : message.LastSize
                    };

                    this.Ticks.Add(last);
                }
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

                if (IsValidForSubscribe(item))
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
                channels = new[] { "heartbeat", "ticker", "user", "matches" }
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


            Log.Trace("Subscribe: Sent subcribe.");

        }

        private bool IsValidForSubscribe(Symbol symbol)
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
            WebSocket.Send(JsonConvert.SerializeObject(new { type = "unsubscribe", channels = new[] { "heartbeat", "users", "ticker" } }));
        }
        #endregion

    }
}
