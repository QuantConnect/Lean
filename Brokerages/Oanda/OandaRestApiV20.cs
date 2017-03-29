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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using Oanda.RestV20.Api;
using Oanda.RestV20.Model;
using Oanda.RestV20.Session;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using DateTime = System.DateTime;
using MarketOrder = QuantConnect.Orders.MarketOrder;
using Order = QuantConnect.Orders.Order;
using OandaOrder = Oanda.RestV20.Model.Order;

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Oanda REST API v20 implementation
    /// </summary>
    public class OandaRestApiV20 : OandaRestApiBase
    {
        private readonly DefaultApi _apiRest;
        private readonly DefaultApi _apiStreaming;

        private TransactionStreamSession _eventsSession;
        private PricingStreamSession _ratesSession;
        private readonly Dictionary<Symbol, DateTimeZone> _symbolExchangeTimeZones = new Dictionary<Symbol, DateTimeZone>();

        /// <summary>
        /// Initializes a new instance of the <see cref="OandaRestApiV20"/> class.
        /// </summary>
        /// <param name="symbolMapper">The symbol mapper.</param>
        /// <param name="orderProvider">The order provider.</param>
        /// <param name="securityProvider">The holdings provider.</param>
        /// <param name="environment">The Oanda environment (Trade or Practice)</param>
        /// <param name="accessToken">The Oanda access token (can be the user's personal access token or the access token obtained with OAuth by QC on behalf of the user)</param>
        /// <param name="accountId">The account identifier.</param>
        public OandaRestApiV20(OandaSymbolMapper symbolMapper, IOrderProvider orderProvider, ISecurityProvider securityProvider, Environment environment, string accessToken, string accountId)
            : base(symbolMapper, orderProvider, securityProvider, environment, accessToken, accountId)
        {
            var basePathRest = environment == Environment.Trade ? 
                "https://api-fxtrade.oanda.com/v3" : 
                "https://api-fxpractice.oanda.com/v3";

            var basePathStreaming = environment == Environment.Trade ? 
                "https://stream-fxtrade.oanda.com/v3" : 
                "https://stream-fxpractice.oanda.com/v3";

            _apiRest = new DefaultApi(basePathRest);
            _apiStreaming = new DefaultApi(basePathStreaming);
        }

        /// <summary>
        /// Gets the list of available tradable instruments/products from Oanda
        /// </summary>
        public override List<string> GetInstrumentList()
        {
            var response = _apiRest.GetAcountInstruments(Authorization, AccountId);

            return response.Instruments.Select(x => x.Name).ToList();
        }

        /// <summary>
        /// Gets all open orders on the account. 
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from Oanda</returns>
        public override List<Order> GetOpenOrders()
        {
            var response = _apiRest.GetAccount(Authorization, AccountId);

            var orders = response.Account.Orders;
            return orders.Select(ConvertOrder).ToList();
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            return new List<Holding>();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<Cash> GetCashBalance()
        {
            var response = _apiRest.GetAccountSummary(Authorization, AccountId);

            return new List<Cash>
            {
                new Cash(response.Account.Currency, 
                    response.Account.Balance.ToDecimal(),
                    GetUsdConversion(response.Account.Currency))
            };
        }

        /// <summary>
        /// Retrieves the current rate for each of a list of instruments
        /// </summary>
        /// <param name="instruments">the list of instruments to check</param>
        /// <returns>Dictionary containing the current quotes for each instrument</returns>
        public override Dictionary<string, Tick> GetRates(List<string> instruments)
        {
            return new Dictionary<string, Tick>();
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            return false;
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            return false;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            return false;
        }

        /// <summary>
        /// Starts streaming transactions for the active account
        /// </summary>
        public override void StartTransactionStream()
        {
            _eventsSession = new TransactionStreamSession(this);
            _eventsSession.DataReceived += OnTransactionDataReceived;
            _eventsSession.StartSession();
        }

        /// <summary>
        /// Stops streaming transactions for the active account
        /// </summary>
        public override void StopTransactionStream()
        {
            if (_eventsSession != null)
            {
                _eventsSession.DataReceived -= OnTransactionDataReceived;
                _eventsSession.StopSession();
            }
        }

        /// <summary>
        /// Starts streaming prices for a list of instruments
        /// </summary>
        public override void StartPricingStream(List<string> instruments)
        {
            _ratesSession = new PricingStreamSession(this, instruments);
            _ratesSession.DataReceived += OnPricingDataReceived;
            _ratesSession.StartSession();
        }

        /// <summary>
        /// Stops streaming prices for all instruments
        /// </summary>
        public override void StopPricingStream()
        {
            if (_ratesSession != null)
            {
                _ratesSession.DataReceived -= OnPricingDataReceived;
                _ratesSession.StopSession();
            }
        }

        /// <summary>
        /// Returns a DateTime from an RFC3339 string (with high resolution)
        /// </summary>
        /// <param name="time">The time string</param>
        private static DateTime GetTickDateTimeFromString(string time)
        {
            // remove nanoseconds, DateTime.ParseExact will throw with 9 digits after seconds
            return OandaBrokerage.GetDateTimeFromString(time.Remove(25, 3));
        }

        /// <summary>
        /// Event handler for streaming events
        /// </summary>
        /// <param name="json">The event object</param>
        private void OnTransactionDataReceived(string json)
        {
            var obj = (JObject)JsonConvert.DeserializeObject(json);
            var type = obj["type"].ToString();

            switch (type)
            {
                case "HEARTBEAT":
                    //var heartBeat = obj.ToObject<TransactionHeartbeat>();
                    lock (LockerConnectionMonitor)
                    {
                        LastHeartbeatUtcTime = DateTime.UtcNow;
                    }
                    break;

                case "ORDER_FILL":
                    var transaction = obj.ToObject<OrderFillTransaction>();

                    var order = OrderProvider.GetOrderByBrokerageId(transaction.OrderID);
                    order.PriceCurrency = SecurityProvider.GetSecurity(order.Symbol).SymbolProperties.QuoteCurrency;

                    const int orderFee = 0;
                    var fill = new OrderEvent(order, DateTime.UtcNow, orderFee, "Oanda Fill Event")
                    {
                        Status = OrderStatus.Filled,
                        FillPrice = transaction.Price.ToDecimal(),
                        FillQuantity = transaction.Units.ToInt32()
                    };

                    // flip the quantity on sell actions
                    if (order.Direction == OrderDirection.Sell)
                    {
                        fill.FillQuantity *= -1;
                    }
                    OnOrderEvent(fill);
                    break;
            }
        }

        /// <summary>
        /// Event handler for streaming ticks
        /// </summary>
        /// <param name="json">The data object containing the received tick</param>
        private void OnPricingDataReceived(string json)
        {
            var obj = (JObject)JsonConvert.DeserializeObject(json);
            var type = obj["type"].ToString();

            switch (type)
            {
                case "HEARTBEAT":
                    //var heartBeat = obj.ToObject<PricingHeartbeat>();
                    lock (LockerConnectionMonitor)
                    {
                        LastHeartbeatUtcTime = DateTime.UtcNow;
                    }
                    break;

                case "PRICE":
                    var data = obj.ToObject<Price>();

                    var securityType = SymbolMapper.GetBrokerageSecurityType(data.Instrument);
                    var symbol = SymbolMapper.GetLeanSymbol(data.Instrument, securityType, Market.Oanda);
                    var time = GetTickDateTimeFromString(data.Time);

                    // live ticks timestamps must be in exchange time zone
                    DateTimeZone exchangeTimeZone;
                    if (!_symbolExchangeTimeZones.TryGetValue(symbol, out exchangeTimeZone))
                    {
                        exchangeTimeZone = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, symbol, securityType).TimeZone;
                        _symbolExchangeTimeZones.Add(symbol, exchangeTimeZone);
                    }
                    time = time.ConvertFromUtc(exchangeTimeZone);

                    var bidPrice = Convert.ToDecimal(data.Bids.Last().Price);
                    var askPrice = Convert.ToDecimal(data.Asks.Last().Price);
                    var tick = new Tick(time, symbol, bidPrice, askPrice);

                    lock (Ticks)
                    {
                        Ticks.Add(tick);
                    }
                    break;
            }
        }

        /// <summary>
        /// Initializes a streaming rates session with the given instruments on the given account
        /// </summary>
        /// <param name="instruments">list of instruments to stream rates for</param>
        /// <returns>the WebResponse object that can be used to retrieve the rates as they stream</returns>
        public WebResponse StartRatesSession(List<string> instruments)
        {
            var instrumentList = string.Join(",", instruments);

            var requestString = _apiStreaming.GetBasePath() + "/accounts/" + AccountId + "/pricing/stream" +
                "?instruments=" + Uri.EscapeDataString(instrumentList);

            var request = WebRequest.CreateHttp(requestString);
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = Authorization;

            try
            {
                var response = request.GetResponse();
                return response;
            }
            catch (WebException ex)
            {
                var stream = GetResponseStream(ex.Response);
                using (var reader = new StreamReader(stream))
                {
                    var result = reader.ReadToEnd();
                    throw new Exception(result);
                }
            }
        }

        /// <summary>
        /// Initializes a streaming events session which will stream events for the given accounts
        /// </summary>
        /// <returns>the WebResponse object that can be used to retrieve the events as they stream</returns>
        public WebResponse StartEventsSession()
        {
            var requestString = _apiStreaming.GetBasePath() + "/accounts/" + AccountId + "/transactions/stream";

            var request = WebRequest.CreateHttp(requestString);
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = Authorization;

            try
            {
                var response = request.GetResponse();
                return response;
            }
            catch (WebException ex)
            {
                var stream = GetResponseStream(ex.Response);
                using (var reader = new StreamReader(stream))
                {
                    var result = reader.ReadToEnd();
                    throw new Exception(result);
                }
            }
        }

        private static Stream GetResponseStream(WebResponse response)
        {
            var stream = response.GetResponseStream();
            if (response.Headers["Content-Encoding"] == "gzip")
            {
                // if we received a gzipped response, handle that
                if (stream != null) stream = new GZipStream(stream, CompressionMode.Decompress);
            }
            return stream;
        }

        /// <summary>
        /// Downloads a list of TradeBars at the requested resolution
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="startTimeUtc">The starting time (UTC)</param>
        /// <param name="endTimeUtc">The ending time (UTC)</param>
        /// <param name="resolution">The requested resolution</param>
        /// <param name="requestedTimeZone">The requested timezone for the data</param>
        /// <returns>The list of bars</returns>
        public override IEnumerable<TradeBar> DownloadTradeBars(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, Resolution resolution, DateTimeZone requestedTimeZone)
        {
            var oandaSymbol = SymbolMapper.GetBrokerageSymbol(symbol);
            var startUtc = startTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endUtc = endTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");

            // Oanda only has 5-second bars, we return these for Resolution.Second
            var period = resolution == Resolution.Second ? TimeSpan.FromSeconds(5) : resolution.ToTimeSpan();

            var response = _apiRest.GetInstrumentCandles(Authorization, oandaSymbol, "M", ToGranularity(resolution).ToString(), null, startUtc, endUtc);
            foreach (var candle in response.Candles)
            {
                var time = Time.UnixTimeStampToDateTime(Convert.ToDouble(candle.Time, CultureInfo.InvariantCulture));
                if (time > endTimeUtc)
                    break;

                yield return new TradeBar(
                    time.ConvertFromUtc(requestedTimeZone),
                    symbol,
                    candle.Bid.O.ToDecimal(),
                    candle.Bid.H.ToDecimal(),
                    candle.Bid.L.ToDecimal(),
                    candle.Bid.C.ToDecimal(),
                    0,
                    period);
            }
        }

        /// <summary>
        /// Downloads a list of QuoteBars at the requested resolution
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="startTimeUtc">The starting time (UTC)</param>
        /// <param name="endTimeUtc">The ending time (UTC)</param>
        /// <param name="resolution">The requested resolution</param>
        /// <param name="requestedTimeZone">The requested timezone for the data</param>
        /// <returns>The list of bars</returns>
        public override IEnumerable<QuoteBar> DownloadQuoteBars(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, Resolution resolution, DateTimeZone requestedTimeZone)
        {
            var oandaSymbol = SymbolMapper.GetBrokerageSymbol(symbol);
            var startUtc = startTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endUtc = endTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");

            // Oanda only has 5-second bars, we return these for Resolution.Second
            var period = resolution == Resolution.Second ? TimeSpan.FromSeconds(5) : resolution.ToTimeSpan();

            var response = _apiRest.GetInstrumentCandles(Authorization, oandaSymbol, "BA", ToGranularity(resolution).ToString(), null, startUtc, endUtc);
            foreach (var candle in response.Candles)
            {
                var time = Time.UnixTimeStampToDateTime(Convert.ToDouble(candle.Time, CultureInfo.InvariantCulture));
                if (time > endTimeUtc)
                    break;

                yield return new QuoteBar(
                    time.ConvertFromUtc(requestedTimeZone),
                    symbol,
                    new Bar(
                        candle.Bid.O.ToDecimal(),
                        candle.Bid.H.ToDecimal(),
                        candle.Bid.L.ToDecimal(),
                        candle.Bid.C.ToDecimal()
                    ),
                    0,
                    new Bar(
                        candle.Ask.O.ToDecimal(),
                        candle.Ask.H.ToDecimal(),
                        candle.Ask.L.ToDecimal(),
                        candle.Ask.C.ToDecimal()
                    ),
                    0,
                    period);
            }
        }

        private string Authorization
        {
            get { return "Bearer " + AccessToken; }
        }

        /// <summary>
        /// Converts the specified Oanda order into a qc order.
        /// The 'task' will have a value if we needed to issue a rest call for the stop price, otherwise it will be null
        /// </summary>
        private Order ConvertOrder(OandaOrder order)
        {
            return new MarketOrder();

            //Order qcOrder;
            //switch (order.type)
            //{
            //    case "limit":
            //        qcOrder = new LimitOrder();
            //        if (order.side == "buy")
            //        {
            //            ((LimitOrder)qcOrder).LimitPrice = Convert.ToDecimal(order.lowerBound);
            //        }

            //        if (order.side == "sell")
            //        {
            //            ((LimitOrder)qcOrder).LimitPrice = Convert.ToDecimal(order.upperBound);
            //        }
            //        break;

            //    case "stop":
            //        qcOrder = new StopLimitOrder();
            //        if (order.side == "buy")
            //        {
            //            ((StopLimitOrder)qcOrder).LimitPrice = Convert.ToDecimal(order.lowerBound);
            //        }

            //        if (order.side == "sell")
            //        {
            //            ((StopLimitOrder)qcOrder).LimitPrice = Convert.ToDecimal(order.upperBound);
            //        }
            //        break;

            //    case "marketIfTouched":
            //        //when market reaches the price sell at market.
            //        qcOrder = new StopMarketOrder { Price = Convert.ToDecimal(order.price), StopPrice = Convert.ToDecimal(order.price) };
            //        break;

            //    case "market":
            //        qcOrder = new MarketOrder();
            //        break;

            //    default:
            //        throw new NotSupportedException("The Oanda order type " + order.type + " is not supported.");
            //}

            //var securityType = SymbolMapper.GetBrokerageSecurityType(order.instrument);
            //qcOrder.Symbol = SymbolMapper.GetLeanSymbol(order.instrument, securityType, Market.Oanda);
            //qcOrder.Quantity = ConvertQuantity(order);
            //qcOrder.Status = OrderStatus.None;
            //qcOrder.BrokerId.Add(order.id.ToString());

            //var orderByBrokerageId = OrderProvider.GetOrderByBrokerageId(order.id);
            //if (orderByBrokerageId != null)
            //{
            //    qcOrder.Id = orderByBrokerageId.Id;
            //}

            //qcOrder.Duration = OrderDuration.Custom;
            //qcOrder.DurationValue = XmlConvert.ToDateTime(order.expiry, XmlDateTimeSerializationMode.Utc);
            //qcOrder.Time = XmlConvert.ToDateTime(order.time, XmlDateTimeSerializationMode.Utc);

            //return qcOrder;
        }

        /// <summary>
        /// Converts a LEAN Resolution to a CandlestickGranularity
        /// </summary>
        /// <param name="resolution">The resolution to convert</param>
        /// <returns></returns>
        private static CandlestickGranularity ToGranularity(Resolution resolution)
        {
            CandlestickGranularity interval;

            switch (resolution)
            {
                case Resolution.Second:
                    interval = CandlestickGranularity.S5;
                    break;

                case Resolution.Minute:
                    interval = CandlestickGranularity.M1;
                    break;

                case Resolution.Hour:
                    interval = CandlestickGranularity.H1;
                    break;

                case Resolution.Daily:
                    interval = CandlestickGranularity.D;
                    break;

                default:
                    throw new ArgumentException("Unsupported resolution: " + resolution);
            }

            return interval;
        }
    }
}
