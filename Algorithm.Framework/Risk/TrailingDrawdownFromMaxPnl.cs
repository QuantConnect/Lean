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
    /// Provides an implementation of <see cref="IRiskManagementModel"/> that limits the drawdown
    /// per holding to the specified percentage
    /// </summary>
    public class TrailingDrawdownFromMaxPnl : RiskManagementModel
    {
        private readonly decimal _maximumDrawdownPercent;
        private readonly Dictionary<Symbol, decimal> _trailing = new Dictionary<Symbol, decimal>();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TrailingDrawdownFromMaxPnl"/> class
        /// </summary>
        /// <param name="maximumDrawdownPercent">The maximum affordable drawdown percentage per security</param>
        public TrailingDrawdownFromMaxPnl(decimal maximumDrawdownPercent)
        {
            _maximumDrawdownPercent = Math.Abs(maximumDrawdownPercent);
        }

        /// <summary>
        /// Manages the algorithm's risk at each time step
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The current portfolio targets to be assessed for risk</param>
        public override IEnumerable<IPortfolioTarget> ManageRisk(QCAlgorithm algorithm, IPortfolioTarget[] targets)
        {
            foreach (var kvp in algorithm.Securities)
            {
                var symbol = kvp.Key;
                var security = kvp.Value;

                // Remove if no open position for the instrument
                if (!security.Invested)
                {
                    if (_trailing.ContainsKey(symbol))
                    {
                        _trailing.Remove(symbol);
                    }
                    continue;
                }

                var pnl = security.Holdings.UnrealizedProfitPercent * 100;

                // Add newly invested securities
                if (!_trailing.ContainsKey(symbol))
                {
                    _trailing.Add(symbol, pnl);
                    continue;
                }

                // Check for new max
                if (pnl > _trailing[symbol])
                {
                    _trailing[symbol] = pnl;
                    continue;
                }

                // If PNL deviates from local max for more than affordable percentage -> create 0 target
                if (pnl < _trailing[symbol] - _maximumDrawdownPercent)
                {
                    yield return new PortfolioTarget(security.Symbol, 0);
                }
            }
        }
    }
}
