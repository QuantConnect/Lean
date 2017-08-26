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

        #region Declarations
        /// <summary>
        /// Collection of ask prices for subscribed symbols
        /// </summary>
        public Dictionary<Symbol, ConcurrentDictionary<string, decimal>> AskPrices { get; private set; }
        /// <summary>
        /// Collection of bid prices for subscribed symbols
        /// </summary>
        public Dictionary<Symbol, ConcurrentDictionary<string, decimal>> BidPrices { get; private set; }
        private object _tickLocker = new object();
        private ConcurrentDictionary<Symbol, Tick> _previousTick = new ConcurrentDictionary<Symbol, Tick>();
        private CancellationTokenSource _canceller = new CancellationTokenSource();
        /// <summary>
        /// Collection of partial split messages
        /// </summary>
        public ConcurrentDictionary<long, GDAXFill> FillSplit { get; set; }
        private string _passPhrase;
        private string _wssUrl;
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
        public GDAXBrokerage(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, string passPhrase)
            : base(wssUrl, websocket, restClient, apiKey, apiSecret, Market.GDAX, "GDAX")
        {
            FillSplit = new ConcurrentDictionary<long, GDAXFill>();
            AskPrices = new Dictionary<Symbol, ConcurrentDictionary<string, decimal>>();
            BidPrices = new Dictionary<Symbol, ConcurrentDictionary<string, decimal>>();
            _passPhrase = passPhrase;
            _wssUrl = wssUrl;
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
                else if (raw.Type == "received")
                {
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

            var orderEvent = new OrderEvent
            (
                cached.First().Key, ConvertProductId(message.ProductId), message.Time, OrderStatus.PartiallyFilled,
                message.Side == "sell" ? OrderDirection.Sell : OrderDirection.Buy,
                message.Price, message.Size,
                GetFee(cached.First().Value), "GDAX Match Event"
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

            var orderEvent = new OrderEvent
            (
                cached.First().Key, ConvertProductId(message.ProductId), message.Time, OrderStatus.Filled,
                message.Side == "sell" ? OrderDirection.Sell : OrderDirection.Buy,
                message.Price, split.OrderQuantity - split.TotalQuantity(),
                GetFee(cached.First().Value), "GDAX Fill Event"
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
            if (_previousTick[symbol].AskPrice != min || _previousTick[symbol].BidPrice != max && min > 0 && max > 0)
            {
                lock (_tickLocker)
                {
                    Tick updating = new Tick
                    {
                        AskPrice = min,
                        BidPrice = max,
                        Value = (min + max) / 2m,
                        Time = DateTime.UtcNow,
                        Symbol = symbol
                    };

                    _previousTick[updating.Symbol] = updating;
                    this.Ticks.Add(updating);
                }
            }

        }

        /// <summary>
        /// Poll for new tick to refresh the baseline state of real-time order book
        /// </summary>
        /// <param name="symbol"></param>
        private void PollTick(Symbol symbol)
        {

            int delay = 60000;
            var token = _canceller.Token;
            var listener = Task.Factory.StartNew(() =>
            {
                Log.Trace("PollLatestTick: " + "Started polling for ticks: " + symbol.Value.ToString());
                while (true)
                {
                    //todo: adding a new baseline tick should result in pruning of price AskPrices and BidPrices
                    _previousTick.AddOrUpdate(symbol, GetTick(symbol));
                    Thread.Sleep(delay);
                    if (token.IsCancellationRequested) break;
                }
                Log.Trace("PollLatestTick: " + "Stopped polling for ticks: " + symbol.Value.ToString());
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        #region IDataQueueHandler
        /// <summary>
        /// Creates websocket message subscriptions for the supplied symbols
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public override void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            if (!this.IsConnected)
            {
                this.Connect();
            }
			//todo: heartbeat disabled for now
            //WebSocket.Send("{ \"type\": \"heartbeat\", \"on\": true }");

            foreach (var item in symbols)
            {
			//todo: cleaner way to filter out bad symbols
                if (item.Value.Length != 6)
                {
                    continue;
                }

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
                if (!_previousTick.Keys.Contains(item))
                {
                    _previousTick.TryAdd(item, new Tick());
                }

                //Set off a task to poll for latest tick
                PollTick(item);
            }

            var payload = new
            {
                product_ids = symbols.Where(s => s.Value.Length == 6).Select(s => s.Value.ToString().Substring(0, 3) + "-" + s.Value.ToString().Substring(3)).ToArray(),
            };

            if (payload.product_ids.Length == 0)
            {
                return;
            }

            var token = GetAuthenticationToken(JsonConvert.SerializeObject(payload), "GET", "/users/self");

            var json = JsonConvert.SerializeObject(new
            {
                type = "subscribe",
                product_ids = payload.product_ids,
				//todo: wss auth disabled for now
                //key = ApiKey,
                //signature = token.Signature,
                //timestamp = token.Timestamp,
                //passphrase = _passPhrase
            });

            WebSocket.Send(json);

            Log.Trace("Subscribe: Sent subcribe.");

        }

        /// <summary>
        /// Cancels the ticker polling task
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            _canceller.Cancel();
        }
        #endregion

    }
}
