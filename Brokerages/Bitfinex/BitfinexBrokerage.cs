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
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TradingApi.Bitfinex;
using TradingApi.ModelObjects.Bitfinex.Json;

namespace QuantConnect.Brokerages.Bitfinex
{

    /// <summary>
    /// Bitfinex exchange REST integration.
    /// </summary>
    public partial class BitfinexBrokerage : Brokerage, IDataQueueHandler
    {

        #region Declarations
        /// <summary>
        /// Ticks collection
        /// </summary>
        protected List<Tick> Ticks = new List<Tick>();
        CancellationTokenSource _tickerToken;
        /// <summary>
        /// Divisor for prices. Scales prices/volumes to allow trades on 0.01 of unit
        /// </summary>
        protected decimal ScaleFactor = 1;
        readonly object _fillLock = new object();
        const string buy = "buy";
        const string sell = "sell";
        /// <summary>
        /// Currently limited to BTCUSD
        /// </summary>
        protected Symbol Symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.Bitfinex);
        /// <summary>
        /// List of known orders
        /// </summary>
        public ConcurrentDictionary<int, Order> CachedOrderIDs = new ConcurrentDictionary<int, Order>();
        /// <summary>
        /// List of filled orders
        /// </summary>
        protected readonly FixedSizeHashQueue<int> FilledOrderIDs = new FixedSizeHashQueue<int>(10000);
        /// <summary>
        /// List of unknown orders
        /// </summary>
        protected readonly FixedSizeHashQueue<int> UnknownOrderIDs = new FixedSizeHashQueue<int>(1000);
        /// <summary>
        /// Name of wallet
        /// </summary>
        protected string Wallet;
        const string _exchange = "bitfinex";
        /// <summary>
        /// Api Key
        /// </summary>
        protected string ApiKey;
        /// <summary>
        /// Api Secret
        /// </summary>
        protected string ApiSecret;
        TradingApi.Bitfinex.BitfinexApi _restClient;
        /// <summary>
        /// Security Provider
        /// </summary>
        protected ISecurityProvider SecurityProvider;

        const string _exchangeMarket  = "exchange market";
        const string _exchangeLimit = "exchange limit";
        const string _exchangeStop = "exchange stop"  ;     
        const string _market = "market";
        const string _limit = "limit";
        const string _stop = "stop";

        public ConcurrentDictionary<int, BitfinexFill> FillSplit { get; set; }
        #endregion

        /// <summary>
        /// Create bitfinex brokerage
        /// </summary>
        public BitfinexBrokerage(string apiKey, string apiSecret, string wallet, BitfinexApi restClient, decimal scaleFactor, ISecurityProvider securityProvider)
            : base("bitfinex")
        {
            ApiKey = apiKey;
            ApiSecret = apiSecret;
            Wallet = wallet;
            _restClient = restClient;
            ScaleFactor = scaleFactor;
            SecurityProvider = securityProvider;
            FillSplit = new ConcurrentDictionary<int, BitfinexFill>();
        }

        /// <summary>
        /// Determines if ticker polling is active
        /// </summary>
        public override bool IsConnected
        {
            get { return this._tickerToken != null && !this._tickerToken.IsCancellationRequested; }
        }

        private decimal GetPrice(Order order)
        {
            if (order is StopMarketOrder)
            {
                return ((StopMarketOrder)order).StopPrice * ScaleFactor;
            }
            else if (order is LimitOrder)
            {
                return ((LimitOrder)order).LimitPrice * ScaleFactor;
            }

            return order.Price <= 0 ? order.Id : (order.Price * ScaleFactor);
        }

        /// <summary>
        /// Place a new order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool PlaceOrder(Orders.Order order)
        {
            //todo: wait for callback from auth before posting
            Authenticate();

            int quantity = (int)Math.Floor(SecurityProvider.GetHoldingsQuantity(order.Symbol));
            Orders.Order crossOrder = null;
            if (OrderCrossesZero(order, quantity))
            {
                crossOrder = order.Clone();
                var firstOrderQuantity = -quantity;
                var secondOrderQuantity = order.Quantity - firstOrderQuantity;
                crossOrder.Quantity = secondOrderQuantity;
                order.Quantity = firstOrderQuantity;
            }

            return this.PlaceOrder(order, crossOrder);
        }

