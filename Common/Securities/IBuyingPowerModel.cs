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
        /// Check if there is sufficient buying power to execute this order.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the order</param>
        /// <returns>Returns buying power information for an order</returns>
        HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(HasSufficientBuyingPowerForOrderParameters parameters);

        /// <summary>
        /// Get the maximum market order quantity to obtain a position with a given buying power percentage.
        /// Will not take into account free buying power.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the target signed buying power percentage</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        GetMaximumOrderQuantityResult GetMaximumOrderQuantityForTargetBuyingPower(GetMaximumOrderQuantityForTargetBuyingPowerParameters parameters);

        /// <summary>
        /// Get the maximum market order quantity to obtain a delta in the buying power used by a security.
        /// The deltas sign defines the position side to apply it to, positive long, negative short.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the delta buying power</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        /// <remarks>Used by the margin call model to reduce the position by a delta percent.</remarks>
        GetMaximumOrderQuantityResult GetMaximumOrderQuantityForDeltaBuyingPower(GetMaximumOrderQuantityForDeltaBuyingPowerParameters parameters);

        /// <summary>
        /// Gets the amount of buying power reserved to maintain the specified position
        /// </summary>
        /// <param name="parameters">A parameters object containing the security</param>
        /// <returns>The reserved buying power in account currency</returns>
        ReservedBuyingPowerForPosition GetReservedBuyingPowerForPosition(ReservedBuyingPowerForPositionParameters parameters);
    }
}
