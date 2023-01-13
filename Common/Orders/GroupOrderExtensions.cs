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
using System.Linq;
using QuantConnect.Logging;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Orders.Fills;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Group (combo) orders extension methods for easiest combo order manipulation
    /// </summary>
    public static class GroupOrderExtensions
    {
        /// <summary>
        /// Gets the grouped orders (legs) of a group order
        /// </summary>
        /// <param name="order">Target order, which can be any of the legs of the combo</param>
        /// <param name="orderProvider">Order provider to use to access the existing orders</param>
        /// <param name="orders">List of orders in the combo</param>
        /// <returns>False if any of the orders in the combo is not yet found in the order provider. True otherwise</returns>
        /// <remarks>If the target order is not a combo order, the resulting list will contain that single order alone</remarks>
        public static bool TryGetGroupOrders(this Order order, Func<int, Order> orderProvider, out List<Order> orders)
        {
            orders = new List<Order> { order };
            if (order.GroupOrderManager != null)
            {
                lock (order.GroupOrderManager.OrderIds)
                {
                    foreach (var otherOrdersId in order.GroupOrderManager.OrderIds.Where(id => id != order.Id))
                    {
                        var otherOrder = orderProvider(otherOrdersId);
                        if (otherOrder != null)
                        {
                            orders.Add(otherOrder);
                        }
                        else
                        {
                            // this will happen while all the orders haven't arrived yet, we will retry
                            return false;
                        }
                    }
                }

                if (order.GroupOrderManager.Count != orders.Count)
                {
                    if (Log.DebuggingEnabled)
                    {
                        Log.Debug($"GroupOrderExtensions.TryGetGroupOrders(): missing orders of group {order.GroupOrderManager.Id}." +
                            $" We have {orders.Count}/{order.GroupOrderManager.Count} orders will skip");
                    }
                    return false;
                }
            }

            orders.Sort((x, y) => x.Id.CompareTo(y.Id));

            return true;
        }

        /// <summary>
        /// Gets the securities corresponding to each order in the group
        /// </summary>
        /// <param name="orders">List of orders to map</param>
        /// <param name="securityProvider">The security provider to use</param>
        /// <param name="securities">The resulting map of order to security</param>
        /// <returns>True if the mapping is successful, false otherwise.</returns>
        public static bool TryGetGroupOrdersSecurities(this List<Order> orders, ISecurityProvider securityProvider, out Dictionary<Order, Security> securities)
        {
            securities = new(orders.Count);
            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var security = securityProvider.GetSecurity(order.Symbol);

                if(security == null)
                {
                    return false;
                }
                securities[order] = security;
            }
            return true;
        }

        public static string GetErrorMessage(this Dictionary<Order, Security> securities, HasSufficientBuyingPowerForOrderResult hasSufficientBuyingPowerResult)
        {
            return $"Order Error: ids: [{string.Join(",", securities.Keys.Select(o => o.Id))}]," +
                $" Insufficient buying power to complete orders (Value:[{string.Join(",", securities.Select(o => o.Key.GetValue(o.Value).SmartRounding()))}])," +
                $" Reason: {hasSufficientBuyingPowerResult.Reason}.";
        }
    }
}
