/*
Copyright(c) 2016 Markus Trenkwalder

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using QuantConnect.Orders;

using HistoryRequest = QuantConnect.Data.HistoryRequest;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Kraken {
    using DataType;

    public class KrakenApi : Brokerage, IDataQueueHandler {

        KrakenRestApi _restApi;
        
        private static readonly TimeSpan SubscribeDelay = TimeSpan.FromMilliseconds(250);
        private DateTime _lastSubscribeRequestUtcTime = DateTime.MinValue;
        private bool _subscriptionsPending;


        private bool _isConnected;
        private Thread _connectionMonitorThread;
        private volatile bool _connectionLost;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The UTC time of the last received heartbeat message
        /// </summary>
        protected DateTime LastHeartbeatUtcTime;
        
        /// <summary>
        /// A lock object used to synchronize access to LastHeartbeatUtcTime
        /// </summary>
        protected readonly object LockerConnectionMonitor = new object();

        /// <summary>
        /// The list of ticks received
        /// </summary>
        protected readonly List<Tick> Ticks = new List<Tick>();

        /// <summary>
        /// The list of currently subscribed symbols
        /// </summary>
        protected HashSet<Symbol> SubscribedSymbols = new HashSet<Symbol>();

        /// <summary>
        /// A lock object used to synchronize access to subscribed symbols
        /// </summary>
        protected readonly object LockerSubscriptions = new object();

        /// <summary>
        /// The symbol mapper
        /// </summary>
        protected KrakenSymbolMapper SymbolMapper;

        
        /// <summary>
        /// Initializes a new instance of the <see cref="Kraken"/> class.
        /// </summary>
        /// <param name="key">The API key.</param>
        /// <param name="secret">The API secret.</param>
        /// <param name="rateLimitMilliseconds">The rate limit in milliseconds.</param>
        public KrakenApi(KrakenSymbolMapper symbolMapper, string key, string secret, int rateLimitMilliseconds = 5000)
            : base("Kraken Brokerage") {
            
            _restApi = new KrakenRestApi(key, secret, rateLimitMilliseconds);

            this.SymbolMapper = symbolMapper;

            // symbolMapper.UpdateSymbols(_restApi);
        }

        #region IDataQueueHandler

        StringBuilder tickerStringbuilder = new StringBuilder();
        //! IMPLEMENT IDATAQUEUEHANDLER methods
        public IEnumerable<BaseData> GetNextTicks() {
        
            tickerStringbuilder.Clear();

            foreach(Symbol symbol in SubscribedSymbols) {

                string krakenSymbol = SymbolMapper.GetBrokerageSymbol(symbol);

                tickerStringbuilder.Append(krakenSymbol);
                tickerStringbuilder.Append(", ");
            }

            Dictionary<string, Ticker> ticks = _restApi.GetTicker(tickerStringbuilder.ToString());
            
            foreach(KeyValuePair<string, Ticker> pair in ticks)
                yield return KrakenTickToLeanTick(pair);
            
        }

        public Tick GetTick(Symbol symbol) {

            string krakenSymbol = SymbolMapper.GetBrokerageSymbol(symbol);

            Dictionary<string, Ticker> ticks = _restApi.GetTicker(krakenSymbol);

            Ticker krakenTick = ticks[krakenSymbol];

            return KrakenTickToLeanTick(new KeyValuePair<string, Ticker>(krakenSymbol, krakenTick));
        }

        Tick KrakenTickToLeanTick(KeyValuePair<string, Ticker> pair) {

            Symbol symbol = SymbolMapper.GetLeanSymbol(pair.Key, SecurityType.Crypto, Market.Kraken);

            Ticker krakenTick = pair.Value;

            //!+ IZVEDI KAJ POMENI RAZLIKA MED WHOLE LOT IN LOT !!

            QuantConnect.Data.Market.Tick leanTick = new Tick();

            leanTick.Symbol = symbol;
            leanTick.Time = DateTime.UtcNow;

            leanTick.BidPrice = krakenTick.Bid[0];
            leanTick.BidSize = krakenTick.Bid[1];
            leanTick.AskPrice = krakenTick.Ask[0];
            leanTick.AskSize = krakenTick.Ask[1];

            leanTick.Exchange = Market.Kraken;

            leanTick.Value = krakenTick.Closed[0];
            leanTick.Quantity = krakenTick.Closed[1];
            leanTick.DataType = MarketDataType.Tick;

            return leanTick;
            
        }

        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols) {

            foreach(Symbol symbol in symbols)
                SubscribedSymbols.Add(symbol);
        }

        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols) {

            foreach (Symbol symbol in symbols)
                SubscribedSymbols.Remove(symbol);
        }

        #endregion

        #region Brokerage
        #region BrokerageEvents
        /// <summary>
        /// Event that fires each time an order is filled
        /// </summary>
        public event EventHandler<OrderEvent> OrderStatusChanged;

        /// <summary>
        /// Event that fires each time a short option position is assigned
        /// </summary>
        public event EventHandler<OrderEvent> OptionPositionAssigned;

        /// <summary>
        /// Event that fires each time a user's brokerage account is changed
        /// </summary>
        public event EventHandler<AccountEvent> AccountChanged;

        /// <summary>
        /// Event that fires when an error is encountered in the brokerage
        /// </summary>
        public event EventHandler<BrokerageMessageEvent> Message;
        #endregion
        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected { get; }

        #region TRANSLATORS
        private string TranslateDirectionToKraken(OrderDirection direction) {

            if (direction == OrderDirection.Buy)
                return "buy";

            if (direction == OrderDirection.Sell)
                return "sell";

            throw new KrakenException("Can't \"hold\" an order on Kraken!");
        }

        private string TranslateOrderTypeToKraken(OrderType orderType)
        {

            switch(orderType) {

                case OrderType.Limit:
                    return "limit";
                
                case OrderType.Market:
                    return "market";
                
                case OrderType.StopLimit:
                    break; // return "stop-loss-limit";
                
                case OrderType.StopMarket:
                    break; // return "stop-loss-"
                
            }

            throw new KrakenException("Unsupported order type");
        }

        private OrderType TranslateOrderTypeToLean(string type) {

            switch(type) {

                case "limit":
                    return OrderType.Limit;

                case "market":
                    return OrderType.Market;
            }

            throw new KrakenException("Unsupported order type");
        }


        int ResolutionToInterval(Resolution res) {

            switch (res) {

                case Resolution.Daily:
                    return 1440;

                case Resolution.Hour:
                    return 60;

                case Resolution.Minute:
                    return 1;


                case Resolution.Second:
                case Resolution.Tick:
                default:
                    throw new KrakenException("This kind of res. not supported)");
            }
        }

        DateTime FromUnix(long unixTime) {
            return DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
        }

        long ToUnix(DateTime dateTime) {
            return ((DateTimeOffset) dateTime).ToUnixTimeSeconds();
        }

        #endregion
        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order) {

            KrakenOrder krakenOrder = new KrakenOrder();

            krakenOrder.Pair = SymbolMapper.GetBrokerageSymbol(order.Symbol);

            // buy/sell
            krakenOrder.Type = TranslateDirectionToKraken(order.Direction);
            krakenOrder.OrderType = TranslateOrderTypeToKraken(order.Type);
            krakenOrder.Volume = order.AbsoluteQuantity;

            if(order.Type == OrderType.Limit)
                krakenOrder.Price = order.Price;

            // krakenOrder.Leverage = ?

            var result = _restApi.AddOrder(krakenOrder);

            if(result.Txid != null & result.Txid.Length != 0) {

                order.BrokerId.AddRange(result.Txid);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order) {
        
            //! UPDATE ORDER           
            //todo: maybe make it so, that this broker doesn't support updating orders (because it doesnt)
            bool success = false;
            
            if(CancelOrder(order)) {
            
                order.BrokerId.Clear();    

                if(PlaceOrder(order)) {

                    return true;
                } else {

                    // try again
                    if(PlaceOrder(order)) {

                        return true;
                    } else {
                        throw new KrakenException("The update failed! Order was canceled but not placed again");
                    }
                }
            }

            return false;
            
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order) {

            int sum = 0;

            foreach(string txid in order.BrokerId) {

                var result = _restApi.CancelOrder(txid);
                sum += result.Count;
            }

            return sum > 0;

        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect() {

            // NOP
            //
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect() {

            // NOP
        }

        /// <summary>
        /// Dispose of the brokerage instance
        /// </summary>
        public override void Dispose()
        {
            
            // NOP
        }

        public DateTime UnixTimeStampToDateTime(double unixTimeStamp) {

            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        /// <summary>
        /// Gets all open orders on the account. 
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from IB</returns>
        public override List<Order> GetOpenOrders() {

            List<Order> list = new List<Order>();

            Dictionary<string, OrderInfo> orders = _restApi.GetOpenOrders();
            
            foreach(KeyValuePair<string, OrderInfo> pair in orders) {

                OrderInfo info = pair.Value;

                OrderDescription desc = info.Descr;

                // check for debug purposes here
                if (pair.Key != desc.Pair)
                    throw new KrakenException("this doesn't match, please inspect!!");

                var SOR = new SubmitOrderRequest(

                    TranslateOrderTypeToLean(info.Descr.OrderType),
                    SecurityType.Crypto,
                    this.SymbolMapper.GetLeanSymbol(desc.Pair, SecurityType.Crypto, Market.Kraken),
                    info.Volume - info.VolumeExecuted,
                    info.StopPrice.HasValue ? info.StopPrice.Value : 0m,
                    info.LimitPrice.HasValue ? info.LimitPrice.Value : 0m,
                    UnixTimeStampToDateTime(info.OpenTm),
                    ""
                );

                var order = Order.CreateOrder(SOR);
                list.Add(order);
            }

            return list;
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings() {
        
            var list = new List<Holding>();

            /*
            private Holding ConvertHolding(Position position) {

                var securityType = SymbolMapper.GetBrokerageSecurityType(position.instrument);

                return new Holding {

                    Symbol = SymbolMapper.GetLeanSymbol(position.instrument, securityType, Market.Oanda),
                    Type = securityType,
                    AveragePrice = (decimal) position.avgPrice,
                    ConversionRate = 1.0m,
                    CurrencySymbol = "$",
                    Quantity = position.side == "sell" ? -position.units : position.units
                };
            }*/

            /*
            Dictionary<string, decimal> krakenBalance = _restApi.GetAccountBalance();

            foreach(KeyValuePair<string, decimal> pair in krakenBalance) {

                string  asset  = pair.Key;
                decimal amount = pair.Value;

                var leanSymbol = this.SymbolMapper.GetLeanSymbol(pair.Key, SecurityType.Crypto, Market.Kraken);

                Cash cash = new Cash("ETH", 0M, 0M);

                Holding h = new Holding();
                
                h.Type = SecurityType.Crypto;

                h.CurrencySymbol = Currencies.CurrencySymbols[asset];

                h.AveragePrice = ?;

                h.Quantity = amount;

                h.MarketPrice = ?;

                h.ConversionRate = ?;

                h.MarketValue = ?;

                h.UnrealizedPnL = ?;

                list.Add(h);
            }
            */
            return list;
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<Cash> GetCashBalance() {

            var list = new List<Cash>();

            Dictionary<string, AssetPair> assetInfo = _restApi.GetAssetPairs();

            Dictionary<string, decimal> balance = _restApi.GetAccountBalance();

            //CashBook.AccountCurrency == "ETH"

            List<string> pairs = new List<string>();

            StringBuilder b = new StringBuilder();
            
            Dictionary<string, string> pairToAsset = new Dictionary<string, string>();

            foreach(KeyValuePair<string,decimal> currency_ammount in balance) {

                string asset = currency_ammount.Key;
                decimal ammount = currency_ammount.Value;

                if (asset == "ETH" || asset == "XETH") {

                    list.Add(new Cash("ETH", ammount, 1));
                }
                else {

                    KeyValuePair<string, bool> pair = SymbolMapper.GetPair(asset, "ETH");
                    // build 
                    b.Append(pair.Key);
                    b.Append(", ");
                    pairToAsset[pair.Key] = asset;
                }    
            }

            Dictionary<string, Ticker> ticks = _restApi.GetTicker(b.ToString());

            foreach (KeyValuePair<string, Ticker> t in ticks) {

                string asset = pairToAsset[t.Key];

                decimal conversionRate = (t.Value.Ask[0] + t.Value.Bid[0]) / 2m;

                Cash cash = new Cash(asset, balance[asset], conversionRate);

                list.Add(cash);
            }

            return list;
        }

        /// <summary>
        /// Specifies whether the brokerage will instantly update account balances
        /// </summary>
        public override bool AccountInstantlyUpdated
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            // TradeBar

            Symbol leanSymbol = request.Symbol;

            string krakenSymbol = SymbolMapper.GetBrokerageSymbol(leanSymbol);
            
            long startTime = ((DateTimeOffset)request.StartTimeUtc).ToUnixTimeSeconds();
            
            long endTime = ((DateTimeOffset) request.EndTimeUtc).ToUnixTimeSeconds();
            TickType tickType = request.TickType;

            Resolution resolution = request.Resolution;

            int interval = ResolutionToInterval(resolution);

            DateTimeZone zone = request.DataTimeZone;

            Type dataType = request.DataType;
            
            while(startTime > endTime) {

                GetOHLCResult result = _restApi.GetOHLC(krakenSymbol, interval,  (int)startTime);

                startTime = result.Last;
                
                Dictionary<string, List<OHLC>> dict = result.Pairs;               
                List<OHLC> list = dict[krakenSymbol];

                foreach(OHLC candle in list) {

                    if(candle.Time <= endTime)
                        yield return new TradeBar(FromUnix(candle.Time), leanSymbol, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume, TimeSpan.FromMinutes(interval));
                }
            }

            yield return null;
        }

       
        public IEnumerable<TradeBar> DownloadTradeBars(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, Resolution resolution, DateTimeZone requestedTimeZone) {

            string krakenSymbol = SymbolMapper.GetBrokerageSymbol(symbol);

            int interval = ResolutionToInterval(resolution);

            GetOHLCResult result = _restApi.GetOHLC(krakenSymbol, interval, (int)ToUnix(startTimeUtc));

            Dictionary<string, List<OHLC>> dict = result.Pairs;

            List<OHLC> list = dict[krakenSymbol];

            foreach (OHLC candle in list)
                yield return new TradeBar(DateTimeOffset.FromUnixTimeSeconds(candle.Time).DateTime, symbol, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume, TimeSpan.FromMinutes(interval));

        }
        #endregion
    }
}