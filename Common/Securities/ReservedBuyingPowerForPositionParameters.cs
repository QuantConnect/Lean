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
    /// Defines the parameters for <see cref="IBuyingPowerModel.GetReservedBuyingPowerForPosition"/>
    /// </summary>
    public class ReservedBuyingPowerForPositionParameters
    {
        /// <summary>
        /// Gets the security
        /// </summary>
        public Security Security { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReservedBuyingPowerForPositionParameters"/> class
        /// </summary>
        /// <param name="security">The security</param>
        public ReservedBuyingPowerForPositionParameters(Security security)
        {
            Security = security;
        }

        /// <summary>
        /// Creates the result using the specified reserved buying power in units of the account currency
        /// </summary>
        /// <param name="reservedBuyingPower">The reserved buying power in units of the account currency</param>
        /// <returns>The reserved buying power</returns>
        public ReservedBuyingPowerForPosition ResultInAccountCurrency(decimal reservedBuyingPower)
        {
            return new ReservedBuyingPowerForPosition(reservedBuyingPower);
        }
    }
}