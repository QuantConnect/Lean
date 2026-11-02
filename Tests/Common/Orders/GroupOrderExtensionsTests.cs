/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2025 QuantConnect Corporation.
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
using System.Collections.Generic;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class GroupOrderExtensionsTests
    {
        private static IEnumerable<TestCaseData> GroupQuantityTestCases
        {
            get
            {
                yield return new TestCaseData(CreateLegs(1), OrderDirection.Buy, 1).SetDescription("If brokerage returns already calculated quantity");
                yield return new TestCaseData(CreateLegs(-1), OrderDirection.Sell, -1).SetDescription("If brokerage returns already calculated quantity");
                yield return new TestCaseData(CreateLegs(1, -1), OrderDirection.Buy, 1).SetDescription("Bull Call Spread");
                yield return new TestCaseData(CreateLegs(-1, 1), OrderDirection.Sell, -1).SetDescription("Bear Call Spread");
                yield return new TestCaseData(CreateLegs(1, -2, 1), OrderDirection.Buy, 1).SetDescription("Bull Butterfly");
                yield return new TestCaseData(CreateLegs(-1, 2, -1), OrderDirection.Sell, -1).SetDescription("Bear Butterfly");
                yield return new TestCaseData(CreateLegs(1, 1), OrderDirection.Buy, 1).SetDescription("Bull Strangle");
                yield return new TestCaseData(CreateLegs(-1, -1), OrderDirection.Sell, -1).SetDescription("Bear Strangle");
                yield return new TestCaseData(CreateLegs(10, -20, 10), OrderDirection.Buy, 10);
                yield return new TestCaseData(CreateLegs(-10, 20, -10), OrderDirection.Sell, -10);
            }
        }

        [Test, TestCaseSource(nameof(GroupQuantityTestCases))]
        public void GetGroupQuantityByEachLegQuantityShouldReturnExpectedGCD(Leg[] legs, OrderDirection direction, int expected)
        {
            var legQuantities = legs.Select(x => Convert.ToDecimal(x.Quantity));
            var result = GroupOrderExtensions.GetGroupQuantityByEachLegQuantity(legQuantities, direction);
            Assert.AreEqual(Convert.ToDecimal(expected), result);
        }

        private static Leg[] CreateLegs(params int[] quantities) => [.. quantities.Select(q => Leg.Create(null, q))];

        [Test]
        public void GetGroupIdOfTheLegTickets()
        {
            var groupOrderManager = new GroupOrderManager(33, 2, 10);
            var tickets = new List<OrderTicket>
            {
                CreateLegTicket(groupOrderManager, Symbols.SPY_C_192_Feb19_2016, 10, OrderStatus.Submitted, 1),
                CreateLegTicket(groupOrderManager, Symbols.SPY_P_192_Feb19_2016, -10, OrderStatus.Submitted, 2)
            };

            Assert.AreEqual(33, tickets.GetGroupId());
            Assert.IsNull(new List<OrderTicket>().GetGroupId());
        }

        [Test]
        public void IsFilledOnlyWhenEveryLegTicketIsFilled()
        {
            var groupOrderManager = new GroupOrderManager(33, 2, 10);
            var tickets = new List<OrderTicket>
            {
                CreateLegTicket(groupOrderManager, Symbols.SPY_C_192_Feb19_2016, 10, OrderStatus.Filled, 1),
                CreateLegTicket(groupOrderManager, Symbols.SPY_P_192_Feb19_2016, -10, OrderStatus.PartiallyFilled, 2)
            };
            Assert.IsFalse(tickets.IsFilled());

            tickets[1] = CreateLegTicket(groupOrderManager, Symbols.SPY_P_192_Feb19_2016, -10, OrderStatus.Filled, 2);
            Assert.IsTrue(tickets.IsFilled());

            Assert.IsFalse(new List<OrderTicket>().IsFilled());
        }

        private static OrderTicket CreateLegTicket(GroupOrderManager groupOrderManager, Symbol symbol, decimal quantity, OrderStatus status, int orderId)
        {
            var request = new SubmitOrderRequest(OrderType.ComboMarket, symbol.SecurityType, symbol, quantity, 0, 0,
                new DateTime(2016, 2, 16, 11, 53, 30), "", groupOrderManager: groupOrderManager);
            request.SetOrderId(orderId);

            var ticket = new OrderTicket(null, request);
            var order = Order.CreateOrder(request);
            order.Status = status;
            ticket.SetOrder(order);

            return ticket;
        }
    }
}
