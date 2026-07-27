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
using System.Linq;

namespace QuantConnect.Securities.Option.StrategyMatcher
{
    /// <summary>
    /// Provides an implementation of <see cref="IOptionStrategyMatchObjectiveFunction"/> that minimizes the total
    /// quantity of short option contracts left uncovered, either within their matched strategy (such as the second
    /// short leg of a ladder) or unmatched entirely. Uncovered shorts are charged naked option margin, typically an
    /// order of magnitude larger than the margin of covered, risk-defined strategies, which makes this quantity a
    /// cheap and deterministic proxy for the total margin required to hold the positions.
    /// </summary>
    public class UncoveredShortQuantityOptionStrategyMatchObjectiveFunction : IOptionStrategyMatchObjectiveFunction
    {
        /// <summary>
        /// Computes the score as the negated total quantity of uncovered short option contracts, so the solution
        /// covering the most short contracts wins and a solution without uncovered shorts scores zero, the maximum.
        /// A short leg is covered when its strategy holds long options of the same right, or the underlying lots
        /// with the offsetting sign, quantity for quantity.
        /// </summary>
        public decimal ComputeScore(OptionPositionCollection input, OptionStrategyMatch match, OptionPositionCollection unmatched)
        {
            var uncovered = 0m;
            foreach (var strategy in match.Strategies)
            {
                var netCalls = 0m;
                var netPuts = 0m;
                foreach (var leg in strategy.OptionLegs)
                {
                    if (leg.Right == OptionRight.Call)
                    {
                        netCalls += leg.Quantity;
                    }
                    else
                    {
                        netPuts += leg.Quantity;
                    }
                }

                // at the matching level underlying legs are expressed in lots,
                // long lots cover short calls and short lots cover short puts
                var underlyingLots = strategy.UnderlyingLegs.Sum(leg => leg.Quantity);
                uncovered += Math.Max(0, -netCalls - Math.Max(0, underlyingLots));
                uncovered += Math.Max(0, -netPuts - Math.Max(0, -underlyingLots));
            }

            foreach (var position in unmatched)
            {
                if (position.Quantity < 0 && position.Symbol.SecurityType.IsOption())
                {
                    // unmatched short options fall through to stand-alone groups charged naked option margin
                    uncovered -= position.Quantity;
                }
            }

            return -uncovered;
        }
    }
}
