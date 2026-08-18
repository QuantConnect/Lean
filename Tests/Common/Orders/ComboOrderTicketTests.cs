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
using NUnit.Framework;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class ComboOrderTicketTests
    {
        private int _orderId;

        [Test]
        public void ExposesTheGroupOrderManagerIdOfItsLegs()
        {
            var groupOrderManager = new GroupOrderManager(33, 2, 10);
            var comboTicket = new ComboOrderTicket(new[]
            {
                CreateLegTicket(groupOrderManager, Symbols.SPY_C_192_Feb19_2016, 10, OrderStatus.Submitted),
                CreateLegTicket(groupOrderManager, Symbols.SPY_P_192_Feb19_2016, -10, OrderStatus.Submitted)
            });

            Assert.AreEqual(33, comboTicket.GroupOrderManagerId);
            Assert.AreEqual(2, comboTicket.Tickets.Count);
            Assert.AreSame(comboTicket, comboTicket.Tickets);
        }

        [Test]
        public void FilledOnlyWhenEveryLegIsFilled()
        {
            var groupOrderManager = new GroupOrderManager(1, 2, 10);
            var firstLeg = CreateLegTicket(groupOrderManager, Symbols.SPY_C_192_Feb19_2016, 10, OrderStatus.Filled);
            var secondLeg = CreateLegTicket(groupOrderManager, Symbols.SPY_P_192_Feb19_2016, -10, OrderStatus.PartiallyFilled);

            var comboTicket = new ComboOrderTicket(new[] { firstLeg, secondLeg });
            Assert.IsFalse(comboTicket.Filled);

            comboTicket = new ComboOrderTicket(new[]
            {
                CreateLegTicket(groupOrderManager, Symbols.SPY_C_192_Feb19_2016, 10, OrderStatus.Filled),
                CreateLegTicket(groupOrderManager, Symbols.SPY_P_192_Feb19_2016, -10, OrderStatus.Filled)
            });
            Assert.IsTrue(comboTicket.Filled);
        }

        [Test]
        public void EmptyTicketHasNoGroupIdAndIsNotFilled()
        {
            var comboTicket = new ComboOrderTicket();

            Assert.IsNull(comboTicket.GroupOrderManagerId);
            Assert.IsFalse(comboTicket.Filled);
        }

        private OrderTicket CreateLegTicket(GroupOrderManager groupOrderManager, Symbol symbol, decimal quantity, OrderStatus status)
        {
            var request = new SubmitOrderRequest(OrderType.ComboMarket, symbol.SecurityType, symbol, quantity, 0, 0,
                new DateTime(2016, 2, 16, 11, 53, 30), "", groupOrderManager: groupOrderManager);
            request.SetOrderId(++_orderId);

            var ticket = new OrderTicket(null, request);
            var order = Order.CreateOrder(request);
            order.Status = status;
            ticket.SetOrder(order);

            return ticket;
        }
    }
}
