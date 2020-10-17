using Newtonsoft.Json;
using QuantConnect.Logging;
using QuantConnect.Orders;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.Samco.Messages;
using OrderResponse = QuantConnect.Brokerages.Samco.Messages.OrderResponse;
using QuantConnect.Data.Market;
using System.Linq;
using QuantConnect.Interfaces;
using System.Collections.Concurrent;

namespace QuantConnect.Brokerages.Samco
{
    /// <summary>
    /// Utility methods for Samco brokerage
    /// </summary>
    public class SamcoBrokerageAPI
    {
        public readonly string tokenHeader = "x-session-token";
        public string token = "";
        private readonly SamcoSymbolMapper _symbolMapper = new SamcoSymbolMapper();
        public IRestClient RestClient { get; }
        public ConcurrentDictionary<int, Orders.Order> CachedOrderIDs = new ConcurrentDictionary<int, Orders.Order>();

        public SamcoBrokerageAPI(IRestClient restClient)
        {
            RestClient = restClient;

        }

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
            token = obj["sessionToken"].Value<string>();
        }

        private void SignRequest(IRestRequest request)
        {
            request.AddHeader(tokenHeader, token);
        }

        public QuoteResponse GetQuote(Symbol symbol)
        {
            string endpoint = $"/quote/getQuote?symbolName={_symbolMapper.GetBrokerageSymbol(symbol)}";
            var req = new RestRequest(endpoint, Method.GET);
            var response = ExecuteRestRequest(req);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(
                    $"SamcoBrokerage.GetTick: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}"
                );
            }

