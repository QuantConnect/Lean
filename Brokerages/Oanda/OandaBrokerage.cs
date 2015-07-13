using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using QuantConnect.Brokerages.Oanda.DataType;
using QuantConnect.Brokerages.Oanda.DataType.Communications;
using QuantConnect.Brokerages.Oanda.Framework;
using QuantConnect.Brokerages.Oanda.Session;
using QuantConnect.Brokerages.Tradier;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using RestSharp;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Oanda Brokerage model.
    /// </summary>
    public class OandaBrokerage : Brokerage
    {
        /// <summary>
        /// The account identifier for Oanda Accounts.
        /// </summary>
        public int AccountId { get; private set; }

        private DateTime _issuedAt;

        private TimeSpan _lifeSpan = TimeSpan.FromSeconds(86399); // 1 second less than a day

        private Timer _orderFillTimer;

        private readonly object _fillLock = new object();
        
        private readonly Dictionary<string, SecurityType> _instrumentSecurityTypeMap;

        /// <summary>
        /// Gets the oanda environment.
        /// </summary>
        /// <value>
        /// The oanda environment.
        /// </value>
        public static Environment OandaEnvironment { get; private set; }

        private readonly object _lockAccessCredentials = new object();

        //This should correlate to the Trades/Positions list in Oanda.
        private readonly IHoldingsProvider _holdingsProvider;

        //This should correlate to the Orders list in Oanda.
        private readonly IOrderProvider _orderProvider;

        /// <summary>
        /// The QC User id, used for refreshing the session
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// Access Token Access:
        /// </summary>
        public static string AccessToken { get; private set; }

        /// <summary>
        /// Refresh Token Access:
        /// </summary>
        // TODO not sure if the refresh token is necessary
        public string RefreshToken { get; private set; }


        /// <summary>
        /// Creates a new Brokerage instance with the specified name
        /// </summary>
        /// <param name="name">The name of the brokerage</param>
        public OandaBrokerage(string name) : base(name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OandaBrokerage"/> class.
        /// </summary>
        /// <param name="orderMapping">The order mapping.</param>
        /// <param name="holdingsProvider">The holdings provider.</param>
        /// <param name="accountId">The account identifier.</param>
        public OandaBrokerage(IOrderProvider orderProvider, IHoldingsProvider holdingsProvider, int accountId)
            : base("Oanda Brokerage")
        {
            _orderProvider = orderProvider;
            _holdingsProvider = holdingsProvider;
            AccountId = accountId;
            _instrumentSecurityTypeMap = MapInstrumentToSecurityType(GetInstrumentsAsync().Result);
        }

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected
        {
            get { return _issuedAt + _lifeSpan > DateTime.Now; }
        }

        /// <summary>
        /// Gets all open orders on the account. 
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from Oanda</returns>
        public override List<Order> GetOpenOrders()
        {
            var orderList = GetOrderListAsync().Result.Select(ConvertOrder).ToList();
            return orderList;
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            var holdings = GetPositions().Select(ConvertHolding).ToList();
            return holdings;
        }

        /// <summary>
        /// Converts the Oanda position into a QuantConnect holding.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        protected Holding ConvertHolding(Position position)
        {
            return new Holding
            {
                Symbol = position.instrument,
                Type = _instrumentSecurityTypeMap[position.instrument],
                AveragePrice = (decimal)position.avgPrice,
                ConversionRate = 1.0m,
                CurrencySymbol = "$",
                Quantity = position.units
            };
        }



        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<Cash> GetCashBalance()
        {
            var cash = new List<Cash>();
            var requestString = EndpointResolver.ResolveEndpoint(OandaEnvironment, Server.Account) + "accounts/" + AccountId;
            using (var task = MakeRequestAsync<Account>(requestString))
            {
                task.Wait();
                var accountResponse = task.Result;
                //TODO figure how exchange rates work in Oanda. probably need to call http://developer.oanda.com/exchange-rates-api/v1/currencies/
                cash.Add(new Cash(accountResponse.accountCurrency, accountResponse.balance.ToDecimal(), new decimal(1.0)));
            }
            return cash;
        }
        
        private static string GetCommaSeparatedList(IEnumerable<string> items)
        {
            var result = new StringBuilder();
            foreach (var item in items)
            {
                result.Append(item + ",");
            }
            return result.ToString().Trim(',');
        }


        private Dictionary<string, SecurityType> MapInstrumentToSecurityType(List<Instrument> instruments)
        {
            var result = new Dictionary<string, SecurityType>();
            foreach (var instrument in instruments)
            {
                var isForex = Regex.IsMatch(instrument.instrument, "[A-Z]{3}_[A-Z]{3}") && Regex.IsMatch(instrument.displayName, "[A-Z]{3}/[A-Z]{3}");
                result.Add(instrument.instrument, isForex ? SecurityType.Forex : SecurityType.Cfd);
            }
            return result;
        }

        /// <summary>
        /// Gets the list of available tradable instruments/products from Oanda
        /// </summary>
        /// <returns></returns>
        public async Task<List<Instrument>> GetInstrumentsAsync(List<string> instrumentNames = null)
        {
            var requestString = EndpointResolver.ResolveEndpoint(OandaEnvironment, Server.Rates) + "instruments?accountId=" + AccountId;
            if (instrumentNames != null)
            {
                var instrumentsParam = GetCommaSeparatedList(instrumentNames);
                requestString += "&instruments=" + Uri.EscapeDataString(instrumentsParam);
            }
            var instrumentResponse = await MakeRequestAsync<InstrumentsResponse>(requestString);
            var instruments = new List<Instrument>();
            instruments.AddRange(instrumentResponse.instruments);
            return instruments;
        }

        /// <summary>
        /// Secondary (internal) request handler. differs from primary in that parameters are placed in the body instead of the request string
        /// </summary>
        /// <typeparam name="T">response type</typeparam>
        /// <param name="method">method to use (usually POST or PATCH)</param>
        /// <param name="requestParams">the parameters to pass in the request body</param>
        /// <param name="requestString">the request to make</param>
        /// <returns>response, via type T</returns>
        private async Task<T> MakeRequestWithBody<T>(string method, Dictionary<string, string> requestParams, string requestString)
        {
            // Create the body
            var requestBody = CreateParamString(requestParams);
            var request = WebRequest.CreateHttp(requestString);
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + AccessToken;
            request.Method = method;
            request.ContentType = "application/x-www-form-urlencoded";

            using (var writer = new StreamWriter(await request.GetRequestStreamAsync()))
            {
                // Write the body
                await writer.WriteAsync(requestBody);
            }

            // Handle the response
            try
            {
                using (var response = await request.GetResponseAsync())
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    return (T)serializer.ReadObject(response.GetResponseStream());
                }
            }
            catch (WebException ex)
            {
                var response = (HttpWebResponse)ex.Response;
                var stream = new StreamReader(response.GetResponseStream());
                var result = stream.ReadToEnd();
                throw new Exception(result);
            }
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
                {"instrument", order.Symbol},
                {"units", Convert.ToInt32(order.AbsoluteQuantity).ToString()}
            };

            PopulateOrderRequestParameters(order, requestParams);

            Log.Trace(string.Format("OandaBrokerage.PlaceOrder(): {0} to {1} {2} units of {3}", order.Type, order.Direction, order.Quantity, order.Symbol));


            var priorOrderPositions = GetTradeListAsync(requestParams).Result;

            var postOrderResponse = PostOrderAsync(requestParams).Result;

            if (postOrderResponse != null)
            {
                OnOrderEvent(new OrderEvent(order) { Status = OrderStatus.Submitted });
            } 
            else
            {
                return false;
            }

            // we need to determine if there was an existing order and wheter we closed it with market orders.

            if (order.Type == OrderType.Market && order.Direction == OrderDirection.Buy)
            {
                //assume that we are opening a new buy market order
                if (postOrderResponse.tradeOpened != null && postOrderResponse.tradeOpened.id > 0)
                {
                    var tradeOpenedId = postOrderResponse.tradeOpened.id;
                    requestParams = new Dictionary<string, string>();
                    var tradeListResponse = GetTradeListAsync(requestParams).Result;
                    if (tradeListResponse.trades.Any(trade => trade.id == tradeOpenedId))
                    {
                        OnOrderEvent(new OrderEvent(order) { Status = OrderStatus.Filled });
                    }
                }

                if (postOrderResponse.tradesClosed != null)
                {
                    var tradePositionClosedIds = postOrderResponse.tradesClosed.Select(tradesClosed => tradesClosed.id).ToList();
                    var priorOrderPositionIds = priorOrderPositions.trades.Select(previousTrade => previousTrade.id).ToList();
                    var verifyClosedOrder = tradePositionClosedIds.Intersect(priorOrderPositionIds).Count() == tradePositionClosedIds.Count();
                    if (verifyClosedOrder)
                    {
                        OnOrderEvent(new OrderEvent(order) { Status = OrderStatus.Filled });
                    }
                }
            }

            if (order.Type == OrderType.Market && order.Direction == OrderDirection.Sell)
            {                
                //assume that we are opening a new buy market order
                if (postOrderResponse.tradeOpened != null && postOrderResponse.tradeOpened.id > 0)
                {
                    var tradeOpenedId = postOrderResponse.tradeOpened.id;
                    requestParams = new Dictionary<string, string>();
                    var tradeListResponse = GetTradeListAsync(requestParams).Result;
                    if (tradeListResponse.trades.Any(trade => trade.id == tradeOpenedId))
                    {
                        OnOrderEvent(new OrderEvent(order) { Status = OrderStatus.Filled });
                    }
                }

                if (postOrderResponse.tradesClosed != null)
                {
                    var tradePositionClosedIds = postOrderResponse.tradesClosed.Select(tradesClosed => tradesClosed.id).ToList();
                    var priorOrderPositionIds = priorOrderPositions.trades.Select(previousTrade => previousTrade.id).ToList();
                    var verifyClosedOrder = tradePositionClosedIds.Intersect(priorOrderPositionIds).Count() == tradePositionClosedIds.Count();
                    if (verifyClosedOrder)
                    {
                        OnOrderEvent(new OrderEvent(order) { Status = OrderStatus.Filled });   
                    }
                }
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

                //var orderDuration = GetOrderDuration(order.Duration);
                //var limitPrice = GetLimitPrice(order);
                //var stopPrice = GetStopPrice(order);

            }

            throw new NotImplementedException();
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
                requestParams.Add("price", order.Price.ToString(CultureInfo.InvariantCulture));
                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        requestParams.Add("lowerBound", ((LimitOrder) order).LimitPrice.ToString(CultureInfo.InvariantCulture));
                        break;

                    case OrderDirection.Sell:
                        requestParams.Add("upperBound", ((LimitOrder) order).LimitPrice.ToString(CultureInfo.InvariantCulture));
                        break;
                }

                requestParams.Add("expiry", XmlConvert.ToString(order.DurationValue, XmlDateTimeSerializationMode.Utc));
            }

            //this type should contain a stop and a limit to that stop.
            if (order.Type == OrderType.StopLimit)
            {
                requestParams.Add("type", "stop");
                requestParams.Add("price", order.Price.ToString(CultureInfo.InvariantCulture));
                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        requestParams.Add("upperBound",
                            ((StopLimitOrder) order).StopPrice.ToString(CultureInfo.InvariantCulture));
                        break;
                    case OrderDirection.Sell:
                        requestParams.Add("lowerBound",
                            ((StopLimitOrder) order).StopPrice.ToString(CultureInfo.InvariantCulture));
                        break;
                }
                requestParams.Add("expiry", XmlConvert.ToString(order.DurationValue, XmlDateTimeSerializationMode.Utc));
            }

            if (order.Type == OrderType.StopMarket)
            {
                requestParams.Add("type", "marketIftouched");
                requestParams.Add("price", order.Price.ToString(CultureInfo.InvariantCulture));
                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        requestParams.Add("upperBound", ((StopMarketOrder) order).Price.ToString(CultureInfo.InvariantCulture));
                        break;
                    case OrderDirection.Sell:
                        requestParams.Add("lowerBound", ((StopLimitOrder) order).Price.ToString(CultureInfo.InvariantCulture));
                        break;
                }
                requestParams.Add("expiry", XmlConvert.ToString(order.DurationValue, XmlDateTimeSerializationMode.Utc));
            }
        }


        /// <summary>
        /// Checks for fill events by registering to the event session to receive events.
        /// </summary>
        private void CheckForFills()
        {
            // reentrance gaurd
            if (!Monitor.TryEnter(_fillLock))
            {
                return;
            }
            try
            {
                var session = new EventsSession(AccountId);
                session.DataReceived += OnEventReceived;
                session.StartSession();
            }
            finally
            {
                Monitor.Exit(_fillLock);
            }
        }

        private void OnEventReceived(Event data)
        {
            Console.Out.Write("---- On Event Received ----");
            Console.Out.Write(data.transaction);
        }

        /// <summary>
        /// Obtain the active open Trade List from Oanda.
        /// </summary>
        /// <param name="requestParams"></param>
        /// <returns></returns>
        public async Task<TradesResponse> GetTradeListAsync(Dictionary<string, string> requestParams = null)
        {
            var requestString = EndpointResolver.ResolveEndpoint(OandaEnvironment, Server.Account) + "accounts/" + AccountId + "/trades";
            return await MakeRequestAsync<TradesResponse>(requestString, "GET", requestParams);
        }

        /// <summary>
        /// Retrieves the current rate for each of a list of instruments
        /// </summary>
        /// <param name="instruments">the list of instruments to check</param>
        /// <returns>List of Price objects with the current price for each instrument</returns>
        public async Task<List<Price>> GetRatesAsync(List<Instrument> instruments)
        {
            var requestBuilder = new StringBuilder(EndpointResolver.ResolveEndpoint(OandaEnvironment, Server.Rates) + "prices?instruments=");

            foreach (var instrument in instruments)
            {
                requestBuilder.Append(instrument.instrument + ",");
            }
            var requestString = requestBuilder.ToString().Trim(',');
            requestString = requestString.Replace(",", "%2C");

            var pricesResponse = await MakeRequestAsync<PricesResponse>(requestString);
            var prices = new List<Price>();
            prices.AddRange(pricesResponse.prices);

            return prices;
        }

        /// <summary>
        /// Posts an order on the given account with the given parameters
        /// </summary>
        /// <param name="requestParams">the parameters to use in the request</param>
        /// <returns>PostOrderResponse with details of the results (throws if if fails)</returns>
        public async Task<PostOrderResponse> PostOrderAsync(Dictionary<string, string> requestParams)
        {
            var requestString = EndpointResolver.ResolveEndpoint(OandaEnvironment, Server.Account) + "accounts/" + AccountId + "/orders";
            return await MakeRequestWithBody<PostOrderResponse>("POST", requestParams, requestString);
        }

        /// <summary>
        /// Retrieves the list of open orders belonging to the account
        /// </summary>
        /// <param name="requestParams">optional additional parameters for the request (name, value pairs)</param>
        /// <returns>List of Order objects (or empty list, if no orders)</returns>
        public async Task<List<DataType.Order>> GetOrderListAsync(Dictionary<string, string> requestParams = null)
        {
            var requestString = EndpointResolver.ResolveEndpoint(OandaEnvironment, Server.Account) + "accounts/" + AccountId + "/orders";
            var ordersResponse = await MakeRequestAsync<OrdersResponse>(requestString, "GET", requestParams);
            var orders = new List<DataType.Order>();
            orders.AddRange(ordersResponse.orders);
            return orders;
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
                var response = CancelOrderAsync(orderId);
                OnOrderEvent(new OrderEvent(order, "Oanda Cancel Order Event") { Status = OrderStatus.Canceled });
            }

            return true;
        }

        private async Task<Order> CancelOrderAsync(long orderId)
        {
            var requestString = EndpointResolver.ResolveEndpoint(OandaEnvironment, Server.Account) + "accounts/" + AccountId + "/orders/" + orderId;
            return await MakeRequestAsync<Order>(requestString, "DELETE");
        }
        

        /// <summary>
        /// Verify we have a user session; or refresh the access token.
        /// </summary>
        public bool RefreshSession()
        {
            var raw = "";
            bool success;
            lock (_lockAccessCredentials)
            {
                //Create the client for connection:
                var client = new RestClient("https://www.quantconnect.com/terminal/");

                //Create the GET call:
                var request = new RestRequest("processOanda", Method.GET);
                request.AddParameter("uid", UserId.ToString(), ParameterType.GetOrPost);
                request.AddParameter("accessToken", AccessToken, ParameterType.GetOrPost);
                request.AddParameter("refreshToken", RefreshToken, ParameterType.GetOrPost);

                //Submit the call:
                var result = client.Execute(request);
                raw = result.Content;

                //Decode to token response: update internal access parameters:
                var newTokens = JsonConvert.DeserializeObject<TokenResponse>(result.Content); 
                if (newTokens != null && newTokens.Success)
                {
                    AccessToken = newTokens.AccessToken;
                    RefreshToken = newTokens.RefreshToken;
                    _issuedAt = newTokens.IssuedAt;
                    _lifeSpan = TimeSpan.FromSeconds(newTokens.ExpiresIn);
                    Log.Trace("SESSION REFRESHED: Access: " + AccessToken + " Refresh: " + RefreshToken + " Issued At: " + _lifeSpan + " JSON>>"
                        + result.Content);
                    OnSessionRefreshed(newTokens);
                    success = true;
                } 
                else
                {
                    Log.Error("Oanda.RefreshSession(): Error Refreshing Session: URL: " + client.BuildUri(request) + " Response: " + result.Content);
                    success = false;
                }
            }

            if (!success)
            {
                // if we can't refresh our tokens then we must stop the algorithm
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "RefreshSession", "Failed to refresh access token: " + raw));
            }

            return success;
        }

        /// <summary>
        /// Event invocator for the SessionRefreshed event
        /// </summary>
        protected virtual void OnSessionRefreshed(TokenResponse e)
        {
            var handler = SessionRefreshed;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            if (IsConnected) return;
            RefreshSession();
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Event fired when our session has been refreshed/tokens updated
        /// </summary>
        public event EventHandler<TokenResponse> SessionRefreshed;

        /// <summary>
        /// Set the access token and login information for the Oanda brokerage 
        /// </summary>
        /// <param name="userId">Userid for this brokerage</param>
        /// <param name="accessToken">Viable access token</param>
        /// <param name="refreshToken">Our refresh token</param>
        /// <param name="issuedAt">When the token was issued</param>
        /// <param name="lifeSpan">Life span for our token.</param>
        public void SetTokens(int userId, string accessToken, string refreshToken, DateTime issuedAt, TimeSpan lifeSpan)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            _issuedAt = issuedAt;
            _lifeSpan = lifeSpan;
            CheckForFills();
        }

        private static Stream GetResponseStream(WebResponse response)
        {
            var stream = response.GetResponseStream();
            if (response.Headers["Content-Encoding"] == "gzip")
            {	// if we received a gzipped response, handle that
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }
            return stream;
        }

        /// <summary>
        /// Initializes a streaming events session which will stream events for the given accounts
        /// </summary>
        /// <param name="accountId">the account IDs you want to stream on</param>
        /// <returns>the WebResponse object that can be used to retrieve the events as they stream</returns>
        public static async Task<WebResponse> StartEventsSession(List<int> accountId = null)
        {
            var requestString = EndpointResolver.ResolveEndpoint(OandaEnvironment, Server.StreamingEvents) + "events";

            if (accountId != null && accountId.Count > 0)
            {
                var accountIds = accountId.Aggregate("", (current, account) => current + (account + ","));
                accountIds = accountIds.Trim(',');
                requestString += "?accountIds=" + WebUtility.UrlEncode(accountIds);
            }

            var request = WebRequest.CreateHttp(requestString);
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + AccessToken;

            try
            {
                var response = await request.GetResponseAsync();
                return response;
            }
            catch (WebException ex)
            {
                var response = (HttpWebResponse)ex.Response;
                var stream = new StreamReader(response.GetResponseStream());
                var result = stream.ReadToEnd();
                throw new Exception(result);
            }
        }

        /// <summary>
        /// Primary (internal) request handler
        /// </summary>
        /// <typeparam name="T">The response type</typeparam>
        /// <param name="requestString">the request to make</param>
        /// <param name="method">method for the request (defaults to GET)</param>
        /// <param name="requestParams">optional parameters (note that if provided, it's assumed the requestString doesn't contain any)</param>
        /// <returns>response via type T</returns>
        public async Task<T> MakeRequestAsync<T>(string requestString, string method = "GET", Dictionary<string, string> requestParams = null)
        {
            if (requestParams != null && requestParams.Count > 0)
            {
                var parameters = CreateParamString(requestParams);
                requestString = requestString + "?" + parameters;
            }
            var request = WebRequest.CreateHttp(requestString);
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + AccessToken;
            request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
            request.Method = method;

            try
            {
                using (var response = await request.GetResponseAsync())
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    var stream = GetResponseStream(response);
                    return (T)serializer.ReadObject(stream);
                }
            }
            catch (WebException ex)
            {
                var stream = GetResponseStream(ex.Response);
                var reader = new StreamReader(stream);
                var result = reader.ReadToEnd();
                throw new Exception(result);
            }
        }

        private List<Position> GetPositions()
        {
            using (var task = GetPositionsAsync(AccountId))
            {
                task.Wait();
                var result = task.Result;
                return result;
            }
        }

        /// <summary>
        /// Retrieves the current non-zero positions for a given account
        /// </summary>
        /// <param name="accountId">positions will be retrieved for this account id</param>
        /// <returns>List of Position objects with the details for each position (or empty list iff no positions)</returns>
        public async Task<List<Position>> GetPositionsAsync(int accountId)
        {
            var requestString = EndpointResolver.ResolveEndpoint(OandaEnvironment, Server.Account) + "accounts/" + accountId + "/positions";
            var positionResponse = await MakeRequestAsync<PositionsResponse>(requestString);
            var positions = new List<Position>();
            positions.AddRange(positionResponse.positions);
            return positions;
        }

        /// <summary>
        /// Helper function to create the parameter string out of a dictionary of parameters
        /// </summary>
        /// <param name="requestParams">the parameters to convert</param>
        /// <returns>string containing all the parameters for use in requests</returns>
        private static string CreateParamString(Dictionary<string, string> requestParams)
        {
            var requestBody = requestParams.Aggregate("", (current, pair) => current + (WebUtility.UrlEncode(pair.Key) + "=" + WebUtility.UrlEncode(pair.Value) + "&"));
            requestBody = requestBody.Trim('&');
            return requestBody;
        }

        /// <summary>
        /// Sets the account identifier.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        public void SetAccountId(int accountId)
        {
            AccountId = accountId;
        }

        /// <summary>
        /// Sets the Brokerage environment.
        /// </summary>
        /// <param name="environment">The oanda environment.</param>
        public void SetEnvironment(string environment)
        {
            switch (environment.ToLowerInvariant())
            {
                case "sandbox":
                    OandaEnvironment = Environment.Sandbox;
                    break;
                case "practice":
                    OandaEnvironment = Environment.Practice;
                    break;
                case "trade":
                    OandaEnvironment = Environment.Trade;
                    break;
                default:
                    throw new ArgumentException("Unexpected or unexpected Oanda Environment: " + environment);
            }

        }
        
        /// <summary>
        /// Converts the specified tradier order into a qc order.
        /// The 'task' will have a value if we needed to issue a rest call for the stop price, otherwise it will be null
        /// </summary>
        protected Order ConvertOrder(DataType.Order order)
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
                    qcOrder = new StopMarketOrder { Price = Convert.ToDecimal(order.price), StopPrice = Convert.ToDecimal(order.price)};
                    break;
                case "market":
                    qcOrder = new MarketOrder();
                    break;

                default:
                    throw new NotImplementedException("The Oanda order type " + order.type + " is not implemented.");
            }
            qcOrder.Symbol = order.instrument;
            qcOrder.Quantity = order.units;
            qcOrder.SecurityType = _instrumentSecurityTypeMap[order.instrument];

            //TODO need further thought on how to translate this to QC, typically any open order 
            qcOrder.Status = OrderStatus.None;
            qcOrder.BrokerId.Add(order.id);
            qcOrder.Duration = OrderDuration.Specific;
            qcOrder.DurationValue = XmlConvert.ToDateTime(order.expiry, XmlDateTimeSerializationMode.Utc);
            qcOrder.Time = XmlConvert.ToDateTime(order.time, XmlDateTimeSerializationMode.Utc);
            
            return qcOrder;
        }
    }
}
