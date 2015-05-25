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

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Provides helper methods for setup handlers
    /// </summary>
    public static class SetupHandler
    {
        /// <summary>
        /// Sets the transaction models in the algorithm based on the selected brokerage properties
        /// </summary>
        public static void UpdateTransactionModels(IAlgorithm algorithm, IBrokerageModel model)
        {
            if (model.GetType() == typeof (DefaultBrokerageModel))
            {
                // if we're using the default don't do anything
                return;
            }

            foreach (var security in algorithm.Securities.Values)
            {
                security.TransactionModel = model.GetTransactionModel(security);
            }
        }
    }
}