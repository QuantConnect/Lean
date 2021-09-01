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
using QuantConnect.Brokerages.Samco.SamcoMessages;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Util;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;

namespace QuantConnect.Brokerages.Samco
{
    /// <summary>
    /// Utility methods for Samco brokerage
    /// </summary>
    public class SamcoBrokerageAPI : IDisposable
    {
        private readonly RateGate _restRateLimiter = new RateGate(10, TimeSpan.FromSeconds(1));
        private readonly string _tokenHeader = "x-session-token";
        private string _token = "";

        /// <summary>
        /// Constructor for Samco API
        /// </summary>
        public SamcoBrokerageAPI()
        {
            RestClient = new RestClient("https://api.stocknote.com");
        }

        /// <summary>
        /// Gets the RestClient.
        /// </summary>
        /// <value>An instance of the RestClient</value>
        public IRestClient RestClient { get; }

        /// <summary>
        /// Samco API Token
        /// </summary>
        /// <returns>A Samco API Token</returns>
        public string SamcoToken => _token;

        /// <summary>
        /// Initialize a new Samco API instance.
        /// </summary>
        /// <param name="login">Your Samco User ID</param>
        /// <param name="password">Your Samco login Password</param>
        /// <param name="yearOfBirth">Birth year as registered with Samco</param>
        public void Authorize(string login, string password, string yearOfBirth)
        {
            var auth = new AuthRequest
            {
                userId = login,
                password = password,
                yob = yearOfBirth
            };

            var request = new RestRequest("/login", Method.POST);
            request.AddJsonBody(JsonConvert.SerializeObject(auth));

            IRestResponse response = RestClient.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(
                    $"SamcoBrokerage.Authorize: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}"
                );
            }
            var obj = JsonConvert.DeserializeObject<JObject>(response.Content);
            _token = obj["sessionToken"].Value<string>();
        }

