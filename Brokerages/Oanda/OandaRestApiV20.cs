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
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using DateTime = System.DateTime;
using LimitOrder = QuantConnect.Orders.LimitOrder;
using MarketOrder = QuantConnect.Orders.MarketOrder;
using Order = QuantConnect.Orders.Order;
using OandaLimitOrder = Oanda.RestV20.Model.LimitOrder;
using OrderType = QuantConnect.Orders.OrderType;
using TimeInForce = QuantConnect.Orders.TimeInForce;
using QuantConnect.Data;

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
        /// <param name="aggregator">Consolidate ticks.</param>
        /// <param name="environment">The Oanda environment (Trade or Practice)</param>
        /// <param name="accessToken">The Oanda access token (can be the user's personal access token or the access token obtained with OAuth by QC on behalf of the user)</param>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="agent">The Oanda agent string</param>
        public OandaRestApiV20(OandaSymbolMapper symbolMapper, IOrderProvider orderProvider, ISecurityProvider securityProvider, IDataAggregator aggregator, Environment environment, string accessToken, string accountId, string agent)
            : base(symbolMapper, orderProvider, securityProvider, aggregator, environment, accessToken, accountId, agent)
        {
            var basePathRest = environment == Environment.Trade ?
                "https://api-fxtrade.oanda.com/v3" :
                "https://api-fxpractice.oanda.com/v3";

            var basePathStreaming = environment == Environment.Trade ?
                "https://stream-fxtrade.oanda.com/v3" :
                "https://stream-fxpractice.oanda.com/v3";

            _apiRest = new DefaultApi(basePathRest);
            _apiRest.Configuration.AddDefaultHeader(OandaAgentKey, Agent);

            _apiStreaming = new DefaultApi(basePathStreaming);
        }

        /// <summary>
        /// Gets the account base currency
        /// </summary>
        public override string GetAccountBaseCurrency()
        {
            var response = _apiRest.GetAccount(Authorization, AccountId);

            return response.Account.Currency;
        }

        /// <summary>
        /// Gets the list of available tradable instruments/products from Oanda
        /// </summary>
        public override List<string> GetInstrumentList()
        {
            var response = _apiRest.GetAccountInstruments(Authorization, AccountId);

            return response.Instruments.Select(x => x.Name).ToList();
        }

        /// <summary>
        /// Gets all open orders on the account.
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from Oanda</returns>
        public override List<Order> GetOpenOrders()
        {
            var json = _apiRest.ListPendingOrdersAsJson(Authorization, AccountId);

            var response = (JObject)JsonConvert.DeserializeObject(json);

            return response["orders"].Select(ConvertOrder).ToList();
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            var response = _apiRest.ListOpenPositions(Authorization, AccountId);

            return response.Positions.Select(ConvertHolding).ToList();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<CashAmount> GetCashBalance()
        {
            var response = _apiRest.GetAccountSummary(Authorization, AccountId);

            return new List<CashAmount>
            {
                new CashAmount(response.Account.Balance.ToDecimal(),
                    response.Account.Currency)
            };
        }

        /// <summary>
        /// Retrieves the current rate for each of a list of instruments
        /// </summary>
        /// <param name="instruments">the list of instruments to check</param>
        /// <returns>Dictionary containing the current quotes for each instrument</returns>
        public override Dictionary<string, Tick> GetRates(List<string> instruments)
        {
            var response = _apiRest.GetPrices(Authorization, AccountId, instruments);

            return response.Prices
                .ToDictionary(
                    x => x.Instrument,
                    x => new Tick { BidPrice = x.Bids.Last().Price.ToDecimal(), AskPrice = x.Asks.Last().Price.ToDecimal() }
                );
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            var orderFee = OrderFee.Zero;
            var marketOrderFillQuantity = 0;
            var marketOrderFillPrice = 0m;
            var marketOrderRemainingQuantity = 0;
            var marketOrderStatus = OrderStatus.Filled;
            var request = GenerateOrderRequest(order);
            order.PriceCurrency = SecurityProvider.GetSecurity(order.Symbol).SymbolProperties.QuoteCurrency;

            lock (Locker)
            {
                var response = _apiRest.CreateOrder(Authorization, AccountId, request);
                order.BrokerId.Add(response.Data.OrderCreateTransaction.Id);

                // Market orders are special, due to the callback not being triggered always,
                // if the order was Filled/PartiallyFilled, find fill quantity and price and inform the user
                if (order.Type == OrderType.Market)
                {
                    var fill = response.Data.OrderFillTransaction;
                    marketOrderFillPrice = fill.Price.ConvertInvariant<decimal>();

                    if (fill.TradeOpened != null && fill.TradeOpened.TradeID.Length > 0)
                    {
                        marketOrderFillQuantity = fill.TradeOpened.Units.ConvertInvariant<int>();
                    }

                    if (fill.TradeReduced != null && fill.TradeReduced.TradeID.Length > 0)
                    {
                        marketOrderFillQuantity = fill.TradeReduced.Units.ConvertInvariant<int>();
                    }

                    if (fill.TradesClosed != null && fill.TradesClosed.Count > 0)
                    {
                        marketOrderFillQuantity += fill.TradesClosed.Sum(trade => trade.Units.ConvertInvariant<int>());
                    }

                    marketOrderRemainingQuantity = Convert.ToInt32(order.AbsoluteQuantity - Math.Abs(marketOrderFillQuantity));
                    if (marketOrderRemainingQuantity > 0)
                    {
                        marketOrderStatus = OrderStatus.PartiallyFilled;
                        // The order was not fully filled lets save it so the callback can inform the user
                        PendingFilledMarketOrders[order.Id] = marketOrderStatus;
                    }
                }
            }
            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee) { Status = OrderStatus.Submitted });

            // If 'marketOrderRemainingQuantity < order.AbsoluteQuantity' is false it means the order was not even PartiallyFilled, wait for callback
            if (order.Type == OrderType.Market && marketOrderRemainingQuantity < order.AbsoluteQuantity)
            {
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Oanda Fill Event")
                {
                    Status = marketOrderStatus,
                    FillPrice = marketOrderFillPrice,
                    FillQuantity = marketOrderFillQuantity
                });
            }

            return true;
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            Log.Trace("OandaBrokerage.UpdateOrder(): " + order);

            if (!order.BrokerId.Any())
            {
                // we need the brokerage order id in order to perform an update
                Log.Trace("OandaBrokerage.UpdateOrder(): Unable to update order without BrokerId.");
                return false;
            }

            var request = GenerateOrderRequest(order);

            var orderId = order.BrokerId.First();
            var response = _apiRest.ReplaceOrder(Authorization, AccountId, orderId, request);

            // replace the brokerage order id
            order.BrokerId[0] = response.Data.OrderCreateTransaction.Id;

            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero) { Status = OrderStatus.UpdateSubmitted });

            // check if the updated (marketable) order was filled
            if (response.Data.OrderFillTransaction != null)
            {
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "Oanda Fill Event")
                {
                    Status = OrderStatus.Filled,
                    FillPrice = response.Data.OrderFillTransaction.Price.ToDecimal(),
                    FillQuantity = response.Data.OrderFillTransaction.Units.ConvertInvariant<int>()
                });
            }

            return true;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            Log.Trace("OandaBrokerage.CancelOrder(): " + order);

            if (!order.BrokerId.Any())
            {
                Log.Trace("OandaBrokerage.CancelOrder(): Unable to cancel order without BrokerId.");
                return false;
            }

            foreach (var orderId in order.BrokerId)
            {
                _apiRest.CancelOrder(Authorization, AccountId, orderId);
                OnOrderEvent(new OrderEvent(order,
                    DateTime.UtcNow,
                    OrderFee.Zero,
                    "Oanda Cancel Order Event") { Status = OrderStatus.Canceled });
            }

            return true;
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
                    TransactionsConnectionHandler.KeepAlive(DateTime.UtcNow);
                    break;

                case "ORDER_FILL":
                    var transaction = obj.ToObject<OrderFillTransaction>();

                    Order order;
                    lock (Locker)
                    {
                        order = OrderProvider.GetOrderByBrokerageId(transaction.OrderID);
                    }
                    if (order != null)
                    {
                        OrderStatus status;
                        // Market orders are special: if the order was not in 'PartiallyFilledMarketOrders', means
                        // we already sent the fill event with OrderStatus.Filled, else it means we already informed the user
                        // of a partiall fill, or didn't inform the user, so we need to do it now
                        if (order.Type != OrderType.Market || PendingFilledMarketOrders.TryRemove(order.Id, out status))
                        {
                            order.PriceCurrency = SecurityProvider.GetSecurity(order.Symbol).SymbolProperties.QuoteCurrency;

                            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "Oanda Fill Event")
                            {
                                Status = OrderStatus.Filled,
                                FillPrice = transaction.Price.ToDecimal(),
                                FillQuantity = transaction.Units.ConvertInvariant<int>()
                            });
                        }
                    }
                    else
                    {
                        Log.Error($"OandaBrokerage.OnTransactionDataReceived(): order id not found: {transaction.OrderID}");
                    }
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
                    PricingConnectionHandler.KeepAlive(DateTime.UtcNow);
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

                    var bidPrice = data.Bids.Last().Price.ConvertInvariant<decimal>();
                    var askPrice = data.Asks.Last().Price.ConvertInvariant<decimal>();
                    var tick = new Tick(time, symbol, bidPrice, askPrice);

                    EmitTick(tick);
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
            request.Headers[OandaAgentKey] = Agent;

            try
            {
                return request.GetResponse();
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    throw;
                }

                var stream = GetResponseStream(ex.Response);
                using (var reader = new StreamReader(stream))
                {
                    throw new Exception(reader.ReadToEnd());
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
            request.Headers[OandaAgentKey] = Agent;

            try
            {
                return request.GetResponse();
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    throw;
                }

                var stream = GetResponseStream(ex.Response);
                using (var reader = new StreamReader(stream))
                {
                    throw new Exception(reader.ReadToEnd());
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
            var startUtc = startTimeUtc.ToStringInvariant("yyyy-MM-ddTHH:mm:ssZ");
            var endUtc = endTimeUtc.ToStringInvariant("yyyy-MM-ddTHH:mm:ssZ");

            // Oanda only has 5-second bars, we return these for Resolution.Second
            var period = resolution == Resolution.Second ? TimeSpan.FromSeconds(5) : resolution.ToTimeSpan();

            var response = _apiRest.GetInstrumentCandles(Authorization, oandaSymbol, null, "M", ToGranularity(resolution).ToString(), null, startUtc, endUtc);
            foreach (var candle in response.Candles)
            {
                var time = GetTickDateTimeFromString(candle.Time);
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
            var startUtc = startTimeUtc.ToStringInvariant("yyyy-MM-ddTHH:mm:ssZ");

            // Oanda only has 5-second bars, we return these for Resolution.Second
            var period = resolution == Resolution.Second ? TimeSpan.FromSeconds(5) : resolution.ToTimeSpan();

            var response = _apiRest.GetInstrumentCandles(Authorization, oandaSymbol, null, "BA", ToGranularity(resolution).ToString(), OandaBrokerage.MaxBarsPerRequest, startUtc);
            foreach (var candle in response.Candles)
            {
                var time = GetTickDateTimeFromString(candle.Time);
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
        /// Converts an Oanda order into a LEAN order.
        /// </summary>
        private Order ConvertOrder(JToken order)
        {
            var type = order["type"].ToString();

            Order qcOrder;
            switch (type)
            {
                case "MARKET_IF_TOUCHED":
                    var stopOrder = order.ToObject<MarketIfTouchedOrder>();
                    qcOrder = new StopMarketOrder
                    {
                        StopPrice = stopOrder.Price.ToDecimal()
                    };
                    break;

                case "LIMIT":
                    var limitOrder = order.ToObject<OandaLimitOrder>();
                    qcOrder = new LimitOrder
                    {
                        LimitPrice = limitOrder.Price.ToDecimal()
                    };
                    break;

                case "STOP":
                    var stopLimitOrder = order.ToObject<StopOrder>();
                    qcOrder = new StopLimitOrder
                    {
                        Price = stopLimitOrder.Price.ConvertInvariant<decimal>(),
                        LimitPrice = stopLimitOrder.PriceBound.ConvertInvariant<decimal>()
                    };
                    break;

                case "MARKET":
                    qcOrder = new MarketOrder();
                    break;

                default:
                    throw new NotSupportedException(
                        "An existing " + type + " working order was found and is currently unsupported. Please manually cancel the order before restarting the algorithm.");
            }

            var instrument = order["instrument"].ToString();
            var id = order["id"].ToString();
            var units = order["units"].ConvertInvariant<int>();
            var createTime = order["createTime"].ToString();

            var securityType = SymbolMapper.GetBrokerageSecurityType(instrument);
            qcOrder.Symbol = SymbolMapper.GetLeanSymbol(instrument, securityType, Market.Oanda);
            qcOrder.Time = GetTickDateTimeFromString(createTime);
            qcOrder.Quantity = units;
            qcOrder.Status = OrderStatus.None;
            qcOrder.BrokerId.Add(id);

            var orderByBrokerageId = OrderProvider.GetOrderByBrokerageId(id);
            if (orderByBrokerageId != null)
            {
                qcOrder.Id = orderByBrokerageId.Id;
            }

            var gtdTime = order["gtdTime"];
            if (gtdTime != null)
            {
                var expiry = GetTickDateTimeFromString(gtdTime.ToString());
                qcOrder.Properties.TimeInForce = TimeInForce.GoodTilDate(expiry);
            }

            return qcOrder;
        }

        /// <summary>
        /// Converts an Oanda position into a LEAN holding.
        /// </summary>
        private Holding ConvertHolding(Position position)
        {
            var securityType = SymbolMapper.GetBrokerageSecurityType(position.Instrument);
            var symbol = SymbolMapper.GetLeanSymbol(position.Instrument, securityType, Market.Oanda);

            var longUnits = position._Long.Units.ConvertInvariant<int>();
            var shortUnits = position._Short.Units.ConvertInvariant<int>();

            decimal averagePrice = 0;
            var quantity = 0;
            if (longUnits > 0)
            {
                averagePrice = position._Long.AveragePrice.ToDecimal();
                quantity = longUnits;
            }
            else if (shortUnits < 0)
            {
                averagePrice = position._Short.AveragePrice.ToDecimal();
                quantity = shortUnits;
            }

            return new Holding
            {
                Symbol = symbol,
                Type = securityType,
                AveragePrice = averagePrice,
                CurrencySymbol = "$",
                Quantity = quantity
            };
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

        /// <summary>
        /// Generates an Oanda order request
        /// </summary>
        /// <param name="order">The LEAN order</param>
        /// <returns>The request in JSON format</returns>
        private string GenerateOrderRequest(Order order)
        {
            var instrument = SymbolMapper.GetBrokerageSymbol(order.Symbol);

            string request;
            switch (order.Type)
            {
                case OrderType.Market:
                    var marketOrderRequest = new MarketOrderRequest
                    {
                        Type = MarketOrderRequest.TypeEnum.MARKET,
                        Instrument = instrument,
                        Units = order.Quantity.ToStringInvariant()
                    };
                    request = JsonConvert.SerializeObject(new { order = marketOrderRequest });
                    break;

                case OrderType.Limit:
                    var limitOrderRequest = new LimitOrderRequest
                    {
                        Type = LimitOrderRequest.TypeEnum.LIMIT,
                        Instrument = instrument,
                        Units = order.Quantity.ToStringInvariant(),
                        Price = ((LimitOrder)order).LimitPrice.ToString(CultureInfo.InvariantCulture)
                    };
                    request = JsonConvert.SerializeObject(new { order = limitOrderRequest });
                    break;

                case OrderType.StopMarket:
                    var marketIfTouchedOrderRequest = new MarketIfTouchedOrderRequest
                    {
                        Type = MarketIfTouchedOrderRequest.TypeEnum.MARKETIFTOUCHED,
                        Instrument = instrument,
                        Units = order.Quantity.ToStringInvariant(),
                        Price = ((StopMarketOrder)order).StopPrice.ToString(CultureInfo.InvariantCulture)
                    };
                    request = JsonConvert.SerializeObject(new { order = marketIfTouchedOrderRequest });
                    break;

                case OrderType.StopLimit:
                    var stopOrderRequest = new StopOrderRequest
                    {
                        Type = StopOrderRequest.TypeEnum.STOP,
                        Instrument = instrument,
                        Units = order.Quantity.ToStringInvariant(),
                        Price = ((StopLimitOrder)order).StopPrice.ToString(CultureInfo.InvariantCulture),
                        PriceBound = ((StopLimitOrder)order).LimitPrice.ToString(CultureInfo.InvariantCulture)
                    };
                    request = JsonConvert.SerializeObject(new { order = stopOrderRequest });
                    break;

                default:
                    throw new NotSupportedException("The order type " + order.Type + " is not supported.");
            }

            return request;
        }
    }
}
