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
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture]
    public class OrderProviderTests
    {
        private static readonly DateTime OrderTime = new DateTime(2024, 1, 3, 15, 0, 0);

        [Test]
        public void AddedOrderGetsATicket()
        {
            var orderProvider = new OrderProvider();
            var order = new MarketOrder(Symbols.SPY, 10, OrderTime);

            orderProvider.Add(order);

            var orderTicket = orderProvider.GetOrderTicket(order.Id);
            Assert.That(orderTicket, Is.Not.Null);
            Assert.That(orderTicket.OrderId, Is.EqualTo(order.Id));
            Assert.That(orderTicket.Symbol, Is.EqualTo(order.Symbol));
            Assert.That(orderTicket.Quantity, Is.EqualTo(order.Quantity));
            Assert.That(orderTicket.QuantityFilled, Is.EqualTo(0m));
        }

        [Test]
        public void UnknownOrderHasNoTicket()
        {
            Assert.That(new OrderProvider().GetOrderTicket(1), Is.Null);
        }

        [Test]
        public void OrderEventKeepsTheFilledQuantityOnTheTicket()
        {
            var orderProvider = new OrderProvider();
            var order = new MarketOrder(Symbols.SPY, 10, OrderTime);
            orderProvider.Add(order);

            orderProvider.HandleOrderEvent(new OrderEvent(order, OrderTime, OrderFee.Zero)
            {
                Status = OrderStatus.PartiallyFilled,
                FillQuantity = 4,
                FillPrice = 100m
            });

            var orderTicket = orderProvider.GetOrderTicket(order.Id);
            Assert.That(orderTicket.QuantityFilled, Is.EqualTo(4m));
            Assert.That(orderTicket.QuantityRemaining, Is.EqualTo(6m));
            Assert.That(order.Status, Is.EqualTo(OrderStatus.PartiallyFilled));

            orderProvider.HandleOrderEvent(new OrderEvent(order, OrderTime, OrderFee.Zero)
            {
                Status = OrderStatus.Filled,
                FillQuantity = 6,
                FillPrice = 101m
            });

            Assert.That(orderTicket.QuantityFilled, Is.EqualTo(10m));
            Assert.That(orderTicket.AverageFillPrice, Is.EqualTo(100.6m));
            Assert.That(order.Status, Is.EqualTo(OrderStatus.Filled));
        }

        [Test]
        public void OpenOrderTicketsSkipTheClosedOrders()
        {
            var orderProvider = new OrderProvider();
            var openOrder = new MarketOrder(Symbols.SPY, 10, OrderTime);
            var filledOrder = new MarketOrder(Symbols.AAPL, 5, OrderTime);
            orderProvider.Add(openOrder);
            orderProvider.Add(filledOrder);

            orderProvider.HandleOrderEvent(new OrderEvent(filledOrder, OrderTime, OrderFee.Zero)
            {
                Status = OrderStatus.Filled,
                FillQuantity = 5,
                FillPrice = 100m
            });

            Assert.That(orderProvider.GetOrderTickets().Count(), Is.EqualTo(2));

            var openOrderTickets = orderProvider.GetOpenOrderTickets().ToList();
            Assert.That(openOrderTickets.Count, Is.EqualTo(1));
            Assert.That(openOrderTickets[0].OrderId, Is.EqualTo(openOrder.Id));
        }
    }
}
