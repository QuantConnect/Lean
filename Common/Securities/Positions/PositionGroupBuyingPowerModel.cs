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
    /// Provides a base class for implementations of <see cref="IPositionGroupBuyingPowerModel"/>
    /// </summary>
    public abstract class PositionGroupBuyingPowerModel : IPositionGroupBuyingPowerModel
    {
        /// <summary>
        /// Gets the margin currently allocated to the specified holding
        /// </summary>
        /// <param name="parameters">An object containing the security</param>
        /// <returns>The maintenance margin required for the </returns>
        public abstract MaintenanceMargin GetMaintenanceMargin(PositionGroupMaintenanceMarginParameters parameters);

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        /// <param name="parameters">An object containing the security and quantity</param>
        public abstract InitialMargin GetInitialMarginRequirement(PositionGroupInitialMarginParameters parameters);

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the order</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        public abstract InitialMargin GetInitialMarginRequiredForOrder(PositionGroupInitialMarginForOrderParameters parameters);
    }
}
