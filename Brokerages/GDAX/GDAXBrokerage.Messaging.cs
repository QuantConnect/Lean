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
using WebSocketSharp;
using QuantConnect.Securities;
using QuantConnect.Data;
using QuantConnect.Packets;
using System.Threading;
using RestSharp;

namespace QuantConnect.Brokerages.GDAX
{
    public partial class GDAXBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
    {

        public Dictionary<Symbol, ConcurrentDictionary<string, decimal>> AskPrices { get; private set; }
        public Dictionary<Symbol, ConcurrentDictionary<string, decimal>> BidPrices { get; private set; }
        private object tickLocker = new object();
        private ConcurrentDictionary<Symbol, Tick> previousTick = new ConcurrentDictionary<Symbol, Tick>();
        CancellationTokenSource canceller = new CancellationTokenSource();
        public ConcurrentDictionary<long, GDAXFill> FillSplit { get; set; }
        private string _passPhrase;
        private string _wssUrl;
        private string _accountId;

        public GDAXBrokerage(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, string passPhrase, string accountId)
            : base(wssUrl, websocket, restClient, apiKey, apiSecret, Market.GDAX, "GDAX")
        {
            FillSplit = new ConcurrentDictionary<long, GDAXFill>();
            AskPrices = new Dictionary<Symbol, ConcurrentDictionary<string, decimal>>();
            BidPrices = new Dictionary<Symbol, ConcurrentDictionary<string, decimal>>();
            _passPhrase = passPhrase;
            _wssUrl = wssUrl;
            _accountId = accountId;
        }

        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                var raw = JsonConvert.DeserializeObject<Messages.BaseMessage>(e.Data, JsonSettings);

                if (raw.Type == "heartbeat")
                {
                    LastHeartbeatUtcTime = DateTime.UtcNow;
                    return;
                }
                else if (raw.Type == "error")
                {
                    var error = JsonConvert.DeserializeObject<Messages.Error>(e.Data, JsonSettings);
                    Log.Error("GDAXBrokerage.OnMessage(): " + error.Message);
                }
                else if (raw.Type == "done")
                {
                    OrderDone(e.Data);
                    return;
                }
                else if (raw.Type == "match")
                {
                    OrderMatch(e.Data);
                    return;
                }
                else if (raw.Type == "open" || raw.Type == "change")
                {
                    OrderOpenOrChange(e.Data);
                    return;
                }

