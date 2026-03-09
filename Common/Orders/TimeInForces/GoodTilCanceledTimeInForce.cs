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

using QuantConnect.Securities;

namespace QuantConnect.Orders.TimeInForces
{
    /// <summary>
    /// Good Til Canceled Time In Force - order does never expires
    /// </summary>
    public class GoodTilCanceledTimeInForce : TimeInForce
    {
        /// <summary>
        /// Checks if an order is expired
        /// </summary>
        /// <param name="security">The security matching the order</param>
        /// <param name="order">The order to be checked</param>
        /// <returns>Returns true if the order has expired, false otherwise</returns>
        public override bool IsOrderExpired(Security security, Order order)
        {
            return false;
        }

        /// <summary>
        /// Checks if an order fill is valid
        /// </summary>
        /// <param name="security">The security matching the order</param>
        /// <param name="order">The order to be checked</param>
        /// <param name="fill">The order fill to be checked</param>
        /// <returns>Returns true if the order fill can be emitted, false otherwise</returns>
        public override bool IsFillValid(Security security, Order order, OrderEvent fill)
        {
            return true;
        }
    }
}
