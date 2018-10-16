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
            var context = new ReservedBuyingPowerForPositionContext(security);
            var reservedBuyingPower = model.GetReservedBuyingPowerForPosition(context);
            return reservedBuyingPower.Value;
        }
    }
}