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

using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Provides helper methods for setup handlers
    /// </summary>
    public static class SetupHandler
    {
        /// <summary>
        /// Sets the transaction and settlement models in the algorithm based on the selected brokerage properties.
        /// If the <see cref="DefaultBrokerageModel"/> is specified, then no update is performed.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="model">The brokerage model</param>
        public static void UpdateModels(this IAlgorithm algorithm, IBrokerageModel model)
        {
            if (model.GetType() == typeof (DefaultBrokerageModel))
            {
                // if we're using the default don't do anything
                return;
            }

            foreach (var security in algorithm.Securities.Values)
            {
                algorithm.UpdateModel(model, security);
            }
        }

        /// <summary>
        /// Updates the models for the specified security. If the <see cref="DefaultBrokerageModel"/> is specified,
        /// then no update is performed.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="model">The brokerage model</param>
        /// <param name="security">The security to be updated</param>
        public static void UpdateModel(this IAlgorithm algorithm, IBrokerageModel model, Security security)
        {
            if (model.GetType() == typeof(DefaultBrokerageModel))
            {
                // if we're using the default don't do anything
                return;
            }

            security.TransactionModel = model.GetTransactionModel(security);
            security.SettlementModel = model.GetSettlementModel(security, algorithm.AccountType);
        }
    }
}