                Log.Trace("GDAXWebsocketsBrokerage.OnMessage(): " + e.Data);
            }
            catch (Exception ex)
            {
                Log.Error(ex, string.Format("Parsing wss message failed. Data: {0}", e.Data));
                throw;
            }
        }

        private void OrderMatch(string data)
        {
            var message = JsonConvert.DeserializeObject<Messages.Matched>(data, JsonSettings);
            var cached = CachedOrderIDs.Where(o => o.Value.BrokerId.Contains(message.MakerOrderId) || o.Value.BrokerId.Contains(message.TakerOrderId));

            if (!cached.Any())
            {
                return;
            }

            var split = this.FillSplit[cached.First().Key];
            split.Add(message);

            //todo: fee
            var orderEvent = new OrderEvent
            (
                cached.First().Key, ConvertProductId(message.ProductId), message.Time, OrderStatus.PartiallyFilled,
                message.Side == "sell" ? OrderDirection.Sell : OrderDirection.Buy,
                message.Price, message.Size,
                0, "GDAX Match Event"
            );

            OnOrderEvent(orderEvent);
        }

        private void OrderDone(string data)
        {
            var message = JsonConvert.DeserializeObject<Messages.Done>(data, JsonSettings);

            //first process impact on order book
            var symbol = ConvertProductId(message.ProductId);

            decimal removed;
            if (message.Side == "sell")
            {
                AskPrices[symbol].TryRemove(message.OrderId, out removed);
            }
            else if (message.Side == "buy")
            {
                decimal bid;
                BidPrices[symbol].TryRemove(message.OrderId, out removed);
            }

            EmitTick(message.ProductId);

            if (message.Reason == "canceled")
            {
                return;
            }

            //is this our order?
            var cached = CachedOrderIDs.Where(o => o.Value.BrokerId.Contains(message.OrderId));

            if (!cached.Any())
            {
                return;
            }

            var split = this.FillSplit[cached.First().Key];

            //todo: fee
            var orderEvent = new OrderEvent
            (
                cached.First().Key, ConvertProductId(message.ProductId), message.Time, OrderStatus.Filled,
                message.Side == "sell" ? OrderDirection.Sell : OrderDirection.Buy,
                message.Price, split.OrderQuantity - split.TotalQuantity(),
                0, "GDAX Fill Event"
            );

            Orders.Order outOrder = null;
            CachedOrderIDs.TryRemove(cached.First().Key, out outOrder);

            OnOrderEvent(orderEvent);
        }

        private void OrderOpenOrChange(string data)
        {
            var message = JsonConvert.DeserializeObject<Messages.Open>(data, JsonSettings);

            var symbol = ConvertProductId(message.ProductId);

            //ignore changes that were not previously open
            if (message.Side == "sell" && message.Price > 0 && (message.Type == "open" || AskPrices[symbol].ContainsKey(message.OrderId)))
            {
                AskPrices[symbol].AddOrUpdate(message.OrderId, message.Price);
            }
            else if (message.Side == "buy" && message.Price > 0 && (message.Type == "open" || BidPrices[symbol].ContainsKey(message.OrderId)))
            {
                BidPrices[symbol].AddOrUpdate(message.OrderId, message.Price);
            }

            EmitTick(message.ProductId);
        }

        public Tick GetTick(Symbol symbol)
        {
            var req = new RestRequest(string.Format("/products/{0}/ticker", ConvertSymbol(symbol)), Method.GET);
            var response = RestClient.Execute(req);
            var tick = JsonConvert.DeserializeObject<Messages.Tick>(response.Content);
            //todo: change tick after int change
            return new Tick(tick.Time, symbol, tick.Bid, tick.Ask) { Quantity = (int)tick.Volume };
        }

        private void EmitTick(string productId)
        {
            EmitTick(ConvertProductId(productId));
        }

        /// <summary>
        /// Compares current order book state and emits a non-duplicated tick
        /// </summary>
        /// <param name="symbol"></param>
        private void EmitTick(Symbol symbol)
        {
            var min = AskPrices[symbol].Any() ? AskPrices[symbol].Min(a => a.Value) : 0;
            var max = BidPrices[symbol].Any() ? BidPrices[symbol].Max(a => a.Value) : 0;
            if (previousTick[symbol].AskPrice != min || previousTick[symbol].BidPrice != max && min > 0 && max > 0)
            {
                lock (tickLocker)
                {
                    Tick updating = new Tick
                    {
                        AskPrice = min,
                        BidPrice = max,
                        Value = (min + max) / 2m,
                        Time = DateTime.UtcNow,
                        Symbol = symbol
                    };

                    previousTick[updating.Symbol] = updating;
                    this.Ticks.Add(updating);
                }
            }

        }

        /// <summary>
        /// Poll for new tick to refresh the state of real-time order book
        /// </summary>
        /// <param name="symbol"></param>
        private void PollTick(Symbol symbol)
        {

            int delay = 60000;
            var token = canceller.Token;
            var listener = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    previousTick.AddOrUpdate(symbol, GetTick(symbol));
                    Thread.Sleep(delay);
                    if (token.IsCancellationRequested) break;
                }

            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            Log.Trace("PollLatestTick: " + "Stopped polling for ticks.");
        }

        #region IDataQueueHandler
        public override void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            if (!this.IsConnected)
            {
                this.Connect();
            }

            foreach (var item in symbols)
            {
                //add symbols to ticker data
                foreach (var list in new[] { AskPrices, BidPrices })
                {
                    if (!list.ContainsKey(item))
                    {
                        list.Add(item, new ConcurrentDictionary<string, decimal>());
                    }
                }

                this.ChannelList[item.Value] = new Channel { Name = item.Value, Symbol = item.Value };

                //add empty ticks to most recent. These avoid emitting duplicate ticks
                if (!previousTick.Keys.Contains(item))
                {
                    previousTick.TryAdd(item, new Tick());
                }

                //Set off a task to poll for latest tick
                PollTick(item);
            }

            var payload = new
            {
                Type = "subscribe",
                ProductIds = symbols.Select(s => s.Value.ToString().Substring(0, 3) + "-" + s.Value.ToString().Substring(3)).ToArray(),
            };

            var token = GetAuthenticationToken(JsonConvert.SerializeObject(payload), "", _wssUrl);

            WebSocket.Send(JsonConvert.SerializeObject(new
            {
                type = "subscribe",
                product_ids = payload.ProductIds,
                key = ApiKey,
                passphrase = _passPhrase,
                signature = token.Signature,
                timestamp = token.Timestamp
            }));

            Log.Trace("Subscribe: Sent subcribe.");

        }

        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            canceller.Cancel();
        }
        #endregion

    }
}
