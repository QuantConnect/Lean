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
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Execution model that submits orders while the current spread is in desirably tight extent.
    /// </summary>
    /// <remarks>Note this execution model will not work using <see cref="Resolution.Daily"/>
    /// since Exchange.ExchangeOpen will be false, suggested resolution is <see cref="Resolution.Minute"/></remarks>
    public class SpreadExecutionModel : ExecutionModel
    {
        private readonly decimal _acceptingSpreadPercent;
        private readonly PortfolioTargetCollection _targetsCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpreadExecutionModel"/> class
        /// </summary>
        /// <param name="acceptingSpreadPercent">Maximum spread accepted comparing to current price in percentage.</param>
        /// <param name="asynchronous">If true, orders will be submitted asynchronously</param>
        public SpreadExecutionModel(decimal acceptingSpreadPercent = 0.005m, bool asynchronous = true)
            : base(asynchronous)
        {
            _acceptingSpreadPercent = Math.Abs(acceptingSpreadPercent);
            _targetsCollection = new PortfolioTargetCollection();
        }

        /// <summary>
        /// Submit orders for the specified portfolio targets if the spread is tighter/equal to preset level
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The portfolio targets to be ordered</param>
        public override void Execute(QCAlgorithm algorithm, IPortfolioTarget[] targets)
        {
            // update the complete set of portfolio targets with the new targets
            _targetsCollection.AddRange(targets);

            // for performance we check count value, OrderByMarginImpact and ClearFulfilled are expensive to call
            if (!_targetsCollection.IsEmpty)
            {
                foreach (var target in _targetsCollection.OrderByMarginImpact(algorithm))
                {
                    var symbol = target.Symbol;
                    // calculate remaining quantity to be ordered
                    var unorderedQuantity = OrderSizing.GetUnorderedQuantity(algorithm, target);

                    if (unorderedQuantity != 0)
                    {
                        // get security object
                        var security = algorithm.Securities[symbol];

                        // check order entry conditions
                        if (PriceIsFavorable(security))
                        {
                            algorithm.MarketOrder(symbol, unorderedQuantity, Asynchronous, target.Tag);
                        }
                    }
                }

                _targetsCollection.ClearFulfilled(algorithm);
            }
        }

        /// <summary>
        /// Determines if the current spread is equal or tighter than preset level
        /// </summary>
        protected virtual bool PriceIsFavorable(Security security)
        {
            // Check if this method was overridden in Python
            if (TryInvokePythonOverride(nameof(PriceIsFavorable), out bool result, security))
            {
                return result;
            }

            // Has to be in opening hours of exchange to avoid extreme spread in OTC period
            // Price has to be larger than zero to avoid zero division error, or negative price causing the spread percentage lower than preset value by accident
            return security.Exchange.ExchangeOpen
                && security.Price > 0 && security.AskPrice > 0 && security.BidPrice > 0
                && (security.AskPrice - security.BidPrice) / security.Price <= _acceptingSpreadPercent;
        }
    }
}
