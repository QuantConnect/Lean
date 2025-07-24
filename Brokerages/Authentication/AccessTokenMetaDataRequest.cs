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
using Newtonsoft.Json.Serialization;

namespace QuantConnect.Brokerages.Authentication
{
    /// <summary>
    /// Represents the base request for obtaining an access token, including brokerage and account information.
    /// </summary>
    public abstract class AccessTokenMetaDataRequest
    {
        /// <summary>
        /// Gets the name of the brokerage associated with the access token request.
        /// The value is normalized to lowercase.
        /// </summary>
        public string Brokerage { get; }

        /// <summary>
        /// Gets the account identifier (e.g., account number) associated with the brokerage.
        /// </summary>
        public string AccountId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessTokenMetaDataRequest"/> class.
        /// </summary>
        /// <param name="brokerage">The name of the brokerage making the request. Will be normalized to lowercase.</param>
        /// <param name="accountId">The account number or identifier associated with the brokerage.</param>
        protected AccessTokenMetaDataRequest(string brokerage, string accountId)
        {
#pragma warning disable CA1308 // Normalize strings to uppercase
            Brokerage = brokerage.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
            AccountId = accountId;
        }

        /// <summary>
        /// Serializes the request into a compact JSON string with camelCase property naming.
        /// </summary>
        /// <returns>A JSON string representing the current request.</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None
            });
        }
    }
}
