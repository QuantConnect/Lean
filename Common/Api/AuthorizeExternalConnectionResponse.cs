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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Api
{
    /// <summary>
    /// Response for authenticating with an external brokerage or data provider for a live algorithm
    /// </summary>
    public class AuthorizeExternalConnectionResponse : RestResponse
    {
        /// <summary>
        /// Authentication information from the data provider or brokerage, including the access token or refresh token
        /// </summary>
        [JsonProperty(PropertyName = "authorization")]
        public Dictionary<string, object> Authorization { get; set; }
    }
}
