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
using RestSharp;
using HistoryRequest = QuantConnect.Data.HistoryRequest;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Kraken
{
    using DataType;

    public class KrakenBrokerage : Brokerage, IDataQueueHandler
    {

        /// <summary>
        /// The maximum number of bars per historical data request
        /// </summary>
        public const int MaxBarsPerRequest = 1000;

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

        protected IRestClient RateClient;
        /// <summary>
        /// Initializes a new instance of the <see cref="Kraken"/> class.
        /// </summary>
        /// <param name="key">The API key.</param>
        /// <param name="secret">The API secret.</param>
        /// <param name="rateLimitMilliseconds">The rate limit in milliseconds.</param>
        public KrakenBrokerage(string key, string secret, int rateLimitMilliseconds = 5000)
            : base("Kraken Brokerage")
        {

            
            // copied from GDAX
            RateClient = new RestClient("http://api.fixer.io/latest?base=usd");

            _restApi = new KrakenRestApi(key, secret, rateLimitMilliseconds);

            SymbolMapper = new KrakenSymbolMapper();
            SymbolMapper.UpdateSymbols(_restApi);

            // forward events received from API
            OrderStatusChanged += (sender, orderEvent) => OnOrderEvent(orderEvent);
            AccountChanged += (sender, accountEvent) => OnAccountChanged(accountEvent);
            Message += (sender, messageEvent) => OnMessage(messageEvent);
        }

        #region IDataQueueHandler

        StringBuilder tickerStringbuilder = new StringBuilder();
        //! IMPLEMENT IDATAQUEUEHANDLER methods
        public IEnumerable<BaseData> GetNextTicks()
        {

            tickerStringbuilder.Clear();

            foreach (Symbol symbol in SubscribedSymbols)
            {

                string krakenSymbol = SymbolMapper.GetBrokerageSymbol(symbol);

                tickerStringbuilder.Append(krakenSymbol);
                tickerStringbuilder.Append(", ");
            }

            Dictionary<string, Ticker> ticks = _restApi.GetTicker(tickerStringbuilder.ToString());

            foreach (KeyValuePair<string, Ticker> pair in ticks)
                yield return KrakenTickToLeanTick(pair);

        }

        public Tick GetTick(Symbol symbol)
        {
            string krakenSymbol = SymbolMapper.GetBrokerageSymbol(symbol);

            Dictionary<string, Ticker> ticks = _restApi.GetTicker(krakenSymbol);

            Ticker krakenTick = ticks[krakenSymbol];

            return KrakenTickToLeanTick(new KeyValuePair<string, Ticker>(krakenSymbol, krakenTick));
        }

        Tick KrakenTickToLeanTick(KeyValuePair<string, Ticker> pair)
        {

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

        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {


            foreach (Symbol symbol in symbols)
            {
                if (symbol.Value.Contains("UNIVERSE") || symbol.SecurityType != SecurityType.Forex && symbol.SecurityType != SecurityType.Crypto)
                {
                    continue;
                }

                SubscribedSymbols.Add(symbol);
            }
        }

        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (Symbol symbol in symbols)
                SubscribedSymbols.RemoveWhere(subscribedSymbol => subscribedSymbol.Value == symbol.Value);
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
        public override bool IsConnected { get { return true; } }

        #region TRANSLATORS
        private string TranslateDirectionToKraken(OrderDirection direction)
        {
            if (direction == OrderDirection.Buy)
                return "buy";

            if (direction == OrderDirection.Sell)
                return "sell";

            throw new KrakenException("Can't \"hold\" an order on Kraken!");
        }

        private string TranslateOrderTypeToKraken(OrderType orderType)
        {
            switch (orderType)
            {
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

        private OrderType TranslateOrderTypeToLean(string type)
        {

            switch (type)
            {

                case "limit":
                    return OrderType.Limit;

                case "market":
                    return OrderType.Market;
            }

            throw new KrakenException("Unsupported order type");
        }


        int ResolutionToInterval(Resolution res)
        {

            switch (res)
            {

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

        DateTime FromUnix(int unixTime)
        {
            return Time.UnixTimeStampToDateTime(checked((double)unixTime));
        }

        long ToUnix(DateTime dateTime)
        {
            return checked((long)Time.DateTimeToUnixTimeStamp(dateTime));
        }

        #endregion
        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Orders.Order order)
        {

            Order krakenOrder = new Order();

            krakenOrder.Pair = SymbolMapper.GetBrokerageSymbol(order.Symbol);

            // buy/sell
            krakenOrder.Type = TranslateDirectionToKraken(order.Direction);
            krakenOrder.OrderType = TranslateOrderTypeToKraken(order.Type);
            krakenOrder.Volume = order.AbsoluteQuantity;

            if (order.Type == OrderType.Limit)
                krakenOrder.Price = order.Price;

            // krakenOrder.Leverage = ?

            var result = _restApi.AddOrder(krakenOrder);

            if (result.Txid != null & result.Txid.Length != 0)
            {

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
        public override bool UpdateOrder(Orders.Order order)
        {

            //! UPDATE ORDER           
            //todo: maybe make it so, that this broker doesn't support updating orders (because it doesnt)
            bool success = false;

            if (CancelOrder(order))
            {

                order.BrokerId.Clear();

                if (PlaceOrder(order))
                {

                    return true;
                }
                else
                {

                    // try again
                    if (PlaceOrder(order))
                    {

                        return true;
                    }
                    else
                    {
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
        public override bool CancelOrder(Orders.Order order)
        {

            int sum = 0;

            foreach (string txid in order.BrokerId)
            {

                var result = _restApi.CancelOrder(txid);
                sum += result.Count;
            }

            return sum > 0;

        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            // NOP
            // Maybe should do implement "ping" functionality to check if Kraken API is online
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            // NOP
        }

        /// <summary>
        /// Dispose of the brokerage instance
        /// </summary>
        public override void Dispose()
        {
            // NOP
        }

        /// <summary>
        /// Gets all open orders on the account. 
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from IB</returns>
        public override List<Orders.Order> GetOpenOrders()
        {
            List<Orders.Order> list = new List<Orders.Order>();

            Dictionary<string, OrderInfo> orders = _restApi.GetOpenOrders();

            foreach (KeyValuePair<string, OrderInfo> pair in orders)
            {
                OrderInfo info = pair.Value;

                OrderDescription desc = info.Descr;

                // This is for error-caching purposes here, will get removed in final release
                if (pair.Key != desc.Pair)
                    throw new KrakenException($"Pair strings {pair.Key} and {desc.Pair} don't match, please inspect!");

                var SOR = new SubmitOrderRequest(

                    TranslateOrderTypeToLean(info.Descr.OrderType),
                    SecurityType.Crypto,
                    this.SymbolMapper.GetLeanSymbol(desc.Pair, SecurityType.Crypto, Market.Kraken),
                    info.Volume - info.VolumeExecuted,
                    info.StopPrice.HasValue ? info.StopPrice.Value : 0m,
                    info.LimitPrice.HasValue ? info.LimitPrice.Value : 0m,
                    Time.UnixTimeStampToDateTime(info.OpenTm),
                    ""
                );

                var order = Orders.Order.CreateOrder(SOR);
                list.Add(order);
            }

            return list;
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {

            var list = new List<Holding>();

            // Set MarketPrice in each Holding
            /*
            var KrakenSymbols = holdings
                .Select(x => _symbolMapper.GetBrokerageSymbol(x.Symbol))
                .ToList();

            if (KrakenSymbols.Count > 0)
            {
                var quotes = _api.GetRates(KrakenSymbols);
                
                foreach (var holding in holdings)
                {
                    var KrakenSymbol = _symbolMapper.GetBrokerageSymbol(holding.Symbol);
                    Tick tick;
                    if (quotes.TryGetValue(KrakenSymbol, out tick))
                    {
                        holding.MarketPrice = (tick.BidPrice + tick.AskPrice) / 2;
                    }
                }
            }

            return holdings;*/
            return list;
        }

        private decimal GetConversionRate(string currency)
        {
            Log.Trace($"GetConversionRate({currency})");

            var response = RateClient.Execute(new RestSharp.RestRequest(Method.GET));
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, (int)response.StatusCode, "GetConversionRate: error returned from conversion rate service."));
                return 0;
            }

            var raw = JsonConvert.DeserializeObject<JObject>(response.Content);
            var rate = raw.SelectToken("rates." + currency).Value<decimal>();
            if (rate == 0)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, (int)response.StatusCode, "GetConversionRate: zero value returned from conversion rate service."));
                return 0;
            }

            return 1m / rate;
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<Cash> GetCashBalance()
        {
            Log.Trace("GetCashBalance()");
            
            // CashBook.AccountCurrency = "USD";
            
            var list = new List<Cash>();

            Dictionary<string, decimal> balance   = _restApi.GetAccountBalance();


            foreach(var KVPair in balance)
            {
                Log.Trace($"{KVPair.Key}, {KVPair.Value}");
            }

            List<string> pairs = new List<string>();


            HashSet<string> wantedPairs = new HashSet<string>();

            Dictionary<string, List<string>> assetToPairs = new Dictionary<string, List<string>>();

            foreach (KeyValuePair<string, decimal> currency_ammount in balance)
            {
                string asset = currency_ammount.Key;
                decimal amount = currency_ammount.Value;

                Log.Trace($"Currency; asset name {asset}, amount: {amount}");

                //! TODO THIS DOESNT WORK AS EXPECTED, WANTS XRPETH PAIR WHICH DOES NOT EXIST
                if (asset == "ZUSD")
                {
                    list.Add(new Cash("USD", amount, 1));
                }
                else if (new [] {"ZEUR", "ZJPY", "ZGBP", "ZKRW", "ZCAD"}.Contains(asset))
                {
                    string leanSymbol = SymbolMapper.KrakenToLeanCode(asset);

                    decimal price = GetConversionRate(leanSymbol);

                    Cash cash = new Cash(leanSymbol, amount, price);
                }
                else
                {
                    try
                    {
                        string pair = SymbolMapper.GetPair(asset, "USD"); //! <-- GetPair might not work properly
                        // build 

                        wantedPairs.Add(pair);

                        assetToPairs[asset] = new List<string>() { pair };
                    }
                    catch(Exception e)
                    {
                        Log.Trace($"Catched exception in GetCashBalance(), with message: {e}");

                        string pair = SymbolMapper.GetPair(asset, "XBT"); //! <-- GetPair might not work properly
                        // build 

                        wantedPairs.Add(pair);
                        wantedPairs.Add("XXBTZUSD");
                        assetToPairs[asset] = new List<string>() { pair, "XXBTZUSD" };
                    }
                }
            }

            StringBuilder b = new StringBuilder();

            List<string> wantedPairsList = wantedPairs.ToList();

            for (int i = 0; i < wantedPairsList.Count;i++)
            {
                b.Append(wantedPairsList[i]);

                if(i != wantedPairsList.Count-1)
                    b.Append(", ");
            }

            Dictionary<string, Ticker> ticks = _restApi.GetTicker(b.ToString());

            /* It works but it very slow
             
            Dictionary<string, Ticker> ticks =  new Dictionary<string, Ticker>();

            foreach(string pair in wantedPairs)
            {
                var dict = _restApi.GetTicker(pair);

                foreach(var KVPair in dict)
                    ticks[KVPair.Key] = KVPair.Value;
            }*/


            foreach (KeyValuePair<string, List<string>> KVPair in assetToPairs)
            {
                string asset = KVPair.Key;

                decimal price = 1;

                foreach(string pair in KVPair.Value)
                {
                    Ticker t = ticks[pair];
                    
                    decimal conversionRate = (t.Ask[0] + t.Bid[0]) / 2m;

                    price *= conversionRate;
                }

                Cash cash = new Cash(SymbolMapper.KrakenToLeanCode(asset), balance[asset], price);

                list.Add(cash);
            }

            return list;
        }

        /// <summary>
        /// Specifies whether the brokerage will instantly update account balances
        /// </summary>
        public override bool AccountInstantlyUpdated {
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

            long startTime = checked((long) Time.DateTimeToUnixTimeStamp(request.StartTimeUtc));
            long endTime   = checked((long) Time.DateTimeToUnixTimeStamp(request.EndTimeUtc  ));

            TickType tickType = request.TickType;

            Resolution resolution = request.Resolution;

            int interval = ResolutionToInterval(resolution);

            DateTimeZone zone = request.DataTimeZone;

            Type dataType = request.DataType;

            while (startTime > endTime)
            {

                GetOHLCResult result = _restApi.GetOHLC(krakenSymbol, interval, (int)startTime);

                startTime = result.Last;

                Dictionary<string, List<OHLC>> dict = result.Pairs;
                List<OHLC> list = dict[krakenSymbol];

                foreach (OHLC candle in list)
                {

                    if (candle.Time <= endTime)
                        yield return new TradeBar(FromUnix(candle.Time), leanSymbol, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume, TimeSpan.FromMinutes(interval));
                }
            }

            yield return null;
        }

        public IEnumerable<TradeBar> DownloadTradeBars(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, Resolution resolution, DateTimeZone requestedTimeZone)
        {
            string krakenSymbol = SymbolMapper.GetBrokerageSymbol(symbol);

            int interval = ResolutionToInterval(resolution);

            GetOHLCResult result = _restApi.GetOHLC(krakenSymbol, interval, checked((int)ToUnix(startTimeUtc)));

            Dictionary<string, List<OHLC>> dict = result.Pairs;

            List<OHLC> list = dict[krakenSymbol];

            foreach (OHLC candle in list)
                yield return new TradeBar(Time.UnixTimeStampToDateTime(checked((double)candle.Time)), symbol, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume, TimeSpan.FromMinutes(interval));

        }
        #endregion
    }
}