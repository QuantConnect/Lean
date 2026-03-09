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
using System.Collections.Concurrent;
using QuantConnect.Orders;

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /// <summary>
    /// Class used to keep track of CancelPending orders and their original or updated status
    /// </summary>
    public class CancelPendingOrders
    {
        private readonly ConcurrentDictionary<int, CancelPendingOrder> _cancelPendingOrders = new ConcurrentDictionary<int, CancelPendingOrder>();

        /// <summary>
        /// Amount of CancelPending Orders
        /// </summary>
        public int GetCancelPendingOrdersSize => _cancelPendingOrders.Count;

        /// <summary>
        /// Adds an order which will be canceled and we want to keep track of it Status in case of fallback
        /// </summary>
        /// <param name="orderId">The order id</param>
        /// <param name="status">The order Status, before the cancel request</param>
        public void Set(int orderId, OrderStatus status)
        {
            _cancelPendingOrders[orderId] = new CancelPendingOrder { Status = status };
        }

        /// <summary>
        /// Updates an order that is pending to be canceled.
        /// </summary>
        /// <param name="newStatus">The new status of the order. If its OrderStatus.Canceled or OrderStatus.Filled it will be removed</param>
        /// <param name="orderId">The id of the order</param>
        public void UpdateOrRemove(int orderId, OrderStatus newStatus)
        {
            CancelPendingOrder cancelPendingOrder;
            if (_cancelPendingOrders.TryGetValue(orderId, out cancelPendingOrder))
            {
                // The purpose of this pattern 'trygetvalue/lock/if containskey' is to guarantee that threads working on the same order will be correctly synchronized
                // Thread 1 at HandleCancelOrderRequest() processing a failed cancel request will call RemoveAndFallback() and revert order status
                // Thread 2 at HandleOrderEvent() with a filled order event will call UpdateOrRemove() and remove the order, ignoring its 'saved' status.
                lock (cancelPendingOrder)
                {
                    if (newStatus.IsClosed())
                    {
                        RemoveOrderFromCollection(orderId);
                    }
                    else if (newStatus != OrderStatus.CancelPending)
                    {
                        // check again because it might of been removed by the failed to cancel event
                        if (_cancelPendingOrders.ContainsKey(orderId))
                        {
                            _cancelPendingOrders[orderId].Status = newStatus;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes an order which we failed to cancel and falls back the order Status to previous value
        /// </summary>
        /// <param name="order">The order that failed to be canceled</param>
        public void RemoveAndFallback(Order order)
        {
            CancelPendingOrder cancelPendingOrder;
            if (_cancelPendingOrders.TryGetValue(order.Id, out cancelPendingOrder))
            {
                // The purpose of this pattern 'trygetvalue/lock/if containskey' is to guarantee that threads working on the same order will be correctly synchronized
                // Thread 1 at HandleCancelOrderRequest() processing a failed cancel request will call RemoveAndFallback() and revert order status
                // Thread 2 at HandleOrderEvent() with a filled order event will call UpdateOrRemove() and remove the order, ignoring its 'saved' status.
                lock (cancelPendingOrder)
                {
                    if (_cancelPendingOrders.ContainsKey(order.Id))
                    {
                        // update Status before removing from _cancelPendingOrders
                        order.Status = _cancelPendingOrders[order.Id].Status;
                        RemoveOrderFromCollection(order.Id);
                    }
                }
            }
        }

        private void RemoveOrderFromCollection(int orderId)
        {
            CancelPendingOrder cancelPendingOrderTrash;
            _cancelPendingOrders.TryRemove(orderId, out cancelPendingOrderTrash);
        }

        private class CancelPendingOrder
        {
            public OrderStatus Status { get; set; }
        }
    }
}
