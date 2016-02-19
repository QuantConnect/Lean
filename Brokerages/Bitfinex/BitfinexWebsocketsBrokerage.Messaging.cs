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
        public void OnMessage(object sender, MessageEventArgs e)
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
                    var data = raw[2].ToObject(typeof(string[]));
                    PopulateTrade(data);
                }
                else if (term == "ws")
                {
                    var data = raw[2].ToObject(typeof(string[][]));
                    PopulateWallet(data);
                }
                else if (_channelId.ContainsKey(id) && _channelId[id] == "ticker")
                {
                    PopulateTicker(e.Data);
                    return;
                }
            }
            else if (raw.chanId == 0)
            {
                if (raw.status == "FAIL")
                {
                    throw new Exception("Failed to authenticate with ws gateway");
                }
                Log.Trace("Successful wss auth");
            }
            else if (raw.@event == "info" && (raw.code == "20051" || raw.code == "20060"))
            {
                this.Unsubscribe(null, null);
                this.Disconnect();
                Reconnect();
            }
            else if (raw.channel == "ticker" && raw.@event == "subscribed")
            {
                if (!this._channelId.ContainsKey((int)raw.chanId))
                {
                    this._channelId[(int)raw.chanId] = "ticker";
                }
            }
            Log.Trace(e.Data);
        }


        private void PopulateTicker(string response)
        {
            var data = JsonConvert.DeserializeObject<string[]>(response);
            var msg = new TickerMessage(data);
            lock (_ticks)
            {
                _ticks.Add(new Tick
                {
                    AskPrice = msg.ASK / divisor,
                    BidPrice = msg.BID / divisor,
                    AskSize = (long)Math.Round(msg.ASK_SIZE * divisor, 0),
                    BidSize = (long)Math.Round(msg.BID_SIZE * divisor, 0),
                    Time = DateTime.UtcNow,
                    Value = msg.LAST_PRICE / divisor,
                    TickType = TickType.Quote,
                    Symbol = symbol.Value,
                    DataType = MarketDataType.Tick,
                    Quantity = (int)(Math.Round(msg.VOLUME, 2) * divisor)
                });
            }
        }

        //todo: Currently data is not used
        private void PopulateWallet(string[][] data)
        {
            if (data.Length > 0)
            {
                lock (_cash)
                {
                    _cash.Clear();
                    for (int i = 0; i < data.Length; i++)
                    {
                        var msg = new WalletMessage(data[i]);
                        _cash.Add(new Securities.Cash(msg.GetString("WLT_CURRENCY"), msg.GetDecimal("WLT_BALANCE"), 1));
                    }
                }
            }
        }

        private void PopulateTrade(string[] data)
        {
            var msg = new TradeMessage(data);
            int brokerId = msg.TRD_ORD_ID;
            var cached = cachedOrderIDs.Where(o => o.Value.BrokerId.Contains(brokerId.ToString()));
            //todo: handle partial fill
            if (cached.Count() > 0 && cached.First().Value != null)
            {
                var fill = new OrderEvent
                (
                    cached.First().Key, symbol, msg.TRD_TIMESTAMP, MapOrderStatus(msg),
                    msg.TRD_AMOUNT_EXECUTED > 0 ? OrderDirection.Buy : OrderDirection.Sell,
                    msg.TRD_PRICE_EXECUTED / divisor, (int)(msg.TRD_AMOUNT_EXECUTED * divisor),
                    msg.FEE, "Bitfinex Fill Event"
                );

                filledOrderIDs.Add(cached.First().Key);
                //todo: remove from cache?
                if (fill.Status == OrderStatus.Filled)
                {
                    BitfinexOrder outOrder = cached.First().Value;
                    cachedOrderIDs.TryRemove(cached.First().Key, out outOrder);
                }
                OnOrderEvent(fill);
            }
            else
            {
                unknownOrderIDs.Add(brokerId);
            }
        }

        private void Authenticate()
        {
            string key = apiKey;
            string payload = "AUTH" + DateTime.UtcNow.Ticks.ToString();
            _ws.Send(JsonConvert.SerializeObject(new
            {
                @event = "auth",
                apiKey = key,
                authSig = GetHexHashSignature(payload, apiSecret),
                authPayload = payload
            }));
        }

        private void UnAuthenticate()
        {
            _ws.Send(JsonConvert.SerializeObject(new
            {
                @event = "unauth"
            }));
            _ws.Close();
        }

    }
}
