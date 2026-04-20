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
    /// Represents a Lean platform token request, including all fields required by the
    /// <c>live/auth0/refresh</c> endpoint. Optional fields are omitted from JSON when null.
    /// </summary>
    public class OAuthTokenRequest
    {
        /// <summary>
        /// Gets the name of the brokerage associated with the access token request.
        /// The value is normalized to lowercase.
        /// </summary>
        public string Brokerage { get; set; }

        /// <summary>
        /// Gets the account identifier associated with the brokerage.
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// Gets the OAuth refresh token used to obtain a new access token.
        /// Omitted from JSON when null.
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets the Lean deploy identifier for brokerages that require it.
        /// Omitted from JSON when null.
        /// </summary>
        public string DeployId { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="OAuthTokenRequest"/> with all fields.
        /// Use named parameters to supply only the fields required by the target brokerage.
        /// </summary>
        /// <param name="brokerage">The brokerage name. Normalized to lowercase.</param>
        /// <param name="accountId">The account number or identifier.</param>
        /// <param name="refreshToken">OAuth refresh token; omitted from JSON when null.</param>
        /// <param name="deployId">Lean deploy identifier; omitted from JSON when null.</param>
        public OAuthTokenRequest(
            string brokerage,
            string accountId,
            string refreshToken = null,
            string deployId = null)
        {
#pragma warning disable CA1308 // Normalize strings to uppercase
            Brokerage = brokerage.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
            AccountId = accountId;
            RefreshToken = refreshToken;
            DeployId = deployId;
        }

        /// <summary>
        /// Serializes the request into a compact camelCase JSON string.
        /// Null properties are excluded from the output.
        /// </summary>
        /// <returns>A JSON string representing the current request.</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None
            });
        }
    }
}
