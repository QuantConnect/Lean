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
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace QuantConnect.Data.Custom.SmartInsider
{
    /// <summary>
    /// Describes how the transaction was executed
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SmartInsiderExecution
    {
        /// <summary>
        /// Took place via the open market
        /// </summary>
        [EnumMember(Value = "On Market")]
        Market,

        /// <summary>
        /// Via a companywide tender offer to all shareholders
        /// </summary>
        [EnumMember(Value = "Tender Offer")]
        TenderOffer,

        /// <summary>
        /// Under a specific agreement between the issuer and shareholder
        /// </summary>
        [EnumMember(Value = "Off Market Agreement")]
        OffMarket,

        /// <summary>
        /// Field is not in this enum
        /// </summary>
        Error
    }
}
