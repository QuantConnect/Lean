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

using RestSharp;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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

        public long GetNonce()
        {
            return (DateTime.UtcNow - dt1970).Ticks;
        }

        /// <summary>
        /// Creates an auth token and adds to the request
        /// </summary>
        /// <param name="request">the rest request</param>
        /// <param name="payload">the body of the request</param>
        /// <returns>a token representing the request params</returns>
        public void SignRequest(IRestRequest request, string payload)
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

        private IRestResponse ExecuteRestRequest(IRestRequest request, BitfinexEndpointType endpointType)
        {
            const int maxAttempts = 10;
            var attempts = 0;
            IRestResponse response;

            do
            {
                _restRateLimiter.WaitToProceed();
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
    }
}
