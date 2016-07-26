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
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Tradier
{
    /// <summary>
    /// Token response model from QuantConnect terminal
    /// </summary>
    public class TokenResponse
    {
        /// Access token for current requests:
        [JsonProperty(PropertyName = "sAccessToken")]
        public string AccessToken;

        /// Refersh token for next time
        [JsonProperty(PropertyName = "sRefreshToken")]
        public string RefreshToken;

        /// Seconds the tokens expires
        [JsonProperty(PropertyName = "iExpiresIn")]
        public int ExpiresIn;

        /// Scope of token access
        [JsonProperty(PropertyName = "sScope")]
        public string Scope;

        /// Time the token was issued:
        [JsonProperty(PropertyName = "dtIssuedAt")]
        public DateTime IssuedAt;

        /// Success flag:
        [JsonProperty(PropertyName = "success")]
        public bool Success;

        /// <summary>
        ///  Default constructor:
        /// </summary>
        public TokenResponse() 
        { }
    }

}
