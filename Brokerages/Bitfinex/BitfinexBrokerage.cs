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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using QuantConnect.Brokerages.Bitfinex.Rest;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;
using RestSharp;
using static QuantConnect.Brokerages.Bitfinex.Rest.Constants;

namespace QuantConnect.Brokerages.Bitfinex
{
    /// <summary>
    /// Bitfinex exchange REST integration.
    /// </summary>
    public partial class BitfinexBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler, IGetTick
    {

        #region Declarations
        private readonly object _fillLock = new object();
        private const string Buy = "buy";
        private const string Sell = "sell";
        private const string ExchangeMarket = "exchange market";
        private const string ExchangeLimit = "exchange limit";
        private const string ExchangeStop = "exchange stop";
        private const string Market = "market";
        private const string Limit = "limit";
        private const string Stop = "stop";
        private const string Usd = "usd";
        private readonly object _lockerConnectionMonitor = new object();
        private DateTime _lastHeartbeatUtcTime = DateTime.UtcNow;
        private readonly IAlgorithm _algorithm;

        private readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            FloatParseHandling = FloatParseHandling.Decimal
        };

        /// <summary>
        /// List of unknown orders
        /// </summary>
        protected readonly FixedSizeHashQueue<string> UnknownOrderIDs = new FixedSizeHashQueue<string>(1000);

        /// <summary>
        /// Stores fill messages
        /// </summary>
        public ConcurrentDictionary<int, BitfinexFill> FillSplit { get; set; }
 
        private enum ChannelCode
        {
            pubticker = 0,
            stats = 1,
            trades = 2
        }

        #endregion

        /// <summary>
        /// Create brokerage instance
        /// </summary>
        /// <param name="url">w</param>
        /// <param name="websocket"></param>
        /// <param name="restClient"></param>
        /// <param name="apiKey"></param>
        /// <param name="apiSecret"></param>
        /// <param name="algorithm"></param>
        public BitfinexBrokerage(string url, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, IAlgorithm algorithm)
            : base(url, websocket, restClient, apiKey, apiSecret, QuantConnect.Market.Bitfinex, "bitfinex")
        {
            WebSocket = websocket;

            _algorithm = algorithm;
            FillSplit = new ConcurrentDictionary<int, BitfinexFill>();

            WebSocket.Open += (s, e) => { Authenticate(); };
        }


        #region IBrokerage

        /// <summary>
        /// Add bitfinex order and prepare for fill message
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool PlaceOrder(Order order)
        {

            LockStream();

            var quantity = order.Quantity;
            FillSplit.TryAdd(order.Id, new BitfinexFill(order));

            var holdingsQuantity = _algorithm.Securities.ContainsKey(order.Symbol) ? _algorithm.Securities[order.Symbol].Holdings.Quantity : 0;
            order.PriceCurrency = order.Symbol.Value.Substring(3, 3);
            Order crossOrder = null;

            if (OrderCrossesZero(order, holdingsQuantity))
            {
                crossOrder = order.Clone();
                //first liquidate holdings
                var firstOrderQuantity = -holdingsQuantity;
                //then keep going with the difference
                var secondOrderQuantity = order.Quantity - firstOrderQuantity;
                crossOrder.Quantity = secondOrderQuantity;
                order.Quantity = firstOrderQuantity;
            }

            var result = PlaceOrder(order, crossOrder);
            order.Quantity = quantity;

            UnlockStream();

            return result;
        }

