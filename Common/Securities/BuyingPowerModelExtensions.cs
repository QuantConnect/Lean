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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides extension methods as backwards compatibility shims
    /// </summary>
    public static class BuyingPowerModelExtensions
    {
        /// <summary>
        /// Gets the amount of buying power reserved to maintain the specified position
        /// </summary>
        /// <param name="model">The <see cref="IBuyingPowerModel"/></param>
        /// <param name="security">The security</param>
        /// <returns>The reserved buying power in account currency</returns>
        public static decimal GetReservedBuyingPowerForPosition(this IBuyingPowerModel model, Security security)
        {
            var context = new ReservedBuyingPowerForPositionParameters(security);
            var reservedBuyingPower = model.GetReservedBuyingPowerForPosition(context);
            return reservedBuyingPower.Value;
        }

        /// <summary>
        /// Gets the buying power available for a trade
        /// </summary>
        /// <param name="model">The <see cref="IBuyingPowerModel"/></param>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security to be traded</param>
        /// <param name="direction">The direction of the trade</param>
        /// <returns>The buying power available for the trade</returns>
        public static decimal GetBuyingPower(
            this IBuyingPowerModel model,
            SecurityPortfolioManager portfolio,
            Security security,
            OrderDirection direction
            )
        {
            var context = new BuyingPowerParameters(portfolio, security, direction);
            var buyingPower = model.GetBuyingPower(context);

            // existing implementations assume certain non-account currency units, so return raw value
            return buyingPower.Value;
        }

        /// <summary>
        /// Check if there is sufficient buying power to execute this order.
        /// </summary>
        /// <param name="model">The <see cref="IBuyingPowerModel"/></param>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security to be traded</param>
        /// <param name="order">The order</param>
        /// <returns>Returns buying power information for an order</returns>
        public static HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
            this IBuyingPowerModel model,
            SecurityPortfolioManager portfolio,
            Security security,
            Order order
            )
        {
            var parameters = new HasSufficientBuyingPowerForOrderParameters(portfolio, security, order);

            return model.HasSufficientBuyingPowerForOrder(parameters);
        }

        /// <summary>
        /// Get the maximum market order quantity to obtain a position with a given value in account currency
        /// </summary>
        /// <param name="model">The <see cref="IBuyingPowerModel"/></param>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security to be traded</param>
        /// <param name="target">The target percent holdings</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        public static GetMaximumOrderQuantityForTargetValueResult GetMaximumOrderQuantityForTargetValue(
            this IBuyingPowerModel model,
            SecurityPortfolioManager portfolio,
            Security security,
            decimal target
            )
        {
            var parameters = new GetMaximumOrderQuantityForTargetValueParameters(portfolio, security, target);

            return model.GetMaximumOrderQuantityForTargetValue(parameters);
        }
    }
}