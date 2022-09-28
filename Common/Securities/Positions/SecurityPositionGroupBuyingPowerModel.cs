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

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides an implementation of <see cref="IPositionGroupBuyingPowerModel"/> for groups containing exactly one security
    /// </summary>
    public class SecurityPositionGroupBuyingPowerModel : PositionGroupBuyingPowerModel
    {
        /// <summary>
        /// Gets the margin currently allocated to the specified holding
        /// </summary>
        /// <param name="parameters">An object containing the security</param>
        /// <returns>The maintenance margin required for the </returns>
        public override MaintenanceMargin GetMaintenanceMargin(PositionGroupMaintenanceMarginParameters parameters)
        {
            // SecurityPositionGroupBuyingPowerModel models buying power the same as non-grouped, so we can simply sum up
            // the reserved buying power via the security's model. We should really only ever get a single position here,
            // but it's not incorrect to ask the model for what the reserved buying power would be using default modeling
            var buyingPower = 0m;
            foreach (var position in parameters.PositionGroup)
            {
                var security = parameters.Portfolio.Securities[position.Symbol];
                var result = security.BuyingPowerModel.GetMaintenanceMargin(
                    MaintenanceMarginParameters.ForQuantityAtCurrentPrice(security, position.Quantity)
                );

                buyingPower += result;
            }

            return buyingPower;
        }

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        /// <param name="parameters">An object containing the security and quantity</param>
        public override InitialMargin GetInitialMarginRequirement(PositionGroupInitialMarginParameters parameters)
        {
            var initialMarginRequirement = 0m;
            foreach (var position in parameters.PositionGroup)
            {
                var security = parameters.Portfolio.Securities[position.Symbol];
                initialMarginRequirement += security.BuyingPowerModel.GetInitialMarginRequirement(
                    security, position.Quantity
                );
            }

            return initialMarginRequirement;
        }

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the order</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        public override InitialMargin GetInitialMarginRequiredForOrder(
            PositionGroupInitialMarginForOrderParameters parameters
            )
        {
            var initialMarginRequirement = 0m;
            foreach (var position in parameters.PositionGroup)
            {
                // TODO : Support combo order by pull symbol-specific order
                var security = parameters.Portfolio.Securities[position.Symbol];
                initialMarginRequirement += security.BuyingPowerModel.GetInitialMarginRequiredForOrder(
                    new InitialMarginRequiredForOrderParameters(parameters.Portfolio.CashBook, security, parameters.Order)
                );
            }

            return initialMarginRequirement;
        }

        /// <summary>
        /// Get the maximum position group order quantity to obtain a position with a given buying power
        /// percentage. Will not take into account free buying power.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the target
        ///     signed buying power percentage</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        public override GetMaximumLotsResult GetMaximumLotsForTargetBuyingPower(
            GetMaximumLotsForTargetBuyingPowerParameters parameters
            )
        {
            if (parameters.PositionGroup.Count != 1)
            {
                return parameters.Error(
                    $"{nameof(SecurityPositionGroupBuyingPowerModel)} only supports position groups containing exactly one position."
                );
            }

            var position = parameters.PositionGroup.Single();
            var security = parameters.Portfolio.Securities[position.Symbol];
            var result = security.BuyingPowerModel.GetMaximumOrderQuantityForTargetBuyingPower(
                parameters.Portfolio, security, parameters.TargetBuyingPower, parameters.MinimumOrderMarginPortfolioPercentage
            );

            var quantity = result.Quantity / security.SymbolProperties.LotSize;
            return new GetMaximumLotsResult(quantity, result.Reason, result.IsError);
        }

        /// <summary>
        /// Get the maximum market position group order quantity to obtain a delta in the buying power used by a position group.
        /// The deltas sign defines the position side to apply it to, positive long, negative short.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the delta buying power</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        /// <remarks>Used by the margin call model to reduce the position by a delta percent.</remarks>
        public override GetMaximumLotsResult GetMaximumLotsForDeltaBuyingPower(
            GetMaximumLotsForDeltaBuyingPowerParameters parameters
            )
        {
            if (parameters.PositionGroup.Count != 1)
            {
                return parameters.Error(
                    $"{nameof(SecurityPositionGroupBuyingPowerModel)} only supports position groups containing exactly one position."
                );
            }

            var position = parameters.PositionGroup.Single();
            var security = parameters.Portfolio.Securities[position.Symbol];
            var result = security.BuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower(
                new GetMaximumOrderQuantityForDeltaBuyingPowerParameters(
                    parameters.Portfolio, security, parameters.DeltaBuyingPower, parameters.MinimumOrderMarginPortfolioPercentage
                )
            );

            var quantity = result.Quantity * security.SymbolProperties.LotSize;
            return new GetMaximumLotsResult(quantity, result.Reason, result.IsError);
        }

        /// <summary>
        /// Check if there is sufficient buying power for the position group to execute this order.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the order</param>
        /// <returns>Returns buying power information for an order against a position group</returns>
        public override HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
            HasSufficientPositionGroupBuyingPowerForOrderParameters parameters
            )
        {
            if (parameters.PositionGroup.Count != 1)
            {
                return parameters.Error(
                    $"{nameof(SecurityPositionGroupBuyingPowerModel)} only supports position groups containing exactly one position."
                );
            }

            var position = parameters.PositionGroup.Single();
            var security = parameters.Portfolio.Securities[position.Symbol];
            return security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(
                parameters.Portfolio, security, parameters.Orders.Single()
            );
        }

        /// <summary>
        /// Additionally check initial margin requirements if the algorithm only has default position groups
        /// </summary>
        protected override HasSufficientBuyingPowerForOrderResult PassesPositionGroupSpecificBuyingPowerForOrderChecks(
            HasSufficientPositionGroupBuyingPowerForOrderParameters parameters,
            decimal availableBuyingPower
            )
        {
            // only check initial margin requirements when the algorithm is only using default position groups
            if (!parameters.Portfolio.Positions.IsOnlyDefaultGroups)
            {
                return null;
            }

            var symbol = parameters.PositionGroup.Single().Symbol;
            var security = parameters.Portfolio.Securities[symbol];
            return security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(
                parameters.Portfolio, security, parameters.Orders.Single()
            );
        }
    }
}
