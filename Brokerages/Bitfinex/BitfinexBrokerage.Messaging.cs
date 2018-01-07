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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.Bitfinex
{
    public partial class BitfinexBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
    {
        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnMessageImpl(object sender, WebSocketMessage e)
        {
            try
            {
                var raw = JsonConvert.DeserializeObject<dynamic>(e.Message, settings);

                if (raw.Type == JTokenType.Array)
                {
                    string id = raw[0];
                    string term = raw[1].Type == JTokenType.String ? raw[1] : "";

                    if (term == "hb")
                    {
                        //heartbeat
                        _lastHeartbeatUtcTime = DateTime.UtcNow;
                        return;
                    }
                    else if (ChannelList.ContainsKey(id) && ChannelList[id].Name == "ticker")
                    {
                        //ticker
                        PopulateTicker(e.Message, ChannelList[id].Symbol);
                        return;
                    }
                    else if (ChannelList.ContainsKey(id) && ChannelList[id].Name == "trades" && (term == "te" || term == "tu"))
                    {
                        if (term == "tu")
                        {
                            return;
                        }
                        //todo: optional trade ticker
                        PopulateTradeTicker(e.Message, ChannelList[id].Symbol);
                        return;
                    }
                    else if (id == "0" && term == "tu")
                    {
                        //expect a "te" and "tu" for each fill. The "tu" will include fees, so we won't act upon a "te"
                        var data = raw[2].ToObject(typeof(string[]));
                        PopulateTrade(term, data);
                        return;
                    }
                    else if (term == "ws")
                    {
                        //wallet
                        var data = raw[2].ToObject(typeof(string[][]));
                        PopulateWallet(data);
                        return;
                    }
                    else if (!ChannelList.ContainsKey(id))
                    {
                        return;
                    }
                }
                else if ((raw.channel == "ticker" || raw.channel == "trades") && raw.@event == "subscribed")
                {
                    var channel = (string)raw.channel;
                    var currentChannelId = (string)raw.chanId;
                    var pair = (string)raw.pair;

                    var removing = ChannelList.Where(c => c.Value.Name == channel && c.Value.Symbol == pair).Select(c => c.Key).ToArray();

                    foreach (var item in removing)
                    {
                        if (item != currentChannelId)
                        {
                            ChannelList.Remove(item);
                        }
                    }
                    ChannelList[currentChannelId] = new Channel { Name = channel, Symbol = raw.pair };
                    return;
                }
                else if (raw.chanId == 0)
                {
                    if (raw.status == "FAIL")
                    {
                        throw new Exception("Failed to authenticate with ws gateway");
                    }
                    Log.Trace("BitfinexBrokerage.OnMessage(): Successful wss auth");
                    return;
                }
                else if (raw.code == "20051" || raw.code == "20061")
                {
                    //hard reset - only close and allow reconnect timeout to handle
                    Log.Trace("BitfinexBrokerage.OnMessage(): Broker restart sequence is starting.");
                    WebSocket.Close();
                    return;
                }

                Log.Trace("BitfinexBrokerage.OnMessage(): " + e.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, string.Format("Parsing wss message failed. Data: {0}", e.Message));
                throw;
            }
        }

        private void PopulateTicker(string response, string symbol)
        {
            var data = JsonConvert.DeserializeObject<string[]>(response, settings);
            var msg = new Messages.Ticker(data);
            lock (Ticks)
            {
                Ticks.Add(new Tick
                {
                    AskPrice = msg.Ask,
                    BidPrice = msg.Bid,
                    AskSize = msg.AskSize,
                    BidSize = msg.BidSize,
                    Time = DateTime.UtcNow,
                    Value = (msg.Ask + msg.Bid) / 2m,
                    TickType = TickType.Quote,
                    Symbol = Symbol.Create(symbol.ToUpper(), SecurityType.Crypto, BrokerageMarket),
                    DataType = MarketDataType.Tick
                });
            }
        }

        private void PopulateTradeTicker(string response, string symbol)
        {
            var data = JsonConvert.DeserializeObject<string[]>(response, settings);

            var msg = new Messages.Trade(data);
            lock (Ticks)
            {
                Ticks.Add(new Tick
                {
                    Time = DateTime.UtcNow,
                    Value = msg.Price,
                    TickType = TickType.Trade,
                    Symbol = Symbol.Create(symbol.ToUpper(), SecurityType.Crypto, BrokerageMarket),
                    DataType = MarketDataType.Tick,
                    Quantity = msg.Amount
                });
            }
        }

        private void PopulateWallet(string[][] data)
        {
            foreach (var item in data)
            {
                var msg = new Messages.Wallet(item);
                if (msg.Name == GetWallet())
                {
                    OnAccountChanged(new Securities.AccountEvent(msg.Currency.ToUpper(), msg.Balance));
                }
            }
        }

        private void PopulateTrade(string term, string[] data)
        {
            var msg = new Messages.Fill(term, data);

            var cached = CachedOrderIDs.Where(o => o.Value.BrokerId.Contains(msg.OrdId.ToString()));

            if (cached.Any())
            {
                if (msg.FeeCurrency != "USD")
                {
                    msg.Fee = msg.Fee * msg.PriceExecuted;
                    msg.FeeCurrency = "USD";
                }
                var split = FillSplit[cached.First().Key];
                var added = split.Add(msg);
                if (!added)
                {
                    //ignore fill message duplicate
                    Log.Trace("BitfinexBrokerage.PopulateTrade:" + "Fill message duplicate:" + string.Join(",", data));
                    return;
                }

                var fill = new OrderEvent
                (
                    cached.First().Key, cached.First().Value.Symbol, msg.Timestamp, OrderStatus.PartiallyFilled,
                    cached.First().Value.Direction, msg.PriceExecuted, msg.AmountExecuted,
                    0, "Bitfinex Fill Event"
                );
                fill.FillPriceCurrency = cached.First().Value.Symbol.Value;
                fill.OrderFee = Math.Abs(msg.Fee);

                if (split.IsCompleted())
                {
                    fill.Status = OrderStatus.Filled;
                    fill.FillQuantity = msg.AmountExecuted;

                    var outOrder = cached.First().Value;
                    CachedOrderIDs.TryRemove(cached.First().Key, out outOrder);
                }

                OnOrderEvent(fill);
            }
            else
            {
                UnknownOrderIDs.Add(msg.OrdId.ToString());
            }
        }

        /// <summary>
        /// Authenticate with wss
        /// </summary>
        public void Authenticate()
        {
            var key = ApiKey;
            var authNonce = DateTime.UtcNow.Ticks;
            var payload = "AUTH" + authNonce;
            WebSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "auth",
                apiKey = key,
                authSig = GetHexHashSignature(payload, ApiSecret),
                authNonce = authNonce,
                authPayload = payload
            }));
        }

        private void UnAuthenticate()
        {
            WebSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "unauth"
            }));
        }

        /// <summary>
        /// Get queued tick data
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Data.BaseData> GetNextTicks()
        {
            lock (Ticks)
            {
                var copy = Ticks.ToArray();
                Ticks.Clear();
                return copy;
            }
        }

        /// <summary>
        /// Add subscription to Websockets service
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public void Subscribe(Packets.LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            if (!IsConnected)
            {
                Connect();
            }

            //todo: attempt unsubscribe to cleanup existing. Otherwise, dupe sub messages to be taken as sub confirmation

            foreach (var item in symbols)
            {
                if (item.Value.Contains("UNIVERSE"))
                {
                    continue;
                }

                WebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "subscribe",
                    channel = "ticker",
                    pair = item.Value
                }));

                WebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "subscribe",
                    channel = "trades",
                    pair = item.Value
                }));

                Log.Trace("BitfinexBrokerage.Subscribe(): Sent subcribe for " + item.Value);
            }
        }

        /// <summary>
        /// Remove subscription from Websockets service
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public void Unsubscribe(Packets.LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (var item in symbols)
            {
                lock (ChannelList)
                {
                    foreach (var channel in ChannelList.Where(c => c.Value.Symbol == item.ToString()))
                    {
                        Unsubscribe(channel.Key);
                    }
                }
            }
        }

        private void Unsubscribe(string id)
        {
            try
            {
                WebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "unsubscribe",
                    channelId = id
                }));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error encountered whilst attempting unsubscribe.");
            }
        }

        /// <summary>
        /// Sybscribes to the given symbols
        /// </summary>
        /// <param name="symbols"></param>
        public override void Subscribe(IEnumerable<Symbol> symbols)
        {
            Subscribe(null, symbols);
        }
    }
}