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

using System;
using QuantConnect.Api;

namespace QuantConnect.Brokerages.Authentication
{
    /// <summary>
    /// Represents a response containing metadata about an access token issued by Lean.
    /// </summary>
    public abstract class AccessTokenMetaDataResponse : RestResponse
    {
        /// <summary>
        /// Gets the access token provided by Lean.
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        /// Gets the type of the token (e.g., "Bearer").
        /// </summary>
        public TokenType TokenType { get; }

        /// <summary>
        /// Gets the UTC expiration timestamp of the access token, with a 1-minute safety buffer applied.
        /// </summary>
        public DateTime Expiration { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessTokenMetaDataResponse"/> class.
        /// </summary>
        /// <param name="accessToken">The access token string provided by the authentication service.</param>
        /// <param name="tokenType">The type of the token (e.g., Bearer).</param>
        /// <param name="expires">The expiration time of the access token (in UTC), with safety buffer applied.</param>
        protected AccessTokenMetaDataResponse(string accessToken, TokenType tokenType, DateTime expires)
        {
            AccessToken = accessToken;
            TokenType = tokenType;
            Expiration = expires;
        }
    }
}
