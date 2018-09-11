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
using QuantConnect.Orders;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Binance utility methods
    /// </summary>
    public partial class BinanceBrokerage
    {
        /// <summary>
        /// Timestamp in milliseconds
        /// </summary>
        /// <returns></returns>
        private long GetNonce()
        {
            return (long)(Time.TimeStamp() * 1000);
        }

        /// <summary>
        /// Creates a signature for signed endpoints
        /// </summary>
        /// <param name="payload">the body of the request</param>
        /// <returns>a token representing the request params</returns>
        private string AuthenticationToken(string payload)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiSecret)))
            {
                return ByteArrayToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            }
        }

        /// <summary>
        /// If an IP address exceeds a certain number of requests per minute
        /// HTTP 429 return code is used when breaking a request rate limit.
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

        /// <summary>
        /// Provides the current tickers price
        /// </summary>
        /// <returns></returns>
        public Messages.PriceTicker[] GetUsdConversionRates()
        {
            
            string endpoint = $"/api/v3/ticker/price";
            var req = new RestRequest(endpoint, Method.GET);
            var response = ExecuteRestRequest(req);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BinanceBrokerage.GetTick: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            return JsonConvert.DeserializeObject<Messages.PriceTicker[]>(response.Content);
        }

        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
    }
}
