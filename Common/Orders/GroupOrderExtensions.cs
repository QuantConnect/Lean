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

namespace QuantConnect.Orders
{
    /// <summary>
    ///
    /// </summary>
    public static class GroupOrderExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="order"></param>
        /// <param name="orderProvider"></param>
        /// <param name="orders"></param>
        /// <returns></returns>
        public static bool TryGetGroupOrders(this Order order, Func<int, Order> orderProvider, out List<Order> orders)
        {
            orders = new List<Order> { order };
            if (order.GroupOrderManager != null)
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
                        // this shouldn't happen
                        Log.Error($"BacktestingBrokerage.Scan(): missing order {otherOrdersId} of group: {order.GroupOrderManager.Id}");
                        return false;
                    }
                }

                if (order.GroupOrderManager.Count != orders.Count)
                {
                    Log.Debug($"TryGetGroupOrders(): missing orders of group {order.GroupOrderManager.Id}." +
                        $" We have {orders.Count}/{order.GroupOrderManager.Count} orders will skip");
                    return false;
                }
            }

            orders.Sort((x, y) => x.Id.CompareTo(y.Id));

            return true;
        }

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
