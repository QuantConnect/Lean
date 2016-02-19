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
        protected List<Tick> _ticks = new List<Tick>();
        TradingApi.Bitfinex.BitfinexApi _client;
        CancellationTokenSource _tickerToken;
        protected decimal _divisor = 100;
        private readonly object _fillLock = new object();
        const string buy = "buy";
        const string sell = "sell";
        //todo: support other currencies
        protected Symbol _symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.Bitcoin);
        protected ConcurrentDictionary<int, BitfinexOrder> _cachedOrderIDs = new ConcurrentDictionary<int, BitfinexOrder>();
        protected readonly FixedSizeHashQueue<int> _filledOrderIDs = new FixedSizeHashQueue<int>(10000);
        //todo: record fills when none found
        protected readonly FixedSizeHashQueue<int> _unknownOrderIDs = new FixedSizeHashQueue<int>(1000);
        protected string _wallet;
        const string _exchange = "bitfinex";
        #endregion

        protected string apiKey;
        protected string apiSecret;

        public BitfinexBrokerage()
            : base("bitfinex")
        {
            this.Initialize();
        }

        private void Initialize()
        {
            //todo: use json config
            //apiSecret = ConfigurationManager.AppSettings["ApiSecret"];
            //apiKey = ConfigurationManager.AppSettings["ApiKey"];

            apiSecret = Config.Get("bitfinex-api-secret");
            apiKey = Config.Get("bitfinex-api-key");

            _wallet = Config.Get("bitfinex-wallet", "exchange");

            if (string.IsNullOrEmpty(apiSecret))
                throw new Exception("Missing ApiSecret in App.config");

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Missing ApiKey in App.config");

            _client = new BitfinexApi(apiSecret, apiKey);
            _tickerToken = new CancellationTokenSource();

        }

        public override bool IsConnected
        {
            get { return true; }
        }

        private decimal GetPrice(Order order)
        {
            if (order is StopMarketOrder)
            {
                return ((StopMarketOrder)order).StopPrice * _divisor;
            }
            else if (order is LimitOrder)
            {
                return ((LimitOrder)order).LimitPrice * _divisor;
            }

            return order.Price <= 0 ? order.Id : (order.Price * _divisor);
        }

        public override bool PlaceOrder(Orders.Order order)
        {
            var newOrder = new BitfinexNewOrderPost
            {
                Amount = ((order.Quantity < 0 ? order.Quantity * -1 : order.Quantity) / _divisor).ToString(),
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
                        Price = order.Price / _divisor,
                        Quantity = order.Quantity * (int)_divisor,
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

        public override bool UpdateOrder(Orders.Order order)
        {

            foreach (var id in order.BrokerId)
            {
                var post = new BitfinexCancelReplacePost
                {
                    Amount = (order.Quantity / _divisor).ToString(),
                    CancelOrderId = int.Parse(id),
                    Symbol = order.Symbol.Value,
                    Price = order.Price <= 0 ? order.Id.ToString() : (order.Price * _divisor).ToString(),
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
                        this._cachedOrderIDs.TryRemove(order.Id, out cached);
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

        public override void Connect()
        {

        }

        public override void Disconnect()
        {
            this._tickerToken.Cancel();
        }

        public List<BitfinexOrder> GetOpenBitfinexOrders()
        {
            var list = new List<BitfinexOrder>();

            try
            {
                var response = _client.GetActiveOrders();
                if (response != null)
                {
                    foreach (var item in response)
                    {
                        list.Add(new BitfinexOrder
                        {
                            Quantity = Convert.ToInt32(decimal.Parse(item.OriginalAmount) * _divisor),
                            BrokerId = new List<string> { item.Id.ToString() },
                            Symbol = item.Symbol,
                            Time = Time.UnixTimeStampToDateTime(double.Parse(item.Timestamp)),
                            Price = decimal.Parse(item.Price) / _divisor,
                            Status = MapOrderStatus(item),
                            OriginalAmount = decimal.Parse(item.OriginalAmount) * _divisor,
                            RemainingAmount = decimal.Parse(item.RemainingAmount) * _divisor,
                            ExecutedAmount = decimal.Parse(item.ExecutedAmount) * _divisor
                        });
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            foreach (var openOrder in list)
            {
                //todo: find order id then update local cache
                //UpdateCachedOpenOrder(openOrder.Id, openOrder);
            }

            return list;
        }

        public override List<Order> GetOpenOrders()
        {

            var list = this.GetOpenBitfinexOrders().Select(o => (Order)o).ToList();
            return list;
        }

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
                    Quantity = decimal.Parse(item.Amount) * _divisor,
                    Type = SecurityType.Forex,
                    CurrencySymbol = "B",
                    ConversionRate = (decimal.Parse(ticker.Mid) / _divisor),
                    MarketPrice = (decimal.Parse(ticker.Mid) / _divisor),
                    AveragePrice = (decimal.Parse(item.Base) / _divisor),
                });
            }
            return list;
        }

        //todo: handle other currencies
        public override List<Securities.Cash> GetCashBalance()
        {
            var list = new List<Securities.Cash>();
            var response = _client.GetBalances();
            foreach (var item in response)
            {
                if (item.Type == _wallet)
                {
                    if (item.Currency == "usd")
                    {
                        list.Add(new Securities.Cash(item.Currency, item.Amount, 1));
                    }
                    else
                    {
                        var ticker = _client.GetPublicTicker(TradingApi.ModelObjects.BtcInfo.PairTypeEnum.btcusd, TradingApi.ModelObjects.BtcInfo.BitfinexUnauthenicatedCallsEnum.pubticker);
                        list.Add(new Securities.Cash("BTC", item.Amount * _divisor, decimal.Parse(ticker.Mid) / _divisor));
                    }
                }
            }
            return list;
        }

        public IEnumerable<Data.BaseData> GetNextTicks()
        {
            lock (_ticks)
            {
                var copy = _ticks.ToArray();
                _ticks.Clear();
                return copy;
            }
        }

        public void Subscribe(Packets.LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            var task = Task.Run(() => { this.RequestTicker(); }, _tickerToken.Token);
        }

        private void RequestTicker()
        {
            var response = _client.GetPublicTicker(TradingApi.ModelObjects.BtcInfo.PairTypeEnum.btcusd, TradingApi.ModelObjects.BtcInfo.BitfinexUnauthenicatedCallsEnum.pubticker);
            lock (_ticks)
            {
                _ticks.Add(new Tick
                {
                    AskPrice = decimal.Parse(response.Ask) / _divisor,
                    BidPrice = decimal.Parse(response.Bid) / _divisor,
                    Time = Time.UnixTimeStampToDateTime(double.Parse(response.Timestamp)),
                    Value = decimal.Parse(response.LastPrice) / _divisor,
                    TickType = TickType.Quote,
                    Symbol = _symbol,
                    DataType = MarketDataType.Tick,
                    Quantity = (int)(Math.Round(decimal.Parse(response.Volume), 2) * _divisor)
                });
            }
            if (!_tickerToken.IsCancellationRequested)
            {
                Thread.Sleep(8000);
                RequestTicker();
            }
        }

        public void Unsubscribe(Packets.LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            _tickerToken.Cancel();
        }

        private void UpdateCachedOpenOrder(int key, BitfinexOrder updatedOrder)
        {
            BitfinexOrder cachedOpenOrder;
            if (_cachedOrderIDs.TryGetValue(key, out cachedOpenOrder))
            {
                cachedOpenOrder = updatedOrder;
            }
            else
            {
                _cachedOrderIDs[key] = updatedOrder;
            }
        }

    }

}
