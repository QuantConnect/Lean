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
    /// Details regarding the way holdings will be or were processed in a buyback execution
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SmartInsiderExecutionHolding
    {
        /// <summary>
        /// Held in treasury until they are sold back to the market
        /// </summary>
        [EnumMember(Value = "In Treasury")]
        Treasury,

        /// <summary>
        /// Immediately cancelled
        /// </summary>
        [EnumMember(Value = "For Cancellation")]
        Cancellation,

        /// <summary>
        /// Held in trust, generally to cover employee renumerative plans
        /// </summary>
        [EnumMember(Value = "In Trust")]
        Trust,

        /// <summary>
        /// Shares will be used to satisfy employee tax liabilities
        /// </summary>
        [EnumMember(Value = "To Satisfy Employee Tax")]
        SatisfyEmployeeTax,

        /// <summary>
        /// Not disclosed by the issuer in the announcements
        /// </summary>
        [EnumMember(Value = "Not Reported")]
        NotReported,

        /// <summary>
        /// Shares will be used to satisfy vesting of employee stock
        /// </summary>
        [EnumMember(Value = "To Satisfy Vesting of Stock")]
        SatisfyStockVesting,

        /// <summary>
        /// The field was not found in the enum, or is representative of a SatisfyStockVesting entry.
        /// </summary>
        /// <remarks>The EnumMember attribute is kept for backwards compatibility</remarks>
        [EnumMember(Value = "Missing Lookup Formula for BuybackHoldingTypeId 10.00")]
        Error
    }
}
