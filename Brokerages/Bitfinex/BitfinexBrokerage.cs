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
        protected decimal ScaleFactor = 100;
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
        #endregion

        /// <summary>
        /// Create bitfinex brokerage
        /// </summary>
        public BitfinexBrokerage(string apiKey, string apiSecret, string wallet, BitfinexApi restClient)
            : base("bitfinex")
        {
            ApiKey = apiKey;
            ApiSecret = apiSecret;
            Wallet = wallet;
            _restClient = restClient;
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
            var newOrder = new BitfinexNewOrderPost
            {
                Amount = ((order.Quantity < 0 ? order.Quantity * -1 : order.Quantity) / ScaleFactor).ToString(),
                Price = GetPrice(order).ToString(),
                Symbol = order.Symbol.Value,
                Type = MapOrderType(order.Type),
                Exchange = _exchange,
                Side = order.Quantity > 0 ? buy : sell
            };

            var response = _restClient.SendOrder(newOrder);

            if (response != null)
            {
                if (response.OrderId != 0)
                {
                    UpdateCachedOpenOrder(order.Id, new BitfinexOrder
                    {
                        Id = order.Id,
                        BrokerId = new List<string> { response.OrderId.ToString() },
                        Price = order.Price / ScaleFactor,
                        Quantity = order.Quantity * (int)ScaleFactor,
                        Status = OrderStatus.Submitted,
                        Symbol = order.Symbol,
                        Time = order.Time,
                    });

                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Bitfinex Order Event") { Status = OrderStatus.Submitted });
                    Log.Trace("Order completed successfully orderid:" + response.OrderId.ToString());
                    return true;
                }
            }

            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Bitfinex Order Event") { Status = OrderStatus.Invalid });
            Log.Trace("Order failed Order Id: " + order.Id + " timestamp:" + order.Time + " quantity: " + order.Quantity.ToString());
            return false;
        }

        /// <summary>
        /// Update an existing order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool UpdateOrder(Orders.Order order)
        {

            bool hasFaulted = false;
            foreach (string id in order.BrokerId)
            {
                var post = new BitfinexCancelReplacePost
                {
                    Amount = (order.Quantity / ScaleFactor).ToString(),
                    CancelOrderId = int.Parse(id),
                    Symbol = order.Symbol.Value,
                    Price = order.Price <= 0 ? order.Id.ToString() : (order.Price * ScaleFactor).ToString(),
                    Type = MapOrderType(order.Type),
                    Exchange = _exchange,
                    Side = order.Quantity > 0 ? buy : sell
                };
                var response = _restClient.CancelReplaceOrder(post);
                if (response.OrderId == 0)
                {
                    hasFaulted = true;
                    break;
                }
            }

            if (hasFaulted)
            {
                return false;
            }

            UpdateCachedOpenOrder(order.Id, order);
            return true;
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
                Log.Trace("CancelOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity);

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

        private List<BitfinexOrder> GetOpenBitfinexOrders()
        {
            var list = new List<BitfinexOrder>();

            try
            {
                var response = _restClient.GetActiveOrders();
                if (response != null)
                {
                    foreach (var item in response)
                    {
                        list.Add(new BitfinexOrder
                        {
                            Quantity = Convert.ToInt32(decimal.Parse(item.OriginalAmount) * ScaleFactor),
                            BrokerId = new List<string> { item.Id.ToString() },
                            Symbol = Symbol,
                            Time = Time.UnixTimeStampToDateTime(double.Parse(item.Timestamp)),
                            Price = decimal.Parse(item.Price) / ScaleFactor,
                            Status = MapOrderStatus(item),
                            OriginalAmount = decimal.Parse(item.OriginalAmount) * ScaleFactor,
                            RemainingAmount = decimal.Parse(item.RemainingAmount) * ScaleFactor,
                            ExecutedAmount = decimal.Parse(item.ExecutedAmount) * ScaleFactor
                        });
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

            foreach (var item in list)
            {
                if (item.Status != OrderStatus.Canceled && item.Status != OrderStatus.Filled && item.Status != OrderStatus.Invalid)
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
                    CurrencySymbol = "B",
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
        /// Begin ticker polling
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public virtual void Subscribe(Packets.LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            var task = Task.Run(() => { this.RequestTicker(); }, _tickerToken.Token);
        }

        private void RequestTicker()
        {
            var response = _restClient.GetPublicTicker(TradingApi.ModelObjects.BtcInfo.PairTypeEnum.btcusd, TradingApi.ModelObjects.BtcInfo.BitfinexUnauthenicatedCallsEnum.pubticker);
            lock (Ticks)
            {
                Ticks.Add(new Tick
                {
                    AskPrice = decimal.Parse(response.Ask) / ScaleFactor,
                    BidPrice = decimal.Parse(response.Bid) / ScaleFactor,
                    Time = Time.UnixTimeStampToDateTime(double.Parse(response.Timestamp)),
                    Value = decimal.Parse(response.LastPrice) / ScaleFactor,
                    TickType = TickType.Quote,
                    Symbol = Symbol,
                    DataType = MarketDataType.Tick,
                    Quantity = (int)(Math.Round(decimal.Parse(response.Volume), 2) * ScaleFactor)
                });
            }
            if (!_tickerToken.IsCancellationRequested)
            {
                Thread.Sleep(8000);
                RequestTicker();
            }
        }

        /// <summary>
        /// End ticker polling
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public virtual void Unsubscribe(Packets.LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            _tickerToken.Cancel();
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

    }

}