        private bool PlaceOrder(Orders.Order order, Orders.Order crossOrder = null)
        {

            int totalQuantity = order.Quantity + (crossOrder != null ? crossOrder.Quantity : 0);

            var newOrder = new BitfinexNewOrderPost
            {
                Amount = (Math.Abs(order.Quantity) / ScaleFactor).ToString(),
                Price = GetPrice(order).ToString(),
                Symbol = order.Symbol.Value,
                Type = MapOrderType(order.Type),
                Exchange = _exchange,
                Side = order.Quantity > 0 ? buy : sell
            };

            var response = _restClient.SendOrder(newOrder);

            if (response != null && response.OrderId != 0)
            {
                if (CachedOrderIDs.ContainsKey(order.Id))
                {
                    CachedOrderIDs[order.Id].BrokerId.Add(response.OrderId.ToString());
                }
                else
                {
                    Order caching = null;
                    if (order.Type == OrderType.Market)
                    {
                        caching = new MarketOrder();
                    }
                    else if (order.Type == OrderType.Limit)
                    {
                        caching = new LimitOrder();
                    }
                    else if (order.Type == OrderType.StopMarket)
                    {
                        caching = new StopMarketOrder();
                    }
                    else
                    {
                        throw new Exception("BitfinexBrokerage.PlaceOrder(): Unsupported order type was encountered: " + order.Type.ToString());
                    }

                    caching.Id = order.Id;
                    caching.BrokerId = new List<string> { response.OrderId.ToString() };
                    caching.Price = order.Price / ScaleFactor;
                    caching.Quantity = totalQuantity * (int)ScaleFactor;
                    caching.Status = OrderStatus.Submitted;
                    caching.Symbol = order.Symbol;
                    caching.Time = order.Time;

                    CachedOrderIDs.TryAdd(order.Id, caching);
                }
                if (crossOrder != null && crossOrder.Status != OrderStatus.Submitted)
                {
                    order.Status = OrderStatus.Submitted;
                    //switching active order
                    return PlaceOrder(crossOrder, order);
                }

                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Bitfinex Order Event") { Status = OrderStatus.Submitted });
                Log.Trace("BitfinexBrokerage.PlaceOrder(): Order completed successfully orderid:" + order.Id.ToString());
            }
            else
            {
                //todo: maybe only secondary of cross order failed and order will partially fill.
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Bitfinex Order Event") { Status = OrderStatus.Invalid });
                Log.Trace("BitfinexBrokerage.PlaceOrder(): Order failed Order Id: " + order.Id + " timestamp:" + order.Time + " quantity: " + order.Quantity.ToString());
                return false;
            }
            return true;
        }

        /// <summary>
        /// Update an existing order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool UpdateOrder(Orders.Order order)
        {
            bool cancelled;
            foreach (string id in order.BrokerId)
            {
                cancelled = this.CancelOrder(order);
                if (!cancelled)
                {
                    return false;
                }

            }
            return this.PlaceOrder(order);
        }