        private bool PlaceOrder(Order order, Order crossOrder = null)
        {
            var totalQuantity = order.Quantity + (crossOrder != null ? crossOrder.Quantity : 0);

            var newOrder = new PlaceOrderPost
            {
                Amount = Math.Abs(order.Quantity).ToString(),
                Price = GetPrice(order).ToString(),
                Symbol = order.Symbol.Value,
                Type = MapOrderType(order.Type),
                Exchange = BrokerageMarket,
                Side = order.Quantity > 0 ? Buy : Sell
            };

            var response = ExecutePost(NewOrderRequestUrl, newOrder);

            var placing = JsonConvert.DeserializeObject<PlaceOrderResponse>(response.Content);

            if (placing != null && placing.OrderId != 0)
            {
                if (CachedOrderIDs.ContainsKey(order.Id))
                {
                    CachedOrderIDs[order.Id].BrokerId.Add(placing.OrderId.ToString());
                }
                else
                {
                    if (order.Type == OrderType.Market || order.Type == OrderType.Limit || order.Type == OrderType.StopMarket)
                    {
                    }
                    else
                    {
                        throw new Exception("BitfinexBrokerage.PlaceOrder(): Unsupported order type was encountered: " + order.Type);
                    }

                    order.BrokerId = new List<string> { placing.OrderId.ToString() };
                    CachedOrderIDs.TryAdd(order.Id, order);
                }
                if (crossOrder != null && crossOrder.Status != OrderStatus.Submitted)
                {
                    order.Status = OrderStatus.Submitted;
                    //Calling place order recursively, but switching the active order
                    return PlaceOrder(crossOrder, order);
                }

                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Bitfinex Order Event") { Status = OrderStatus.Submitted });
                Log.Trace("BitfinexBrokerage.PlaceOrder(): Order completed successfully orderId:" + order.Id);
            }
            else
            {
                //todo: maybe only secondary of cross order failed and order will partially fill. This will leave us inconsistent
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Bitfinex Order Event") { Status = OrderStatus.Invalid });
                Log.Trace("BitfinexBrokerage.PlaceOrder(): Order failed Order Id: " + order.Id + " timestamp:" + order.Time + " quantity: " + order.Quantity);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Update an existing order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool UpdateOrder(Order order)
        {
            bool cancelled;
            foreach (var id in order.BrokerId)
            {
                cancelled = CancelOrder(order);
                if (!cancelled)
                {
                    return false;
                }
            }
            return PlaceOrder(order);
        }

        /// <summary>
        /// Cancel an existing order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool CancelOrder(Order order)
        {
            try
            {
                Log.Trace("BitfinexBrokerage.CancelOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity);

                foreach (var id in order.BrokerId)
                {
                    var cancelPost = new OrderIdPost
                    {
                        OrderId = Convert.ToInt64(id)
                    };

                    var response = ExecutePost(OrderCancelRequestUrl, cancelPost);
                    var cancelling = JsonConvert.DeserializeObject<OrderStatusResponse>(response.Content);

                    if (cancelling.Id > 0)
                    {
                        Order cached;
                        CachedOrderIDs.TryRemove(order.Id, out cached);

                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Bitfinex Cancel Order Event") { Status = OrderStatus.Canceled });
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception err)
            {
                new BrokerageMessageEvent(BrokerageMessageType.Error, 0, "BitfinexBrokerage.CancelOrder(): OrderID: " + order.Id + " - " + err);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Retreive orders from exchange
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {
            var list = new List<Order>();

            var response = ExecutePost(ActiveOrdersRequestUrl, new PostBase());

            if (response == null || response.Content == "[]" || !response.Content.StartsWith("["))
            {
                return list;
            }

            var getting = JsonConvert.DeserializeObject<OrderStatusResponse[]>(response.Content);

            foreach (var item in getting.Where(g => !g.IsCancelled))
            {
                //do not return open orders for inactive wallet
                if (_algorithm.BrokerageModel.AccountType == AccountType.Cash && !item.Type.StartsWith("exchange"))
                {
                    continue;
                }
                else if (_algorithm.BrokerageModel.AccountType == AccountType.Margin && item.Type.StartsWith("exchange"))
                {
                    continue;
                }

                Order order = null;
                if (item.Type == ExchangeMarket || item.Type == Market)
                {
                    order = new MarketOrder();
                }
                else if (item.Type == ExchangeLimit || item.Type == Limit)
                {
                    order = new LimitOrder
                    {
                        LimitPrice = decimal.Parse(item.Price)
                    };
                }
                else if (item.Type == ExchangeStop || item.Type == Stop)
                {
                    order = new StopMarketOrder
                    {
                        StopPrice = decimal.Parse(item.Price)
                    };
                }
                else
                {
                    new BrokerageMessageEvent(BrokerageMessageType.Error, 0, "BitfinexBrokerage.GetOpenOrders(): Unsupported order type returned from brokerage" + item.Type);
                    continue;
                }

                order.Quantity = decimal.Parse(item.RemainingAmount);
                order.BrokerId = new List<string> { item.Id.ToString() };
                order.Symbol = Symbol.Create(item.Symbol.ToUpper(), SecurityType.Crypto, BrokerageMarket);
                order.Time = Time.UnixTimeStampToDateTime(double.Parse(item.Timestamp));
                order.Price = decimal.Parse(item.Price);
                order.Status = MapOrderStatus(item);
                list.Add(order);
            }


            foreach (var item in list)
            {
                if (item.Status.IsOpen())
                {
                    var cached = CachedOrderIDs.Where(c => c.Value.BrokerId.Contains(item.BrokerId.First()));
                    if (cached.Count() > 0 && cached.First().Value != null)
                    {
                        CachedOrderIDs[cached.First().Key] = item;
                        item.Id = cached.First().Key;
                    }
                    //todo: if there was no cached order and order gets filled. Will tie into an order id
                }
            }
            return list;
        }

        /// <summary>
        /// Retreive holdings from exchange
        /// </summary>
        /// <returns></returns>
        public override List<Holding> GetAccountHoldings()
        {
            return new List<Holding>();
        }

        /// <summary>
        /// Get Cash Balances from exchange
        /// </summary>
        /// <returns></returns>
        public override List<Cash> GetCashBalance()
        {
            try
            {
                var list = new List<Cash>();
                var response = ExecutePost(BalanceRequestUrl, new PostBase());

                var getting = JsonConvert.DeserializeObject<IList<BalanceResponse>>(response.Content);

                foreach (var item in getting)
                {
                    if (item.Type == GetWallet() && item.Amount > 0)
                    {
                        if (item.Currency.Equals(Usd, StringComparison.InvariantCultureIgnoreCase))
                        {
                            list.Add(new Cash(item.Currency.ToUpper(), item.Amount, 1));
                        }
                        else
                        {
                            var baseSymbol = (item.Currency + Usd).ToLower();
                            var ticker = GetTick(baseSymbol);
                            list.Add(new Cash(item.Currency.ToUpper(), item.Amount, ticker.Price));
                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                new BrokerageMessageEvent(BrokerageMessageType.Error, 0, "BitfinexBrokerage.GetCashBalance(): " + ex.ToString());
                return null;
            }
        }

        #endregion

        /// <summary>
        /// Logs out and closes connection
        /// </summary>
        public override void Disconnect()
        {
            UnAuthenticate();
            WebSocket.Close();
        }

        /// <summary>
        /// A hard reset can occur which requires us to re-auth and re-subscribe
        /// </summary>
        protected override void Reconnect()
        {
            var subscribed = GetSubscribed();

            try
            {
                if (WebSocket.IsOpen)
                {
                    // connection is still good
                    LastHeartbeatUtcTime = DateTime.UtcNow;
                    return;
                }
                Log.Trace($"BitfinexBrokerage(): Reconnecting... IsConnected: {IsConnected}");

                //try to clean up state
                try
                {
                    WebSocket.Error -= OnError;
                    UnAuthenticate();
                    Unsubscribe(null, subscribed);
                    if (IsConnected)
                    {
                        WebSocket.Close();
                        Wait(ConnectionTimeout, () => !WebSocket.IsOpen);
                    }
                }
                catch (Exception ex)
                {
                    Log.Trace("BitfinexBrokerage(): Exception encountered cleaning up state.", ex);
                }
                if (!IsConnected)
                {
                    WebSocket.Connect();
                    Wait(ConnectionTimeout, () => WebSocket.IsOpen);
                }
                Log.Trace("BitfinexBrokerage(): Attempting Subscribe");
            }
            catch (Exception ex)
            {
                Log.Trace("Exception encountered reconnecting.", ex);
            }
            finally
            {
                Authenticate();
                WebSocket.Error += OnError;

                Subscribe(null, subscribed);
            }
        }

        private Tick GetTick(string symbol)
        {
            return GetTick(Symbol.Create(symbol, SecurityType.Crypto, BrokerageMarket));
        }

        /// <summary>
        /// Get the latest tick for symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public Tick GetTick(Symbol symbol)
        {
            var url = PubTickerRequestUrl + "/" + symbol.Value.ToLower();
            var response = ExecuteGet(url);

            var data = JsonConvert.DeserializeObject<TickerGet>(response.Content);

            return new Tick
            {
                DataType = MarketDataType.Tick,
                TickType = TickType.Quote,
                Exchange = BrokerageMarket,
                Quantity = data.Volume,
                Time = Time.UnixTimeStampToDateTime(data.Timestamp),
                Value = data.Mid,
                BidPrice = data.Bid,
                AskPrice = data.Ask
            };
        }

        private IRestResponse ExecutePost(string resource, object data)
        {
            if (data.GetType().BaseType == typeof(PostBase) || data.GetType() == typeof(PostBase))
            {
                ((PostBase)data).Nonce = Time.DateTimeToUnixTimeStamp(DateTime.UtcNow).ToString();
                ((PostBase)data).Request = resource;
            }

            var json = JsonConvert.SerializeObject(data);
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            var request = new RestRequest(resource, Method.POST);
            //add auth headers
            request.AddHeader(ApiBfxKey, ApiKey);
            request.AddHeader(ApiBfxPayload, payload);
            request.AddHeader(ApiBfxSig, GetHexHashSignature(payload, ApiSecret));

            var response = RestClient.Execute(request);
            CheckForError(response);

            return response;
        }

        private IRestResponse ExecuteGet(string url)
        {
            try
            {
                var response = RestClient.Execute(new RestRequest(url));
                CheckForError(response);
                return response;
            }
            catch (Exception ex)
            {
                new BrokerageMessageEvent(BrokerageMessageType.Error, 0, "BitfinexBrokerage.ExecuteGet(): " + ex.ToString());
                return null;
            }
        }

        private string GetWallet()
        {
            return _algorithm.BrokerageModel.AccountType == AccountType.Margin ? "trading" : "exchange";
        }

    }
}