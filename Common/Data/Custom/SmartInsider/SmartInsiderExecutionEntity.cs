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
    /// Entity that intends to or executed the transaction
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SmartInsiderExecutionEntity
    {
        /// <summary>
        /// Issuer of the stock
        /// </summary>
        [EnumMember(Value = "Issuer")]
        Issuer,

        /// <summary>
        /// Subsidiary of the issuer
        /// </summary>
        [EnumMember(Value = "Subsidiary")]
        Subsidiary,

        /// <summary>
        /// Brokers are commonly used to repurchase shares under mandate to avoid insider
        /// information rules and to allow repurchases to carry on through close periods
        /// </summary>
        [EnumMember(Value = "Broker")]
        Broker,

        /// <summary>
        /// Unknown - Transaction
        /// </summary>
        [EnumMember(Value = "Employer Benefit Trust")]
        EmployerBenefitTrust,

        /// <summary>
        /// To cater for shares which will need to be transferred to employees as part of remunerative plans
        /// </summary>
        [EnumMember(Value = "Employee Benefit Trust")]
        EmployeeBenefitTrust,

        /// <summary>
        /// Undisclosed independent third party. Likely to be a broker.
        /// </summary>
        [EnumMember(Value = "Independent 3rd Party")]
        ThirdParty,

        /// <summary>
        /// The field was not found in this enum
        /// </summary>
        Error
    }
}
