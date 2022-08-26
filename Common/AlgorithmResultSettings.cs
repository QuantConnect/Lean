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
using QuantConnect.Brokerages;

namespace QuantConnect
{
    /// <summary>
    /// This class includes algorithm settings to be included in the result packet mainly for report generation.
    /// </summary>
    public class AlgorithmResultSettings
    {
        /// <summary>
        /// The algorithm's account currency
        /// </summary>
        [JsonProperty(PropertyName = "AccountCurrency", NullValueHandling = NullValueHandling.Ignore)]
        public string AccountCurrency;

        /// <summary>
        /// The algorithm's brokerage model
        /// </summary>
        /// <remarks> Required to set the correct brokerage model on report generation.</remarks>
        [JsonProperty(PropertyName = "Brokerage")]
        public BrokerageName BrokerageName;

        /// <summary>
        /// The algorithm's account type
        /// </summary>
        /// <remarks> Required to set the correct brokerage model on report generation.</remarks>
        [JsonProperty(PropertyName = "AccountType")]
        public AccountType AccountType;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmResultSettings"/> class
        /// </summary>
        public AlgorithmResultSettings(string accountCurrency, BrokerageName brokerageName, AccountType accountType)
        {
            AccountCurrency = accountCurrency;
            BrokerageName = brokerageName;
            AccountType = accountType;
        }

        /// <summary>
        /// Initializes a new empty instance of the <see cref="AlgorithmResultSettings"/> class
        /// </summary>
        public AlgorithmResultSettings()
        {
        }
    }
}
