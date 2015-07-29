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
using System.Collections.Generic;
using QuantConnect.Orders;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a type capable of fetching Order instances by its QC order id or by a brokerage id
    /// </summary>
    public interface IOrderProvider
    {
        /// <summary>
        /// Gets the current number of orders that have been processed
        /// </summary>
        int OrdersCount { get; }

        /// <summary>
        /// Get the order by its id
        /// </summary>
        /// <param name="orderId">Order id to fetch</param>
        /// <returns>The order with the specified id, or null if no match is found</returns>
        Order GetOrderById(int orderId);

        /// <summary>
        /// Gets the order by its brokerage id
        /// </summary>
        /// <param name="brokerageId">The brokerage id to fetch</param>
        /// <returns>The first order matching the brokerage id, or null if no match is found</returns>
        Order GetOrderByBrokerageId(int brokerageId);

        /// <summary>
        /// Gets and enumerable of <see cref="OrderTicket"/> matching the specified <paramref name="filter"/>
        /// </summary>
        /// <param name="filter">The filter predicate used to find the required order tickets. If null is specified then all tickets are returned</param>
        /// <returns>An enumerable of <see cref="OrderTicket"/> matching the specified <paramref name="filter"/></returns>
        IEnumerable<OrderTicket> GetOrderTickets(Func<OrderTicket, bool> filter = null);

        /// <summary>
        /// Gets all orders matching the specified filter. Specifying null will return an enumerable
        /// of all orders.
        /// </summary>
        /// <param name="filter">Delegate used to filter the orders</param>
        /// <returns>All open orders this order provider currently holds</returns>
        IEnumerable<Order> GetOrders(Func<Order, bool> filter = null);
    }
}