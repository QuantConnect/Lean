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
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class ContingentOrderTests
    {
        private static readonly DateTime Time = new DateTime(2024, 1, 2, 15, 0, 0);

        [TestCase(ContingencyType.OneTriggersOther, ContingencyRole.Parent, true)]
        [TestCase(ContingencyType.OneTriggersOther, ContingencyRole.Child, true)]
        [TestCase(ContingencyType.OneTriggersOther, null, false)]
        [TestCase(ContingencyType.OneCancelsOther, null, true)]
        [TestCase(ContingencyType.OneCancelsOther, ContingencyRole.Parent, false)]
        [TestCase(ContingencyType.OneCancelsOther, ContingencyRole.Child, false)]
        [TestCase(ContingencyType.OneUpdatesOther, null, true)]
        [TestCase(ContingencyType.OneUpdatesOther, ContingencyRole.Parent, false)]
        public void ValidatesRoleForContingencyType(ContingencyType type, ContingencyRole? role, bool valid)
        {
            Assert.AreEqual(valid, ContingencyLink.IsValidRole(type, role));
            if (valid)
            {
                Assert.DoesNotThrow(() => new ContingencyLink(1, type, role));
            }
            else
            {
                Assert.Throws<ArgumentException>(() => new ContingencyLink(1, type, role));
            }
        }

        [Test]
        public void OrderRegistersItsIdInTheSet()
        {
            var set = new OrderContingency(7, 2, []);
            var first = new LimitOrder(Symbols.SPY, 10, 100, Time) { Contingency = set.WithLinks(null) };
            Assert.IsEmpty(set.OrderIds);

            first.Id = 3;
            CollectionAssert.AreEquivalent(new[] { 3 }, set.OrderIds);
            CollectionAssert.AreEquivalent(new[] { 3 }, first.Contingency.OrderIds);

            // the contingency can be set after the id too
            var second = new LimitOrder(Symbols.SPY, 10, 100, Time) { Id = 4 };
            second.Contingency = set.WithLinks(null);
            CollectionAssert.AreEquivalent(new[] { 3, 4 }, set.OrderIds);
            Assert.AreEqual(7, second.Contingency.Id);
            Assert.AreEqual(2, second.Contingency.Count);
        }

        [Test]
        public void OrderIdRegistrationIsThreadSafe()
        {
            var set = new OrderContingency(1, 500, []);
            Parallel.For(1, 501, id => _ = new MarketOrder(Symbols.SPY, 1, Time) { Contingency = set.WithLinks(null), Id = id });
            Assert.AreEqual(500, set.OrderIds.Count);
        }

        [Test]
        public void CloneSharesTheSetButNotTheLinks()
        {
            var bracket = CreateBracket();
            var takeProfit = bracket[1];

            var clone = takeProfit.Clone();

            Assert.AreNotSame(takeProfit.Contingency, clone.Contingency);
            Assert.AreSame(takeProfit.Contingency.OrderIds, clone.Contingency.OrderIds);
            Assert.AreEqual(takeProfit.Contingency.Id, clone.Contingency.Id);
            Assert.AreNotSame(takeProfit.Contingency.Links, clone.Contingency.Links);
            Assert.AreEqual(2, clone.Contingency.Links.Count);
            Assert.IsTrue(clone.IsWaitingForTrigger());

            // the trigger state of the clone is independent
            takeProfit.GetContingencyLink(ContingencyRole.Child).Triggered = true;
            Assert.IsFalse(takeProfit.IsWaitingForTrigger());
            Assert.IsTrue(clone.IsWaitingForTrigger());
        }

        [Test]
        public void CreateOrderFromRequestSetsContingency()
        {
            var set = new OrderContingency(1, 1, []);
            var links = new List<ContingencyLink> { new(1, ContingencyType.OneTriggersOther, ContingencyRole.Child) };
            var request = new SubmitOrderRequest(OrderType.StopMarket, SecurityType.Equity, Symbols.SPY, -10, 90, 0, 0, 0, false, Time, "tag",
                contingency: set.WithLinks(links));
            request.SetOrderId(5);

            var order = Order.CreateOrder(request);

            // the set is shared, the links are cloned
            Assert.AreSame(set.OrderIds, order.Contingency.OrderIds);
            Assert.AreEqual(1, order.Contingency.Id);
            Assert.AreNotSame(links[0], order.Contingency.Links.Single());
            Assert.AreEqual(OrderStatus.New, order.Status);
            Assert.IsTrue(order.IsContingent());
            Assert.IsTrue(order.IsWaitingForTrigger());
            CollectionAssert.AreEquivalent(new[] { 5 }, set.OrderIds);
        }

        [TestCase(OrderStatus.Filled)]
        [TestCase(OrderStatus.Canceled)]
        [TestCase(OrderStatus.Invalid)]
        public void ClosedOrderIsNotWaitingForTrigger(OrderStatus status)
        {
            var set = new OrderContingency(1, 1, []);
            var request = new SubmitOrderRequest(OrderType.StopMarket, SecurityType.Equity, Symbols.SPY, -10, 90, 0, 0, 0, false, Time, "tag",
                contingency: set.WithLinks([new(1, ContingencyType.OneTriggersOther, ContingencyRole.Child)]));
            request.SetOrderId(5);
            var order = Order.CreateOrder(request);
            Assert.IsTrue(order.Contingency.IsWaitingForTrigger);

            order.Status = status;

            Assert.IsFalse(order.Contingency.IsWaitingForTrigger);
            Assert.IsFalse(order.IsWaitingForTrigger());
            // the link is still not triggered
            Assert.IsFalse(order.Contingency.Links.Single().Triggered);
            // nor the clone of the closed order
            Assert.IsFalse(order.Clone().Contingency.IsWaitingForTrigger);
        }

        [Test]
        public void NonContingentOrder()
        {
            var order = new MarketOrder(Symbols.SPY, 1, Time) { Id = 1 };

            Assert.IsFalse(order.IsContingent());
            Assert.IsFalse(order.IsWaitingForTrigger());
            Assert.IsNull(order.GetTriggeredTime());
            Assert.AreEqual(Time, order.GetWorkingTime());
            Assert.IsTrue(order.TryGetContingentOrders(_ => null, out var orders));
            Assert.AreSame(order, orders.Single());
            Assert.IsEmpty(order.GetContingentChildren(orders));
            Assert.IsEmpty(order.GetContingentSiblings(orders));
        }

        [Test]
        public void BracketRelationships()
        {
            var bracket = CreateBracket();
            var entry = bracket[0];
            var takeProfit = bracket[1];
            var stopLoss = bracket[2];

            Assert.IsFalse(entry.IsWaitingForTrigger());
            Assert.IsTrue(takeProfit.IsWaitingForTrigger());
            Assert.IsTrue(stopLoss.IsWaitingForTrigger());

            CollectionAssert.AreEquivalent(new[] { takeProfit, stopLoss }, entry.GetContingentChildren(bracket));
            CollectionAssert.AreEquivalent(new[] { entry }, takeProfit.GetContingentParents(bracket));
            CollectionAssert.AreEquivalent(new[] { stopLoss }, takeProfit.GetContingentSiblings(bracket));
            CollectionAssert.AreEquivalent(new[] { takeProfit }, stopLoss.GetContingentSiblings(bracket));
            Assert.IsEmpty(entry.GetContingentSiblings(bracket));
            Assert.IsTrue(takeProfit.IsContingentSibling(stopLoss));
            Assert.IsFalse(takeProfit.IsContingentSibling(entry));
            Assert.IsFalse(takeProfit.IsContingentSibling(takeProfit));

            var triggeredTime = Time.AddMinutes(5);
            var child = takeProfit.GetContingencyLink(ContingencyRole.Child);
            child.TriggeredTime = triggeredTime;
            child.Triggered = true;
            Assert.IsFalse(takeProfit.IsWaitingForTrigger());
            Assert.AreEqual(triggeredTime, takeProfit.GetTriggeredTime());
            Assert.AreEqual(triggeredTime, takeProfit.GetWorkingTime());
            Assert.AreEqual(Time, entry.GetWorkingTime());
        }

        [Test]
        public void TryGetContingentOrdersRequiresAllOrders()
        {
            var bracket = CreateBracket();
            var orders = bracket.ToDictionary(x => x.Id);

            Assert.IsTrue(bracket[1].TryGetContingentOrders(id => orders.GetValueOrDefault(id), out var result));
            CollectionAssert.AreEqual(bracket, result);

            orders.Remove(bracket[2].Id);
            Assert.IsFalse(bracket[1].TryGetContingentOrders(id => orders.GetValueOrDefault(id), out _));
            CollectionAssert.AreEqual(bracket.Take(2), bracket[1].GetExistingContingentOrders(id => orders.GetValueOrDefault(id)));
        }

        [Test]
        public void TryGetContingentOrdersRequiresTheExpectedCount()
        {
            // only 2 out of 3 orders have been created yet
            var manager = new OrderContingency(1, 3, []);
            var first = new MarketOrder(Symbols.SPY, 1, Time) { Contingency = manager.WithLinks(null), Id = 1 };
            var second = new MarketOrder(Symbols.SPY, 1, Time) { Contingency = manager.WithLinks(null), Id = 2 };
            var orders = new Dictionary<int, Order> { { 1, first }, { 2, second } };

            Assert.IsFalse(first.TryGetContingentOrders(id => orders.GetValueOrDefault(id), out _));
        }

        [Test]
        public void DescendantsOfAChain()
        {
            // 1 triggers 2 which triggers 3 and 4, where 4 triggers 5
            var manager = new OrderContingency(1, 5, []);
            var orders = new List<Order>
            {
                CreateOrder(1, manager, new ContingencyLink(1, ContingencyType.OneTriggersOther, ContingencyRole.Parent)),
                CreateOrder(2, manager, new ContingencyLink(1, ContingencyType.OneTriggersOther, ContingencyRole.Child),
                    new ContingencyLink(2, ContingencyType.OneTriggersOther, ContingencyRole.Parent)),
                CreateOrder(3, manager, new ContingencyLink(2, ContingencyType.OneTriggersOther, ContingencyRole.Child)),
                CreateOrder(4, manager, new ContingencyLink(2, ContingencyType.OneTriggersOther, ContingencyRole.Child),
                    new ContingencyLink(3, ContingencyType.OneTriggersOther, ContingencyRole.Parent)),
                CreateOrder(5, manager, new ContingencyLink(3, ContingencyType.OneTriggersOther, ContingencyRole.Child)),
            };

            CollectionAssert.AreEquivalent(new[] { 2, 3, 4, 5 }, orders[0].GetContingentDescendants(orders).Select(x => x.Id));
            CollectionAssert.AreEquivalent(new[] { 5 }, orders[3].GetContingentDescendants(orders).Select(x => x.Id));
            Assert.IsEmpty(orders[4].GetContingentDescendants(orders));
        }

        [Test]
        public void ComboLegsAreNotSiblings()
        {
            // two combo orders of two legs each, one cancels the other
            var manager = new OrderContingency(1, 4, []);
            var firstCombo = new GroupOrderManager(1, 2, 1);
            var secondCombo = new GroupOrderManager(2, 2, 1);
            ContingencyLink Member() => new(1, ContingencyType.OneCancelsOther);
            var orders = new List<Order>
            {
                new ComboMarketOrder(Symbols.SPY, 1, Time, firstCombo) { Contingency = manager.WithLinks([Member()]), Id = 1 },
                new ComboMarketOrder(Symbols.AAPL, -1, Time, firstCombo) { Contingency = manager.WithLinks([Member()]), Id = 2 },
                new ComboMarketOrder(Symbols.SPY, -1, Time, secondCombo) { Contingency = manager.WithLinks([Member()]), Id = 3 },
                new ComboMarketOrder(Symbols.AAPL, 1, Time, secondCombo) { Contingency = manager.WithLinks([Member()]), Id = 4 },
            };

            Assert.IsTrue(orders[0].IsSameGroupOrder(orders[1]));
            Assert.IsFalse(orders[0].IsContingentSibling(orders[1]));
            CollectionAssert.AreEquivalent(new[] { 3, 4 }, orders[0].GetContingentSiblings(orders).Select(x => x.Id));
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, orders[3].GetContingentSiblings(orders).Select(x => x.Id));
        }

        [Test]
        public void RoundTripSerialization()
        {
            var bracket = CreateBracket();
            var takeProfit = bracket[1];
            var child = takeProfit.GetContingencyLink(ContingencyRole.Child);
            child.TriggeredTime = Time.AddMinutes(1);
            child.Triggered = true;

            var json = JsonConvert.SerializeObject(takeProfit);
            var deserialized = JsonConvert.DeserializeObject<Order>(json, new OrderJsonConverter());

            Assert.AreEqual(OrderType.Limit, deserialized.Type);
            Assert.AreEqual(takeProfit.Contingency.Id, deserialized.Contingency.Id);
            Assert.AreEqual(3, deserialized.Contingency.Count);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, deserialized.Contingency.OrderIds);
            Assert.AreEqual(2, deserialized.Contingency.Links.Count);

            var deserializedChild = deserialized.GetContingencyLink(ContingencyRole.Child);
            Assert.AreEqual(child.Id, deserializedChild.Id);
            Assert.AreEqual(ContingencyType.OneTriggersOther, deserializedChild.Type);
            Assert.IsTrue(deserializedChild.Triggered);
            Assert.AreEqual(child.TriggeredTime, deserializedChild.TriggeredTime);

            var deserializedMember = deserialized.GetSiblingLink();
            Assert.AreEqual(ContingencyType.OneCancelsOther, deserializedMember.Type);
            Assert.IsFalse(deserializedMember.Triggered);
            Assert.IsNull(deserializedMember.TriggeredTime);

            // held orders don't serialize the trigger state
            var stopLossJson = JsonConvert.SerializeObject(bracket[2]);
            StringAssert.DoesNotContain("triggered", stopLossJson);
            Assert.IsTrue(JsonConvert.DeserializeObject<Order>(stopLossJson, new OrderJsonConverter()).IsWaitingForTrigger());
        }

        [Test]
        public void NonContingentOrdersDoNotSerializeTheContingency()
        {
            var json = JsonConvert.SerializeObject(new LimitOrder(Symbols.SPY, 10, 100, Time) { Id = 1 });

            StringAssert.DoesNotContain("contingency", json);
            var deserialized = JsonConvert.DeserializeObject<Order>(json, new OrderJsonConverter());
            Assert.IsNull(deserialized.Contingency);
        }

        [TestCase("'Contingency':{'Id':4,'Count':2,'OrderIds':[8,9],'Links':[{'Id':1,'Type':0}]}", true)]
        [TestCase("'contingency':{'id':4,'count':2,'orderIds':[8,9],'links':[{'id':1,'type':0,'role':null}]}", true)]
        // resilient: missing or malformed information
        [TestCase("'contingency':{'id':4,'count':2,'orderIds':[8,9]}", false)]
        [TestCase("'contingency':{'links':[{'id':1,'type':0}]}", false)]
        // a role for a one cancels other link is invalid, it's skipped
        [TestCase("'contingency':{'id':4,'count':2,'orderIds':[8,9],'links':[{'id':1,'type':0,'role':0}]}", false)]
        [TestCase("'contingency':null", false)]
        [TestCase("'contingency':5", false)]
        [TestCase("'contingency':{'id':4,'count':2,'orderIds':[8,9],'links':'invalid'}", false)]
        public void DeserializesDifferentFormats(string contingency, bool expectedContingent)
        {
            var json = @"{'Type':1,'LimitPrice':100,'Id':8,'Symbol':{'Value':'SPY','ID':'SPY R735QTJ8XC9X','Permtick':'SPY'},'Price':0,
'Time':'2024-01-02T15:00:00Z','Quantity':10,'Status':1,'BrokerId':[],'SecurityType':1," + contingency + "}";

            var order = JsonConvert.DeserializeObject<Order>(json.Replace('\'', '"'), new OrderJsonConverter());

            Assert.AreEqual(8, order.Id);
            Assert.AreEqual(expectedContingent, order.IsContingent());
            if (expectedContingent)
            {
                Assert.AreEqual(4, order.Contingency.Id);
                Assert.AreEqual(2, order.Contingency.Count);
                CollectionAssert.AreEquivalent(new[] { 8, 9 }, order.Contingency.OrderIds);
                var link = order.Contingency.Links.Single();
                Assert.AreEqual(ContingencyType.OneCancelsOther, link.Type);
                Assert.IsNull(link.Role);
            }
        }

        [Test]
        public void DeserializationSkipsInvalidLinks()
        {
            // an invalid role for the contingency type and a garbage entry
            var json = @"{'type':0,'id':8,'symbol':{'value':'SPY','id':'SPY R735QTJ8XC9X','permtick':'SPY'},'price':0,
'time':'2024-01-02T15:00:00Z','quantity':10,'status':1,'brokerId':[],'securityType':1,
'contingency':{'id':4,'count':2,'orderIds':[8,9],'links':[{'id':1,'type':0,'role':1}, 5, {'id':2,'type':1,'role':1,'triggered':true}]}}";

            var order = JsonConvert.DeserializeObject<Order>(json.Replace('\'', '"'), new OrderJsonConverter());

            var link = order.Contingency.Links.Single();
            Assert.AreEqual(2, link.Id);
            Assert.AreEqual(ContingencyRole.Child, link.Role);
            Assert.IsTrue(link.Triggered);
        }

        [Test]
        public void ContingencyDeserializesOnItsOwn()
        {
            var json = JsonConvert.SerializeObject(CreateBracket()[1].Contingency);

            var contingency = JsonConvert.DeserializeObject<OrderContingency>(json);

            Assert.AreEqual(1, contingency.Id);
            Assert.AreEqual(3, contingency.Count);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, contingency.OrderIds);
            Assert.AreEqual(2, contingency.Links.Count);
            Assert.AreEqual(ContingencyRole.Child, contingency.Links[0].Role);
            Assert.IsNull(contingency.Links[1].Role);
        }

        [Test]
        public void OrderTicketExposesTheContingency()
        {
            var set = new OrderContingency(1, 1, []);
            var links = new List<ContingencyLink> { new(1, ContingencyType.OneTriggersOther, ContingencyRole.Child) };
            var request = new SubmitOrderRequest(OrderType.Limit, SecurityType.Equity, Symbols.SPY, -10, 0, 110, 0, 0, false, Time, "",
                contingency: set.WithLinks(links));
            request.SetOrderId(1);
            var ticket = new OrderTicket(null, request);

            // before the order is set it uses the request
            Assert.AreSame(request.Contingency, ticket.Contingency);
            Assert.IsTrue(ticket.Contingency.IsWaitingForTrigger);

            var order = Order.CreateOrder(request);
            ticket.SetOrder(order);
            Assert.IsTrue(ticket.Contingency.IsWaitingForTrigger);

            order.GetContingencyLink(ContingencyRole.Child).Triggered = true;
            Assert.IsFalse(ticket.Contingency.IsWaitingForTrigger);

            // turns into a ticket and back
            var newTicket = order.ToOrderTicket(null);
            Assert.AreSame(order.Contingency, newTicket.SubmitRequest.Contingency);
            Assert.AreEqual(1, newTicket.SubmitRequest.Contingency.Links.Count);
            Assert.IsFalse(newTicket.Contingency.IsWaitingForTrigger);
        }

        /// <summary>
        /// Creates a bracket: an entry which triggers a take profit and a stop loss where one cancels the other
        /// </summary>
        public static List<Order> CreateBracket(int managerId = 1, int firstOrderId = 1, ContingencyType exitsContingencyType = ContingencyType.OneCancelsOther,
            decimal quantity = 100)
        {
            var manager = new OrderContingency(managerId, 3, []);
            return new List<Order>
            {
                new LimitOrder(Symbols.SPY, quantity, 100, Time)
                {
                    Contingency = manager.WithLinks([new(1, ContingencyType.OneTriggersOther, ContingencyRole.Parent)]),
                    Status = OrderStatus.Submitted,
                    Id = firstOrderId
                },
                new LimitOrder(Symbols.SPY, -quantity, 110, Time)
                {
                    Contingency = manager.WithLinks([new(1, ContingencyType.OneTriggersOther, ContingencyRole.Child), new(2, exitsContingencyType)]),
                    Status = OrderStatus.Submitted,
                    Id = firstOrderId + 1
                },
                new StopMarketOrder(Symbols.SPY, -quantity, 90, Time)
                {
                    Contingency = manager.WithLinks([new(1, ContingencyType.OneTriggersOther, ContingencyRole.Child), new(2, exitsContingencyType)]),
                    Status = OrderStatus.Submitted,
                    Id = firstOrderId + 2
                }
            };
        }

        private static Order CreateOrder(int id, OrderContingency set, params ContingencyLink[] links)
        {
            return new MarketOrder(Symbols.SPY, 1, Time)
            {
                Contingency = set.WithLinks(links),
                Status = OrderStatus.Submitted,
                Id = id
            };
        }
    }
}
