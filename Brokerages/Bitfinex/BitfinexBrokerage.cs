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
    /// Bitfinex REST integration.
    /// </summary>
    public partial class BitfinexBrokerage : Brokerage, IDataQueueHandler
    {

        #region Declarations
        /// <summary>
        /// Ticks collection
        /// </summary>
        protected List<Tick> ticks = new List<Tick>();
        TradingApi.Bitfinex.BitfinexApi _client;
        CancellationTokenSource _tickerToken;
        /// <summary>
        /// Divisor for prices. Scales prices/volumes to allow trades on 0.01 of unit
        /// </summary>
        protected decimal divisor = 100;
        readonly object _fillLock = new object();
        const string buy = "buy";
        const string sell = "sell";
        /// <summary>
        /// Currently limited to BTCUSD
        /// </summary>
        protected Symbol symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.Bitcoin);
        /// <summary>
        /// List of known orders
        /// </summary>
        protected ConcurrentDictionary<int, BitfinexOrder> cachedOrderIDs = new ConcurrentDictionary<int, BitfinexOrder>();
        /// <summary>
        /// List of filled orders
        /// </summary>
        protected readonly FixedSizeHashQueue<int> filledOrderIDs = new FixedSizeHashQueue<int>(10000);       
        //todo: record fills when none found
        /// <summary>
        /// List of unknown orders
        /// </summary>
        protected readonly FixedSizeHashQueue<int> unknownOrderIDs = new FixedSizeHashQueue<int>(1000);
        /// <summary>
        /// Name of wallet
        /// </summary>
        protected string wallet;
        const string _exchange = "bitfinex";
        /// <summary>
        /// Api Key
        /// </summary>
        protected string apiKey;
        /// <summary>
        /// Api Secret
        /// </summary>
        protected string apiSecret;
        #endregion

        /// <summary>
        /// Create bitfinex brokerage
        /// </summary>
        public BitfinexBrokerage()
            : base("bitfinex")
        {
            this.Initialize();
        }

        private void Initialize()
        {

            //todo: also stored in BrokerageData
            apiSecret = Config.Get("bitfinex-api-secret");
            apiKey = Config.Get("bitfinex-api-key");

            wallet = Config.Get("bitfinex-wallet", "exchange");

            if (string.IsNullOrEmpty(apiSecret))
                throw new Exception("Missing ApiSecret in App.config");

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Missing ApiKey in App.config");

            _client = new BitfinexApi(apiSecret, apiKey);
            _tickerToken = new CancellationTokenSource();

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
                return ((StopMarketOrder)order).StopPrice * divisor;
            }
            else if (order is LimitOrder)
            {
                return ((LimitOrder)order).LimitPrice * divisor;
            }

            return order.Price <= 0 ? order.Id : (order.Price * divisor);
        }

        /// <summary>
        /// Place a new order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool PlaceOrder(Orders.Order order)
        {
            var newOrder = new BitfinexNewOrderPost
            {
                Amount = ((order.Quantity < 0 ? order.Quantity * -1 : order.Quantity) / divisor).ToString(),
                Price = GetPrice(order).ToString(),
                Symbol = order.Symbol.Value,
                Type = MapOrderType(order.Type),
                Exchange = _exchange,
                Side = order.Quantity > 0 ? buy : sell
            };

            var response = _client.SendOrder(newOrder);

            if (response != null)
            {
                if (response.OrderId != 0)
                {
                    UpdateCachedOpenOrder(order.Id, new BitfinexOrder
                    {
                        Id = order.Id,
                        BrokerId = new List<string> { response.OrderId.ToString() },
                        Price = order.Price / divisor,
                        Quantity = order.Quantity * (int)divisor,
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

            foreach (var id in order.BrokerId)
            {
                var post = new BitfinexCancelReplacePost
                {
                    Amount = (order.Quantity / divisor).ToString(),
                    CancelOrderId = int.Parse(id),
                    Symbol = order.Symbol.Value,
                    Price = order.Price <= 0 ? order.Id.ToString() : (order.Price * divisor).ToString(),
                    Type = MapOrderType(order.Type),
                    Exchange = _exchange,
                    Side = order.Quantity > 0 ? buy : sell
                };
                var response = _client.CancelReplaceOrder(post);
                if (response.Id > 0)
                {
                    UpdateCachedOpenOrder(order.Id, (BitfinexOrder)order);
                }
                else
                {
                    return false;
                }
            }

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
                    var response = _client.CancelOrder(int.Parse(id));
                    if (response.Id > 0)
                    {
                        BitfinexOrder cached;
                        this.cachedOrderIDs.TryRemove(order.Id, out cached);
                        const int orderFee = 0;
                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Bitfinex Fill Event") { Status = OrderStatus.Canceled });
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
        /// REST service does not require manual connection
        /// </summary>
        public override void Connect()
        {

        }

        /// <summary>
        /// Cancel ticker polling
        /// </summary>
        public override void Disconnect()
        {
            this._tickerToken.Cancel();
        }
 
        private List<BitfinexOrder> GetOpenBitfinexOrders()
        {
            var list = new List<BitfinexOrder>();

            try
            {
                var response = _client.GetActiveOrders();
                if (response != null)
                {
                    foreach (var item in response)
                    {
                        symbol = Symbol.Create(item.Symbol, SecurityType.Forex, Market.Bitcoin);
                        list.Add(new BitfinexOrder
                        {
                            Quantity = Convert.ToInt32(decimal.Parse(item.OriginalAmount) * divisor),
                            BrokerId = new List<string> { item.Id.ToString() },
                            Symbol = symbol,
                            Time = Time.UnixTimeStampToDateTime(double.Parse(item.Timestamp)),
                            Price = decimal.Parse(item.Price) / divisor,
                            Status = MapOrderStatus(item),
                            OriginalAmount = decimal.Parse(item.OriginalAmount) * divisor,
                            RemainingAmount = decimal.Parse(item.RemainingAmount) * divisor,
                            ExecutedAmount = decimal.Parse(item.ExecutedAmount) * divisor
                        });
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            //todo: find order id then update local cache
            //foreach (var openOrder in list)
            //{              
                //UpdateCachedOpenOrder(openOrder.Id, openOrder);
            //}

            return list;
        }

        /// <summary>
        /// Retreive orders from exchange
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {

            var list = this.GetOpenBitfinexOrders().Select(o => (Order)o).ToList();
            return list;
        }

        /// <summary>
        /// Retreive holding from exchange
        /// </summary>
        /// <returns></returns>
        public override List<Holding> GetAccountHoldings()
        {

            var list = new List<Holding>();

            var response = _client.GetActivePositions();
            foreach (var item in response)
            {
                var ticker = _client.GetPublicTicker(TradingApi.ModelObjects.BtcInfo.PairTypeEnum.btcusd, TradingApi.ModelObjects.BtcInfo.BitfinexUnauthenicatedCallsEnum.pubticker);
                list.Add(new Holding
                {
                    Symbol = Symbol.Create(item.Symbol, SecurityType.Forex, Market.Bitcoin.ToString()),
                    Quantity = decimal.Parse(item.Amount) * divisor,
                    Type = SecurityType.Forex,
                    CurrencySymbol = "B",
                    ConversionRate = (decimal.Parse(ticker.Mid) / divisor),
                    MarketPrice = (decimal.Parse(ticker.Mid) / divisor),
                    AveragePrice = (decimal.Parse(item.Base) / divisor),
                });
            }
            return list;
        }

        
        /// <summary>
        /// Get Cash Balances
        /// </summary>
        /// <returns></returns>
        //todo: handle other currencies
        public override List<Securities.Cash> GetCashBalance()
        {
            var list = new List<Securities.Cash>();
            var response = _client.GetBalances();
            foreach (var item in response)
            {
                if (item.Type == wallet)
                {
                    if (item.Currency == "usd")
                    {
                        list.Add(new Securities.Cash(item.Currency, item.Amount, 1));
                    }
                    else
                    {
                        var ticker = _client.GetPublicTicker(TradingApi.ModelObjects.BtcInfo.PairTypeEnum.btcusd, TradingApi.ModelObjects.BtcInfo.BitfinexUnauthenicatedCallsEnum.pubticker);
                        list.Add(new Securities.Cash("BTC", item.Amount * divisor, decimal.Parse(ticker.Mid) / divisor));
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Get tick data
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Data.BaseData> GetNextTicks()
        {
            lock (ticks)
            {
                var copy = ticks.ToArray();
                ticks.Clear();
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
            var response = _client.GetPublicTicker(TradingApi.ModelObjects.BtcInfo.PairTypeEnum.btcusd, TradingApi.ModelObjects.BtcInfo.BitfinexUnauthenicatedCallsEnum.pubticker);
            lock (ticks)
            {
                ticks.Add(new Tick
                {
                    AskPrice = decimal.Parse(response.Ask) / divisor,
                    BidPrice = decimal.Parse(response.Bid) / divisor,
                    Time = Time.UnixTimeStampToDateTime(double.Parse(response.Timestamp)),
                    Value = decimal.Parse(response.LastPrice) / divisor,
                    TickType = TickType.Quote,
                    Symbol = symbol,
                    DataType = MarketDataType.Tick,
                    Quantity = (int)(Math.Round(decimal.Parse(response.Volume), 2) * divisor)
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

        private void UpdateCachedOpenOrder(int key, BitfinexOrder updatedOrder)
        {
            BitfinexOrder cachedOpenOrder;
            if (cachedOrderIDs.TryGetValue(key, out cachedOpenOrder))
            {
                cachedOpenOrder = updatedOrder;
            }
            else
            {
                cachedOrderIDs[key] = updatedOrder;
            }
        }

    }

}
