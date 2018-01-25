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
    /// Represents a security's model of buying power
    /// </summary>
    public interface IBuyingPowerModel
    {
        /// <summary>
        /// Gets the current leverage of the security
        /// </summary>
        /// <param name="security">The security to get leverage for</param>
        /// <returns>The current leverage in the security</returns>
        decimal GetLeverage(Security security);

        /// <summary>
        /// Sets the leverage for the applicable securities, i.e, equities
        /// </summary>
        /// <remarks>
        /// This is added to maintain backwards compatibility with the old margin/leverage system
        /// </remarks>
        /// <param name="security">The security to set leverage for</param>
        /// <param name="leverage">The new leverage</param>
        void SetLeverage(Security security, decimal leverage);

        /// <summary>
        /// Gets the margin cash available for a trade
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security to be traded</param>
        /// <param name="direction">The direction of the trade</param>
        /// <returns>The margin available for the trade</returns>
        decimal GetMarginRemaining(SecurityPortfolioManager portfolio, Security security, OrderDirection direction);

        /// <summary>
        /// The percentage of the holding's absolute cost that must be held in free cash in order to avoid a margin call
        /// </summary>
        decimal GetMaintenanceMarginRequirement(Security security);

        /// <summary>
        /// Check if there is sufficient buying power to execute this order.
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security to be traded</param>
        /// <param name="order">The order to be checked</param>
        /// <returns>Returns true if there is sufficient buying power to execute the order, false otherwise</returns>
        bool HasSufficientBuyingPowerForOrder(SecurityPortfolioManager portfolio, Security security, Order order);

        /// <summary>
        /// Get the maximum market order quantity to obtain a position with a given value in account currency
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security to be traded</param>
        /// <param name="targetPortfolioValue">The value in account currency that we want our holding to have</param>
        /// <returns>Returns the maximum allowed market order quantity</returns>
        decimal GetMaximumOrderQuantityForTargetValue(SecurityPortfolioManager portfolio, Security security, decimal targetPortfolioValue);

        /// <summary>
        /// Gets the amount of buying power reserved to maintain the specified position
        /// </summary>
        /// <param name="security">The security for the position</param>
        /// <returns>The reserved buying power in account currency</returns>
        decimal GetReservedBuyingPowerForPosition(Security security);
    }
}
