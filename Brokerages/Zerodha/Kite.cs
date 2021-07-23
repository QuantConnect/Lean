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
using System.IO;
using System.Linq;
using System.Net;
using System.Globalization;
using QuantConnect.Brokerages.Zerodha.Messages;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using CsvHelper;
using CsvHelper.Configuration;
using System.Threading;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// The API client class. In production, you may initialize a single instance of this class per `APIKey`.
    /// </summary>
    public class Kite
    {
        // Default root API endpoint. It's possible to
        // override this by passing the `Root` parameter during initialisation.
        private string _root = "https://api.kite.trade";
        private string _apiKey;
        private string _accessToken;
        private WebProxy _proxy;
        private int _timeout;

        private Action _sessionHook;

        //private Cache cache = new Cache();

        private readonly Dictionary<string, string> _routes = new Dictionary<string, string>
        {
            ["parameters"] = "/parameters",
            ["api.token"] = "/session/token",
            ["api.refresh"] = "/session/refresh_token",

            ["instrument.margins"] = "/margins/{segment}",

            ["user.profile"] = "/user/profile",
            ["user.margins"] = "/user/margins",
            ["user.segment_margins"] = "/user/margins/{segment}",

            ["orders"] = "/orders",
            ["trades"] = "/trades",
            ["orders.history"] = "/orders/{order_id}",

            ["orders.place"] = "/orders/{variety}",
            ["orders.modify"] = "/orders/{variety}/{order_id}",
            ["orders.cancel"] = "/orders/{variety}/{order_id}",
            ["orders.trades"] = "/orders/{order_id}/trades",

            ["gtt"] = "/gtt/triggers",
            ["gtt.place"] = "/gtt/triggers",
            ["gtt.info"] = "/gtt/triggers/{id}",
            ["gtt.modify"] = "/gtt/triggers/{id}",
            ["gtt.delete"] = "/gtt/triggers/{id}",

            ["portfolio.positions"] = "/portfolio/positions",
            ["portfolio.holdings"] = "/portfolio/holdings",
            ["portfolio.positions.modify"] = "/portfolio/positions",

            ["market.instruments.all"] = "/instruments",
            ["market.instruments"] = "/instruments/{exchange}",
            ["market.quote"] = "/quote",
            ["market.ohlc"] = "/quote/ohlc",
            ["market.ltp"] = "/quote/ltp",
            ["market.historical"] = "/instruments/historical/{instrument_token}/{interval}",
            ["market.trigger_range"] = "/instruments/trigger_range/{transaction_type}",
        };

        /// <summary>
        /// Initialize a new Kite Connect client instance.
        /// </summary>
        /// <param name="APIKey">API Key issued to you</param>
        /// <param name="AccessToken">The token obtained after the login flow in exchange for the `RequestToken` . 
        /// Pre-login, this will default to None,but once you have obtained it, you should persist it in a database or session to pass 
        /// to the Kite Connect class initialisation for subsequent requests.</param>
        /// <param name="Root">API end point root. Unless you explicitly want to send API requests to a non-default endpoint, this can be ignored.</param>
        /// <param name="Timeout">Time in milliseconds for which  the API client will wait for a request to complete before it fails</param>
        /// <param name="Proxy">To set proxy for http request. Should be an object of WebProxy.</param>
        /// <param name="Pool">Number of connections to server. Client will reuse the connections if they are alive.</param>
        public Kite(string APIKey, string AccessToken = null, string Root = null, int Timeout = 7000, WebProxy Proxy = null, int Pool = 2)
        {
            _accessToken = AccessToken;
            _apiKey = APIKey;
            if (!String.IsNullOrEmpty(Root)) this._root = Root;

            _timeout = Timeout;
            _proxy = Proxy;

            ServicePointManager.DefaultConnectionLimit = Pool;
        }

        /// <summary>
        /// Set a callback hook for session (`TokenException` -- timeout, expiry etc.) errors.
		/// An `AccessToken` (login session) can become invalid for a number of
        /// reasons, but it doesn't make sense for the client to
		/// try and catch it during every API call.
        /// A callback method that handles session errors
        /// can be set here and when the client encounters
        /// a token error at any point, it'll be called.
        /// This callback, for instance, can log the user out of the UI,
		/// clear session cookies, or initiate a fresh login.
        /// </summary>
        /// <param name="Method">Action to be invoked when session becomes invalid.</param>
        public void SetSessionExpiryHook(Action Method)
        {
            _sessionHook = Method;
        }

        /// <summary>
        /// Set the `AccessToken` received after a successful authentication.
        /// </summary>
        /// <param name="AccessToken">Access token for the session.</param>
        public void SetAccessToken(string AccessToken)
        {
            this._accessToken = AccessToken;
        }

        /// <summary>
        /// Do the token exchange with the `RequestToken` obtained after the login flow,
		/// and retrieve the `AccessToken` required for all subsequent requests.The
        /// response contains not just the `AccessToken`, but metadata for
        /// the user who has authenticated.
        /// </summary>
        /// <param name="RequestToken">Token obtained from the GET paramers after a successful login redirect.</param>
        /// <param name="AppSecret">API secret issued with the API key.</param>
        /// <returns>User structure with tokens and profile data</returns>
        public User GenerateSession(string RequestToken, string AppSecret)
        {
            string checksum = Extensions.ToSHA256(_apiKey + RequestToken + AppSecret);

            var param = new Dictionary<string, dynamic>
            {
                {"api_key", _apiKey},
                {"request_token", RequestToken},
                {"checksum", checksum}
            };

            var userData = Post("api.token", param);

            return new User(userData);
        }

        /// <summary>
        /// Kill the session by invalidating the access token
        /// </summary>
        /// <param name="AccessToken">Access token to invalidate. Default is the active access token.</param>
        /// <returns>Json response in the form of nested string dictionary.</returns>
        public Dictionary<string, dynamic> InvalidateAccessToken(string AccessToken = null)
        {
            var param = new Dictionary<string, dynamic>();

            Utils.AddIfNotNull(param, "api_key", _apiKey);
            Utils.AddIfNotNull(param, "access_token", AccessToken);

            return Delete("api.token", param);
        }

        /// <summary>
        /// Invalidates RefreshToken
        /// </summary>
        /// <param name="RefreshToken">RefreshToken to invalidate</param>
        /// <returns>Json response in the form of nested string dictionary.</returns>
        public Dictionary<string, dynamic> InvalidateRefreshToken(string RefreshToken)
        {
            var param = new Dictionary<string, dynamic>();

            Utils.AddIfNotNull(param, "api_key", _apiKey);
            Utils.AddIfNotNull(param, "refresh_token", RefreshToken);

            return Delete("api.token", param);
        }

        /// <summary>
        /// Renew AccessToken using RefreshToken
        /// </summary>
        /// <param name="RefreshToken">RefreshToken to renew the AccessToken.</param>
        /// <param name="AppSecret">API secret issued with the API key.</param>
        /// <returns>TokenRenewResponse that contains new AccessToken and RefreshToken.</returns>
        public TokenSet RenewAccessToken(string RefreshToken, string AppSecret)
        {
            var param = new Dictionary<string, dynamic>();

            string checksum = Extensions.ToSHA256(_apiKey + RefreshToken + AppSecret);

            Utils.AddIfNotNull(param, "api_key", _apiKey);
            Utils.AddIfNotNull(param, "refresh_token", RefreshToken);
            Utils.AddIfNotNull(param, "checksum", checksum);

            return new TokenSet(Post("api.refresh", param));
        }

        /// <summary>
        /// Gets currently logged in user details
        /// </summary>
        /// <returns>User profile</returns>
        public Profile GetProfile()
        {
            var profileData = Get("user.profile");

            return new Profile(profileData);
        }

        /// <summary>
        /// Get account balance and cash margin details for all segments.
        /// </summary>
        /// <returns>User margin response with both equity and commodity margins.</returns>
        public UserMarginsResponse GetMargins()
        {
            var marginsData = Get("user.margins");
            return new UserMarginsResponse(marginsData["data"]);
        }

        /// <summary>
        /// Get account balance and cash margin details for a particular segment.
        /// </summary>
        /// <param name="Segment">Trading segment (eg: equity or commodity)</param>
        /// <returns>Margins for specified segment.</returns>
        public UserMargin GetMargins(string Segment)
        {
            var userMarginData = Get("user.segment_margins", new Dictionary<string, dynamic> { { "segment", Segment } });
            return new UserMargin(userMarginData["data"]);
        }

        /// <summary>
        /// Place an order
        /// </summary>
        /// <param name="Exchange">Name of the exchange</param>
        /// <param name="TradingSymbol">Tradingsymbol of the instrument</param>
        /// <param name="TransactionType">BUY or SELL</param>
        /// <param name="Quantity">Quantity to transact</param>
        /// <param name="Price">For LIMIT orders</param>
        /// <param name="Product">Margin product applied to the order (margin is blocked based on this)</param>
        /// <param name="OrderType">Order type (MARKET, LIMIT etc.)</param>
        /// <param name="Validity">Order validity</param>
        /// <param name="DisclosedQuantity">Quantity to disclose publicly (for equity trades)</param>
        /// <param name="TriggerPrice">For SL, SL-M etc.</param>
        /// <param name="SquareOffValue">Price difference at which the order should be squared off and profit booked (eg: Order price is 100. Profit target is 102. So squareoff = 2)</param>
        /// <param name="StoplossValue">Stoploss difference at which the order should be squared off (eg: Order price is 100. Stoploss target is 98. So stoploss = 2)</param>
        /// <param name="TrailingStoploss">Incremental value by which stoploss price changes when market moves in your favor by the same incremental value from the entry price (optional)</param>
        /// <param name="Variety">You can place orders of varieties; regular orders, after market orders, cover orders etc. </param>
        /// <param name="Tag">An optional tag to apply to an order to identify it (alphanumeric, max 8 chars)</param>
        /// <returns>Json response in the form of nested string dictionary.</returns>
        public JObject PlaceOrder(
            string Exchange,
            string TradingSymbol,
            string TransactionType,
            uint Quantity,
            decimal? Price = null,
            string Product = null,
            string OrderType = null,
            string Validity = null,
            int? DisclosedQuantity = null,
            decimal? TriggerPrice = null,
            decimal? SquareOffValue = null,
            decimal? StoplossValue = null,
            decimal? TrailingStoploss = null,
            string Variety = Constants.VARIETY_REGULAR,
            string Tag = "")
        {
            var param = new Dictionary<string, dynamic>();

            Utils.AddIfNotNull(param, "exchange", Exchange);
            Utils.AddIfNotNull(param, "tradingsymbol", TradingSymbol);
            Utils.AddIfNotNull(param, "transaction_type", TransactionType);
            Utils.AddIfNotNull(param, "quantity", Quantity.ToStringInvariant());
            Utils.AddIfNotNull(param, "price", Price.ToString());
            Utils.AddIfNotNull(param, "product", Product);
            Utils.AddIfNotNull(param, "order_type", OrderType);
            Utils.AddIfNotNull(param, "validity", Validity);
            Utils.AddIfNotNull(param, "disclosed_quantity", DisclosedQuantity.ToString());
            Utils.AddIfNotNull(param, "trigger_price", TriggerPrice.ToString());
            Utils.AddIfNotNull(param, "squareoff", SquareOffValue.ToString());
            Utils.AddIfNotNull(param, "stoploss", StoplossValue.ToString());
            Utils.AddIfNotNull(param, "trailing_stoploss", TrailingStoploss.ToString());
            Utils.AddIfNotNull(param, "variety", Variety);
            Utils.AddIfNotNull(param, "tag", Tag);

            return Post("orders.place", param);
        }

        /// <summary>
        /// Modify an open order.
        /// </summary>
        /// <param name="OrderId">Id of the order to be modified</param>
        /// <param name="ParentOrderId">Id of the parent order (obtained from the /orders call) as BO is a multi-legged order</param>
        /// <param name="Exchange">Name of the exchange</param>
        /// <param name="TradingSymbol">Tradingsymbol of the instrument</param>
        /// <param name="TransactionType">BUY or SELL</param>
        /// <param name="Quantity">Quantity to transact</param>
        /// <param name="Price">For LIMIT orders</param>
        /// <param name="Product">Margin product applied to the order (margin is blocked based on this)</param>
        /// <param name="OrderType">Order type (MARKET, LIMIT etc.)</param>
        /// <param name="Validity">Order validity</param>
        /// <param name="DisclosedQuantity">Quantity to disclose publicly (for equity trades)</param>
        /// <param name="TriggerPrice">For SL, SL-M etc.</param>
        /// <param name="Variety">You can place orders of varieties; regular orders, after market orders, cover orders etc. </param>
        /// <returns>Json response in the form of nested string dictionary.</returns>
        public JObject ModifyOrder(
            string OrderId,
            string ParentOrderId = null,
            string Exchange = null,
            string TradingSymbol = null,
            string TransactionType = null,
            uint? Quantity = null,
            decimal? Price = null,
            string Product = null,
            string OrderType = null,
            string Validity = Constants.VALIDITY_DAY,
            int? DisclosedQuantity = null,
            decimal? TriggerPrice = null,
            string Variety = Constants.VARIETY_REGULAR)
        {
            var param = new Dictionary<string, dynamic>();

            string VarietyString = Variety;
            string ProductString = Product;

            if ((ProductString == "bo" || ProductString == "co") && VarietyString != ProductString)
            {
                throw new Exception(String.Format(CultureInfo.InvariantCulture, "Invalid variety. It should be: {0}", ProductString));
            }

            Utils.AddIfNotNull(param, "order_id", OrderId);
            Utils.AddIfNotNull(param, "parent_order_id", ParentOrderId);
            Utils.AddIfNotNull(param, "trigger_price", TriggerPrice.ToString());
            Utils.AddIfNotNull(param, "variety", Variety);

            if (VarietyString == "bo" && ProductString == "bo")
            {
                Utils.AddIfNotNull(param, "quantity", Quantity.ToStringInvariant());
                Utils.AddIfNotNull(param, "price", Price.ToString());
                Utils.AddIfNotNull(param, "disclosed_quantity", DisclosedQuantity.ToString());
            }
            else if (VarietyString != "co" && ProductString != "co")
            {
                Utils.AddIfNotNull(param, "exchange", Exchange);
                Utils.AddIfNotNull(param, "tradingsymbol", TradingSymbol);
                Utils.AddIfNotNull(param, "transaction_type", TransactionType);
                Utils.AddIfNotNull(param, "quantity", Quantity.ToStringInvariant());
                Utils.AddIfNotNull(param, "price", Price.ToString());
                Utils.AddIfNotNull(param, "product", Product);
                Utils.AddIfNotNull(param, "order_type", OrderType);
                Utils.AddIfNotNull(param, "validity", Validity);
                Utils.AddIfNotNull(param, "disclosed_quantity", DisclosedQuantity.ToString());
            }

            return Put("orders.modify", param);
        }

        /// <summary>
        /// Cancel an order
        /// </summary>
        /// <param name="OrderId">Id of the order to be cancelled</param>
        /// <param name="Variety">You can place orders of varieties; regular orders, after market orders, cover orders etc. </param>
        /// <param name="ParentOrderId">Id of the parent order (obtained from the /orders call) as BO is a multi-legged order</param>
        /// <returns>Json response in the form of nested string dictionary.</returns>
        public JObject CancelOrder(string OrderId, string Variety = Constants.VARIETY_REGULAR, string ParentOrderId = null)
        {
            var param = new Dictionary<string, dynamic>();

            Utils.AddIfNotNull(param, "order_id", OrderId);
            Utils.AddIfNotNull(param, "parent_order_id", ParentOrderId);
            Utils.AddIfNotNull(param, "variety", Variety);

            return Delete("orders.cancel", param);
        }

        /// <summary>
        /// Gets the collection of orders from the orderbook.
        /// </summary>
        /// <returns>List of orders.</returns>
        public List<Order> GetOrders()
        {
            var ordersData = Get("orders");

            List<Order> orders = new List<Order>();

            foreach (JObject item in ordersData["data"])
            {
                orders.Add(new Order(item));
            }

            return orders;
        }

        /// <summary>
        /// Gets information about given OrderId.
        /// </summary>
        /// <param name="OrderId">Unique order id</param>
        /// <returns>List of order objects.</returns>
        public List<Order> GetOrderHistory(string OrderId)
        {
            var param = new Dictionary<string, dynamic>
            {
                { "order_id", OrderId }
            };

            var orderData = Get("orders.history", param);

            List<Order> orderHistory = new List<Order>();

            foreach (JObject item in orderData["data"])
            {
                orderHistory.Add(new Order(item));
            }

            return orderHistory;
        }

        /// <summary>
        /// Retreive the list of trades executed (all or ones under a particular order).
        /// An order can be executed in tranches based on market conditions.
        /// These trades are individually recorded under an order.
        /// </summary>
        /// <param name="OrderId">is the ID of the order (optional) whose trades are to be retrieved. If no `OrderId` is specified, all trades for the day are returned.</param>
        /// <returns>List of trades of given order.</returns>
        public List<Trade> GetOrderTrades(string OrderId = null)
        {
            Dictionary<string, dynamic> tradesdata;
            if (!string.IsNullOrEmpty(OrderId))
            {
                var param = new Dictionary<string, dynamic>
                {
                    { "order_id", OrderId }
                };
                tradesdata = Get("orders.trades", param);
            }
            else
                tradesdata = Get("trades");

            List<Trade> trades = new List<Trade>();

            foreach (Dictionary<string, dynamic> item in tradesdata["data"])
            {
                trades.Add(new Trade(item));
            }

            return trades;
        }

        /// <summary>
        /// Retrieve the list of positions.
        /// </summary>
        /// <returns>Day and net positions.</returns>
        public PositionResponse GetPositions()
        {
            var positionsdata = Get("portfolio.positions")["data"];
            var positionResponse = new PositionResponse(positionsdata);
            return positionResponse;
        }

        /// <summary>
        /// Retrieve the list of equity holdings.
        /// </summary>
        /// <returns>List of holdings.</returns>
        public List<Messages.Holding> GetHoldings()
        {
            var holdingsData = Get("portfolio.holdings")["data"];

            List<Messages.Holding> holdings = new List<Messages.Holding>();

            foreach (JObject item in holdingsData)
            {
                holdings.Add(new Messages.Holding(item));
            }
            return holdings;
        }

        /// <summary>
        /// Modify an open position's product type.
        /// </summary>
        /// <param name="Exchange">Name of the exchange</param>
        /// <param name="TradingSymbol">Tradingsymbol of the instrument</param>
        /// <param name="TransactionType">BUY or SELL</param>
        /// <param name="PositionType">overnight or day</param>
        /// <param name="Quantity">Quantity to convert</param>
        /// <param name="OldProduct">Existing margin product of the position</param>
        /// <param name="NewProduct">Margin product to convert to</param>
        /// <returns>Json response in the form of nested string dictionary.</returns>
        public Dictionary<string, dynamic> ConvertPosition(
            string Exchange,
            string TradingSymbol,
            string TransactionType,
            string PositionType,
            int? Quantity,
            string OldProduct,
            string NewProduct)
        {
            var param = new Dictionary<string, dynamic>();

            Utils.AddIfNotNull(param, "exchange", Exchange);
            Utils.AddIfNotNull(param, "tradingsymbol", TradingSymbol);
            Utils.AddIfNotNull(param, "transaction_type", TransactionType);
            Utils.AddIfNotNull(param, "position_type", PositionType);
            Utils.AddIfNotNull(param, "quantity", Quantity.ToString());
            Utils.AddIfNotNull(param, "old_product", OldProduct);
            Utils.AddIfNotNull(param, "new_product", NewProduct);

            return Put("portfolio.positions.modify", param);
        }

        /// <summary>
        /// Retrieve the list of market instruments available to trade.
        /// Note that the results could be large, several hundred KBs in size,
		/// with tens of thousands of entries in the list.
        /// </summary>
        /// <param name="Exchange">Name of the exchange</param>
        /// <returns>List of instruments.</returns>
        public List<CsvInstrument> GetInstruments(string Exchange = null)
        {
            List<CsvInstrument> instruments = new List<CsvInstrument>();
            var param = new Dictionary<string, dynamic>();
            try
            {
                var latestFile = Globals.DataFolder+"ZerodhaInstrument-" + DateTime.Now.Date.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture).Replace(" ", "-").Replace("/", "-") + ".csv";
                if (!File.Exists(latestFile))
                {

                    if (String.IsNullOrEmpty(Exchange))
                    {
                        instruments = Get("market.instruments.all", param);
                    }
                    else
                    {
                        param.Add("exchange", Exchange);
                        instruments = Get("market.instruments", param);
                    }

                    foreach (var item in instruments)
                    {
                        instruments.Add(item);
                    }

                    using (var writer = new StreamWriter(latestFile))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(instruments);
                    }
                }
                else
                {
                    using (var reader = new StreamReader(latestFile))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        instruments = csv.GetRecords<CsvInstrument>().ToList();
                    }
                }

                

                return instruments;
            }
            catch (Exception)
            {

                return instruments;
            }
        }

        /// <summary>
        /// Retrieve quote and market depth of upto 200 instruments
        /// </summary>
        /// <param name="InstrumentIds">Indentification of instrument in the form of EXCHANGE:TRADINGSYMBOL (eg: NSE:INFY) or InstrumentToken (eg: 408065)</param>
        /// <returns>Dictionary of all Quote objects with keys as in InstrumentId</returns>
        public Dictionary<string, Quote> GetQuote(string[] InstrumentIds)
        {
            var param = new Dictionary<string, dynamic>
            {
                { "i", InstrumentIds }
            };
            var quoteData = Get("market.quote", param)["data"];

            Dictionary<string, Quote> quotes = new Dictionary<string, Quote>();
            foreach (string item in InstrumentIds)
            {
                quotes.Add(item, new Quote(quoteData[item]));
            }

            return quotes;
        }

        /// <summary>
        /// Retrieve LTP and OHLC of upto 200 instruments
        /// </summary>
        /// <param name="InstrumentId">Indentification of instrument in the form of EXCHANGE:TRADINGSYMBOL (eg: NSE:INFY) or InstrumentToken (eg: 408065)</param>
        /// <returns>Dictionary of all OHLC objects with keys as in InstrumentId</returns>
        public Dictionary<string, OHLC> GetOHLC(string[] InstrumentId)
        {
            var param = new Dictionary<string, dynamic>
            {
                { "i", InstrumentId }
            };
            Dictionary<string, dynamic> ohlcData = Get("market.ohlc", param)["data"];

            Dictionary<string, OHLC> ohlcs = new Dictionary<string, OHLC>();
            foreach (string item in ohlcData.Keys)
            {
                ohlcs.Add(item, new OHLC(ohlcData[item]));
            }

            return ohlcs;
        }

        /// <summary>
        /// Retrieve LTP of upto 200 instruments
        /// </summary>
        /// <param name="InstrumentId">Indentification of instrument in the form of EXCHANGE:TRADINGSYMBOL (eg: NSE:INFY) or InstrumentToken (eg: 408065)</param>
        /// <returns>Dictionary with InstrumentId as key and LTP as value.</returns>
        public Dictionary<string, LTP> GetLTP(string[] InstrumentId)
        {
            var param = new Dictionary<string, dynamic>
            {
                { "i", InstrumentId }
            };
            Dictionary<string, dynamic> ltpData = Get("market.ltp", param)["data"];

            Dictionary<string, LTP> ltps = new Dictionary<string, LTP>();
            foreach (string item in ltpData.Keys)
            {
                ltps.Add(item, new LTP(ltpData[item]));
            }

            return ltps;
        }

        /// <summary>
        /// Retrieve historical data (candles) for an instrument.
        /// </summary>
        /// <param name="InstrumentToken">Identifier for the instrument whose historical records you want to fetch. This is obtained with the instrument list API.</param>
        /// <param name="FromDate">Date in format yyyy-MM-dd for fetching candles between two days. Date in format yyyy-MM-dd hh:mm:ss for fetching candles between two timestamps.</param>
        /// <param name="ToDate">Date in format yyyy-MM-dd for fetching candles between two days. Date in format yyyy-MM-dd hh:mm:ss for fetching candles between two timestamps.</param>
        /// <param name="Interval">The candle record interval. Possible values are: minute, day, 3minute, 5minute, 10minute, 15minute, 30minute, 60minute</param>
        /// <param name="Continuous">Pass true to get continous data of expired instruments.</param>
        /// <param name="OI">Pass true to get open interest data.</param>
        /// <returns>List of Historical objects.</returns>
        public List<Historical> GetHistoricalData(
            string InstrumentToken,
            DateTime FromDate,
            DateTime ToDate,
            string Interval,
            bool Continuous = false,
            bool OI = false)
        {
            var param = new Dictionary<string, dynamic>
            {
                { "instrument_token", InstrumentToken },
                { "from", FromDate.ToStringInvariant("yyyy-MM-dd HH:mm:ss") },
                { "to", ToDate.ToStringInvariant("yyyy-MM-dd HH:mm:ss") },
                { "interval", Interval },
                { "continuous", Continuous ? "1" : "0" },
                { "oi", OI ? "1" : "0" }
            };

            var historicalData = Get("market.historical", param);

            List<Historical> historicals = new List<Historical>();

            foreach (var item in historicalData["data"]["candles"])
            {
                historicals.Add(new Historical(item));
            }

            return historicals;
        }

        /// <summary>
        /// Retrieve the buy/sell trigger range for Cover Orders.
        /// </summary>
        /// <param name="InstrumentId">Indentification of instrument in the form of EXCHANGE:TRADINGSYMBOL (eg: NSE:INFY) or InstrumentToken (eg: 408065)</param>
        /// <param name="TrasactionType">BUY or SELL</param>
        /// <returns>List of trigger ranges for given instrument ids for given transaction type.</returns>
        public Dictionary<string, TrigerRange> GetTriggerRange(string[] InstrumentId, string TrasactionType)
        {
            var param = new Dictionary<string, dynamic>
            {
                { "i", InstrumentId },
                { "transaction_type", TrasactionType.ToLowerInvariant() }
            };

            var triggerdata = Get("market.trigger_range", param)["data"];

            Dictionary<string, TrigerRange> triggerRanges = new Dictionary<string, TrigerRange>();
            foreach (string item in triggerdata.Keys)
            {
                triggerRanges.Add(item, new TrigerRange(triggerdata[item]));
            }

            return triggerRanges;
        }

        #region GTT

        /// <summary>
        /// Retrieve the list of GTTs.
        /// </summary>
        /// <returns>List of GTTs.</returns>
        public List<GTT> GetGTTs()
        {
            var gttsdata = Get("gtt");

            List<GTT> gtts = new List<GTT>();

            foreach (Dictionary<string, dynamic> item in gttsdata["data"])
            {
                gtts.Add(new GTT(item));
            }

            return gtts;
        }


        /// <summary>
        /// Retrieve a single GTT
        /// </summary>
        /// <param name="GTTId">Id of the GTT</param>
        /// <returns>GTT info</returns>
        public GTT GetGTT(int GTTId)
        {
            var param = new Dictionary<string, dynamic>
            {
                { "id", GTTId.ToStringInvariant() }
            };

            var gttdata = Get("gtt.info", param);

            return new GTT(gttdata["data"]);
        }

        /// <summary>
        /// Place a GTT order
        /// </summary>
        /// <param name="gttParams">Contains the parameters for the GTT order</param>
        /// <returns>Json response in the form of nested string dictionary.</returns>
        public Dictionary<string, dynamic> PlaceGTT(GTTParams gttParams)
        {
            var condition = new Dictionary<string, dynamic>
            {
                { "exchange", gttParams.Exchange },
                { "tradingsymbol", gttParams.TradingSymbol },
                { "trigger_values", gttParams.TriggerPrices },
                { "last_price", gttParams.LastPrice },
                { "instrument_token", gttParams.InstrumentToken }
            };

            var ordersParam = new List<Dictionary<string, dynamic>>();
            foreach (var o in gttParams.Orders)
            {
                var order = new Dictionary<string, dynamic>
                {
                    ["exchange"] = gttParams.Exchange,
                    ["tradingsymbol"] = gttParams.TradingSymbol,
                    ["transaction_type"] = o.TransactionType,
                    ["quantity"] = o.Quantity,
                    ["price"] = o.Price,
                    ["order_type"] = o.OrderType,
                    ["product"] = o.Product
                };
                ordersParam.Add(order);
            }

            var parms = new Dictionary<string, dynamic>
            {
                { "condition", Utils.JsonSerialize(condition) },
                { "orders", Utils.JsonSerialize(ordersParam) },
                { "type", gttParams.TriggerType }
            };

            return Post("gtt.place", parms);
        }

        /// <summary>
        /// Modify a GTT order
        /// </summary>
        /// <param name="GTTId">Id of the GTT to be modified</param>
        /// <param name="gttParams">Contains the parameters for the GTT order</param>
        /// <returns>Json response in the form of nested string dictionary.</returns>
        public Dictionary<string, dynamic> ModifyGTT(int GTTId, GTTParams gttParams)
        {
            var condition = new Dictionary<string, dynamic>
            {
                { "exchange", gttParams.Exchange },
                { "tradingsymbol", gttParams.TradingSymbol },
                { "trigger_values", gttParams.TriggerPrices },
                { "last_price", gttParams.LastPrice },
                { "instrument_token", gttParams.InstrumentToken }
            };

            var ordersParam = new List<Dictionary<string, dynamic>>();
            foreach (var o in gttParams.Orders)
            {
                var order = new Dictionary<string, dynamic>
                {
                    ["exchange"] = gttParams.Exchange,
                    ["tradingsymbol"] = gttParams.TradingSymbol,
                    ["transaction_type"] = o.TransactionType,
                    ["quantity"] = o.Quantity,
                    ["price"] = o.Price,
                    ["order_type"] = o.OrderType,
                    ["product"] = o.Product
                };
                ordersParam.Add(order);
            }

            var parms = new Dictionary<string, dynamic>
            {
                { "condition", Utils.JsonSerialize(condition) },
                { "orders", Utils.JsonSerialize(ordersParam) },
                { "type", gttParams.TriggerType },
                { "id", GTTId.ToStringInvariant() }
            };

            return Put("gtt.modify", parms);
        }

        /// <summary>
        /// Cancel a GTT order
        /// </summary>
        /// <param name="GTTId">Id of the GTT to be modified</param>
        /// <returns>Json response in the form of nested string dictionary.</returns>
        public Dictionary<string, dynamic> CancelGTT(int GTTId)
        {
            var parms = new Dictionary<string, dynamic>
            {
                { "id", GTTId.ToStringInvariant() }
            };

            return Delete("gtt.delete", parms);
        }

        #endregion GTT

        #region HTTP Functions

        /// <summary>
        /// Alias for sending a GET request.
        /// </summary>
        /// <param name="Route">URL route of API</param>
        /// <param name="Params">Additional paramerters</param>
        /// <returns>Varies according to API endpoint</returns>
        private dynamic Get(string Route, Dictionary<string, dynamic> Params = null)
        {
            return Request(Route, "GET", Params);
        }

        /// <summary>
        /// Alias for sending a POST request.
        /// </summary>
        /// <param name="Route">URL route of API</param>
        /// <param name="Params">Additional paramerters</param>
        /// <returns>Varies according to API endpoint</returns>
        private dynamic Post(string Route, Dictionary<string, dynamic> Params = null)
        {
            return Request(Route, "POST", Params);
        }

        /// <summary>
        /// Alias for sending a PUT request.
        /// </summary>
        /// <param name="Route">URL route of API</param>
        /// <param name="Params">Additional paramerters</param>
        /// <returns>Varies according to API endpoint</returns>
        private dynamic Put(string Route, Dictionary<string, dynamic> Params = null)
        {
            return Request(Route, "PUT", Params);
        }

        /// <summary>
        /// Alias for sending a DELETE request.
        /// </summary>
        /// <param name="Route">URL route of API</param>
        /// <param name="Params">Additional paramerters</param>
        /// <returns>Varies according to API endpoint</returns>
        private dynamic Delete(string Route, Dictionary<string, dynamic> Params = null)
        {
            return Request(Route, "DELETE", Params);
        }

        /// <summary>
        /// Adds extra headers to request
        /// </summary>
        /// <param name="Req">Request object to add headers</param>
        private void AddExtraHeaders(ref HttpWebRequest Req)
        {
            var KiteAssembly = System.Reflection.Assembly.GetAssembly(typeof(Kite));
            if (KiteAssembly != null)
            {
                Req.UserAgent = "KiteConnect.Net/" + KiteAssembly.GetName().Version;
            }

            Req.Headers.Add("X-Kite-Version", "3");
            Req.Headers.Add("Authorization", "token " + _apiKey + ":" + _accessToken);

            //if(Req.Method == "GET" && cache.IsCached(Req.RequestUri.AbsoluteUri))
            //{
            //    Req.Headers.Add("If-None-Match: " + cache.GetETag(Req.RequestUri.AbsoluteUri));
            //}

            Req.Timeout = _timeout;
            if (_proxy != null)
            {
                Req.Proxy = _proxy;
            }


        }

        /// <summary>
        /// Make an HTTP request.
        /// </summary>
        /// <param name="Route">URL route of API</param>
        /// <param name="Method">Method of HTTP request</param>
        /// <param name="Params">Additional paramerters</param>
        /// <returns>Varies according to API endpoint</returns>
        private dynamic Request(string Route, string Method, Dictionary<string, dynamic> Params = null)
        {
            string url = _root + _routes[Route];

            if (Params == null)
            {
                Params = new Dictionary<string, dynamic>();
            }

            if (url.Contains("{"))
            {
                var urlparams = Params.ToDictionary(entry => entry.Key, entry => entry.Value);

                foreach (KeyValuePair<string, dynamic> item in urlparams)
                {
                    if (url.Contains("{" + item.Key + "}"))
                    {
                        url = url.Replace("{" + item.Key + "}", (string)item.Value);
                        Params.Remove(item.Key);
                    }
                }
            }

            HttpWebRequest request;
            string paramString = String.Join("&", Params.Select(x => Utils.BuildParam(x.Key, x.Value)));

            if (Method == "POST" || Method == "PUT")
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.AllowAutoRedirect = true;
                request.Method = Method;
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = paramString.Length;
                AddExtraHeaders(ref request);

                using (Stream webStream = request.GetRequestStream())
                using (StreamWriter requestWriter = new StreamWriter(webStream))
                    requestWriter.Write(paramString);
            }
            else
            {
                request = (HttpWebRequest)WebRequest.Create(url + "?" + paramString);
                request.AllowAutoRedirect = true;
                request.Method = Method;
                AddExtraHeaders(ref request);
            }

            var count = 0;
            var maxTries = 5;
            WebResponse webResponse;
            while(true)
            {
                try
                {
                    webResponse = request.GetResponse();
                    break;
                }
                catch (WebException)
                {
                    if (++count > maxTries)
                    {
                        throw;
                    }
                    Thread.Sleep(100);
                }
            }

            using (Stream webStream = webResponse.GetResponseStream())
            {
                using (StreamReader responseReader = new StreamReader(webStream))
                {
                    string response = responseReader.ReadToEnd();

                    HttpStatusCode status = ((HttpWebResponse)webResponse).StatusCode;

                    if (webResponse.ContentType == "application/json")
                    {
                        JObject responseDictionary = Utils.JsonDeserialize(response);

                        if (status != HttpStatusCode.OK)
                        {
                            string errorType = "GeneralException";
                            string message = "";

                            if (responseDictionary["error_type"] != null)
                            {
                                errorType = (string)responseDictionary["error_type"];
                            }

                            if (responseDictionary["message"] != null)
                            {
                                message = (string)responseDictionary["message"];
                            }

                            
                            switch (errorType)
                            {
                                case "GeneralException": throw new GeneralException(message, status);
                                case "TokenException":
                                    {
                                        _sessionHook?.Invoke();
                                        throw new TokenException(message, status);
                                    }
                                case "PermissionException": throw new PermissionException(message, status);
                                case "OrderException": throw new OrderException(message, status);
                                case "InputException": throw new InputException(message, status);
                                case "DataException": throw new DataException(message, status);
                                case "NetworkException": throw new NetworkException(message, status);
                                default: throw new GeneralException(message, status);
                            }
                        }

                        return responseDictionary;
                    }
                    else if (webResponse.ContentType == "text/csv")
                    {
                        //return Utils.ParseCSV(response);
                        CsvConfiguration configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            HasHeaderRecord = true,
                        };
                        var reader = new StringReader(response);
                        var csv = new CsvReader(reader, configuration);
                        var csvRecords= csv.GetRecords<CsvInstrument>().ToList();
                        return csvRecords;
                    }
                    else
                        throw new DataException("Unexpected content type " + webResponse.ContentType + " " + response);
                }
            }
        }

        #endregion

        private void SaveToCsv<T>(List<T> reportData, string path)
        {
            var lines = new List<string>();
            IEnumerable<PropertyDescriptor> props = TypeDescriptor.GetProperties(typeof(T)).OfType<PropertyDescriptor>();
            var header = string.Join(",", props.ToList().Select(x => x.Name));
            lines.Add(header);
            var valueLines = reportData.Select(row => string.Join(",", header.Split(',').Select(a => row.GetType().GetProperty(a).GetValue(row, null))));
            lines.AddRange(valueLines);
            File.WriteAllLines(path, lines.ToArray());
        }

        private List<Instrument> ReadFromCSV(string path)
        {
            List<Instrument> listOfInstruments = new List<Instrument>();
            var csvContent = File.ReadAllLines(path).Skip(1);

            foreach (var item in csvContent)
            {
                try
                {
                    var values = item.Split(',');
                    var model = new Instrument
                    {
                        InstrumentToken = Convert.ToUInt32(values[0], CultureInfo.InvariantCulture),
                        ExchangeToken = Convert.ToUInt32(values[1], CultureInfo.InvariantCulture),
                        TradingSymbol = values[2],
                        Name = values[3],
                        LastPrice = Convert.ToDecimal(values[4], CultureInfo.InvariantCulture),
                        TickSize = Convert.ToDecimal(values[5], CultureInfo.InvariantCulture),

                        Expiry = Convert.ToDateTime(values[6], CultureInfo.InvariantCulture),
                        InstrumentType = values[7],
                        Segment = values[8],
                        Exchange = values[9],
                        Strike = Convert.ToDecimal(values[10], CultureInfo.InvariantCulture),
                        LotSize = Convert.ToUInt32(values[11], CultureInfo.InvariantCulture)

                    };
                    listOfInstruments.Add(model);
                }
                catch (Exception)
                {

                    continue;
                }
            }
            return listOfInstruments;
        }
    }
}
