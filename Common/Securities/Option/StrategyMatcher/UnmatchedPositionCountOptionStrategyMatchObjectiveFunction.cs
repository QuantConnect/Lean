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

using System.Linq;

namespace QuantConnect.Securities.Option.StrategyMatcher
{
    /// <summary>
    /// Provides an implementation of <see cref="IOptionStrategyMatchObjectiveFunction"/> that evaluates the number of unmatched
    /// positions, in number of contracts, giving precedence to solutions that have fewer unmatched contracts.
    /// </summary>
    public class UnmatchedPositionCountOptionStrategyMatchObjectiveFunction : IOptionStrategyMatchObjectiveFunction
    {
        /// <summary>
        /// Computes the delta in matched vs unmatched positions, which gives precedence to solutions that match more contracts.
        /// </summary>
        public decimal ComputeScore(OptionPositionCollection input, OptionStrategyMatch match, OptionPositionCollection unmatched)
        {
            var value = 0m;
            foreach (var strategy in match.Strategies)
            {
                foreach (var leg in strategy.OptionLegs.Concat<OptionStrategy.LegData>(strategy.UnderlyingLegs))
                {
                    value += leg.Quantity;
                }
            }

            return value - unmatched.Count;
        }
    }
}