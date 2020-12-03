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
using Newtonsoft.Json;

namespace QuantConnect.Api
{
    /// <summary>
    /// Account information for an organization
    /// </summary>
    public class Account : RestResponse
    {
        /// <summary>
        /// The organization Id
        /// </summary>
        [JsonProperty(PropertyName = "organizationId")]
        public string OrganizationId { get; set; }

        /// <summary>
        /// The current account balance
        /// </summary>
        [JsonProperty(PropertyName = "creditBalance")]
        public decimal CreditBalance { get; set; }

        /// <summary>
        /// The current organizations credit card
        /// </summary>
        [JsonProperty(PropertyName = "card")]
        public Card Card { get; set; }
    }

    /// <summary>
    /// Credit card
    /// </summary>
    public class Card
    {
        /// <summary>
        /// Credit card brand
        /// </summary>
        [JsonProperty(PropertyName = "brand")]
        public string Brand { get; set; }

        /// <summary>
        /// The credit card expiration
        /// </summary>
        [JsonProperty(PropertyName = "expiration")]
        public DateTime Expiration { get; set; }

        /// <summary>
        /// The last 4 digits of the card
        /// </summary>
        [JsonProperty(PropertyName = "last4")]
        public decimal LastFourDigits { get; set; }
    }
}
