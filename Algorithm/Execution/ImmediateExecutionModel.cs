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

using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Algorithm.Framework.Portfolio;
using System;
using System.Linq;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Provides an implementation of <see cref="IExecutionModel"/> that immediately submits
    /// market orders to achieve the desired portfolio targets
    /// </summary>
    public class ImmediateExecutionModel : ExecutionModel
    {
        private readonly PortfolioTargetCollection _targetsCollection = new PortfolioTargetCollection();

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmediateExecutionModel"/> class.
        /// </summary>
        /// <param name="asynchronous">If true, orders will be submitted asynchronously</param>
        public ImmediateExecutionModel(bool asynchronous = true)
            : base(asynchronous)
        {
        }

        /// <summary>
        /// Immediately submits orders for the specified portfolio targets.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The portfolio targets to be ordered</param>
        public override void Execute(QCAlgorithm algorithm, IPortfolioTarget[] targets)
        {
            _targetsCollection.AddRange(targets);
            // for performance we if empty, OrderByMarginImpact and ClearFulfilled are expensive to call
            if (!_targetsCollection.IsEmpty)
            {
                var isCashAccount = algorithm.BrokerageModel.AccountType == AccountType.Cash;
                var hasOpenPositionReducingOrder = isCashAccount && HasOpenPositionReducingOrder(algorithm);

                foreach (var target in _targetsCollection.OrderByMarginImpact(algorithm))
                {
                    var security = algorithm.Securities[target.Symbol];

                    // calculate remaining quantity to be ordered
                    var quantity = OrderSizing.GetUnorderedQuantity(algorithm, target, security, true);
                    var isPositionReducingOrder = IsPositionReducingOrder(security.Holdings.Quantity, quantity);

                    if (quantity != 0)
                    {
                        // In cash accounts, submit position-reducing orders first and wait for them to fill before
                        // submitting cash-consuming orders. This avoids transient insufficient buying power rejects.
                        if (hasOpenPositionReducingOrder && !isPositionReducingOrder)
                        {
                            continue;
                        }

                        if (security.BuyingPowerModel.AboveMinimumOrderMarginPortfolioPercentage(security, quantity,
                            algorithm.Portfolio, algorithm.Settings.MinimumOrderMarginPortfolioPercentage))
                        {
                            algorithm.MarketOrder(security, quantity, Asynchronous, target.Tag);
                            hasOpenPositionReducingOrder = hasOpenPositionReducingOrder || isPositionReducingOrder;
                        }
                        else if (!PortfolioTarget.MinimumOrderMarginPercentageWarningSent.HasValue)
                        {
                            // will trigger the warning if it has not already been sent
                            PortfolioTarget.MinimumOrderMarginPercentageWarningSent = false;
                        }
                    }
                }

                _targetsCollection.ClearFulfilled(algorithm);
            }
        }

        private static bool HasOpenPositionReducingOrder(QCAlgorithm algorithm)
        {
            return algorithm.Transactions.GetOpenOrders().Any(order =>
            {
                if (!algorithm.Securities.TryGetValue(order.Symbol, out var security))
                {
                    return false;
                }

                return IsPositionReducingOrder(security.Holdings.Quantity, order.Quantity);
            });
        }

        private static bool IsPositionReducingOrder(decimal holdingsQuantity, decimal orderQuantity)
        {
            return holdingsQuantity != 0m
                   && orderQuantity != 0m
                   && Math.Sign(holdingsQuantity) != Math.Sign(orderQuantity);
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
        }
    }
}