        /// <summary>
        /// Cancel an existing order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool CancelOrder(Orders.Order order)
        {
            try
            {
                Log.Trace("BitfinexBrokerage.CancelOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity);

                foreach (var id in order.BrokerId)
                {
                    var response = _restClient.CancelOrder(int.Parse(id));
                    if (response.Id > 0)
                    {
                        Order cached;
                        this.CachedOrderIDs.TryRemove(order.Id, out cached);
                        const int orderFee = 0;
                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Bitfinex Cancel Order Event") { Status = OrderStatus.Canceled });
                    }
                    else
                    {
                        return false;
                    }

                }
            }
            catch (Exception err)
            {
                Log.Error("CancelOrder(): OrderID: " + order.Id + " - " + err);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Setup ticker polling
        /// </summary>
        public override void Connect()
        {
            _tickerToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Cancel ticker polling
        /// </summary>
        public override void Disconnect()
        {
            if (this._tickerToken != null)
            {
                this._tickerToken.Cancel();
            }
        }

        private List<Order> GetOpenBitfinexOrders()
        {
            var list = new List<Order>();

            try
            {
                var response = _restClient.GetActiveOrders();
                if (response != null)
                {
                    foreach (var item in response)
                    {
                        Order order = null;
                        if (item.Type == _exchangeMarket || item.Type == _market)
                        {
                            order = new MarketOrder();
                        }
                        else if (item.Type == _exchangeLimit || item.Type == _limit)
                        {
                            order = new LimitOrder
                            {
                                LimitPrice = decimal.Parse(item.Price) / ScaleFactor
                            };
                        }
                        else if (item.Type == _exchangeStop || item.Type == _stop)
                        {
                            order = new StopMarketOrder
                            {
                                StopPrice = decimal.Parse(item.Price) / ScaleFactor
                            };
                        }
                        else
                        {
                            Log.Error("BitfinexBrokerage.GetOpenBitfinexOrders(): Unsupported order type returned from brokerage" + item.Type);
                            continue;
                        }

                        order.Quantity = Convert.ToInt32(decimal.Parse(item.OriginalAmount) * ScaleFactor);
                        order.BrokerId = new List<string> { item.Id.ToString() };
                        order.Symbol = Symbol;
                        order.Time = Time.UnixTimeStampToDateTime(double.Parse(item.Timestamp));
                        order.Price = decimal.Parse(item.Price) / ScaleFactor;
                        order.Status = MapOrderStatus(item);
                        list.Add(order);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return list;
        }

        /// <summary>
        /// Retreive orders from exchange
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {

            var list = this.GetOpenBitfinexOrders().Select(o => (Order)o).ToList();

            foreach (Order item in list)
            {
                if (item.Status.IsOpen())
                {
                    var cached = this.CachedOrderIDs.Where(c => c.Value.BrokerId.Contains(item.BrokerId.First()));
                    if (cached.Count() > 0 && cached.First().Value != null)
                    {
                        this.CachedOrderIDs[cached.First().Key] = item;
                    }
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

            var list = new List<Holding>();

            var response = _restClient.GetActivePositions();
            foreach (var item in response)
            {
                var ticker = _restClient.GetPublicTicker(TradingApi.ModelObjects.BtcInfo.PairTypeEnum.btcusd, TradingApi.ModelObjects.BtcInfo.BitfinexUnauthenicatedCallsEnum.pubticker);
                list.Add(new Holding
                {
                    Symbol = Symbol.Create(item.Symbol, SecurityType.Forex, Market.Bitfinex),
                    Quantity = decimal.Parse(item.Amount) * ScaleFactor,
                    Type = SecurityType.Forex,
                    CurrencySymbol = "Ƀ",
                    ConversionRate = (decimal.Parse(ticker.Mid) / ScaleFactor),
                    MarketPrice = (decimal.Parse(ticker.Mid) / ScaleFactor),
                    AveragePrice = (decimal.Parse(item.Base) / ScaleFactor),
                });
            }
            return list;
        }


        /// <summary>
        /// Get Cash Balances from exchange
        /// </summary>
        /// <returns></returns>
        //todo: handle other currencies
        public override List<Securities.Cash> GetCashBalance()
        {
            var list = new List<Securities.Cash>();
            var response = _restClient.GetBalances();
            foreach (var item in response)
            {
                if (item.Type == Wallet)
                {
                    if (item.Currency == "usd")
                    {
                        list.Add(new Securities.Cash(item.Currency, item.Amount, 1));
                    }
                    else
                    {
                        var ticker = _restClient.GetPublicTicker(TradingApi.ModelObjects.BtcInfo.PairTypeEnum.btcusd, TradingApi.ModelObjects.BtcInfo.BitfinexUnauthenicatedCallsEnum.pubticker);
                        list.Add(new Securities.Cash("BTC", item.Amount * ScaleFactor, decimal.Parse(ticker.Mid) / ScaleFactor));
                    }
                }
            }
            return list;
        }

        private void UpdateCachedOpenOrder(int key, Order updatedOrder)
        {
            Order cachedOpenOrder;
            if (CachedOrderIDs.TryGetValue(key, out cachedOpenOrder))
            {
                cachedOpenOrder = updatedOrder;
            }
            else
            {
                CachedOrderIDs[key] = updatedOrder;
            }
        }

        /// <summary>
        /// Provided for derived classes
        /// </summary>
        protected virtual void Authenticate()
        { }

        /// <summary>
        /// Determines whether or not the specified order will bring us across the zero line for holdings
        /// </summary>
        protected bool OrderCrossesZero(Order order, decimal quantity)
        {
            if (quantity > 0 && order.Quantity < 0)
            {
                return (quantity + order.Quantity) < 0;
            }
            else if (quantity < 0 && order.Quantity > 0)
            {
                return (quantity + order.Quantity) > 0;
            }
            return false;
        }


    }

}
