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
    /// Represents a response containing the access token metadata returned by the Lean platform.
    /// </summary>
    public class AccessTokenMetaDataResponse : RestResponse
    {
        /// <summary>
        /// Gets or sets the access token provided by the Lean platform.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the type of the token (e.g., "Bearer").
        /// </summary>
        public TokenType TokenType { get; set; } = TokenType.Bearer;
    }
}
