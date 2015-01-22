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
 *
 *	TRADIER BROKERAGE CONTROLLER
*/

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using QuantConnect.Logging;
using RestSharp;

namespace QuantConnect.Brokerages.Tradier
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Tradier Class: 
    ///  - Handle authentication.
    ///  - Data requests.
    ///  - Rate limiting.
    ///  - Placing orders.
    ///  - Getting user data.
    /// </summary>
    public class TradierBrokerage : Brokerage
    {
        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// When we expect this access token to expire
        /// </summary>
        private DateTime ExpectedExpiry
        {
            get
            {
                return _issuedAt + _lifeSpan - TimeSpan.FromMinutes(60);
            }
        }

        /// <summary>
        /// Store a list of errors which have occurred in the API
        /// </summary>
        public List<TradierFault> Faults
        {
            get
            {
                return _faults;
            }
        }

        /// <summary>
        /// Brokerage Name:
        /// </summary>
        public override string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        /// <summary>
        /// Access Token Access:
        /// </summary>
        public string AccessToken
        {
            get
            {
                return _accessToken;
            }
            set
            {
                _accessToken = value;
            }
        }

        /// <summary>
        /// Refresh Token Access:
        /// </summary>
        public string RefreshToken
        {
            get
            {
                return _refreshToken;
            }
            set
            {
                _refreshToken = value;
            }
        }

        /// <summary>
        /// Get the last string returned
        /// </summary>
        public string LastRequest
        {
            get
            {
                return _previousRequestRaw;
            }
        }

        /******************************************************** 
        * PRIVATE VARIABLES
        *********************************************************/
        //Access and Refresh Tokens:
        private string _accessToken = "";
        private string _refreshToken = "";
        private string _previousRequestRaw = "";
        private int _userId = 0;
        private string _name = "Tradier";
        private DateTime _issuedAt = new DateTime();
        private string _scope = "";
        private TimeSpan _lifeSpan = TimeSpan.FromSeconds(86399); // 1 second less than a day
        private object _lockAccessCredentials = new Object();

        //Tradier Spec:
        private Dictionary<TradierApiRequestType, TimeSpan> _rateLimitPeriod;
        private Dictionary<TradierApiRequestType, DateTime> _rateLimitNextRequest;
        private Dictionary<string, Action> _errorHandlers = new Dictionary<string, Action>();
        private List<TradierFault> _faults = new List<TradierFault>();

        //Endpoints:
        private string _requestEndpoint = @"https://api.tradier.com/v1/";
        private readonly Timer _timer;

        /******************************************************** 
        * CLASS CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Create a new Tradier Object:
        /// </summary>
        public TradierBrokerage()
        {
            //Common Brokerage Class:
            Name = "Tradier";

            //Tradier Specific Initialization:
            _rateLimitPeriod = new Dictionary<TradierApiRequestType, TimeSpan>();
            _rateLimitNextRequest = new Dictionary<TradierApiRequestType, DateTime>();

            //Go through each API request type and initialize:
            foreach (TradierApiRequestType requestType in Enum.GetValues(typeof(TradierApiRequestType)))
            {
                //Sandbox and most live are 1sec
                _rateLimitPeriod.Add(requestType, TimeSpan.FromMilliseconds(1000));
                _rateLimitNextRequest.Add(requestType, new DateTime());
            }

            //Swap into sandbox end points / modes.
            _rateLimitPeriod[TradierApiRequestType.Standard] = TimeSpan.FromMilliseconds(500);
            _rateLimitPeriod[TradierApiRequestType.Data] = TimeSpan.FromMilliseconds(500);

            int millisecondsPerDay = (int)TimeSpan.FromDays(1).TotalMilliseconds;

            _timer = new Timer(state => RefreshSession(), this, 0, millisecondsPerDay);
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Set the access token and login information for the tradier brokerage 
        /// </summary>
        /// <param name="userId">Userid for this brokerage</param>
        /// <param name="accessToken">Viable access token</param>
        /// <param name="refreshToken">Our refresh token</param>
        /// <param name="issuedAt">When the token was issued</param>
        /// <param name="lifeSpan">LIfe span for our token.</param>
        public void SetTokens(int userId, string accessToken, string refreshToken, DateTime issuedAt, TimeSpan lifeSpan)
        {
            _accessToken = accessToken;
            _refreshToken = refreshToken;
            _issuedAt = issuedAt;
            _lifeSpan = lifeSpan;
            _userId = userId;
        }


        /// <summary>
        /// Execute a authenticated call:
        /// </summary>
        public T Execute<T>(RestRequest request, TradierApiRequestType type, string rootName = "") where T : new()
        {
            T response;

            lock (_lockAccessCredentials)
            {
                var client = new RestClient(_requestEndpoint);
                client.AddDefaultHeader("Accept", "application/json");
                client.AddDefaultHeader("Authorization", "Bearer " + _accessToken);
                //client.AddDefaultHeader("Content-Type", "application/x-www-form-urlencoded");

                //Wait for the API rate limiting
                while (DateTime.Now < _rateLimitNextRequest[type]) Thread.Sleep(10);
                _rateLimitNextRequest[type] = DateTime.Now + _rateLimitPeriod[type];

                //Send the request:
                var raw = client.Execute(request);
                _previousRequestRaw = raw.Content;

                if (rootName != "")
                {
                    response = DeserializeRemoveRoot<T>(raw.Content, rootName);
                }
                else
                {
                    response = JsonConvert.DeserializeObject<T>(raw.Content);
                }

                if (response == null)
                {
                    var fault = JsonConvert.DeserializeObject<TradierFaultContainer>(raw.Content);
                    if (fault != null)
                    {
                        // JSON Errors:
                        ErrorHandler(fault.Fault);
                    }
                    else
                    {
                        // Text Errors:
                        var textFault = new TradierFault();
                        textFault.Description = raw.Content;
                        ErrorHandler(textFault);
                    }
                }

                if (raw.ErrorException != null)
                {
                    const string message = "Error retrieving response.  Check inner details for more info.";
                    throw new ApplicationException(message, raw.ErrorException);
                }
            }

            return response;
        }


        /// <summary>
        /// Tradier Fault Error Handlers:
        /// </summary>
        public void ErrorHandler(TradierFault fault)
        {
            Log.Error("Tradier.ErrorHandler(): " + fault.Description);

            //Add the fault to record:
            _faults.Add(fault);

            if (_errorHandlers.ContainsKey(fault.Description.ToLower()))
            {
                _errorHandlers[fault.Description.ToLower()]();
            }
        }

        /// <summary>
        /// Setup an error handler for the fault:
        /// </summary>
        public override void AddErrorHander(string key, Action callback)
        {
            key = key.ToLower();

            if (!_errorHandlers.ContainsKey(key))
            {
                _errorHandlers.Add(key, callback);
            }
            else
            {
                _errorHandlers[key] = callback;
            }
        }

        /// <summary>
        /// Verify we have a user session; or refresh the access token.
        /// </summary>
        public override bool RefreshSession()
        {
            //Send: 
            //Get: {"sAccessToken":"123123","iExpiresIn":86399,"dtIssuedAt":"2014-10-15T16:59:52-04:00","sRefreshToken":"123123","sScope":"read write market trade stream","sStatus":"approved","success":true}
            // Or: {"success":false}
            var raw = "";
            var client = new RestClient();
            var request = new RestRequest();

            lock (_lockAccessCredentials)
            {
                try
                {
                    //Create the client for connection:
                    client = new RestClient("https://beta.quantconnect.com/terminal/");

                    //Create the GET call:
                    request = new RestRequest("processTradier", Method.GET);
                    request.AddParameter("uid", _userId.ToString(), ParameterType.GetOrPost);
                    request.AddParameter("accessToken", _accessToken, ParameterType.GetOrPost);
                    request.AddParameter("refreshToken", _refreshToken, ParameterType.GetOrPost);

                    //Submit the call:
                    var result = client.Execute(request);
                    raw = result.Content;

                    //Decode to token response: update internal access parameters:
                    var newTokens = JsonConvert.DeserializeObject<TokenResponse>(result.Content);
                    if (newTokens != null && newTokens.Success)
                    {
                        _accessToken = newTokens.AccessToken;
                        _refreshToken = newTokens.RefreshToken;
                        _issuedAt = newTokens.IssuedAt;
                        _scope = newTokens.Scope;
                        _lifeSpan = TimeSpan.FromSeconds(newTokens.ExpiresIn);
                        Log.Trace("SESSION REFRESHED: Access: " + _accessToken + " Refresh: " + _refreshToken + " Issued At: " + _lifeSpan.ToString() + " JSON>>" + result.Content);
                    }
                    else
                    {
                        Log.Error("Tradier.RefreshSession(): Error Refreshing Session: URL: " + client.BuildUri(request).ToString() + " Response: "  + result.Content);
                        return false;
                    }
                }
                catch (Exception err)
                {
                    Log.Error("Tradier.RefreshSession(): " + err.Message + " >> " + raw);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Using this auth token get the tradier user:
        /// </summary>
        /// <returns>Tradier user model:</returns>
        public TradierUser User()
        {
            var user = new TradierUser();
            try
            {
                //Send Request:
                var request = new RestRequest("user/profile", Method.GET);
                var userContainer = Execute<TradierUserContainer>(request, TradierApiRequestType.Standard);
                user = userContainer.Profile;
            }
            catch (Exception err)
            {
                Log.Error("Tradier.User(): " + err.Message);
            }
            return user;
        }

        /// <summary>
        /// Get all the users balance information:
        /// </summary>
        /// <returns>Balance</returns>
        public TradierBalanceDetails Balance(long accountId)
        {
            var balance = new TradierBalanceDetails();
            var balContainer = new TradierBalance();
            try
            {
                var request = new RestRequest("accounts/{accountId}/balances", Method.GET);
                request.AddParameter("accountId", accountId, ParameterType.UrlSegment);
                balContainer = Execute<TradierBalance>(request, TradierApiRequestType.Standard);
                balance = balContainer.Balances;
            }
            catch (Exception err)
            {
                Log.Error("Tradier.Balances(): " + err.Message);
            }
            return balance;
        }

        /// <summary>
        /// Get a list of the tradier positions for this account:
        /// </summary>
        /// <param name="accountId">Account id we'd like to know</param>
        /// <returns>Array of the symbols we hold.</returns>
        public List<TradierPosition> Positions(long accountId)
        {
            var positions = new List<TradierPosition>();
            try
            {
                var request = new RestRequest("accounts/{accountId}/positions", Method.GET);
                request.AddParameter("accountId", accountId, ParameterType.UrlSegment);
                var positionContainer = Execute<TradierPositionsContainer>(request, TradierApiRequestType.Standard);

                if (positionContainer.TradierPositions != null && positionContainer.TradierPositions.Positions != null)
                {
                    positions = positionContainer.TradierPositions.Positions;
                }
                else
                {
                    Log.Trace("Tradier.Positions(): No positions found");
                }
            }
            catch (Exception err)
            {
                Log.Error("Tradier.Positions(): " + err.Message);
            }
            return positions;
        }

        /// <summary>
        /// Get a list of historical events for this account:
        /// </summary>
        public List<TradierEvent> Events(long accountId)
        {
            var events = new List<TradierEvent>();
            try
            {
                //Download the event history
                var request = new RestRequest("accounts/{accountId}/history", Method.GET);
                request.AddUrlSegment("accountId", accountId.ToString());

                var eventContainer = Execute<TradierEventContainer>(request, TradierApiRequestType.Standard);
                events = eventContainer.TradierEvents.Events;
            }
            catch (Exception err)
            {
                Log.Error("Tradier.Events(): " + err.Message);
            }
            return events;
        }

        /// <summary>
        /// GainLoss of recent trades for this account:
        /// </summary>
        public List<TradierGainLoss> GainLoss(long accountId)
        {
            var gainloss = new List<TradierGainLoss>();
            try
            {
                var request = new RestRequest("accounts/{accountId}/gainloss");
                request.AddUrlSegment("accountId", accountId.ToString());

                var gainLossContainer = Execute<TradierGainLossContainer>(request, TradierApiRequestType.Standard);
                gainloss = gainLossContainer.GainLossClosed.ClosedPositions;
            }
            catch (Exception err)
            {
                Log.Error("Tradier.GainLoss(): " + err.Message);
            }
            return gainloss;
        }

        /// <summary>
        /// Get Intraday and pending orders for users account: accounts/{account_id}/orders
        /// </summary>
        public List<TradierOrder> FetchOrders(long accountId)
        {
            var ordersContainer = new TradierOrdersContainer();
            var orders = new List<TradierOrder>();
            try
            {
                var request = new RestRequest("accounts/{accountId}/orders");
                request.AddUrlSegment("accountId", accountId.ToString());
                ordersContainer = Execute<TradierOrdersContainer>(request, TradierApiRequestType.Standard);

                if (ordersContainer.Orders != null)
                {
                    orders = ordersContainer.Orders.Orders;
                }
                else
                {
                    Log.Trace("Tradier.FetchOrders(): No orders found");
                }
            }
            catch (Exception err)
            {
                Log.Error("Tradier.FetchOrders(): " + err.Message + " >> " + JsonConvert.SerializeObject((object) ordersContainer) );
            }
            return orders;
        }

        /// <summary>
        /// Get information about a specific order: accounts/{account_id}/orders/{id}
        /// </summary>
        public TradierOrderDetailed OrderInformation(long accountId, long orderId)
        {
            var order = new TradierOrderDetailed();
            try
            {
                var request = new RestRequest("accounts/{accountId}/orders/" + orderId);
                request.AddUrlSegment("accountId", accountId.ToString());
                var detailsParent = Execute<TradierOrderDetailedContainer>(request, TradierApiRequestType.Standard);
                order = detailsParent.DetailedOrder;
            }
            catch (Exception err)
            {
                Log.Error("Tradier.OrderInformation(): " + err.Message);
            }
            return order;
        }

        /// <summary>
        /// Place Order through API.
        /// accounts/{account-id}/orders
        /// </summary>
        public TradierOrderResponse PlaceOrder(long accountId, TradierOrderClass classification, TradierOrderDirection direction, string symbol, decimal quantity, decimal price = 0, decimal stop = 0, string optionSymbol = "", TradierOrderType type = TradierOrderType.Market, TradierOrderDuration duration = TradierOrderDuration.GTC)
        {
            var response = new TradierOrderResponse();

            try
            {
                //Compose the request:
                var request = new RestRequest("accounts/{accountId}/orders");
                request.AddUrlSegment("accountId", accountId.ToString());

                //Add data:
                request.AddParameter("class", GetEnumDescription(classification));
                request.AddParameter("symbol", symbol);
                request.AddParameter("duration", GetEnumDescription(duration));
                request.AddParameter("type", GetEnumDescription(type));
                request.AddParameter("quantity", quantity);
                request.AddParameter("side", GetEnumDescription(direction));

                //Add optionals:
                if (price > 0) request.AddParameter("price", price);
                if (stop > 0) request.AddParameter("stop", stop);
                if (optionSymbol != "") request.AddParameter("option_symbol", optionSymbol);

                //Set Method:
                request.Method = Method.POST;

                response = Execute<TradierOrderResponse>(request, TradierApiRequestType.Orders);
            }
            catch (Exception err)
            {
                Log.Error("Tradier.PlaceOrder(): " + err.Message);
            }

            return response;
        }

        /// <summary>
        /// Update an exiting Tradier Order:
        /// </summary>
        public TradierOrderResponse ChangeOrder(long accountId, long orderId, TradierOrderType type = TradierOrderType.Market, TradierOrderDuration duration = TradierOrderDuration.GTC, decimal price = 0, decimal stop = 0)
        {
            var response = new TradierOrderResponse();

            try
            {
                //Create Request:
                var request = new RestRequest("accounts/{accountId}/orders/{orderId}");
                request.AddUrlSegment("accountId", accountId.ToString());
                request.AddUrlSegment("orderId", orderId.ToString());
                request.Method = Method.PUT;

                //Add Data:
                request.AddParameter("type", GetEnumDescription(type));
                request.AddParameter("duration", GetEnumDescription(duration));
                if (price != 0) request.AddParameter("price", price.ToString());
                if (stop != 0) request.AddParameter("stop", stop.ToString());

                //Send:
                response = Execute<TradierOrderResponse>(request, TradierApiRequestType.Orders);
            }
            catch (Exception err)
            {
                Log.Error("Tradier.ChangeOrder(): " + err.Message);
            }

            return response;
        }

        /// <summary>
        /// Cancel the order with this account and id number
        /// </summary>
        public TradierOrderResponse CancelOrder(long accountId, long orderId)
        {
            var response = new TradierOrderResponse();

            try
            {
                //Compose Request:
                var request = new RestRequest("accounts/{accountId}/orders/{orderId}");
                request.AddUrlSegment("accountId", accountId.ToString());
                request.AddUrlSegment("orderId", orderId.ToString());
                request.Method = Method.DELETE;

                //Transmit Request:
                response = Execute<TradierOrderResponse>(request, TradierApiRequestType.Orders);
            }
            catch (Exception err)
            {
                Log.Error("Tradier.CancelOrder(): " + err.Message);
            }

            return response;
        }

        /// <summary>
        /// List of quotes for symbols 
        /// </summary>
        public List<TradierQuote> Quotes(List<string> symbols)
        {
            var data = new List<TradierQuote>();
            try
            {
                //Send Request:
                var request = new RestRequest("markets/quotes", Method.GET);
                var csvSymbols = String.Join(",", symbols);
                request.AddParameter("symbols", csvSymbols, ParameterType.QueryString);

                var dataContainer = Execute<TradierQuoteContainer>(request, TradierApiRequestType.Data, "quotes");
                data = dataContainer.Quotes;
            }
            catch (Exception err)
            {
                Log.Error("Tradier.GetQuotes(): " + err.Message);
            }
            return data;
        }

        /// <summary>
        /// Get the historical bars for this period
        /// </summary>
        public List<TradierTimeSeries> TimeSeries(string symbol, DateTime start, DateTime end, TradierTimeSeriesIntervals interval)
        {
            var data = new List<TradierTimeSeries>();
            try
            {
                //Send Request:
                var request = new RestRequest("markets/timesales", Method.GET);
                request.AddParameter("symbol", symbol, ParameterType.QueryString);
                request.AddParameter("interval", GetEnumDescription(interval), ParameterType.QueryString);
                request.AddParameter("start", start.ToString("yyyy-MM-dd HH:mm"), ParameterType.QueryString);
                request.AddParameter("end", end.ToString("yyyy-MM-dd HH:mm"), ParameterType.QueryString);
                var dataContainer = Execute<TradierTimeSeriesContainer>(request, TradierApiRequestType.Data, "series");
                data = dataContainer.TimeSeries;
            }
            catch (Exception err)
            {
                Log.Error("Tradier.GetQuotes(): " + err.Message);
            }
            return data;
        }


        /// <summary>
        /// Get full daily, weekly or monthly bars of historical periods:
        /// </summary>
        public List<TradierHistoryBar> HistoricalData(string symbol, DateTime start, DateTime end, TradierHistoricalDataIntervals interval = TradierHistoricalDataIntervals.Daily)
        {
            var data = new List<TradierHistoryBar>();

            try
            {
                var request = new RestRequest("markets/history", Method.GET);
                request.AddParameter("symbol", symbol, ParameterType.QueryString);
                request.AddParameter("start", start.ToString("yyyy-MM-dd"), ParameterType.QueryString);
                request.AddParameter("end", end.ToString("yyyy-MM-dd"), ParameterType.QueryString);
                request.AddParameter("interval", GetEnumDescription(interval));
                var dataContainer = Execute<TradierHistoryDataContainer>(request, TradierApiRequestType.Data, "history");
                data = dataContainer.Data;
            }
            catch (Exception err)
            {
                Log.Error("Tradier.GetHistoricalData(): " + err.Message);
            }
            return data;
        }


        /// <summary>
        /// Get the current market status
        /// </summary>
        public TradierMarketStatus MarketStatus()
        {
            var status = new TradierMarketStatus();
            try
            {
                var request = new RestRequest("markets/clock", Method.GET);
                status = Execute<TradierMarketStatus>(request, TradierApiRequestType.Data, "clock");
            }
            catch (Exception err)
            {
                Log.Error("Tradier.MarketStatus(): " + err.Message);
            }
            return status;
        }


        /// <summary>
        /// Get the list of days status for this calendar month, year:
        /// </summary>
        public List<TradierCalendarDay> MarketCalendar(int month, int year)
        {
            var calendarDays = new List<TradierCalendarDay>();
            try
            {
                var request = new RestRequest("markets/calendar", Method.GET);
                request.AddParameter("month", month.ToString());
                request.AddParameter("year", year.ToString());
                var calendarContainer = Execute<TradierCalendarStatus>(request, TradierApiRequestType.Data, "calendar");
                calendarDays = calendarContainer.Days.Days;
            }
            catch (Exception err)
            {
                Log.Error("Tradier.MarketCalendar(): " + err.Message);
            }
            return calendarDays;
        }

        /// <summary>
        /// Get the list of days status for this calendar month, year:
        /// </summary>
        public List<TradierSearchResult> Search(string query, bool includeIndexes = true)
        {
            var results = new List<TradierSearchResult>();
            try
            {
                var request = new RestRequest("markets/search", Method.GET);
                request.AddParameter("q", query);
                request.AddParameter("indexes", includeIndexes.ToString());
                var searchContainer = Execute<TradierSearchContainer>(request, TradierApiRequestType.Data, "securities");
                results = searchContainer.Results;
            }
            catch (Exception err)
            {
                Log.Error("Tradier.Search(): " + err.Message);
            }
            return results;
        }

        /// <summary>
        /// Get the list of days status for this calendar month, year:
        /// </summary>
        public List<TradierSearchResult> LookUp(string query, bool includeIndexes = true)
        {
            var results = new List<TradierSearchResult>();
            try
            {
                var request = new RestRequest("markets/lookup", Method.GET);
                request.AddParameter("q", query);
                request.AddParameter("indexes", includeIndexes.ToString());
                var searchContainer = Execute<TradierSearchContainer>(request, TradierApiRequestType.Data, "securities");
                results = searchContainer.Results;
            }
            catch (Exception err)
            {
                Log.Error("Tradier.LookUp(): " + err.Message);
            }
            return results;
        }


        /// <summary>
        /// Get the current market status
        /// </summary>
        public TradierStreamSession CreateStreamSession()
        {
            var session = new TradierStreamSession();
            try
            {
                var request = new RestRequest("markets/events/session", Method.POST);
                session = Execute<TradierStreamSession>(request, TradierApiRequestType.Data, "stream");
            }
            catch (Exception err)
            {
                Log.Error("Tradier.Stream(): " + err.Message + ">> " + _previousRequestRaw);
            }
            return session;
        }


        /// <summary>
        /// Connect to tradier API strea:
        /// </summary>
        /// <param name="symbols">symbol list</param>
        /// <returns></returns>
        public IEnumerable<TradierStreamData> Stream(List<string> symbols)
        {
            var stream = new List<TradierStreamData>();
            var symbolJoined = String.Join(",", symbols);
            var success = true;
            
            var session = CreateStreamSession();
            if (session == null || session.SessionId == null || session.Url == null)
            {
                Log.Error("Tradier.Stream(): Failed to Created Stream Session", true);
                yield break;
            }
            Log.Trace("Tradier.Stream(): Created Stream Session Id: " + session.SessionId + " Url:" + session.Url, true);

            
            var request = (HttpWebRequest)WebRequest.Create((string) session.Url);
            do
            {
                //Connect to URL:
                success = true;
                request = (HttpWebRequest)WebRequest.Create((string) session.Url);

                //Authenticate a request:
                request.Accept = "application/json";
                request.Headers.Add("Authorization", "Bearer " + _accessToken);

                //Add the desired data:
                var postData = "symbols=" + symbolJoined + "&sessionid=" + session.SessionId; ;
                var encodedData = Encoding.ASCII.GetBytes(postData);

                //Set post:
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = encodedData.Length;

                //Send request:
                try
                {
                    using (var postStream = request.GetRequestStream())
                    {
                        postStream.Write(encodedData, 0, encodedData.Length);
                    }
                }
                catch (Exception err)
                {
                    Log.Error("Tradier.Stream(): Failed to write session parameters to URL: " + err.Message + " >>  ST >>" + err.StackTrace, true);
                    success = false;
                }
            } 
            while (!success);

            //Get response as a stream:
            Log.Trace("Tradier.Stream(): Session Created, Reading Stream...", true);
            var response = (HttpWebResponse)request.GetResponse();
            var tradierStream = response.GetResponseStream();

            using (var sr = new StreamReader(tradierStream))
            using (var jsonReader = new JsonTextReader(sr))
            {
                var serializer = new JsonSerializer();
                jsonReader.SupportMultipleContent = true;
                var successfulRead = true;
                while (successfulRead)
                {
                    try
                    {
                        //Read the jsonSocket in a safe manner: might close and so need handlers, but can't put handlers around a yield.
                        successfulRead = jsonReader.Read();
                    }
                    catch (Exception err) 
                    {
                        Console.Write(err.Message); 
                        successfulRead = false;
                    }

                    if (successfulRead)
                    {
                        //Have a Tradier JSON Object:
                        var tsd = new TradierStreamData();
                        try {
                            tsd = serializer.Deserialize<TradierStreamData>(jsonReader);
                        } catch (Exception err) {
                            // Do nothing for now. Can come back later to fix. Errors are from Tradier not properly json encoding values E.g. "NaN" string.
                            Console.Write(err.Message);
                        }
                        yield return tsd;
                    }
                    else 
                    {
                        //Error in stream or market has closed, neatly exit the stream
                        yield break;
                    }
                }
            }
        }


        /// <summary>
        /// Create an IEnumerable simulated data stream for "live" testing after hours and weekends.
        /// </summary>
        /// <param name="symbols">Symbols to return the simulated stream</param>
        /// <returns>Tradier data:</returns>
        public IEnumerable<TradierStreamData> SimulatedStream(List<string> symbols)
        {
            //Initialize:
            var rand = new Random((int)DateTime.Now.TimeOfDay.TotalMilliseconds);
            var prices = new Dictionary<string, decimal>();

            if (symbols.Count == 0)
                yield break;

            //Setup prices: always sends a bunch of last trades.
            foreach (var symbol in symbols)
            {
                //Random starting price from $5 - $100
                prices.Add(symbol, ((decimal)rand.Next(500, 10000)) / 100m);

                var data = new TradierStreamData();
                //Set back to the Tradier Object:
                data.Symbol = symbol;
                data.TradePrice = prices[symbol];
                data.TradeSize = rand.Next(10, 1000);
                data.Type = "trade";
                yield return data;
            }

            while (true)
            {
                //Initialize
                var data = new TradierStreamData();

                //Randomly pick a symbol:
                var symbol = symbols[rand.Next(0, symbols.Count - 1)];

                //Randomly walk a price change:
                var price = prices[symbol];
                price += (rand.Next(-1, 1)) * price * 0.001m;
                prices[symbol] = price;

                //Set back to the Tradier Object:
                data.Symbol = symbol;
                data.TradePrice = price;
                data.TradeSize = rand.Next(10, 1000);
                data.Type = "trade";

                //Randomly pick a delay:
                Thread.Sleep(rand.Next(200, 700));

                yield return data;
            }
        }


        /// <summary>
        /// Convert the C# Enums back to the Tradier API Equivalent:
        /// </summary>
        private string GetEnumDescription(Enum value)
        {
            // Get the Description attribute value for the enum value
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (EnumMemberAttribute[])fi.GetCustomAttributes(typeof(EnumMemberAttribute), false);

            if (attributes.Length > 0)
            {
                return attributes[0].Value;
            }
            else
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// Get the rype inside the nested root:
        /// </summary>
        private T DeserializeRemoveRoot<T>(string json, string rootName)
        {
            var obj = default(T);

            try
            {
                //Dynamic deserialization:
                dynamic dynDeserialized = JsonConvert.DeserializeObject(json);
                obj = JsonConvert.DeserializeObject<T>(dynDeserialized[rootName].ToString());
            }
            catch (Exception err)
            {
                Log.Error("Tradier.DeserializeRemoveRoot(): Root Name (" + rootName + "): " + err.Message);
            }

            return obj;
        }


    } // End of Tradier:

}
