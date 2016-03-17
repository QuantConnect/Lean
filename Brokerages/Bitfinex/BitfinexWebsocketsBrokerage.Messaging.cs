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
using QuantConnect.Logging;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace QuantConnect.Brokerages.Bitfinex
{
    public partial class BitfinexWebsocketsBrokerage
    {

        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                var raw = JsonConvert.DeserializeObject<dynamic>(e.Data);

                if (raw.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    int id = raw[0];
                    string term = raw[1];

                    if (term == "hb")
                    {
                        //heartbeat
                        _heartbeatCounter = DateTime.UtcNow;
                        return;
                    }
                    else if (term == "tu" || term == "te")
                    {
                        //trade execution/update
                        var data = raw[2].ToObject(typeof(string[]));
                        PopulateTrade(data);
                    }
                    else if (term == "ws")
                    {
                        //wallet
                        var data = raw[2].ToObject(typeof(string[][]));
                        PopulateWallet(data);
                    }
                    else if (_channelId.ContainsKey(id) && _channelId[id] == "ticker")
                    {
                        //ticker
                        PopulateTicker(e.Data);
                        return;
                    }
                }
                else if (raw.channel == "ticker" && raw.@event == "subscribed")
                {
                    if (!this._channelId.ContainsKey((int)raw.chanId))
                    {
                        this._channelId[(int)raw.chanId] = "ticker";
                    }
                }
                else if (raw.chanId == 0)
                {
                    if (raw.status == "FAIL")
                    {
                        throw new Exception("Failed to authenticate with ws gateway");
                    }
                    Log.Trace("BitfinexWebsocketsBrokerage.OnMessage(): Successful wss auth");
                }
                else if (raw.@event == "info" && raw.code == "20051")
                {
                    //hard reset
                    this.Reconnect();
                }
                else if (raw.@event == "info" && raw.code == "20061")
                {
                    //soft reset
                    UnAuthenticate();
                    Unsubscribe(null, null);
                    Subscribe(null, null);
                    Authenticate();
                }

                Log.Trace("BitfinexWebsocketsBrokerage.OnMessage(): " + e.Data);
            }
            catch (Exception ex)
            {
                Log.Error(ex, string.Format("Parsing wss message failed. Data: {0}", e.Data));
            }
        }
        
        private void PopulateTicker(string response)
        {
            var data = JsonConvert.DeserializeObject<string[]>(response);
            var msg = new TickerMessage(data);
            lock (Ticks)
            {
                Ticks.Add(new Tick
                {
                    AskPrice = msg.ASK / ScaleFactor,
                    BidPrice = msg.BID / ScaleFactor,
                    AskSize = (long)Math.Round(msg.ASK_SIZE * ScaleFactor, 0),
                    BidSize = (long)Math.Round(msg.BID_SIZE * ScaleFactor, 0),
                    Time = DateTime.UtcNow,
                    Value = msg.LAST_PRICE / ScaleFactor,
                    TickType = TickType.Quote,
                    Symbol = Symbol,
                    DataType = MarketDataType.Tick,
                    Quantity = (int)(Math.Round(msg.VOLUME * ScaleFactor, 2))
                });
            }
        }

        //todo: Currently populated but not used
        private void PopulateWallet(string[][] data)
        {
            if (data.Length > 0)
            {
                lock (_cashLock)
                {
                    _cash.Clear();
                    for (int i = 0; i < data.Length; i++)
                    {
                        var msg = new WalletMessage(data[i]);
                        _cash.Add(new Securities.Cash(msg.WLT_CURRENCY, msg.WLT_BALANCE, 1));
                    }
                }
            }
        }

        private void PopulateTrade(string[] data)
        {
            var msg = new TradeMessage(data);
            int brokerId = msg.TRD_ORD_ID;
            var cached = CachedOrderIDs.Where(o => o.Value.BrokerId.Contains(brokerId.ToString()));

            if (cached.Count() > 0 && cached.First().Value != null)
            {
                var fill = new OrderEvent
                (
                    cached.First().Key, Symbol, msg.TRD_TIMESTAMP, MapOrderStatus(msg),
                    msg.TRD_AMOUNT_EXECUTED > 0 ? OrderDirection.Buy : OrderDirection.Sell,
                    msg.TRD_PRICE_EXECUTED / ScaleFactor, (int)(msg.TRD_AMOUNT_EXECUTED * ScaleFactor),
                    msg.FEE / ScaleFactor, "Bitfinex Fill Event"
                );
                fill.FillPrice = msg.TRD_PRICE_EXECUTED / ScaleFactor;

                if (msg.FEE_CURRENCY == "BTC")
                {
                    msg.FEE = (msg.FEE * msg.TRD_PRICE_EXECUTED) / ScaleFactor;
                }

                FilledOrderIDs.Add(cached.First().Key);

                if (fill.Status == OrderStatus.Filled)
                {
                    Order outOrder = cached.First().Value;
                    CachedOrderIDs.TryRemove(cached.First().Key, out outOrder);
                }
                OnOrderEvent(fill);
            }
            else
            {
                UnknownOrderIDs.Add(brokerId);
            }
        }


        /// <summary>
        /// Authenticate with wss
        /// </summary>
        protected override void Authenticate()
        {
            string key = ApiKey;
            string payload = "AUTH" + DateTime.UtcNow.Ticks.ToString();
            _webSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "auth",
                apiKey = key,
                authSig = GetHexHashSignature(payload, ApiSecret),
                authPayload = payload
            }));
        }

        private void UnAuthenticate()
        {
            _webSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "unauth"
            }));
            _webSocket.Close();
        }

    }
}
