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

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Represents a position group's model of buying power
    /// </summary>
    public interface IPositionGroupBuyingPowerModel : IEquatable<IPositionGroupBuyingPowerModel>
    {
        /// <summary>
        /// Gets the margin currently allocated to the specified holding
        /// </summary>
        /// <param name="parameters">An object containing the security</param>
        /// <returns>The maintenance margin required for the </returns>
        MaintenanceMargin GetMaintenanceMargin(PositionGroupMaintenanceMarginParameters parameters);

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        /// <param name="parameters">An object containing the security and quantity</param>
        InitialMargin GetInitialMarginRequirement(PositionGroupInitialMarginParameters parameters);

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the order</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        InitialMargin GetInitialMarginRequiredForOrder(
            PositionGroupInitialMarginForOrderParameters parameters
        );

        /// <summary>
        /// Computes the impact on the portfolio's buying power from adding the position group to the portfolio. This is
        /// a 'what if' analysis to determine what the state of the portfolio would be if these changes were applied. The
        /// delta (before - after) is the margin requirement for adding the positions and if the margin used after the changes
        /// are applied is less than the total portfolio value, this indicates sufficient capital.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio and a position group containing the contemplated
        /// changes to the portfolio</param>
        /// <returns>Returns the portfolio's total portfolio value and margin used before and after the position changes are applied</returns>
        ReservedBuyingPowerImpact GetReservedBuyingPowerImpact(
            ReservedBuyingPowerImpactParameters parameters
        );

        /// <summary>
        /// Check if there is sufficient buying power for the position group to execute this order.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the order</param>
        /// <returns>Returns buying power information for an order against a position group</returns>
        HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
            HasSufficientPositionGroupBuyingPowerForOrderParameters parameters
        );

        /// <summary>
        /// Computes the amount of buying power reserved by the provided position group
        /// </summary>
        ReservedBuyingPowerForPositionGroup GetReservedBuyingPowerForPositionGroup(
            ReservedBuyingPowerForPositionGroupParameters parameters
        );

        /// <summary>
        /// Get the maximum position group order quantity to obtain a position with a given buying power
        /// percentage. Will not take into account free buying power.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the target
        ///     signed buying power percentage</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        GetMaximumLotsResult GetMaximumLotsForTargetBuyingPower(
            GetMaximumLotsForTargetBuyingPowerParameters parameters
        );

        /// <summary>
        /// Get the maximum market position group order quantity to obtain a delta in the buying power used by a position group.
        /// The deltas sign defines the position side to apply it to, positive long, negative short.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the delta buying power</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        /// <remarks>Used by the margin call model to reduce the position by a delta percent.</remarks>
        GetMaximumLotsResult GetMaximumLotsForDeltaBuyingPower(
            GetMaximumLotsForDeltaBuyingPowerParameters parameters
        );

        /// <summary>
        /// Gets the buying power available for a position group trade
        /// </summary>
        /// <param name="parameters">A parameters object containing the algorithm's portfolio, security, and order direction</param>
        /// <returns>The buying power available for the trade</returns>
        PositionGroupBuyingPower GetPositionGroupBuyingPower(
            PositionGroupBuyingPowerParameters parameters
        );
    }
}
