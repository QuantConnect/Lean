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

using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Computes the maintenance margin a portfolio would require for a match of option positions, grouping each matched
    /// strategy as <see cref="OptionStrategyPositionGroupResolver"/> does and pricing the unmatched positions on their own
    /// </summary>
    internal class OptionStrategyMatchMarginFunction
    {
        private readonly SecurityPortfolioManager _portfolio;

        /// <summary>
        /// Creates a new instance pricing matches against the specified portfolio
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        public OptionStrategyMatchMarginFunction(SecurityPortfolioManager portfolio)
        {
            _portfolio = portfolio;
        }

        /// <summary>
        /// Computes the maintenance margin of the specified match of the positions
        /// </summary>
        /// <param name="positions">The positions that were matched</param>
        /// <param name="match">The strategies matched from the positions</param>
        /// <param name="unmatched">The positions left unmatched</param>
        /// <param name="margin">The maintenance margin of the matched strategies and of the unmatched positions</param>
        /// <returns>False when a security lacks a price, so the margin can't be computed</returns>
        public bool TryGetMaintenanceMargin(OptionPositionCollection positions, OptionStrategyMatch match,
            OptionPositionCollection unmatched, out decimal margin)
        {
            margin = 0;
            Symbol option = null;
            foreach (var position in positions)
            {
                // the option margin model requires no margin for an option or underlying without a price,
                // which would make a naked short look free
                if (!HasPrice(position.Symbol) || position.Symbol.HasUnderlying && !HasPrice(position.Symbol.Underlying))
                {
                    return false;
                }

                if (!position.IsUnderlying)
                {
                    option = position.Symbol;
                }
            }

            if (option == null)
            {
                return false;
            }

            var contractMultiplier = OptionStrategyPositionGroupResolver.GetContractMultiplier(_portfolio.Securities, option);
            foreach (var strategy in match.Strategies)
            {
                var group = OptionStrategyPositionGroupResolver.CreatePositionGroup(strategy, contractMultiplier);
                margin += group.BuyingPowerModel.GetMaintenanceMargin(new PositionGroupMaintenanceMarginParameters(_portfolio, group));
            }

            foreach (var position in unmatched)
            {
                var security = _portfolio.Securities[position.Symbol];
                var quantity = position.IsUnderlying ? position.Quantity * contractMultiplier : position.Quantity;
                margin += security.BuyingPowerModel.GetMaintenanceMargin(MaintenanceMarginParameters.ForQuantityAtCurrentPrice(security, quantity));
            }

            return true;
        }

        private bool HasPrice(Symbol symbol)
        {
            return _portfolio.Securities.TryGetValue(symbol, out var security) && security.Price != 0;
        }
    }
}
