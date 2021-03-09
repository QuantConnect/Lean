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
using QuantConnect.Securities.IndexOption;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Defines a margin model for index options.
    /// </summary>
    public class IndexOptionsMarginModel : OptionMarginModel
    {
        /// <summary>
        /// Creates an instance of IndexOptionsMarginModel
        /// </summary>
        /// <param name="requiredFreeBuyingPowerPercent">The percentage used to determine the required unused buying power for the account.</param>
        public IndexOptionsMarginModel(decimal requiredFreeBuyingPowerPercent = 0) : base(requiredFreeBuyingPowerPercent)
        {
        }

        /// <summary>
        /// Gets the margin currently alloted to the specified holding.
        /// </summary>
        /// <param name="security">The option to compute maintenance margin for</param>
        /// <returns>The maintenance margin required for the option</returns>
        protected override decimal GetMaintenanceMargin(Security security)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        /// <param name="security">The option to compute the initial margin for</param>
        /// <returns>The initial margin required for the option (i.e. the equity required to enter a position for this option)</returns>
        protected override decimal GetInitialMarginRequirement(Security security, decimal quantity)
        {
            throw new NotImplementedException();
        }
    }
}
