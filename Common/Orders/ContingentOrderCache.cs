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

using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Provides a thread-safe service for caching the orders of a set of contingent orders (OCO, OTO, OUO, brackets) until all of them
    /// have arrived, so that a brokerage can submit them together. Orders are placed one by one, see <see cref="GroupOrderCacheManager"/>
    /// </summary>
    public class ContingentOrderCache
    {
        /// <summary>
        /// The pending orders by their order id, the original instances so that the brokerage can set their brokerage ids
        /// </summary>
        private readonly ConcurrentDictionary<int, Order> _pendingOrders = new();

        /// <summary>
        /// Attempts to retrieve all the orders in the set of contingent orders from the cache
        /// </summary>
        /// <param name="order">Target order, which can be any of the orders of the set</param>
        /// <param name="orders">All the orders in the set sorted by id: parents come before the orders they trigger</param>
        /// <returns>
        /// True if all the orders of the set were successfully retrieved from the cache, which are removed from it.
        /// Otherwise false, the target order is cached for future retrieval
        /// </returns>
        /// <remarks>If the target order is not a contingent order, the resulting list will contain that single order alone</remarks>
        public bool TryGetContingentCachedOrders(Order order, out List<Order> orders)
        {
            if (!order.TryGetContingentOrders(TryGetOrder, out orders))
            {
                // some order of the set is missing but cache the new one
                _pendingOrders[order.Id] = order;
                return false;
            }

            for (var i = 0; i < orders.Count; i++)
            {
                _pendingOrders.TryRemove(orders[i].Id, out _);
            }
            return true;
        }

        private Order TryGetOrder(int orderId)
        {
            _pendingOrders.TryGetValue(orderId, out var order);
            return order;
        }
    }
}
