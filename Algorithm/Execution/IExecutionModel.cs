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

using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Algorithm framework model that executes portfolio targets
    /// </summary>
    public interface IExecutionModel : INotifiedSecurityChanges
    {
        /// <summary>
        /// Submit orders for the specified portfolio targets.
        /// This model is free to delay or spread out these orders as it sees fit
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The portfolio targets just emitted by the portfolio construction model.
        /// These are always just the new/updated targets and not a complete set of targets</param>
        void Execute(QCAlgorithm algorithm, IPortfolioTarget[] targets);

        /// <summary>
        /// New order event handler
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="orderEvent">Order event to process</param>
        void OnOrderEvent(QCAlgorithm algorithm, OrderEvent orderEvent);
    }
}
