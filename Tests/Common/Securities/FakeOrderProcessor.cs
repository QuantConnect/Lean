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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    /// <summary>
    /// Provides a fake implementation of <see cref="IOrderProcessor"/> for tests
    /// </summary>
    public class FakeOrderProcessor : IOrderProcessor
    {
        private readonly ConcurrentDictionary<int, Order> _orders = new ConcurrentDictionary<int, Order>();
        private readonly ConcurrentDictionary<int, OrderTicket> _tickets = new ConcurrentDictionary<int, OrderTicket>();
        public readonly ConcurrentDictionary<int, OrderRequest> ProcessedOrdersRequests = new ConcurrentDictionary<int, OrderRequest>();
        public void AddOrder(Order order)
        {
            _orders[order.Id] = order;
        }

        public void AddTicket(OrderTicket ticket)
        {
            _tickets[ticket.OrderId] = ticket;
        }
        public int OrdersCount { get; private set; }
        public Order GetOrderById(int orderId)
        {
            Order order;
            _orders.TryGetValue(orderId, out order);
            return order;
        }

        public Order GetOrderByBrokerageId(string brokerageId)
        {
            return _orders.Values.FirstOrDefault(x => x.BrokerId.Contains(brokerageId));
        }

        public IEnumerable<OrderTicket> GetOrderTickets(Func<OrderTicket, bool> filter = null)
        {
            return _tickets.Values.Where(filter ?? (x => true));
        }

        public IEnumerable<OrderTicket> GetOpenOrderTickets(Func<OrderTicket, bool> filter = null)
        {
            return _tickets.Values.Where(x => x.Status.IsOpen() && (filter == null || filter(x)));
        }

        public OrderTicket GetOrderTicket(int orderId)
        {
            OrderTicket ticket;
            _tickets.TryGetValue(orderId, out ticket);
            return ticket;
        }

        public IEnumerable<Order> GetOrders(Func<Order, bool> filter = null)
        {
            return _orders.Values.Where(filter ?? (x => true));
        }

        public List<Order> GetOpenOrders(Func<Order, bool> filter = null)
        {
            return _orders.Values.Where(x => x.Status.IsOpen() && (filter == null || filter(x))).ToList();
        }

        public OrderTicket Process(OrderRequest request)
        {
            ProcessedOrdersRequests.TryAdd(request.OrderId, request);
            switch (request.OrderRequestType)
            {
                case OrderRequestType.Submit:
                    return new OrderTicket(null, (SubmitOrderRequest) request);
                default:
                    throw new NotImplementedException();
            }
        }

        public void Clear()
        {
            _orders.Clear();
            _tickets.Clear();
            ProcessedOrdersRequests.Clear();
        }
    }
}