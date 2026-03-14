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

using QuantConnect.Api;

namespace QuantConnect.Brokerages.Authentication
{
    /// <summary>
    /// Represents credentials required for token-based authentication,
    /// including the access token and its type (e.g., Bearer).
    /// </summary>
    public class LeanTokenCredentials : RestResponse
    {
        /// <summary>
        /// Gets the type of the token (e.g., Bearer).
        /// </summary>
        public TokenType TokenType { get; set; }

        /// <summary>
        /// Gets the token string used for authentication.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeanTokenCredentials"/> class.
        /// </summary>
        /// <param name="tokenType">The type of the token.</param>
        /// <param name="accessToken">The token string.</param>
        public LeanTokenCredentials(TokenType tokenType, string accessToken)
        {
            TokenType = tokenType;
            AccessToken = accessToken;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeanTokenCredentials"/> class.
        /// </summary>
        public LeanTokenCredentials()
        {
        }
    }
}
