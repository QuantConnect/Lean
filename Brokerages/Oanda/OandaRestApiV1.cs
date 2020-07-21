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
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using NodaTime;
using QuantConnect.Brokerages.Oanda.RestV1.DataType;
using QuantConnect.Brokerages.Oanda.RestV1.DataType.Communications;
using QuantConnect.Brokerages.Oanda.RestV1.DataType.Communications.Requests;
using QuantConnect.Brokerages.Oanda.RestV1.Framework;
using QuantConnect.Brokerages.Oanda.RestV1.Session;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Oanda REST API v1 implementation
    /// </summary>
    public class OandaRestApiV1 : OandaRestApiBase
    {
        private EventsSession _eventsSession;
        private RatesSession _ratesSession;
        private readonly Dictionary<Symbol, DateTimeZone> _symbolExchangeTimeZones = new Dictionary<Symbol, DateTimeZone>();

        /// <summary>
        /// Initializes a new instance of the <see cref="OandaRestApiV1"/> class.
        /// </summary>
        /// <param name="symbolMapper">The symbol mapper.</param>
        /// <param name="orderProvider">The order provider.</param>
        /// <param name="securityProvider">The holdings provider.</param>
        /// <param name="aggregator">Consolidate ticks</param>
        /// <param name="environment">The Oanda environment (Trade or Practice)</param>
        /// <param name="accessToken">The Oanda access token (can be the user's personal access token or the access token obtained with OAuth by QC on behalf of the user)</param>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="agent">The Oanda agent string</param>
        public OandaRestApiV1(OandaSymbolMapper symbolMapper, IOrderProvider orderProvider, ISecurityProvider securityProvider, IDataAggregator aggregator, Environment environment, string accessToken, string accountId, string agent)
            : base(symbolMapper, orderProvider, securityProvider, aggregator, environment, accessToken, accountId, agent)
        {
        }

        /// <summary>
        /// Gets the list of available tradable instruments/products from Oanda
        /// </summary>
        public override List<string> GetInstrumentList()
        {
            var requestString = EndpointResolver.ResolveEndpoint(Environment, Server.Rates) + "instruments?accountId=" + AccountId;
            return MakeRequest<InstrumentsResponse>(requestString).instruments.Select(x => x.instrument).ToList();
        }

        /// <summary>
        /// Gets all open orders on the account.
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from Oanda</returns>
        public override List<Order> GetOpenOrders()
        {
            return GetOrderList().Select(ConvertOrder).ToList();
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            return GetPositions(AccountId).Select(ConvertHolding).Where(x => x.Quantity != 0).ToList();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<CashAmount> GetCashBalance()
        {
            var getAccountRequestString = EndpointResolver.ResolveEndpoint(Environment, Server.Account) + "accounts/" + AccountId;
            var accountResponse = MakeRequest<Account>(getAccountRequestString);

            return new List<CashAmount>
            {
                new CashAmount(accountResponse.balance.ToDecimal(), accountResponse.accountCurrency)
            };
        }

        /// <summary>
        /// Retrieves the current rate for each of a list of instruments
        /// </summary>
        /// <param name="instruments">the list of instruments to check</param>
        /// <returns>Dictionary containing the current quotes for each instrument</returns>
        public override Dictionary<string, Tick> GetRates(List<string> instruments)
        {
            var requestBuilder = new StringBuilder(EndpointResolver.ResolveEndpoint(Environment, Server.Rates) + "prices?instruments=");
            requestBuilder.Append(string.Join(",", instruments));
            var requestString = requestBuilder.ToString().Replace(",", "%2C");

            return MakeRequest<PricesResponse>(requestString).prices
                .ToDictionary(
                    x => x.instrument,
                    x => new Tick { BidPrice = Convert.ToDecimal(x.bid), AskPrice = Convert.ToDecimal(x.ask) });
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            var requestParams = new Dictionary<string, string>
            {
                { "instrument", SymbolMapper.GetBrokerageSymbol(order.Symbol) },
                { "units", order.AbsoluteQuantity.ConvertInvariant<int>().ToStringInvariant() }
            };

            var orderFee = OrderFee.Zero;
            var marketOrderFillQuantity = 0;
            var marketOrderRemainingQuantity = 0;
            decimal marketOrderFillPrice;
            var marketOrderStatus = OrderStatus.Filled;
            order.PriceCurrency = SecurityProvider.GetSecurity(order.Symbol).SymbolProperties.QuoteCurrency;
            PopulateOrderRequestParameters(order, requestParams);

            lock (Locker)
            {
                var postOrderResponse = PostOrderAsync(requestParams);
                if (postOrderResponse == null)
                    return false;
                // Market orders are special, due to the callback not being triggered always, if the order was filled,
                // find fill quantity and price and inform the user
                if (postOrderResponse.tradeOpened != null && postOrderResponse.tradeOpened.id > 0)
                {
                    if (order.Type == OrderType.Market)
                    {
                        marketOrderFillQuantity = postOrderResponse.tradeOpened.units;
                    }
                    else
                    {
                        order.BrokerId.Add(postOrderResponse.tradeOpened.id.ToStringInvariant());
                    }
                }

                if (postOrderResponse.tradeReduced != null && postOrderResponse.tradeReduced.id > 0)
                {
                    if (order.Type == OrderType.Market)
                    {
                        marketOrderFillQuantity = postOrderResponse.tradeReduced.units;
                    }
                    else
                    {
                        order.BrokerId.Add(postOrderResponse.tradeReduced.id.ToStringInvariant());
                    }
                }

                if (postOrderResponse.orderOpened != null && postOrderResponse.orderOpened.id > 0)
                {
                    if (order.Type != OrderType.Market)
                    {
                        order.BrokerId.Add(postOrderResponse.orderOpened.id.ToStringInvariant());
                    }
                }

                if (postOrderResponse.tradesClosed != null && postOrderResponse.tradesClosed.Count > 0)
                {
                    marketOrderFillQuantity += postOrderResponse.tradesClosed
                        .Where(trade => order.Type == OrderType.Market)
                        .Sum(trade => trade.units);
                }

                marketOrderFillPrice = postOrderResponse.price.ConvertInvariant<decimal>();
                marketOrderRemainingQuantity = Convert.ToInt32(order.AbsoluteQuantity - Math.Abs(marketOrderFillQuantity));
                if (marketOrderRemainingQuantity > 0)
                {
                    marketOrderStatus = OrderStatus.PartiallyFilled;
                    // The order was not fully filled lets save it so the callback can inform the user
                    PendingFilledMarketOrders[order.Id] = marketOrderStatus;
                }
            }
            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee) { Status = OrderStatus.Submitted });

            // If 'marketOrderRemainingQuantity < order.AbsoluteQuantity' is false it means the order was not even PartiallyFilled, wait for callback
            if (order.Type == OrderType.Market && marketOrderRemainingQuantity < order.AbsoluteQuantity)
            {
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee)
                {
                    Status = marketOrderStatus,
                    FillPrice = marketOrderFillPrice,
                    FillQuantity = marketOrderFillQuantity * Math.Sign(order.Quantity)
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

            var requestParams = new Dictionary<string, string>
            {
                { "instrument", SymbolMapper.GetBrokerageSymbol(order.Symbol) },
                { "units", order.AbsoluteQuantity.ConvertInvariant<int>().ToStringInvariant() },
            };

            // we need the brokerage order id in order to perform an update
            PopulateOrderRequestParameters(order, requestParams);

            if (UpdateOrder(Parse.Long(order.BrokerId.First()), requestParams))
            {
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero) { Status = OrderStatus.UpdateSubmitted });
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
                CancelOrder(Parse.Long(orderId));
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
            _eventsSession = new EventsSession(this, AccountId);
            _eventsSession.DataReceived += OnEventReceived;
            _eventsSession.StartSession();
        }

        /// <summary>
        /// Stops streaming transactions for the active account
        /// </summary>
        public override void StopTransactionStream()
        {
            if (_eventsSession != null)
            {
                _eventsSession.DataReceived -= OnEventReceived;
                _eventsSession.StopSession();
            }
        }

        /// <summary>
        /// Starts streaming prices for a list of instruments
        /// </summary>
        public override void StartPricingStream(List<string> instruments)
        {
            _ratesSession = new RatesSession(this, AccountId, instruments);
            _ratesSession.DataReceived += OnDataReceived;
            _ratesSession.StartSession();
        }

        /// <summary>
        /// Stops streaming prices for all instruments
        /// </summary>
        public override void StopPricingStream()
        {
            if (_ratesSession != null)
            {
                _ratesSession.DataReceived -= OnDataReceived;
                _ratesSession.StopSession();
            }
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

            var candles = GetCandles(oandaSymbol, startUtc, OandaBrokerage.MaxBarsPerRequest, resolution, ECandleFormat.midpoint);

            foreach (var candle in candles)
            {
                var time = OandaBrokerage.GetDateTimeFromString(candle.time);
                if (time > endTimeUtc)
                    break;

                yield return new TradeBar(
                    time.ConvertFromUtc(requestedTimeZone),
                    symbol,
                    Convert.ToDecimal(candle.openMid),
                    Convert.ToDecimal(candle.highMid),
                    Convert.ToDecimal(candle.lowMid),
                    Convert.ToDecimal(candle.closeMid),
                    0,
                    resolution.ToTimeSpan());
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

            var candles = GetCandles(oandaSymbol, startUtc, OandaBrokerage.MaxBarsPerRequest, resolution, ECandleFormat.bidask);

            foreach (var candle in candles)
            {
                var time = OandaBrokerage.GetDateTimeFromString(candle.time);
                if (time > endTimeUtc)
                    break;

                yield return new QuoteBar(
                    time.ConvertFromUtc(requestedTimeZone),
                    symbol,
                    new Bar(
                        Convert.ToDecimal(candle.openBid),
                        Convert.ToDecimal(candle.highBid),
                        Convert.ToDecimal(candle.lowBid),
                        Convert.ToDecimal(candle.closeBid)
                    ),
                    0,
                    new Bar(
                        Convert.ToDecimal(candle.openAsk),
                        Convert.ToDecimal(candle.highAsk),
                        Convert.ToDecimal(candle.lowAsk),
                        Convert.ToDecimal(candle.closeAsk)
                    ),
                    0,
                    period);
            }
        }

        private IEnumerable<Candle> GetCandles(string oandaSymbol, string startUtc, int barsPerRequest, Resolution resolution, ECandleFormat candleFormat)
        {
            var request = new CandlesRequest
            {
                instrument = oandaSymbol,
                granularity = ToGranularity(resolution),
                candleFormat = candleFormat,
                count = barsPerRequest,
                start = Uri.EscapeDataString(startUtc)
            };

            return GetCandles(request);
        }

        /// <summary>
        /// Retrieves the list of open orders belonging to the account
        /// </summary>
        /// <param name="requestParams">optional additional parameters for the request (name, value pairs)</param>
        /// <returns>List of Order objects (or empty list, if no orders)</returns>
        private IEnumerable<RestV1.DataType.Order> GetOrderList(Dictionary<string, string> requestParams = null)
        {
            var requestString = EndpointResolver.ResolveEndpoint(Environment, Server.Account) + "accounts/" + AccountId + "/orders";
            var ordersResponse = MakeRequest<OrdersResponse>(requestString, "GET", requestParams);
            var orders = new List<RestV1.DataType.Order>();
            orders.AddRange(ordersResponse.orders);
            return orders;
        }

        /// <summary>
        /// Retrieves the current non-zero positions for a given account
        /// </summary>
        /// <param name="accountId">positions will be retrieved for this account id</param>
        /// <returns>List of Position objects with the details for each position (or empty list iff no positions)</returns>
        private IEnumerable<Position> GetPositions(string accountId)
        {
            var requestString = EndpointResolver.ResolveEndpoint(Environment, Server.Account) + "accounts/" + accountId + "/positions";
            var positionResponse = MakeRequest<PositionsResponse>(requestString);
            var positions = new List<Position>();
            positions.AddRange(positionResponse.positions);
            return positions;
        }

        /// <summary>
        /// Primary (internal) request handler
        /// </summary>
        /// <typeparam name="T">The response type</typeparam>
        /// <param name="requestString">the request to make</param>
        /// <param name="method">method for the request (defaults to GET)</param>
        /// <param name="requestParams">optional parameters (note that if provided, it's assumed the requestString doesn't contain any)</param>
        /// <returns>response via type T</returns>
        private T MakeRequest<T>(string requestString, string method = "GET", Dictionary<string, string> requestParams = null)
        {
            if (requestParams != null && requestParams.Count > 0)
            {
                var parameters = CreateParamString(requestParams);
                requestString = requestString + "?" + parameters;
            }
            var request = WebRequest.CreateHttp(requestString);
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + AccessToken;
            request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
            request.Headers[OandaAgentKey] = Agent;
            request.Method = method;

            try
            {
                using (var response = request.GetResponse())
                {
                    var stream = GetResponseStream(response);
                    using (var reader = new StreamReader(stream))
                    {
                        var json = reader.ReadToEnd();
                        return JsonConvert.DeserializeObject<T>(json);
                    }
                }
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
                    var result = reader.ReadToEnd();
                    throw new Exception(result);
                }
            }
        }

        /// <summary>
        /// Secondary (internal) request handler. differs from primary in that parameters are placed in the body instead of the request string
        /// </summary>
        /// <typeparam name="T">response type</typeparam>
        /// <param name="method">method to use (usually POST or PATCH)</param>
        /// <param name="requestParams">the parameters to pass in the request body</param>
        /// <param name="requestString">the request to make</param>
        /// <returns>response, via type T</returns>
        private T MakeRequestWithBody<T>(string requestString, string method, Dictionary<string, string> requestParams)
        {
            // Create the body
            var requestBody = CreateParamString(requestParams);
            var request = WebRequest.CreateHttp(requestString);
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + AccessToken;
            request.Headers[OandaAgentKey] = Agent;
            request.Method = method;
            request.ContentType = "application/x-www-form-urlencoded";

            using (var writer = new StreamWriter(request.GetRequestStream()))
            {
                // Write the body
                writer.WriteAsync(requestBody);
            }

            // Handle the response
            try
            {
                using (var response = request.GetResponse())
                {
                    var stream = GetResponseStream(response);
                    using (var reader = new StreamReader(stream))
                    {
                        var json = reader.ReadToEnd();
                        return JsonConvert.DeserializeObject<T>(json);
                    }
                }
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
                    var result = reader.ReadToEnd();
                    throw new Exception(result);
                }
            }
        }

        /// <summary>
        /// Initializes a streaming events session which will stream events for the given accounts
        /// </summary>
        /// <param name="accountId">the account IDs you want to stream on</param>
        /// <returns>the WebResponse object that can be used to retrieve the events as they stream</returns>
        public WebResponse StartEventsSession(List<string> accountId = null)
        {
            var requestString = EndpointResolver.ResolveEndpoint(Environment, Server.StreamingEvents) + "events";

            if (accountId != null && accountId.Count > 0)
            {
                var accountIds = string.Join(",", accountId);
                requestString += "?accountIds=" + WebUtility.UrlEncode(accountIds);
            }

            var request = WebRequest.CreateHttp(requestString);
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + AccessToken;
            request.Headers[OandaAgentKey] = Agent;

            try
            {
                var response = request.GetResponse();
                return response;
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
                    var result = reader.ReadToEnd();
                    throw new Exception(result);
                }
            }
        }

        /// <summary>
        /// Initializes a streaming rates session with the given instruments on the given account
        /// </summary>
        /// <param name="instruments">list of instruments to stream rates for</param>
        /// <param name="accountId">the account ID you want to stream on</param>
        /// <returns>the WebResponse object that can be used to retrieve the rates as they stream</returns>
        public WebResponse StartRatesSession(List<string> instruments, string accountId)
        {
            var instrumentList = string.Join(",", instruments);

            var requestString = EndpointResolver.ResolveEndpoint(Environment, Server.StreamingRates) +
                "prices?accountId=" + accountId + "&instruments=" + Uri.EscapeDataString(instrumentList);

            var request = WebRequest.CreateHttp(requestString);
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + AccessToken;
            request.Headers[OandaAgentKey] = Agent;

            try
            {
                var response = request.GetResponse();
                return response;
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
                    var result = reader.ReadToEnd();
                    throw new Exception(result);
                }
            }
        }

        /// <summary>
        /// Event handler for streaming events
        /// </summary>
        /// <param name="data">The event object</param>
        private void OnEventReceived(Event data)
        {
            if (data.IsHeartbeat())
            {
                TransactionsConnectionHandler.KeepAlive(DateTime.UtcNow);
                return;
            }

            if (data.transaction != null)
            {
                if (data.transaction.type == "ORDER_FILLED")
                {
                    Order order;
                    lock (Locker)
                    {
                        order = OrderProvider.GetOrderByBrokerageId(data.transaction.orderId);
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

                            var fill = new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "Oanda Fill Event")
                            {
                                Status = OrderStatus.Filled,
                                FillPrice = (decimal)data.transaction.price,
                                FillQuantity = data.transaction.units
                            };

                            // flip the quantity on sell actions
                            if (order.Direction == OrderDirection.Sell)
                            {
                                fill.FillQuantity *= -1;
                            }
                            OnOrderEvent(fill);
                        }
                    }
                    else
                    {
                        Log.Error($"OandaBrokerage.OnEventReceived(): order id not found: {data.transaction.orderId}");
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for streaming ticks
        /// </summary>
        /// <param name="data">The data object containing the received tick</param>
        private void OnDataReceived(RateStreamResponse data)
        {
            if (data.IsHeartbeat())
            {
                PricingConnectionHandler.KeepAlive(DateTime.UtcNow);
                return;
            }

            if (data.tick == null) return;

            var securityType = SymbolMapper.GetBrokerageSecurityType(data.tick.instrument);
            var symbol = SymbolMapper.GetLeanSymbol(data.tick.instrument, securityType, Market.Oanda);
            var time = OandaBrokerage.GetDateTimeFromString(data.tick.time);

            // live ticks timestamps must be in exchange time zone
            DateTimeZone exchangeTimeZone;
            if (!_symbolExchangeTimeZones.TryGetValue(symbol, out exchangeTimeZone))
            {
                exchangeTimeZone = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, symbol, securityType).TimeZone;
                _symbolExchangeTimeZones.Add(symbol, exchangeTimeZone);
            }
            time = time.ConvertFromUtc(exchangeTimeZone);

            var bidPrice = Convert.ToDecimal(data.tick.bid);
            var askPrice = Convert.ToDecimal(data.tick.ask);
            var tick = new Tick(time, symbol, bidPrice, askPrice);

            EmitTick(tick);
        }

        /// <summary>
        /// Helper function to create the parameter string out of a dictionary of parameters
        /// </summary>
        /// <param name="requestParams">the parameters to convert</param>
        /// <returns>string containing all the parameters for use in requests</returns>
        private static string CreateParamString(Dictionary<string, string> requestParams)
        {
            return string.Join("&", requestParams.Select(x => WebUtility.UrlEncode(x.Key) + "=" + WebUtility.UrlEncode(x.Value)));
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
        /// Converts the specified Oanda order into a qc order.
        /// The 'task' will have a value if we needed to issue a rest call for the stop price, otherwise it will be null
        /// </summary>
        private Order ConvertOrder(RestV1.DataType.Order order)
        {
            Order qcOrder;
            switch (order.type)
            {
                case "limit":
                    qcOrder = new LimitOrder();
                    if (order.side == "buy")
                    {
                        ((LimitOrder)qcOrder).LimitPrice = Convert.ToDecimal(order.lowerBound);
                    }

                    if (order.side == "sell")
                    {
                        ((LimitOrder)qcOrder).LimitPrice = Convert.ToDecimal(order.upperBound);
                    }
                    break;

                case "stop":
                    qcOrder = new StopLimitOrder();
                    if (order.side == "buy")
                    {
                        ((StopLimitOrder)qcOrder).LimitPrice = Convert.ToDecimal(order.lowerBound);
                    }

                    if (order.side == "sell")
                    {
                        ((StopLimitOrder)qcOrder).LimitPrice = Convert.ToDecimal(order.upperBound);
                    }
                    break;

                case "marketIfTouched":
                    //when market reaches the price sell at market.
                    qcOrder = new StopMarketOrder { Price = Convert.ToDecimal(order.price), StopPrice = Convert.ToDecimal(order.price) };
                    break;

                case "market":
                    qcOrder = new MarketOrder();
                    break;

                default:
                    throw new NotSupportedException("The Oanda order type " + order.type + " is not supported.");
            }

            var securityType = SymbolMapper.GetBrokerageSecurityType(order.instrument);
            qcOrder.Symbol = SymbolMapper.GetLeanSymbol(order.instrument, securityType, Market.Oanda);
            qcOrder.Quantity = ConvertQuantity(order);
            qcOrder.Status = OrderStatus.None;
            qcOrder.BrokerId.Add(order.id.ToStringInvariant());

            var orderByBrokerageId = OrderProvider.GetOrderByBrokerageId(order.id);
            if (orderByBrokerageId != null)
            {
                qcOrder.Id = orderByBrokerageId.Id;
            }

            var expiry = XmlConvert.ToDateTime(order.expiry, XmlDateTimeSerializationMode.Utc);
            qcOrder.Properties.TimeInForce = TimeInForce.GoodTilDate(expiry);
            qcOrder.Time = XmlConvert.ToDateTime(order.time, XmlDateTimeSerializationMode.Utc);

            return qcOrder;
        }

        /// <summary>
        /// Converts the Oanda order quantity into a qc quantity
        /// </summary>
        /// <remarks>
        /// Oanda quantities are always positive and use the direction to denote +/-, where as qc
        /// order quantities determine the direction
        /// </remarks>
        private static int ConvertQuantity(RestV1.DataType.Order order)
        {
            switch (order.side)
            {
                case "buy":
                    return order.units;

                case "sell":
                    return -order.units;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Converts the Oanda position into a QuantConnect holding.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        private Holding ConvertHolding(Position position)
        {
            var securityType = SymbolMapper.GetBrokerageSecurityType(position.instrument);

            return new Holding
            {
                Symbol = SymbolMapper.GetLeanSymbol(position.instrument, securityType, Market.Oanda),
                Type = securityType,
                AveragePrice = (decimal)position.avgPrice,
                CurrencySymbol = "$",
                Quantity = position.side == "sell" ? -position.units : position.units
            };
        }

        private static void PopulateOrderRequestParameters(Order order, Dictionary<string, string> requestParams)
        {
            if (order.Direction != OrderDirection.Buy && order.Direction != OrderDirection.Sell)
            {
                throw new Exception("Invalid Order Direction");
            }

            requestParams.Add("side", order.Direction == OrderDirection.Buy ? "buy" : "sell");

            if (order.Type == OrderType.Market)
            {
                requestParams.Add("type", "market");
            }

            if (order.Type == OrderType.Limit)
            {
                requestParams.Add("type", "limit");
                requestParams.Add("price", ((LimitOrder)order).LimitPrice.ToString(CultureInfo.InvariantCulture));

                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        //Limit Order Does not like Lower Bound Values == Limit Price value
                        //Don't set bounds when placing limit orders.
                        //Orders can be submitted with lower and upper bounds. If the market price on execution falls outside these bounds, it is considered a "Bounds Violation" and the order is cancelled.
                        break;

                    case OrderDirection.Sell:
                        //Limit Order Does not like Lower Bound Values == Limit Price value
                        //Don't set bounds when placing limit orders.
                        //Orders can be submitted with lower and upper bounds. If the market price on execution falls outside these bounds, it is considered a "Bounds Violation" and the order is cancelled.
                        break;
                }

                //3 months is the max expiry for Oanda, and OrderDuration.GTC is only currently available
                requestParams.Add("expiry", XmlConvert.ToString(DateTime.Now.AddMonths(3), XmlDateTimeSerializationMode.Utc));
            }

            //this type should contain a stop and a limit to that stop.
            if (order.Type == OrderType.StopLimit)
            {
                requestParams.Add("type", "stop");
                requestParams.Add("price", ((StopLimitOrder)order).StopPrice.ToString(CultureInfo.InvariantCulture));

                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        requestParams.Add("upperBound", ((StopLimitOrder)order).LimitPrice.ToString(CultureInfo.InvariantCulture));
                        break;

                    case OrderDirection.Sell:
                        requestParams.Add("lowerBound", ((StopLimitOrder)order).LimitPrice.ToString(CultureInfo.InvariantCulture));
                        break;
                }

                //3 months is the max expiry for Oanda, and OrderDuration.GTC is only currently available
                requestParams.Add("expiry", XmlConvert.ToString(DateTime.Now.AddMonths(3), XmlDateTimeSerializationMode.Utc));
            }

            if (order.Type == OrderType.StopMarket)
            {
                requestParams.Add("type", "marketIfTouched");
                requestParams.Add("price", ((StopMarketOrder)order).StopPrice.ToString(CultureInfo.InvariantCulture));

                //3 months is the max expiry for Oanda, and OrderDuration.GTC is only currently available
                requestParams.Add("expiry", XmlConvert.ToString(DateTime.Now.AddMonths(3), XmlDateTimeSerializationMode.Utc));
            }
        }

        /// <summary>
        /// Posts an order on the given account with the given parameters
        /// </summary>
        /// <param name="requestParams">the parameters to use in the request</param>
        /// <returns>PostOrderResponse with details of the results (throws if if fails)</returns>
        private PostOrderResponse PostOrderAsync(Dictionary<string, string> requestParams)
        {
            var requestString = EndpointResolver.ResolveEndpoint(Environment, Server.Account) + "accounts/" + AccountId + "/orders";
            return MakeRequestWithBody<PostOrderResponse>(requestString, "POST", requestParams);
        }

        /// <summary>
        /// Modify the specified order, updating it with the parameters provided
        /// </summary>
        /// <param name="orderId">the identifier of the order to update</param>
        /// <param name="requestParams">the parameters to update (name, value pairs)</param>
        private bool UpdateOrder(long orderId, Dictionary<string, string> requestParams)
        {
            var orderRequest = EndpointResolver.ResolveEndpoint(Environment, Server.Account) + "accounts/" + AccountId + "/orders/" + orderId;

            var order = MakeRequest<RestV1.DataType.Order>(orderRequest);
            if (order != null && order.id > 0)
            {
                var requestString = EndpointResolver.ResolveEndpoint(Environment, Server.Account) + "accounts/" + AccountId + "/orders/" + orderId;
                MakeRequestWithBody<RestV1.DataType.Order>(requestString, "PATCH", requestParams);
                return true;
            }
            else
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UpdateFailed", "Failed to update Oanda order id: " + orderId + "."));
                OnOrderEvent(new OrderEvent(ConvertOrder(order), DateTime.UtcNow, OrderFee.Zero)
                {
                    Status = OrderStatus.Invalid,
                    Message = $"Order currently does not exist with order id: {orderId.ToStringInvariant()}."
                });
            }
            return false;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="orderId">The order id</param>
        private void CancelOrder(long orderId)
        {
            var requestString = EndpointResolver.ResolveEndpoint(Environment, Server.Account) + "accounts/" + AccountId + "/orders/" + orderId;
            MakeRequest<RestV1.DataType.Order>(requestString, "DELETE");
        }

        /// <summary>
        /// More detailed request to retrieve candles
        /// </summary>
        /// <param name="request">the request data to use when retrieving the candles</param>
        /// <returns>List of Candles received (or empty list)</returns>
        private IEnumerable<Candle> GetCandles(Request request)
        {
            var requestString = EndpointResolver.ResolveEndpoint(Environment, Server.Rates) + request.GetRequestString();

            var candlesResponse = MakeRequest<CandlesResponse>(requestString);

            var candles = new List<Candle>();
            if (candlesResponse != null)
            {
                candles.AddRange(candlesResponse.candles);
            }

            return candles;
        }

        /// <summary>
        /// Converts a LEAN Resolution to an EGranularity
        /// </summary>
        /// <param name="resolution">The resolution to convert</param>
        /// <returns></returns>
        private static EGranularity ToGranularity(Resolution resolution)
        {
            EGranularity interval;

            switch (resolution)
            {
                case Resolution.Second:
                    interval = EGranularity.S5;
                    break;

                case Resolution.Minute:
                    interval = EGranularity.M1;
                    break;

                case Resolution.Hour:
                    interval = EGranularity.H1;
                    break;

                case Resolution.Daily:
                    interval = EGranularity.D;
                    break;

                default:
                    throw new ArgumentException("Unsupported resolution: " + resolution);
            }

            return interval;
        }

    }
}
