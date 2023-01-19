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
using QuantConnect.Interfaces;
using QuantConnect.Brokerages;
using System.Collections.Generic;

namespace QuantConnect
{
    /// <summary>
    /// This class includes algorithm configuration settings and parameters.
    /// This is used to include configuration parameters in the result packet to be used for report generation.
    /// </summary>
    public class AlgorithmConfiguration
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
        /// The parameters used by the algorithm
        /// </summary>
        [JsonProperty(PropertyName = "Parameters")]
        public IReadOnlyDictionary<string, string> Parameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmConfiguration"/> class
        /// </summary>
        public AlgorithmConfiguration(string accountCurrency, BrokerageName brokerageName, AccountType accountType,
            IReadOnlyDictionary<string, string> parameters)
        {
            AccountCurrency = accountCurrency;
            BrokerageName = brokerageName;
            AccountType = accountType;
            Parameters = parameters;
        }

        /// <summary>
        /// Initializes a new empty instance of the <see cref="AlgorithmConfiguration"/> class
        /// </summary>
        public AlgorithmConfiguration()
        {
        }

        /// <summary>
        /// Provides a convenience method for creating a <see cref="AlgorithmConfiguration"/> for a given algorithm.
        /// </summary>
        /// <param name="algorithm">Algorithm for which the configuration object is being created</param>
        /// <returns>A new AlgorithmConfiguration object for the specified algorithm</returns>
        public static AlgorithmConfiguration Create(IAlgorithm algorithm)
        {
            return new AlgorithmConfiguration(
                algorithm.AccountCurrency,
                BrokerageModel.GetBrokerageName(algorithm.BrokerageModel),
                algorithm.BrokerageModel.AccountType,
                algorithm.GetParameters());
        }
    }
}
