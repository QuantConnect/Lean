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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Execution model that submits orders while the current spread is in desirably tight extent.
    /// </summary>
    public class SpreadExecutionModel : ExecutionModel
    {
    	private readonly decimal _acceptingSpreadPercent;
        private readonly PortfolioTargetCollection _targetsCollection;

		/// <summary>
        /// Initializes a new instance of the <see cref="SpreadExecutionModel"/> class
        /// </summary>
        /// <param name="acceptingSpreadPercent">Maximum spread accepted comparing to current price in percentage.</param>
        public SpreadExecutionModel(
            decimal acceptingSpreadPercent = 0.005m
            )
        {
            _acceptingSpreadPercent = acceptingSpreadPercent;
            _targetsCollection = new PortfolioTargetCollection();
        }

        /// <summary>
        /// Submit orders for the specified portolio targets if the spread is tighter/equal to preset level
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The portfolio targets to be ordered</param>
        public override void Execute(QCAlgorithm algorithm, IPortfolioTarget[] targets)
        {
            // update the complete set of portfolio targets with the new targets
            _targetsCollection.AddRange(targets);

            // for performance we check count value, OrderByMarginImpact and ClearFulfilled are expensive to call
            if (_targetsCollection.Count > 0)
            {
                foreach (var target in _targetsCollection.OrderByMarginImpact(algorithm))
                {
                    var symbol = target.Symbol;

                    // calculate remaining quantity to be ordered
                    var unorderedQuantity = OrderSizing.GetUnorderedQuantity(algorithm, target);

                    // get security object
                    var security = algorithm.Securities[symbol];

                    // check order entry conditions
                    if (PriceIsFavorable(security) && (unorderedQuantity != 0))
                        {
                            algorithm.MarketOrder(symbol, unorderedQuantity);
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
            // Has to be in opening hours of exchange to avoid extreme spread in OTC period
            // Price has to be larger than zero to avoid zero division error, or negative price causing the spread percentage lower than preset value by accident
            if (security.Exchange.ExchangeOpen && (security.Price > 0) && ((security.AskPrice - security.BidPrice)/security.Price <= _acceptingSpreadPercent))
            {
                return true;
            }
            return false;
        }
    }
}
