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
using System.Collections.Generic;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Contingent orders (OCO, OTO, OUO, brackets) extension methods for easiest manipulation
    /// </summary>
    public static class ContingentOrderExtensions
    {
        /// <summary>
        /// Determines whether the order is part of a set of contingent orders
        /// </summary>
        public static bool IsContingent(this Order order)
        {
            return order.Contingency != null && order.Contingency.Links.Count > 0;
        }

        /// <summary>
        /// Gets the first <see cref="ContingencyType.OneTriggersOther"/> link of the order with the given role, null if none
        /// </summary>
        public static ContingencyLink GetContingencyLink(this Order order, ContingencyRole role)
        {
            return order.Contingency?.GetLink(role);
        }

        /// <summary>
        /// Gets the link of the order to its siblings, the other members of its <see cref="ContingencyType.OneCancelsOther"/>
        /// or <see cref="ContingencyType.OneUpdatesOther"/> contingency, null if none
        /// </summary>
        public static ContingencyLink GetSiblingLink(this Order order)
        {
            return order.Contingency?.GetLink(null);
        }

        /// <summary>
        /// Determines whether the order is a contingent child still held waiting for its parent to fill,
        /// that is, the order is not working in the market yet
        /// </summary>
        public static bool IsWaitingForTrigger(this Order order)
        {
            return order.Contingency?.IsWaitingForTrigger == true;
        }

        /// <summary>
        /// Gets the utc time at which the contingent child order was triggered, null if not a child or not triggered yet
        /// </summary>
        public static DateTime? GetTriggeredTime(this Order order)
        {
            return order.GetContingencyLink(ContingencyRole.Child)?.TriggeredTime;
        }

        /// <summary>
        /// Gets the utc time from which the order is considered to be working in the market:
        /// the time it was triggered for contingent child orders, else its creation time
        /// </summary>
        public static DateTime GetWorkingTime(this Order order)
        {
            return order.GetTriggeredTime() ?? order.Time;
        }

        /// <summary>
        /// Determines whether both orders are members of the same <see cref="ContingencyType.OneCancelsOther"/>
        /// or <see cref="ContingencyType.OneUpdatesOther"/> contingency, so at most one of them is expected to completely fill
        /// </summary>
        public static bool IsContingentSibling(this Order order, Order other)
        {
            return order.IsContingentSibling(order.GetSiblingLink(), other);
        }

        /// <summary>
        /// Determines whether the other order is a sibling of the given one, given its link to its siblings
        /// </summary>
        internal static bool IsContingentSibling(this Order order, ContingencyLink member, Order other)
        {
            return member != null && order.Id != other.Id && other.Contingency != null
                && order.Contingency.Id == other.Contingency.Id && member.Id == other.GetSiblingLink()?.Id
                // legs of the same combo are not siblings, they are a single unit
                && !order.IsSameGroupOrder(other);
        }

        /// <summary>
        /// Determines whether both orders are legs of the same group (combo) order
        /// </summary>
        public static bool IsSameGroupOrder(this Order order, Order other)
        {
            return order.GroupOrderManager != null && other.GroupOrderManager != null
                && order.GroupOrderManager.Id == other.GroupOrderManager.Id;
        }

        /// <summary>
        /// Gets all the orders in the set of contingent orders the given order belongs to
        /// </summary>
        /// <param name="order">Target order, which can be any of the orders in the set</param>
        /// <param name="orderProvider">Order provider to use to access the existing orders</param>
        /// <param name="orders">List of orders in the set, sorted by id</param>
        /// <returns>False if any of the orders in the set is not yet found in the order provider. True otherwise</returns>
        /// <remarks>If the target order is not a contingent order, the resulting list will contain that single order alone</remarks>
        public static bool TryGetContingentOrders(this Order order, Func<int, Order> orderProvider, out List<Order> orders)
        {
            var contingency = order.Contingency;
            if (contingency != null && contingency.OrderIds.Count != contingency.Count)
            {
                // this will happen while all the orders haven't arrived yet, we will retry
                orders = null;
                return false;
            }

            orders = new List<Order>(contingency?.Count ?? 1) { order };
            if (contingency != null)
            {
                lock (contingency.OrderIds)
                {
                    foreach (var otherOrderId in contingency.OrderIds)
                    {
                        if (otherOrderId == order.Id)
                        {
                            continue;
                        }

                        var otherOrder = orderProvider(otherOrderId);
                        if (otherOrder == null)
                        {
                            // this will happen while all the orders haven't arrived yet, we will retry
                            return false;
                        }
                        orders.Add(otherOrder);
                    }
                }

                if (contingency.Count != orders.Count)
                {
                    if (Log.DebuggingEnabled)
                    {
                        Log.Debug($"ContingentOrderExtensions.TryGetContingentOrders(): missing orders of set {contingency.Id}." +
                            $" We have {orders.Count}/{contingency.Count} orders will skip");
                    }
                    return false;
                }
            }

            orders.Sort((x, y) => x.Id.CompareTo(y.Id));
            return true;
        }

        /// <summary>
        /// Gets the orders of the set which exist in the given provider, without requiring all of them to be present
        /// </summary>
        /// <param name="order">Target order, which can be any of the orders in the set</param>
        /// <param name="orderProvider">Order provider to use to access the existing orders</param>
        /// <returns>The existing orders of the set, including the given one, sorted by id</returns>
        public static List<Order> GetExistingContingentOrders(this Order order, Func<int, Order> orderProvider)
        {
            var contingency = order.Contingency;
            var orders = new List<Order>(contingency?.Count ?? 1) { order };
            if (contingency != null)
            {
                lock (contingency.OrderIds)
                {
                    foreach (var otherOrderId in contingency.OrderIds)
                    {
                        if (otherOrderId != order.Id)
                        {
                            var otherOrder = orderProvider(otherOrderId);
                            if (otherOrder != null)
                            {
                                orders.Add(otherOrder);
                            }
                        }
                    }
                }
                orders.Sort((x, y) => x.Id.CompareTo(y.Id));
            }
            return orders;
        }

        /// <summary>
        /// Gets the children the given parent order triggers once filled
        /// </summary>
        /// <param name="order">The parent order</param>
        /// <param name="contingentOrders">The orders in the set</param>
        public static IEnumerable<Order> GetContingentChildren(this Order order, IEnumerable<Order> contingentOrders)
        {
            var parent = order.GetContingencyLink(ContingencyRole.Parent);
            if (parent == null)
            {
                return Enumerable.Empty<Order>();
            }
            return contingentOrders.Where(other => other.Id != order.Id && other.GetContingencyLink(ContingencyRole.Child)?.Id == parent.Id);
        }

        /// <summary>
        /// Gets the parent orders of the given child, more than one when the parent is a combo order
        /// </summary>
        /// <param name="order">The child order</param>
        /// <param name="contingentOrders">The orders in the set</param>
        public static IEnumerable<Order> GetContingentParents(this Order order, IEnumerable<Order> contingentOrders)
        {
            var child = order.GetContingencyLink(ContingencyRole.Child);
            if (child == null)
            {
                return Enumerable.Empty<Order>();
            }
            return contingentOrders.Where(other => other.Id != order.Id && other.GetContingencyLink(ContingencyRole.Parent)?.Id == child.Id);
        }

        /// <summary>
        /// Gets the sibling orders of the given one, the other members of its OCO/OUO contingency.
        /// The legs of the same combo order are not siblings
        /// </summary>
        /// <param name="order">The member order</param>
        /// <param name="contingentOrders">The orders in the set</param>
        public static IEnumerable<Order> GetContingentSiblings(this Order order, IEnumerable<Order> contingentOrders)
        {
            if (order.GetSiblingLink() == null)
            {
                return Enumerable.Empty<Order>();
            }
            return contingentOrders.Where(other => order.IsContingentSibling(other));
        }

        /// <summary>
        /// Gets all the descendants of the given order: its children, their children and so on
        /// </summary>
        /// <param name="order">The parent order</param>
        /// <param name="contingentOrders">The orders in the set</param>
        public static List<Order> GetContingentDescendants(this Order order, IReadOnlyCollection<Order> contingentOrders)
        {
            var result = new List<Order>();
            var visited = new HashSet<int> { order.Id };
            var pending = new Queue<Order>();
            pending.Enqueue(order);
            while (pending.Count > 0)
            {
                foreach (var child in pending.Dequeue().GetContingentChildren(contingentOrders))
                {
                    if (visited.Add(child.Id))
                    {
                        result.Add(child);
                        pending.Enqueue(child);
                    }
                }
            }
            return result;
        }
    }
}
