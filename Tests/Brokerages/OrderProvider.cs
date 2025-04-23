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
using System.Linq;
using System.Threading;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Provides a test implementation of order mapping
    /// </summary>
    public class OrderProvider : IOrderProvider
    {
        private int _orderId;
        private int _groupOrderManagerId;
        private protected readonly IList<Order> _orders;
        private readonly object _lock = new object();

        public OrderProvider(IList<Order> orders)
        {
            _orders = orders;
        }

        public OrderProvider()
        {
            _orders = new List<Order>();
        }

        public void Add(Order order)
        {
            order.Id = Interlocked.Increment(ref _orderId);

            if (order.GroupOrderManager != null && order.GroupOrderManager.Id == 0)
            {
                order.GroupOrderManager.Id = Interlocked.Increment(ref _groupOrderManagerId);
            }

            lock (_lock)
            {
                _orders.Add(order);
            }
        }

        public int OrdersCount => _orders.Count;

        public Order GetOrderById(int orderId)
        {
            Order order;
            lock (_lock)
            {
                order = _orders.FirstOrDefault(x => x.Id == orderId);
            }

            return order?.Clone();
        }

        public List<Order> GetOrdersByBrokerageId(string brokerageId)
        {
            lock (_lock)
            {
                return _orders.Where(o => o.BrokerId.Contains(brokerageId)).Select(o => o.Clone()).ToList();
            }
        }

        public IEnumerable<OrderTicket> GetOrderTickets(Func<OrderTicket, bool> filter = null)
        {
            throw new NotImplementedException("This method has not been implemented");
        }

        public IEnumerable<OrderTicket> GetOpenOrderTickets(Func<OrderTicket, bool> filter = null)
        {
            throw new NotImplementedException();
        }

        public OrderTicket GetOrderTicket(int orderId)
        {
            throw new NotImplementedException("This method has not been implemented");
        }

        public IEnumerable<Order> GetOrders(Func<Order, bool> filter)
        {
            return _orders.Where(filter).Select(x => x.Clone());
        }

        public List<Order> GetOpenOrders(Func<Order, bool> filter = null)
        {
            return _orders.Where(x => x.Status.IsOpen() && (filter == null || filter(x))).Select(x => x.Clone()).ToList();
        }

        /// <summary>
        /// Brokerage order id change is applied to the target order
        /// </summary>
        internal void HandlerBrokerageOrderIdChangedEvent(BrokerageOrderIdChangedEvent brokerageOrderIdChangedEvent)
        {
            lock (_lock)
            {
                var originalOrder = _orders.FirstOrDefault(x => x.Id == brokerageOrderIdChangedEvent.OrderId);

                if (originalOrder == null)
                {
                    // shouldn't happen but let's be careful
                    Log.Error($"OrderProvider.HandlerBrokerageOrderIdChangedEvent(): Lean order id {brokerageOrderIdChangedEvent.OrderId} not found");
                    return;
                }

                originalOrder.BrokerId = brokerageOrderIdChangedEvent.BrokerId;
            }
        }
    }
}
