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
 *
*/

using QuantConnect.Brokerages;
using QuantConnect.Interfaces;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides an implementation of <see cref="ISecurityInitializer"/> that initializes a security
    /// by settings the <see cref="Security.FillModel"/>, <see cref="Security.FeeModel"/>, 
    /// <see cref="Security.SlippageModel"/>, and the <see cref="Security.SettlementModel"/> properties
    /// </summary>
    public class BrokerageModelSecurityInitializer : ISecurityInitializer
    {
        private readonly IAlgorithm _algorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerageModelSecurityInitializer"/> class
        /// for the specified algorithm
        /// </summary>
        /// <param name="algorithm">The algorithm instance used to retrieve the current <see cref="IBrokerageModel"/></param>
        public BrokerageModelSecurityInitializer(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        /// <summary>
        /// Initializes the specified security by setting up the models
        /// </summary>
        /// <param name="security">The security to be initialized</param>
        public void Initialize(Security security)
        {
            // set our models if the user hasn't already set them manually
            if (!security.IsFillModelSet)
            {
                security.FillModel = _algorithm.BrokerageModel.GetFillModel(security);
            }
            if (!security.IsFeeModelSet)
            {
                security.FeeModel = _algorithm.BrokerageModel.GetFeeModel(security);
            }
            if (!security.IsSlippageModelSet)
            {
                security.SlippageModel = _algorithm.BrokerageModel.GetSlippageModel(security);
            }
            if (!security.IsSettlementModelSet)
            {
                security.SettlementModel = _algorithm.BrokerageModel.GetSettlementModel(security, _algorithm.AccountType);
            }
        }
    }
}