            var tick = JsonConvert.DeserializeObject<QuoteResponse>(response.Content);
            return tick;
        }

        public IEnumerable<TradeBar> GetIntradayCandles(string symbol, string exchange, DateTime startDateTime, DateTime endDateTime)
        {

            var start = startDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var end = endDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            string endpoint = $"/intraday/candleData?symbolName={symbol}&fromDate={start}&toDate={end}&exchange={exchange}";

            var restRequest = new RestRequest(endpoint, Method.GET);
            var response = ExecuteRestRequest(restRequest);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(
                    $"SamcoBrokerage.GetHistory: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, " +
                    $"Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            // we need to drop the last bar provided by the exchange as its open time is a history request's end time
            var candles = JsonConvert.DeserializeObject<CandleResponse>(response.Content);

            if (!candles.intradayCandleData.Any())
            {

                yield break;
            }

            foreach (var candle in candles.intradayCandleData)
            {
                yield return new TradeBar()
                {
                    Time = candle.dateTime,
                    Symbol = symbol,
                    Low = candle.low,
                    High = candle.high,
                    Open = candle.open,
                    Close = candle.close,
                    Volume = candle.volume,
                    Value = candle.close,
                    DataType = MarketDataType.TradeBar,
                    Period = Resolution.Minute.ToTimeSpan(),
                    EndTime = candle.dateTime.AddMinutes(1)
                };
            }
        }

        private static string ConvertOrderType(AccountType accountType, OrderType orderType)
        {

            switch (orderType)
            {
                case OrderType.Limit:
                    return "L";
                case OrderType.Market:
                    return "MKT";
                case OrderType.StopMarket:
                    return "SL-M";
                case OrderType.Bracket:
                    return "BO";
                default:
                    throw new NotSupportedException($"SamcoBrokerage.ConvertOrderType: Unsupported order type: {orderType}");
            }
        }

        private static string ConvertOrderDirection(OrderDirection orderDirection)
        {
            if (orderDirection == OrderDirection.Buy || orderDirection == OrderDirection.Sell)
            {
                return orderDirection.ToString().ToUpperInvariant();
            }

            throw new NotSupportedException($"SamcoBrokerage.ConvertOrderDirection: Unsupported order direction: {orderDirection}");
        }

        /// <summary>
        /// Return a relevant price for order depending on order type
        /// Price must be positive
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private static decimal GetOrderPrice(Order order)
        {
            switch (order.Type)
            {
                case OrderType.Limit:
                    return ((LimitOrder)order).LimitPrice;
                case OrderType.Market:
                    // Order price must be positive for market order too;
                    // refuses for price = 0
                    return 0;
                //case OrderType.Bracket:
                //    return ((BracketOrder)order).EntryPrice;
                case OrderType.StopMarket:
                    return ((StopMarketOrder)order).StopPrice;
            }

            throw new NotSupportedException($"SamcoBrokerage.ConvertOrderType: Unsupported order type: {order.Type}");
        }

        /// <summary>
        /// Return a relevant price for order depending on order type
        /// Price must be positive
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private static decimal GetOrderTriggerPrice(Order order)
        {
            switch (order.Type)
            {
                case OrderType.Limit:
                    return ((LimitOrder)order).LimitPrice;
                case OrderType.Market:
                    // Order price must be positive for market order too;
                    // refuses for price = 0
                    return 0;
                case OrderType.Bracket:
                    return ((BracketOrder)order).EntryPrice;
                case OrderType.StopMarket:
                    return ((StopMarketOrder)order).StopPrice;
            }

            throw new NotSupportedException($"SamcoBrokerage.ConvertOrderType: Unsupported order type: {order.Type}");
        }

        private Holding ConvertHolding(HoldingsResponse.HoldingDetail detail)
        {
            var holding = new Holding
            {
                Symbol = _symbolMapper.GetLeanSymbol(detail.tradingSymbol),
                AveragePrice = detail.averagePrice,
                Quantity = detail.holdingsQuantity,
                UnrealizedPnL = (detail.lastTradedPrice - detail.averagePrice) * detail.holdingsQuantity,
                CurrencySymbol = Currencies.GetCurrencySymbol("INR"),
                Type = SecurityType.Equity
            };

            try
            {
                holding.MarketPrice = detail.lastTradedPrice;
            }
            catch (Exception)
            {
                Log.Error($"SamcoBrokerage.ConvertHolding(): failed to set {holding.Symbol} market price");
                throw;
            }
            return holding;
        }

        /// <summary>
        /// If an IP address exceeds a certain number of requests per minute
        /// the 429 status code and JSON response {"error": "ERR_RATE_LIMIT"} will be returned
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public IRestResponse ExecuteRestRequest(IRestRequest request)
        {
            IRestResponse response;
            SignRequest(request);
            response = RestClient.Execute(request);
            return response;
        }
        /// <summary>
        /// Modifies the order, Invokes modifyOrder call from Samco api
        /// </summary>
        /// 
        /// <returns>OrderResponse </returns>
        public OrderResponse ModifyOrder(Order order, IAlgorithm _algorithm)
        {


            var payload = new JsonObject
            {
                { "marketProtection", "--" },
                { "orderValidity", "DAY" },
                { "parentOrderId", order.BrokerId[0] },
                { "quantity", Math.Abs(order.Quantity).ToString(CultureInfo.InvariantCulture) },
                { "disclosedQuantity", Math.Abs(order.Quantity).ToString(CultureInfo.InvariantCulture) },
                { "orderType", ConvertOrderType(_algorithm.BrokerageModel.AccountType, order.Type) },
                { "price", GetOrderPrice(order).ToString(CultureInfo.InvariantCulture) },
                { "triggerPrice", GetOrderPrice(order).ToString(CultureInfo.InvariantCulture) }
            };

            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "order/modifyOrder/{0}", order.Id), Method.PUT);
            request.AddJsonBody(payload.ToString());

            var response = ExecuteRestRequest(request);
            var orderResponse = JsonConvert.DeserializeObject<OrderResponse>(response.Content);
            return orderResponse;


        }

        /// <summary>
        /// Places the order, Invokes PlaceOrder call from Samco api
        /// </summary>
        /// 
        /// <returns>List of Order Details  </returns>
        public OrderResponse PlaceOrder(Order order, IAlgorithm _algorithm)
        {
            
            var payload = new JsonObject
            {
                { "exchange", "NSE" },
                { "priceType", "LTP" },
                { "orderValidity", "DAY" },
                { "afterMarketOrderFlag", "NO" },
                { "productType", "MIS" },
                { "symbolName", _symbolMapper.GetBrokerageSymbol(order.Symbol) },
                { "quantity", Math.Abs(order.Quantity).ToString(CultureInfo.InvariantCulture) },
                { "disclosedQuantity", Math.Abs(order.Quantity).ToString(CultureInfo.InvariantCulture) },
                { "transactionType", ConvertOrderDirection(order.Direction) },
                { "orderType", ConvertOrderType(_algorithm.BrokerageModel.AccountType, order.Type) },
            };

            if (order.Type == OrderType.Market || order.Type == OrderType.StopMarket)
            {
                //payload.Add("marketProtection", "2");
            } else
            {
                //payload.Add("marketProtection", "--" );
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
            var orderResponse = JsonConvert.DeserializeObject<OrderResponse>(response.Content);
            return orderResponse;

        }

        /// <summary>
        /// Places the order, Invokes PlaceBracketOrder call from Samco api
        /// </summary>
        /// 
        /// <returns>List of Order Details  </returns>
        public OrderResponse PlaceBracketOrder(Order order, IAlgorithm _algorithm)
        {

            var stopLossValue = ((BracketOrder)order).StopPrice;
            var squareOffValue = ((BracketOrder)order).LimitPrice;
            var trailingStopLoss = ((BracketOrder)order).TrailingStopLoss;
            var payload = new JsonObject
            {
                { "exchange", "NSE" },
                { "orderValidity", "DAY" },
                { "productType", "BO" },
                { "symbolName", _symbolMapper.GetBrokerageSymbol(order.Symbol) },
                { "quantity", Math.Abs(order.Quantity).ToString(CultureInfo.InvariantCulture) },
                { "disclosedQuantity", Math.Abs(order.Quantity).ToString(CultureInfo.InvariantCulture) },
                { "transactionType", ConvertOrderDirection(order.Direction) },
                { "orderType", "L" },
                { "price", GetOrderPrice(order).ToString(CultureInfo.InvariantCulture) },
                { "priceType", "LTP" }, //LTP / ATP
                { "valueType","Absolute"}, //Absolute / Ticks
                { "trailingStopLoss", trailingStopLoss},
                { "squareOffValue", squareOffValue },
                { "stopLossValue", stopLossValue},
                { "triggerPrice", GetOrderPrice(order).ToString(CultureInfo.InvariantCulture) }
            };

            var request = new RestRequest("order/placeOrderBO", Method.POST);
            request.AddJsonBody(payload.ToString());
            var response = ExecuteRestRequest(request);
            var orderResponse = JsonConvert.DeserializeObject<OrderResponse>(response.Content);
            return orderResponse;

        }

        /// <summary>
        /// Cancels the order, Invokes cancelOrder call from Samco api
        /// </summary>
        /// 
        /// <returns>OrderResponse  </returns>
        public OrderResponse CancelBracketOrder(string orderID)
        {
            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "order/exitBO?orderNumber={0}", orderID), Method.DELETE);
            var response = ExecuteRestRequest(request);
            var orderResponse = JsonConvert.DeserializeObject<OrderResponse>(response.Content);
            return orderResponse;

        }

        /// <summary>
        /// Cancels the order, Invokes cancelOrder call from Samco api
        /// </summary>
        /// 
        /// <returns>OrderResponse  </returns>
        public OrderResponse CancelOrder(string orderID)
        {
            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "order/cancelOrder?orderNumber={0}", orderID), Method.DELETE);
            var response = ExecuteRestRequest(request);
            var orderResponse = JsonConvert.DeserializeObject<OrderResponse>(response.Content);
            return orderResponse;

        }
        /// <summary>
        /// Gets orderbook from SamcoApi, Invokes orderBook call from Samco api
        /// </summary>
        /// 
        /// <returns>OrderBookResponse </returns>
        public OrderBookResponse GetOrderBook()
        {
            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "order/orderBook"), Method.GET);
            var response = ExecuteRestRequest(request);
            var orderBook = JsonConvert.DeserializeObject<OrderBookResponse>(response.Content);
            if (orderBook.status == "Success")
            {
                return orderBook;

            }
            return orderBook;
        }
        /// <summary>
        /// Gets HoldingsResponses  which contains list of Holding Details, Invokes getHoldings call from Samco api
        /// </summary>
        /// <returns>HoldingsResponse </returns>
        /// 
        public HoldingsResponse GetHoldings()
        {

            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "holding/getHoldings"), Method.GET);
            var response = ExecuteRestRequest(request);
            var holdingResponse = JsonConvert.DeserializeObject<HoldingsResponse>(response.Content);
            return holdingResponse;

        }
        /// <summary>
        /// Gets Order Details, Invokes getOrderStatus call from Samco api
        /// </summary>
        /// <returns>OrderResponse </returns>
        /// 
        public OrderResponse GetOrderDetails(string orderID)
        {
            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "order/getOrderStatus?orderNumber={0}", orderID), Method.GET);
            var response = ExecuteRestRequest(request);
            var orderResponse = JsonConvert.DeserializeObject<OrderResponse>(response.Content);
            return orderResponse;

        }
        /// <summary>
        /// Gets User limits i.e. cash balances, Invokes getLimits call from Samco api
        /// </summary>
        /// <returns>UserLimitResponse </returns>
        /// 
        public UserLimitResponse GetUserLimits()
        {
            var request = new RestRequest(string.Format(CultureInfo.InvariantCulture, "limit/getLimits"), Method.GET);
            var response = ExecuteRestRequest(request);
            var userLimitResponse = JsonConvert.DeserializeObject<UserLimitResponse>(response.Content);
            return userLimitResponse;

        }
    }
}
