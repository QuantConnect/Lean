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
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Provides a test implementation of order mapping
    /// </summary>
    public class OrderProvider : IOrderProvider
    {
        private static int _orderID;
        private readonly IList<Order> _orders;

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
            order.Id = Interlocked.Increment(ref _orderID);
            _orders.Add(order);
        }

        public int OrdersCount
        {
            get { return _orders.Count; }
        }

        public Order GetOrderById(int orderId)
        {
            return _orders.FirstOrDefault(x => x.Id == orderId);
        }

        public Order GetOrderByBrokerageId(string brokerageId)
        {
            return _orders.FirstOrDefault(x => x.BrokerId.Contains(brokerageId));
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
            return _orders.Where(filter);
        }

        public List<Order> GetOpenOrders(Func<Order, bool> filter = null)
        {
            return _orders.Where(x => x.Status.IsOpen() && (filter == null || filter(x))).ToList();
        }
    }
}