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
                var result = security.BuyingPowerModel.GetMaintenanceMargin(parameters.IsCurrentHoldings
                    ? MaintenanceMarginParameters.ForCurrentHoldings(security, position.Quantity)
                    : MaintenanceMarginParameters.ForQuantityAtCurrentPrice(security, position.Quantity)
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
    }
}
