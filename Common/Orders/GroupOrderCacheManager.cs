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

using System.Collections.Generic;
using System.Collections.Concurrent;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Provides a thread-safe service for caching and managing original orders when they are part of a group.
    /// </summary>
    public class GroupOrderCacheManager
    {
        /// <summary>
        /// A thread-safe dictionary that caches original orders when they are part of a group.
        /// </summary>
        /// <remarks>
        /// The dictionary uses the order ID as the key and stores the original <see cref="Order"/> objects as values.
        /// This allows for the modification of the original orders, such as setting the brokerage ID, 
        /// without retrieving a cloned instance from the order provider.
        /// </remarks>
        private readonly ConcurrentDictionary<int, Order> _pendingGroupOrders = new();

        /// <summary>
        /// Attempts to retrieve an original order from the cache using the specified order ID.
        /// </summary>
        /// <param name="orderId">The unique identifier of the order to retrieve.</param>
        /// <returns>
        /// The original <see cref="Order"/> if found; otherwise, <c>null</c>.
        /// </returns>
        public Order TryGetOrder(int orderId)
        {
            _pendingGroupOrders.TryGetValue(orderId, out var order);
            return order;
        }

        /// <summary>
        /// Caches an original order in the internal dictionary for future retrieval.
        /// </summary>
        /// <param name="order">The <see cref="Order"/> object to cache.</param>
        public void CacheOrder(Order order)
        {
            _pendingGroupOrders[order.Id] = order;
        }

        /// <summary>
        /// Removes a list of orders from the internal cache.
        /// </summary>
        /// <param name="orders">The list of <see cref="Order"/> objects to remove from the cache.</param>
        public void RemoveCachedOrders(List<Order> orders)
        {
            for (var i = 0; i < orders.Count; i++)
            {
                _pendingGroupOrders.TryRemove(orders[i].Id, out _);
            }
        }
    }
}
