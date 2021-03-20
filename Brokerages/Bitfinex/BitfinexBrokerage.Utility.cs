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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using QuantConnect.Brokerages.Bitfinex.Messages;
using Order = QuantConnect.Orders.Order;

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
        public readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);

        /// <summary>
        /// ApiKey Header
        /// </summary>
        public const string ApiKeyHeader = "bfx-apikey";

        /// <summary>
        /// Nonce Header
        /// </summary>
        public const string NonceHeader = "bfx-nonce";

        /// <summary>
        /// Signature Header
        /// </summary>
        public const string SignatureHeader = "bfx-signature";

        private long _lastNonce;
        private readonly object _lockerNonce = new object();

        private long GetNonce()
        {
            // The nonce provided must be strictly increasing but should not exceed the MAX_SAFE_INTEGER constant value of 9007199254740991.
            lock (_lockerNonce)
            {
                var nonce = (long) Math.Truncate((DateTime.UtcNow - UnixEpoch).TotalMilliseconds * 1000);

                if (nonce == _lastNonce)
                {
                    _lastNonce = ++nonce;
                }

                return nonce;
            }
        }

        /// <summary>
        /// Creates an auth token and adds to the request
        /// https://docs.bitfinex.com/docs/rest-auth
        /// </summary>
        /// <param name="request">the rest request</param>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="parameters">the body of the request</param>
        /// <returns>a token representing the request params</returns>
        private void SignRequest(IRestRequest request, string endpoint, IDictionary<string, object> parameters)
        {
            using (var hmac = new HMACSHA384(Encoding.UTF8.GetBytes(ApiSecret)))
            {
                var json = JsonConvert.SerializeObject(parameters.ToDictionary(p => p.Key, p => p.Value));
                var nonce = GetNonce().ToStringInvariant();
                var payload = $"/api{endpoint}{nonce}{json}";
                var signature = ByteArrayToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));

                request.AddHeader(ApiKeyHeader, ApiKey);
                request.AddHeader(NonceHeader, nonce);
                request.AddHeader(SignatureHeader, signature);
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
            using (var hmac = new HMACSHA384(Encoding.UTF8.GetBytes(ApiSecret)))
            {
                return ByteArrayToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            }
        }

        private Func<Wallet, bool> WalletFilter(AccountType accountType)
        {
            return wallet =>
                wallet.Type.Equals("exchange") && accountType == AccountType.Cash ||
                wallet.Type.Equals("margin") && accountType == AccountType.Margin;
        }

        /// <summary>
        /// Provides the current best bid and ask
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public Tick GetTick(Symbol symbol)
        {
            var endpoint = $"/{ApiVersion}/ticker/{_symbolMapper.GetBrokerageSymbol(symbol)}";

            var restRequest = new RestRequest(endpoint, Method.GET);
            var response = ExecuteRestRequest(restRequest);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BitfinexBrokerage.GetTick: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var tick = JsonConvert.DeserializeObject<Ticker>(response.Content);
            return new Tick(DateTime.UtcNow, symbol, tick.LastPrice, tick.Bid, tick.Ask);
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
            if (order.Status == "ACTIVE")
            {
                return OrderStatus.Submitted;
            }
            else if (order.Status.StartsWith("PARTIALLY FILLED"))
            {
                return OrderStatus.PartiallyFilled;
            }
            else if (order.Status.StartsWith("EXECUTED"))
            {
                return OrderStatus.Filled;
            }
            else if (order.Status.StartsWith("CANCELED"))
            {
                return OrderStatus.Canceled;
            }

            return OrderStatus.None;
        }

        private static string ConvertOrderType(AccountType accountType, OrderType orderType)
        {
            string outputOrderType;
            switch (orderType)
            {
                case OrderType.Limit:
                case OrderType.Market:
                    outputOrderType = orderType.ToStringInvariant().ToUpperInvariant();
                    break;

                case OrderType.StopMarket:
                    outputOrderType = "STOP";
                    break;

                default:
                    throw new NotSupportedException($"BitfinexBrokerage.ConvertOrderType: Unsupported order type: {orderType}");
            }

            return (accountType == AccountType.Cash ? "EXCHANGE " : "") + outputOrderType;
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

        private Holding ConvertHolding(Position position)
        {
            var holding = new Holding
            {
                Symbol = _symbolMapper.GetLeanSymbol(position.Symbol, SecurityType.Crypto, Market.Bitfinex),
                AveragePrice = position.BasePrice,
                Quantity = position.Amount,
                UnrealizedPnL = position.ProfitLoss,
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
            return order =>
                order.IsExchange && accountType == AccountType.Cash ||
                !order.IsExchange && accountType == AccountType.Margin;
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
                hex.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", b);
            return hex.ToString();
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
