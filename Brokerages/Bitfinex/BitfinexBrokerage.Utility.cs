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
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using RestSharp;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Brokerages.Bitfinex
{
    /// <summary>
    /// Utility methods for Bitfinex brokerage
    /// </summary>
    public partial class BitfinexBrokerage
    {
        /// <summary>
        /// Unix Epoch
        /// </summary>
        public readonly DateTime dt1970 = new DateTime(1970, 1, 1);
        /// <summary>
        /// Key Header
        /// </summary>
        public const string KeyHeader = "X-BFX-APIKEY";
        /// <summary>
        /// Signature Header
        /// </summary>
        public const string SignatureHeader = "X-BFX-SIGNATURE";
        /// <summary>
        /// Payload Header
        /// </summary>
        public const string PayloadHeader = "X-BFX-PAYLOAD";

        private long GetNonce()
        {
            return (DateTime.UtcNow - dt1970).Ticks;
        }

        /// <summary>
        /// Creates an auth token and adds to the request
        /// https://docs.bitfinex.com/docs/rest-auth
        /// </summary>
        /// <param name="request">the rest request</param>
        /// <param name="payload">the body of the request</param>
        /// <returns>a token representing the request params</returns>
        private void SignRequest(IRestRequest request, string payload)
        {
            using (HMACSHA384 hmac = new HMACSHA384(Encoding.UTF8.GetBytes(ApiSecret)))
            {
                byte[] payloadByte = Encoding.UTF8.GetBytes(payload);
                string payloadBase64 = Convert.ToBase64String(payloadByte, Base64FormattingOptions.None);
                string payloadSha384hmac = ByteArrayToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64)));

                request.AddHeader(KeyHeader, ApiKey);
                request.AddHeader(PayloadHeader, payloadBase64);
                request.AddHeader(SignatureHeader, payloadSha384hmac);
            }
        }

        /// <summary>
        /// Creates an auth token for ws auth endppoints
        /// https://docs.bitfinex.com/docs/ws-auth
        /// </summary>
        /// <param name="payload">the body of the request</param>
        /// <returns>a token representing the request params</returns>
        private string AuthenticationToken(string payload)
        {
            using (HMACSHA384 hmac = new HMACSHA384(Encoding.UTF8.GetBytes(ApiSecret)))
            {
                return ByteArrayToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            }
        }

        private Func<Messages.Wallet, bool> WalletFilter(AccountType accountType)
        {
            return wallet => wallet.Type.Equals("exchange") && accountType == AccountType.Cash ||
                wallet.Type.Equals("trading") && accountType == AccountType.Margin;
        }

        /// <summary>
        /// Provides the current best bid and ask
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public Tick GetTick(Symbol symbol)
        {
            string endpoint = GetEndpoint($"pubticker/{_symbolMapper.GetBrokerageSymbol(symbol)}");
            var req = new RestRequest(endpoint, Method.GET);
            var response = ExecuteRestRequest(req);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BitfinexBrokerage.GetTick: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var tick = JsonConvert.DeserializeObject<Messages.Tick>(response.Content);
            return new Tick(Time.UnixTimeStampToDateTime(tick.Timestamp), symbol, tick.Bid, tick.Ask) { Quantity = tick.Volume };
        }

        /// <summary>
        /// Returns relative endpoint for current Bitfinex API version
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private string GetEndpoint(string method)
        {
            return $"/{ApiVersion}/{method}";
        }

        private static OrderStatus ConvertOrderStatus(Messages.Order order)
        {
            if (order.IsLive && order.ExecutedAmount == 0)
            {
                return Orders.OrderStatus.Submitted;
            }
            else if (order.ExecutedAmount > 0 && order.RemainingAmount > 0)
            {
                return Orders.OrderStatus.PartiallyFilled;
            }
            else if (order.RemainingAmount == 0)
            {
                return Orders.OrderStatus.Filled;
            }
            else if (order.IsCancelled)
            {
                return Orders.OrderStatus.Canceled;
            }

            return Orders.OrderStatus.None;
        }

        private static string ConvertOrderType(AccountType accountType, OrderType orderType)
        {
            string outputOrderType = string.Empty;
            switch (orderType)
            {
                case OrderType.Limit:
                case OrderType.Market:
                    outputOrderType = orderType.ToLower();
                    break;
                case OrderType.StopMarket:
                    outputOrderType = "stop";
                    break;
                default:
                    throw new NotSupportedException($"BitfinexBrokerage.ConvertOrderType: Unsupported order type: {orderType}");
            }

            return (accountType == AccountType.Cash ? "exchange " : "") + outputOrderType;
        }

        private static string ConvertOrderDirection(OrderDirection orderDirection)
        {
            if (orderDirection == OrderDirection.Buy || orderDirection == OrderDirection.Sell)
            {
                return orderDirection.ToLower();
            }

            throw new NotSupportedException($"BitfinexBrokerage.ConvertOrderDirection: Unsupported order direction: {orderDirection}");
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
                    return 1;
                case OrderType.StopMarket:
                    return ((StopMarketOrder)order).StopPrice;
            }

            throw new NotSupportedException($"BitfinexBrokerage.ConvertOrderType: Unsupported order type: {order.Type}");
        }

        private Holding ConvertHolding(Messages.Position position)
        {
            var holding = new Holding
            {
                Symbol = _symbolMapper.GetLeanSymbol(position.Symbol),
                AveragePrice = position.AveragePrice,
                Quantity = position.Amount,
                UnrealizedPnL = position.PL,
                CurrencySymbol = "$",
                Type = SecurityType.Crypto
            };

            try
            {
                var tick = GetTick(holding.Symbol);
                holding.MarketPrice = tick.Value;
            }
            catch (Exception)
            {
                Log.Error($"BitfinexBrokerage.ConvertHolding(): failed to set {holding.Symbol} market price");
                throw;
            }

            return holding;
        }

        private Func<Messages.Order, bool> OrderFilter(AccountType accountType)
        {
            return order => (order.IsExchange && accountType == AccountType.Cash) ||
                (!order.IsExchange && accountType == AccountType.Margin);
        }

        /// <summary>
        /// If an IP address exceeds a certain number of requests per minute
        /// the 429 status code and JSON response {"error": "ERR_RATE_LIMIT"} will be returned
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private IRestResponse ExecuteRestRequest(IRestRequest request)
        {
            const int maxAttempts = 10;
            var attempts = 0;
            IRestResponse response;

            do
            {
                if (!_restRateLimiter.WaitToProceed(TimeSpan.Zero))
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "RateLimit",
                        "The API request has been rate limited. To avoid this message, please reduce the frequency of API calls."));

                    _restRateLimiter.WaitToProceed();
                }

                response = RestClient.Execute(request);
                // 429 status code: Too Many Requests
            } while (++attempts < maxAttempts && (int)response.StatusCode == 429);

            return response;
        }

        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private bool SubmitOrder(string endpoint, Order order)
        {
            LockStream();

            var payload = new JsonObject();
            payload.Add("request", endpoint);
            payload.Add("nonce", GetNonce().ToStringInvariant());
            payload.Add("symbol", _symbolMapper.GetBrokerageSymbol(order.Symbol));
            payload.Add("amount", Math.Abs(order.Quantity).ToString(CultureInfo.InvariantCulture));
            payload.Add("side", ConvertOrderDirection(order.Direction));
            payload.Add("type", ConvertOrderType(_algorithm.BrokerageModel.AccountType, order.Type));
            payload.Add("price", GetOrderPrice(order).ToString(CultureInfo.InvariantCulture));

            if (order.BrokerId.Any())
            {
                payload.Add("order_id", Parse.Long(order.BrokerId.FirstOrDefault()));
            }

            var orderProperties = order.Properties as BitfinexOrderProperties;
            if (orderProperties != null)
            {
                if (order.Type == OrderType.Limit)
                {
                    payload.Add("is_hidden", orderProperties.Hidden);
                    payload.Add("is_postonly", orderProperties.PostOnly);
                }
            }

            var request = new RestRequest(endpoint, Method.POST);
            request.AddJsonBody(payload.ToString());
            SignRequest(request, payload.ToString());

            var response = ExecuteRestRequest(request);
            var orderFee = OrderFee.Zero;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = JsonConvert.DeserializeObject<Messages.Order>(response.Content);

                if (string.IsNullOrEmpty(raw?.Id))
                {
                    var errorMessage = $"Error parsing response from place order: {response.Content}";
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Bitfinex Order Event") { Status = OrderStatus.Invalid, Message = errorMessage });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, (int)response.StatusCode, errorMessage));

                    UnlockStream();
                    return true;
                }

                var brokerId = raw.Id;
                if (CachedOrderIDs.ContainsKey(order.Id))
                {
                    CachedOrderIDs[order.Id].BrokerId.Clear();
                    CachedOrderIDs[order.Id].BrokerId.Add(brokerId);
                }
                else
                {
                    order.BrokerId.Add(brokerId);
                    CachedOrderIDs.TryAdd(order.Id, order);
                }

                // Generate submitted event
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Bitfinex Order Event") { Status = OrderStatus.Submitted });
                Log.Trace($"Order submitted successfully - OrderId: {order.Id}");

                UnlockStream();
                return true;
            }

            var message = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {response.Content}";
            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Bitfinex Order Event") { Status = OrderStatus.Invalid });
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));

            UnlockStream();
            return true;
        }

        /// <summary>
        /// Maps Resolution to IB representation
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        private string ConvertResolution(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                case Resolution.Second:
                    throw new ArgumentException($"BitfinexBrokerage.ConvertResolution: Unsupported resolution type: {resolution}");
                case Resolution.Minute:
                    return "1m";
                case Resolution.Hour:
                    return "1h";
                case Resolution.Daily:
                default:
                    return "1D";
            }
        }
    }
}