        /// <summary>
        /// Cancels the order, Invokes cancelOrder call from Samco api
        /// </summary>
        /// <returns>OrderResponse</returns>
        public SamcoOrderResponse CancelOrder(string orderID)
        {
            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "order/cancelOrder?orderNumber={0}", orderID), Method.DELETE);
            var response = ExecuteRestRequest(request);
            var orderResponse = JsonConvert.DeserializeObject<SamcoOrderResponse>(response.Content);
            return orderResponse;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _restRateLimiter.Dispose();
        }

        /// <summary>
        /// If an IP address exceeds a certain number of requests per minute the 429 status code and
        /// JSON response {"error": "ERR_RATE_LIMIT"} will be returned
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public IRestResponse ExecuteRestRequest(IRestRequest request)
        {
            const int maxAttempts = 10;
            var attempts = 0;

            IRestResponse response;
            SignRequest(request);
            do
            {
                if (!_restRateLimiter.WaitToProceed(TimeSpan.Zero))
                {
                    Log.Trace("Brokerage.OnMessage(): " + new BrokerageMessageEvent(BrokerageMessageType.Warning, "RateLimit",
                        "The API request has been rate limited. To avoid this message, please reduce the frequency of API calls."));

                    _restRateLimiter.WaitToProceed();
                }

                response = RestClient.Execute(request);
                // 429 status code: Too Many Requests
            } while (++attempts < maxAttempts && (int)response.StatusCode == 429);
            return response;
        }

        /// <summary>
        /// Gets HoldingsResponses which contains list of Holding Details, Invokes getHoldings call
        /// from Samco api
        /// </summary>
        /// <returns>HoldingsResponse</returns>
        public HoldingsResponse GetHoldings()
        {
            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "holding/getHoldings"), Method.GET);
            var response = ExecuteRestRequest(request);
            var holdingResponse = JsonConvert.DeserializeObject<HoldingsResponse>(response.Content);
            return holdingResponse;
        }

        /// <summary>
        /// Get historical intraday candles for given symbol
        /// </summary>
        /// <param name="leanSymbol">Lean symbol</param>
        /// <param name="exchange">Exchange at which symbol is traded. Like NSE/BSE</param>
        /// <param name="startDateTime">Start date of request</param>
        /// <param name="endDateTime">End date of request</param>
        /// <param name="resolution">Resolution for which data is required</param>
        /// <param name="isIndex">If given symbol is index or not</param>
        public IEnumerable<TradeBar> GetIntradayCandles(Symbol leanSymbol, string exchange, DateTime startDateTime, DateTime endDateTime, Resolution resolution = Resolution.Minute, bool isIndex = false)
        {
            var brokerageSymbol = leanSymbol.ID.Symbol;
            var interval = 1;
            if (resolution == Resolution.Hour)
            {
                interval = 60;
            }
            var latestTime = startDateTime;

            do
            {
                latestTime = latestTime.AddDays(29);
                if (endDateTime < latestTime)
                {
                    latestTime = endDateTime;
                }
                var start = startDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                var end = latestTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                string endpoint = $"/intraday/candleData?symbolName={brokerageSymbol}&fromDate={start}&toDate={end}&exchange={exchange}&interval={interval}";
                if (isIndex)
                {
                    endpoint = $"/intraday/indexCandleData?symbolName={brokerageSymbol}&fromDate={start}&toDate={end}&exchange={exchange}&interval={interval}";
                }

                var restRequest = new RestRequest(endpoint, Method.GET);
                var response = ExecuteRestRequest(restRequest);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(
                        $"SamcoBrokerage.GetHistory: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, " +
                        $"Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
                }

                // we need to drop the last bar provided by the exchange as its open time is a history
                // request's end time
                var candles = JsonConvert.DeserializeObject<CandleResponse>(response.Content);

                if (candles.intradayCandleData?.Any() == null)
                {
                    yield break;
                }

                foreach (var candle in candles.intradayCandleData)
                {
                    yield return new TradeBar()
                    {
                        Time = candle.dateTime,
                        Symbol = leanSymbol,
                        Low = candle.low,
                        High = candle.high,
                        Open = candle.open,
                        Close = candle.close,
                        Volume = candle.volume,
                        Value = candle.close,
                        DataType = MarketDataType.TradeBar,
                        Period = resolution.ToTimeSpan()
                    };
                }
                startDateTime = latestTime;
            } while (startDateTime < endDateTime);
        }

        /// <summary>
        /// Gets orderbook from SamcoApi, Invokes orderBook call from Samco api
        /// </summary>
        /// <returns>OrderBookResponse</returns>
        public OrderBookResponse GetOrderBook()
        {
            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "order/orderBook"), Method.GET);
            var response = ExecuteRestRequest(request);
            var orderBook = JsonConvert.DeserializeObject<OrderBookResponse>(response.Content);
            return orderBook;
        }

        /// <summary>
        /// Gets Order Details, Invokes getOrderStatus call from Samco api
        /// </summary>
        /// <returns>OrderResponse</returns>
        public SamcoOrderResponse GetOrderDetails(string orderID)
        {
            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "order/getOrderStatus?orderNumber={0}", orderID), Method.GET);
            var response = ExecuteRestRequest(request);
            var orderResponse = JsonConvert.DeserializeObject<SamcoOrderResponse>(response.Content);
            return orderResponse;
        }

        /// <summary>
        /// Gets position details of the user (The details of equity, derivative, commodity,
        /// currency borrowed or owned by the user).
        /// </summary>
        /// <returns>PostionsResponse</returns>
        public PositionsResponse GetPositions(string positionType = "DAY")
        {
            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "position/getPositions?positionType={0}", positionType), Method.GET);
            var response = ExecuteRestRequest(request);
            var positionsReponse = JsonConvert.DeserializeObject<PositionsResponse>(response.Content);
            return positionsReponse;
        }

        /// <summary>
        /// Get quote for a given symbol.
        /// </summary>
        /// <param name="symbol">brokerage symbol</param>
        /// <param name="exchange">Exchange at which symbol is traded. Like NSE/BSE</param>
        public QuoteResponse GetQuote(string symbol, string exchange = "NSE")
        {
            string endpoint = $"/quote/getQuote?symbolName={HttpUtility.UrlEncode(symbol)}&exchange={exchange.ToUpperInvariant()}";
            var req = new RestRequest(endpoint, Method.GET);
            var response = ExecuteRestRequest(req);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(
                    $"SamcoBrokerage.GetQuote: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}"
                );
            }

            var quote = JsonConvert.DeserializeObject<QuoteResponse>(response.Content);
            return quote;
        }

        /// <summary>
        /// Gets User limits i.e. cash balances, Invokes getLimits call from Samco api
        /// </summary>
        /// <returns>UserLimitResponse</returns>
        public UserLimitResponse GetUserLimits()
        {
            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "limit/getLimits"), Method.GET);
            var response = ExecuteRestRequest(request);
            var userLimitResponse = JsonConvert.DeserializeObject<UserLimitResponse>(response.Content);
            return userLimitResponse;
        }

        /// <summary>
        /// Modifies the order, Invokes modifyOrder call from Samco api
        /// </summary>
        /// <returns>OrderResponse</returns>
        public SamcoOrderResponse ModifyOrder(Order order)
        {
            var payload = new JsonObject
            {
                { "orderValidity", GetOrderValidity(order.TimeInForce) },
                { "quantity", Math.Abs(order.Quantity).ToString(CultureInfo.InvariantCulture) },
                { "orderType", ConvertOrderType(order.Type) },
                { "price", GetOrderPrice(order).ToString(CultureInfo.InvariantCulture) },
                { "triggerPrice", GetOrderTriggerPrice(order).ToString(CultureInfo.InvariantCulture) }
            };

            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "order/modifyOrder/{0}", order.Id), Method.PUT);
            request.AddJsonBody(payload.ToString());
            var response = ExecuteRestRequest(request);
            var orderResponse = JsonConvert.DeserializeObject<SamcoOrderResponse>(response.Content);
            return orderResponse;
        }

        /// <summary>
        /// Places the order, Invokes PlaceOrder call from Samco api
        /// </summary>
        /// <returns>List of Order Details</returns>
        public SamcoOrderResponse PlaceOrder(Order order, string symbol, string exchange, string productType)
        {
            var payload = new JsonObject
            {
                { "exchange", exchange },
                { "orderValidity", GetOrderValidity(order.TimeInForce) },
                { "afterMarketOrderFlag", "NO" },
                { "productType", productType },
                { "symbolName", symbol },
                { "quantity", Math.Abs(order.Quantity).ToString(CultureInfo.InvariantCulture) },
                { "disclosedQuantity", Math.Abs(order.Quantity).ToString(CultureInfo.InvariantCulture) },
                { "transactionType", ConvertOrderDirection(order.Direction) },
                { "orderType", ConvertOrderType(order.Type) },
            };

            if (order.Type == OrderType.Market || order.Type == OrderType.StopMarket)
            {
                payload.Add("marketProtection", "2");
            }

            if (order.Type == OrderType.StopLimit || order.Type == OrderType.StopMarket || order.Type == OrderType.Limit)
            {
                payload.Add("triggerPrice", GetOrderTriggerPrice(order).ToString(CultureInfo.InvariantCulture));
            }
            if (GetOrderPrice(order).ToString(CultureInfo.InvariantCulture) != "0")
            {
                payload.Add("price", GetOrderPrice(order).ToString(CultureInfo.InvariantCulture));
            }
            var request = new RestRequest("order/placeOrder", Method.POST);
            request.AddJsonBody(payload.ToString());
            var response = ExecuteRestRequest(request);
            var orderResponse = JsonConvert.DeserializeObject<SamcoOrderResponse>(response.Content);
            return orderResponse;
        }

        private static string ConvertOrderDirection(OrderDirection orderDirection)
        {
            if (orderDirection == OrderDirection.Buy || orderDirection == OrderDirection.Sell)
            {
                return orderDirection.ToString().ToUpperInvariant();
            }

            throw new NotSupportedException($"SamcoBrokerage.ConvertOrderDirection: Unsupported order direction: {orderDirection}");
        }

        private static string ConvertOrderType(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.Limit:
                    return "L";

                case OrderType.Market:
                    return "MKT";

                case OrderType.StopMarket:
                    return "SL-M";

                default:
                    throw new NotSupportedException($"SamcoBrokerage.ConvertOrderType: Unsupported order type: {orderType}");
            }
        }

        /// <summary>
        /// Return a relevant price for order depending on order type Price must be positive
        /// </summary>
        /// <param name="order"></param>
        /// <returns>A price for order</returns>
        private static decimal GetOrderPrice(Order order)
        {
            switch (order.Type)
            {
                case OrderType.Limit:
                    return ((LimitOrder)order).LimitPrice;

                case OrderType.Market:
                    // Order price must be positive for market order too; refuses for price = 0
                    return 0;

                case OrderType.StopMarket:
                    return ((StopMarketOrder)order).StopPrice;
            }

            throw new NotSupportedException($"SamcoBrokerage.ConvertOrderType: Unsupported order type: {order.Type}");
        }

        /// <summary>
        /// Return a relevant price for order depending on order type Price must be positive
        /// </summary>
        /// <param name="order"></param>
        /// <returns>A trigger price for order</returns>
        private static decimal GetOrderTriggerPrice(Order order)
        {
            switch (order.Type)
            {
                case OrderType.Limit:
                    return ((LimitOrder)order).LimitPrice;

                case OrderType.Market:
                    // Order price must be positive for market order too; refuses for price = 0
                    return 0;

                case OrderType.StopMarket:
                    return ((StopMarketOrder)order).StopPrice;
            }

            throw new NotSupportedException($"SamcoBrokerage.ConvertOrderType: Unsupported order type: {order.Type}");
        }

        private static string GetOrderValidity(TimeInForce orderTimeforce)
        {
            if (orderTimeforce == TimeInForce.GoodTilCanceled || orderTimeforce == TimeInForce.Day)
            {
                return "DAY";
            }
            throw new NotSupportedException($"SamcoBrokerage.GetOrderValidity: Unsupported orderTimeforce: {orderTimeforce}");
        }

        private void SignRequest(IRestRequest request)
        {
            request.AddHeader(_tokenHeader, _token);
        }
    }
}
