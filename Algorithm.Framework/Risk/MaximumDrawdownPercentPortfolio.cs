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

using System;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Algorithm.Framework.Risk
{
    /// <summary>
    /// Provides an implementation of <see cref="IRiskManagementModel"/> that limits the drawdown of the portfolio
    /// to the specified percentage. Once this is triggered the algorithm will need to be manually restarted.
    /// </summary>
    public class MaximumDrawdownPercentPortfolio : RiskManagementModel
    {
        private readonly decimal _maximumDrawdownPercent;
        private decimal _portfolioHigh;
        private bool _initialised = false;
        private bool _isTrailing;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaximumDrawdownPercentPortfolio"/> class
        /// </summary>
        /// <param name="maximumDrawdownPercent">The maximum percentage drawdown allowed for algorithm portfolio
        /// compared with starting value, defaults to 5% drawdown</param>
        /// <param name="isTrailing">If "false", the drawdown will be relative to the starting value of the portfolio.
        /// If "true", the drawdown will be relative the last maximum portfolio value</param>
        public MaximumDrawdownPercentPortfolio(decimal maximumDrawdownPercent = 0.05m, bool isTrailing = false)
        {
            _maximumDrawdownPercent = -Math.Abs(maximumDrawdownPercent);
            _isTrailing = isTrailing;
        }

        /// <summary>
        /// Manages the algorithm's risk at each time step
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The current portfolio targets to be assessed for risk</param>
        public override IEnumerable<IPortfolioTarget> ManageRisk(QCAlgorithm algorithm, IPortfolioTarget[] targets)
        {
            var currentValue = algorithm.Portfolio.TotalPortfolioValue;

            if (!_initialised)
            {
                _portfolioHigh = currentValue; // Set initial portfolio value
                _initialised = true;
            }

            // Update trailing high value if in trailing mode
            if (_isTrailing && (_portfolioHigh < currentValue))
            {
                _portfolioHigh = currentValue;
                yield break; // return if new high reached
            }

            var pnl = GetTotalDrawdownPercent(currentValue);
            if (pnl < _maximumDrawdownPercent)
            {
                foreach(var target in targets)
                    yield return new PortfolioTarget(target.Symbol, 0);
            }
        }

        private decimal GetTotalDrawdownPercent(decimal currentValue)
        {
            return (currentValue / _portfolioHigh) - 1.0m;
        }
    }
}