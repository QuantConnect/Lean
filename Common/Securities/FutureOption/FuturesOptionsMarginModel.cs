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

using QuantConnect.Securities.Future;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Defines a margin model for future options (an option with a future as its underlying).
    /// We re-use the <see cref="FutureMarginModel"/> implementation and multiply its results
    /// by 1.5x to simulate the increased margins seen for future options.
    /// </summary>
    public class FuturesOptionsMarginModel : FutureMarginModel
    {
        private const decimal FixedMarginMultiplier = 1.5m;

        /// <summary>
        /// Creates an instance of FutureOptionMarginModel
        /// </summary>
        /// <param name="requiredFreeBuyingPowerPercent">The percentage used to determine the required unused buying power for the account.</param>
        /// <param name="futureOption">Option Security containing a Future security as the underlying</param>
        public FuturesOptionsMarginModel(decimal requiredFreeBuyingPowerPercent = 0, Option futureOption = null) : base(requiredFreeBuyingPowerPercent, futureOption?.Underlying)
        {
        }

        /// <summary>
        /// Gets the margin currently alloted to the specified holding.
        /// </summary>
        /// <param name="security">The option to compute maintenance margin for</param>
        /// <returns>The maintenance margin required for the option</returns>
        /// <remarks>
        /// We fix the option to 1.5x the maintenance because of its close coupling with the underlying.
        /// The option's contract multiplier is 1x, but might be more sensitive to volatility shocks in the long
        /// run when it comes to calculating the different market scenarios attempting to simulate VaR, resulting
        /// in a margin greater than the underlying's margin.
        /// </remarks>
        protected override decimal GetMaintenanceMargin(Security security)
        {
            return base.GetMaintenanceMargin(((Option)security).Underlying) * FixedMarginMultiplier;
        }

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        /// <param name="security">The option to compute the initial margin for</param>
        /// <returns>The initial margin required for the option (i.e. the equity required to enter a position for this option)</returns>
        /// <remarks>
        /// We fix the option to 1.5x the initial because of its close coupling with the underlying.
        /// The option's contract multiplier is 1x, but might be more sensitive to volatility shocks in the long
        /// run when it comes to calculating the different market scenarios attempting to simulate VaR, resulting
        /// in a margin greater than the underlying's margin.
        /// </remarks>
        protected override decimal GetInitialMarginRequirement(Security security, decimal quantity)
        {
            return base.GetInitialMarginRequirement(((Option)security).Underlying, quantity) * FixedMarginMultiplier;
        }
    }
}
