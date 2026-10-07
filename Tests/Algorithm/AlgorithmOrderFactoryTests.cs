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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmOrderFactoryTests
    {
        private QCAlgorithm _algorithm;
        private Symbol _spy;
        private Symbol _aapl;

        [SetUp]
        public void SetUp()
        {
            _algorithm = new QCAlgorithm();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
            _algorithm.SetCash(100000);
            _algorithm.SetFinishedWarmingUp();
            _algorithm.SetLiveMode(false);
            _algorithm.SetDateTime(new DateTime(2024, 1, 3, 16, 0, 0));
            _algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            _algorithm.SetCurrentSlice(new Slice(DateTime.MinValue, Enumerable.Empty<BaseData>(), DateTime.MinValue));

            _spy = AddEquity("SPY", 100);
            _aapl = AddEquity("AAPL", 200);
        }

        [Test]
        public void PlainOrderIsNotContingent()
        {
            var order = _algorithm.OrderFactory.LimitOrder(_spy, 10, 99, tag: "tag");
            var tickets = _algorithm.Order(order);

            var request = tickets.Single().SubmitRequest;
            Assert.AreSame(order, request);
            Assert.AreSame(Ticket(order), tickets[0]);
            Assert.IsTrue(order.OrderId > 0);
            Assert.AreEqual(_algorithm.UtcTime, request.Time);
            Assert.AreEqual(OrderType.Limit, request.OrderType);
            Assert.AreEqual(99, request.LimitPrice);
            Assert.AreEqual("tag", request.Tag);
            Assert.IsNull(request.Contingency);
            Assert.IsNull(tickets[0].Contingency);
        }

        [Test]
        public void ExistingOrderMethodsAreNotContingent()
        {
            var requests = new[]
            {
                _algorithm.LimitOrder(_spy, 10, 99).SubmitRequest,
                _algorithm.StopMarketOrder(_spy, -10, 90).SubmitRequest,
                _algorithm.StopLimitOrder(_spy, -10, 90, 89).SubmitRequest,
                _algorithm.LimitIfTouchedOrder(_spy, -10, 110, 109).SubmitRequest,
                _algorithm.TrailingStopOrder(_spy, -10, 0.1m, true).SubmitRequest,
                _algorithm.MarketOrder(_spy, 10, asynchronous: true).SubmitRequest
            };

            Assert.IsTrue(requests.All(x => x.Contingency == null && x.GroupOrderManager == null));
            CollectionAssert.AreEqual(new[] { OrderType.Limit, OrderType.StopMarket, OrderType.StopLimit, OrderType.LimitIfTouched, OrderType.TrailingStop, OrderType.Market },
                requests.Select(x => x.OrderType));
            // the trailing stop price is calculated from the current price
            Assert.AreEqual(90, requests[4].StopPrice);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void BracketOrder(bool limitEntry)
        {
            var properties = new OrderProperties { TimeInForce = TimeInForce.Day };
            var tickets = _algorithm.BracketOrder(_spy, 10, takeProfitPrice: 110, stopLossPrice: 90, limitPrice: limitEntry ? 99 : null,
                asynchronous: true, tag: "bracket", orderProperties: properties);

            Assert.AreEqual(3, tickets.Count);
            var requests = tickets.Select(x => x.SubmitRequest).ToList();
            CollectionAssert.AreEqual(new[] { limitEntry ? OrderType.Limit : OrderType.Market, OrderType.Limit, OrderType.StopMarket }, requests.Select(x => x.OrderType));
            CollectionAssert.AreEqual(new[] { 10m, -10m, -10m }, requests.Select(x => x.Quantity));
            Assert.AreEqual(110, requests[1].LimitPrice);
            Assert.AreEqual(90, requests[2].StopPrice);
            Assert.IsTrue(requests.All(x => x.Tag == "bracket" && x.OrderProperties.TimeInForce is DayTimeInForce));
            // each order gets it's own properties instance
            Assert.AreEqual(3, requests.Select(x => x.OrderProperties).Distinct().Count());

            AssertBracket(requests, ContingencyType.OneCancelsOther);
            Assert.IsFalse(tickets[0].Contingency.IsWaitingForTrigger);
            Assert.IsTrue(tickets[1].Contingency.IsWaitingForTrigger);
            Assert.IsTrue(tickets[2].Contingency.IsWaitingForTrigger);
        }

        [TestCase(ContingencyType.OneCancelsOther)]
        [TestCase(ContingencyType.OneUpdatesOther)]
        public void BracketThroughOrderFactory(ContingencyType contingencyType)
        {
            var entry = _algorithm.OrderFactory.StopLimitOrder(_spy, -10, 95, 94).Bracket(80, 105, stopLossLimitPrice: 106, contingencyType: contingencyType);
            var tickets = _algorithm.Order(entry);

            var requests = tickets.Select(x => x.SubmitRequest).ToList();
            CollectionAssert.AreEqual(new[] { OrderType.StopLimit, OrderType.Limit, OrderType.StopLimit }, requests.Select(x => x.OrderType));
            CollectionAssert.AreEqual(new[] { -10m, 10m, 10m }, requests.Select(x => x.Quantity));
            Assert.AreEqual(80, requests[1].LimitPrice);
            Assert.AreEqual(105, requests[2].StopPrice);
            Assert.AreEqual(106, requests[2].LimitPrice);
            AssertBracket(requests, contingencyType);

            // the submitted request is the ticket's, the whole set was submitted
            Assert.AreSame(tickets[0], Ticket(entry));
            Assert.AreSame(entry, tickets[0].SubmitRequest);
            CollectionAssert.AreEqual(tickets, entry.Contingency.Requests.Select(Ticket));
            Assert.IsTrue(tickets.All(ticket => ticket.SubmitRequest.Contingency.Id == entry.Contingency.Id));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void OneCancelsOtherOrUpdatesOther(bool oneCancelsOther)
        {
            var orders = new List<SubmitOrderRequest> { _algorithm.OrderFactory.LimitOrder(_spy, -10, 110), _algorithm.OrderFactory.StopMarketOrder(_spy, -10, 90), _algorithm.OrderFactory.StopMarketOrder(_aapl, 5, 250) };
            var tickets = oneCancelsOther ? _algorithm.OneCancelsOtherOrder(orders) : _algorithm.OneUpdatesOtherOrder(orders);

            Assert.AreEqual(3, tickets.Count);
            var contingency = tickets[0].SubmitRequest.Contingency;
            Assert.AreEqual(3, contingency.Count);
            CollectionAssert.AreEquivalent(new[] { _spy, _aapl }, contingency.Symbols);
            CollectionAssert.AreEquivalent(new[] { OrderDirection.Buy, OrderDirection.Sell }, contingency.Directions);
            CollectionAssert.AreEquivalent(new[] { OrderType.Limit, OrderType.StopMarket }, contingency.OrderTypes);
            foreach (var ticket in tickets)
            {
                Assert.AreSame(contingency.OrderIds, ticket.SubmitRequest.Contingency.OrderIds);
                var link = ticket.SubmitRequest.Contingency.Links.Single();
                Assert.AreEqual(1, link.Id);
                Assert.AreEqual(oneCancelsOther ? ContingencyType.OneCancelsOther : ContingencyType.OneUpdatesOther, link.Type);
                Assert.IsNull(link.Role);
                Assert.IsFalse(ticket.Contingency.IsWaitingForTrigger);
            }
        }

        [Test]
        public void EachSubmissionGetsANewManagerId()
        {
            var first = _algorithm.BracketOrder(_spy, 10, 110, 90, limitPrice: 99);
            var second = _algorithm.BracketOrder(_spy, 10, 110, 90, limitPrice: 99);

            Assert.AreEqual(1, first[0].Contingency.Id);
            Assert.AreEqual(2, second[0].Contingency.Id);
        }

        [Test]
        public void OneTriggersOtherChain()
        {
            // parent triggers two independent children, the first one triggers a one cancels other in turn
            var takeProfit = _algorithm.OrderFactory.LimitOrder(_aapl, -5, 250);
            var stopLoss = _algorithm.OrderFactory.StopMarketOrder(_aapl, -5, 150);
            var firstChild = _algorithm.OrderFactory.MarketOrder(_aapl, 5).Triggers(_algorithm.OrderFactory.OneCancelsOther(takeProfit, stopLoss));
            var secondChild = _algorithm.OrderFactory.LimitOrder(_spy, -10, 120);
            var parent = _algorithm.OrderFactory.LimitOrder(_spy, 10, 99);

            var tickets = _algorithm.OneTriggersOtherOrder(parent, new List<SubmitOrderRequest> { firstChild, secondChild });

            // parents first, depth first
            CollectionAssert.AreEqual(new[] { Ticket(parent), Ticket(firstChild), Ticket(takeProfit), Ticket(stopLoss), Ticket(secondChild) }, tickets);
            Assert.IsTrue(tickets.All(x => x.Contingency.Count == 5));
            // each order has its own contingency, the set is shared
            Assert.AreEqual(1, tickets.Select(x => x.Contingency.OrderIds).Distinct().Count());

            AssertContingencies(parent, (1, ContingencyType.OneTriggersOther, ContingencyRole.Parent));
            AssertContingencies(firstChild, (1, ContingencyType.OneTriggersOther, ContingencyRole.Child), (2, ContingencyType.OneTriggersOther, ContingencyRole.Parent));
            AssertContingencies(takeProfit, (2, ContingencyType.OneTriggersOther, ContingencyRole.Child), (3, ContingencyType.OneCancelsOther, null));
            AssertContingencies(stopLoss, (2, ContingencyType.OneTriggersOther, ContingencyRole.Child), (3, ContingencyType.OneCancelsOther, null));
            AssertContingencies(secondChild, (1, ContingencyType.OneTriggersOther, ContingencyRole.Child));

            Assert.IsFalse(Ticket(parent).Contingency.IsWaitingForTrigger);
            Assert.IsTrue(tickets.Skip(1).All(x => x.Contingency.IsWaitingForTrigger));
        }

        [Test]
        public void OneCancelsOtherEntriesEachWithItsOwnBracket()
        {
            var breakoutUp = _algorithm.OrderFactory.StopMarketOrder(_spy, 10, 105).Bracket(120, 100);
            var breakoutDown = _algorithm.OrderFactory.StopMarketOrder(_spy, -10, 95).Bracket(80, 100);

            var tickets = _algorithm.Order(_algorithm.OrderFactory.OneCancelsOther(breakoutUp, breakoutDown));

            Assert.AreEqual(6, tickets.Count);
            Assert.IsTrue(tickets.All(x => x.Contingency.Count == 6));
            // the contingency ids follow the composition: each bracket first, then the one cancels other relating the entries
            AssertContingencies(breakoutUp, (1, ContingencyType.OneTriggersOther, ContingencyRole.Parent), (5, ContingencyType.OneCancelsOther, null));
            AssertContingencies(breakoutDown, (3, ContingencyType.OneTriggersOther, ContingencyRole.Parent), (5, ContingencyType.OneCancelsOther, null));
            Assert.IsTrue(tickets.Skip(1).Take(2).All(ticket => ticket.Contingency.Links.Any(link => link.Id == 2 && link.Role == null)));
            Assert.IsTrue(tickets.Skip(4).All(ticket => ticket.Contingency.Links.Any(link => link.Id == 4 && link.Role == null)));
            Assert.IsFalse(Ticket(breakoutUp).Contingency.IsWaitingForTrigger);
            Assert.IsFalse(Ticket(breakoutDown).Contingency.IsWaitingForTrigger);
            Assert.AreEqual(4, tickets.Count(x => x.Contingency.IsWaitingForTrigger));
        }

        [Test]
        public void ComboOrdersInAContingency()
        {
            var legs = new List<Leg> { Leg.Create(_spy, 1), Leg.Create(_aapl, -1) };
            var exit = _algorithm.OrderFactory.ComboLimitOrder(legs, -2, 50);
            var parent = _algorithm.OrderFactory.ComboMarketOrder(legs, 2, asynchronous: true);
            // the legs are a single unit, they trigger together: all of them are required
            Assert.Throws<ArgumentException>(() => parent[1].Triggers(exit));
            var tickets = _algorithm.OneTriggersOtherOrder(parent, exit);
            Assert.IsTrue(parent.All(leg => leg.Contingency.Links.Single().Role == ContingencyRole.Parent));
            Assert.IsTrue(exit.All(leg => leg.Contingency.Links.Single().Role == ContingencyRole.Child));


            Assert.AreEqual(4, tickets.Count);
            CollectionAssert.AreEqual(tickets.Take(2), parent.Select(Ticket));
            CollectionAssert.AreEqual(tickets.Skip(2), exit.Select(Ticket));
            var requests = tickets.Select(x => x.SubmitRequest).ToList();
            Assert.IsTrue(requests.All(x => x.Contingency.Count == 4));
            CollectionAssert.AreEqual(new[] { OrderType.ComboMarket, OrderType.ComboMarket, OrderType.ComboLimit, OrderType.ComboLimit }, requests.Select(x => x.OrderType));
            CollectionAssert.AreEqual(new[] { 2m, -2m, -2m, 2m }, requests.Select(x => x.Quantity));

            // each combo has it's own group manager, shared by its legs
            Assert.AreSame(requests[0].GroupOrderManager, requests[1].GroupOrderManager);
            Assert.AreSame(requests[2].GroupOrderManager, requests[3].GroupOrderManager);
            Assert.AreNotEqual(requests[0].GroupOrderManager.Id, requests[2].GroupOrderManager.Id);
            Assert.AreEqual(50, requests[2].GroupOrderManager.LimitPrice);

            // each leg has the contingencies of its combo, their own instance
            foreach (var request in requests.Take(2))
            {
                var contingency = request.Contingency.Links.Single();
                Assert.AreEqual(ContingencyRole.Parent, contingency.Role);
            }
            Assert.AreNotSame(requests[0].Contingency.Links[0], requests[1].Contingency.Links[0]);
            Assert.IsTrue(requests.Skip(2).All(x => x.Contingency.Links.Single().Role == ContingencyRole.Child && x.Contingency.Links.Single().Id == 1));
        }

        [Test]
        public void ComboOrderTriggersOtherOrders()
        {
            var legs = new List<Leg> { Leg.Create(_spy, 1), Leg.Create(_aapl, -1) };
            var exit = _algorithm.OrderFactory.ComboLimitOrder(legs, -2, 50);
            var parent = _algorithm.OrderFactory.ComboMarketOrder(legs, 2, asynchronous: true);

            // the parent must be a single unit: one order or the legs of one combo order
            Assert.Throws<ArgumentException>(() => _algorithm.OneTriggersOtherOrder(parent.Concat(exit), new[] { _algorithm.OrderFactory.MarketOrder(_spy, 1) }));
            Assert.Throws<ArgumentException>(() => _algorithm.OneTriggersOtherOrder(new List<SubmitOrderRequest>(), exit));
            Assert.Throws<ArgumentException>(() => _algorithm.OneTriggersOtherOrder(new[] { _algorithm.OrderFactory.MarketOrder(_spy, 1), _algorithm.OrderFactory.MarketOrder(_aapl, 1) }, exit));

            var tickets = _algorithm.OneTriggersOtherOrder(parent, exit);

            Assert.AreEqual(4, tickets.Count);
            CollectionAssert.AreEqual(tickets.Take(2), parent.Select(Ticket));
            CollectionAssert.AreEqual(tickets.Skip(2), exit.Select(Ticket));
            Assert.IsTrue(parent.All(leg => leg.Contingency.Links.Single().Role == ContingencyRole.Parent));
            Assert.IsTrue(exit.All(leg => leg.Contingency.Links.Single().Role == ContingencyRole.Child));
            Assert.IsTrue(tickets.Skip(2).All(ticket => ticket.Contingency.IsWaitingForTrigger));
        }

        [Test]
        public void SubmitsUnrelatedOrdersTogether()
        {
            var plain = _algorithm.OrderFactory.LimitOrder(_spy, 10, 99);
            var combo = _algorithm.OrderFactory.ComboLimitOrder(new List<Leg> { Leg.Create(_spy, 1), Leg.Create(_aapl, -1) }, 2, 50);
            var bracket = _algorithm.OrderFactory.LimitOrder(_aapl, 5, 199).Bracket(210, 190);

            var tickets = _algorithm.Order(combo.Append(plain).Append(bracket));

            // each one on its own: the combo legs, the plain order and the whole bracket
            Assert.AreEqual(6, tickets.Count);
            CollectionAssert.AreEqual(combo.Append(plain).Append(bracket).Concat(bracket.Contingency.Requests.Skip(1)).Select(Ticket), tickets);
            Assert.IsTrue(combo.All(leg => leg.Contingency == null && leg.GroupOrderManager.Id > 0));
            Assert.IsNull(plain.Contingency);
            Assert.AreEqual(3, bracket.Contingency.Count);
            Assert.AreEqual(2, tickets.Count(ticket => ticket.Contingency?.IsWaitingForTrigger == true));
        }

        [Test]
        public void IncompleteComboIsRejected()
        {
            var plain = _algorithm.OrderFactory.LimitOrder(_spy, 10, 99);
            var combo = _algorithm.OrderFactory.ComboLimitOrder(new List<Leg> { Leg.Create(_spy, 1), Leg.Create(_aapl, -1) }, 2, 50);

            Assert.Throws<ArgumentException>(() => _algorithm.Order(new[] { plain, combo[1] }));

            // nothing was submitted
            Assert.IsTrue(new[] { plain }.Concat(combo).All(request => request.OrderId <= 0));
            Assert.IsEmpty(_algorithm.Transactions.GetOrders());

            // all the legs are fine
            Assert.AreEqual(3, _algorithm.Order(combo.Append(plain)).Count);
        }

        [Test]
        public void ExistingComboMethodsAreNotContingent()
        {
            var legs = new List<Leg> { Leg.Create(_spy, 1, 100), Leg.Create(_aapl, -1, 200) };
            var tickets = _algorithm.ComboLegLimitOrder(legs, 2);

            Assert.AreEqual(2, tickets.Count);
            Assert.IsTrue(tickets.All(x => x.SubmitRequest.OrderType == OrderType.ComboLegLimit && x.SubmitRequest.Contingency == null
                && x.SubmitRequest.GroupOrderManager.Count == 2));
            CollectionAssert.AreEqual(new[] { 100m, 200m }, tickets.Select(x => x.SubmitRequest.LimitPrice));

            Assert.Throws<ArgumentException>(() => _algorithm.ComboLegLimitOrder(new List<Leg> { Leg.Create(_spy, 1) }, 1));
            Assert.Throws<ArgumentException>(() => _algorithm.ComboLimitOrder(legs, 1, 10));
            Assert.Throws<ArgumentException>(() => _algorithm.ComboLimitOrder(new List<Leg> { Leg.Create(_spy, 1) }, 1, 0));
        }

        [Test]
        public void HeldTrailingStopPriceIsSetOnceTriggered()
        {
            var trailingStop = _algorithm.OrderFactory.TrailingStopOrder(_spy, -10, 0.1m, true);
            var explicitTrailingStop = _algorithm.OrderFactory.TrailingStopOrder(_spy, -10, 85, 0.1m, true);
            _algorithm.Order(_algorithm.OrderFactory.LimitOrder(_spy, 10, 99).Triggers(trailingStop, explicitTrailingStop));

            Assert.AreEqual(0, trailingStop.StopPrice);
            Assert.AreEqual(0.1m, trailingStop.TrailingAmount);
            Assert.IsTrue(trailingStop.TrailingAsPercentage);
            Assert.AreEqual(85, explicitTrailingStop.StopPrice);

            // when working right away it's calculated from the current price
            var working = _algorithm.OrderFactory.TrailingStopOrder(_spy, -10, 0.1m, true);
            _algorithm.Order(working);
            Assert.AreEqual(90, working.StopPrice);
            Assert.AreEqual(90, Ticket(working).SubmitRequest.StopPrice);
        }

        [Test]
        public void HeldMarketOrdersAreNotConverted()
        {
            // market is closed
            _algorithm.SetDateTime(new DateTime(2024, 1, 3, 3, 0, 0));
            var child = _algorithm.OrderFactory.MarketOrder(_spy, -10);
            var parent = _algorithm.OrderFactory.MarketOrder(_spy, 10).Triggers(child);

            _algorithm.Order(parent);

            // the working market order is converted into market on open, as usual
            Assert.AreEqual(OrderType.MarketOnOpen, parent.OrderType);
            Assert.AreEqual(OrderType.Market, child.OrderType);
        }

        [Test]
        public void OrderRequestCanOnlyBeSubmittedOnce()
        {
            var request = _algorithm.OrderFactory.LimitOrder(_spy, 10, 99);
            _algorithm.Order(request);

            Assert.Throws<ArgumentException>(() => _algorithm.Order(request));
            Assert.Throws<ArgumentException>(() => _algorithm.Order(_algorithm.OrderFactory.LimitOrder(_spy, 10, 99).Triggers(request)));
            Assert.Throws<ArgumentException>(() => request.Triggers(_algorithm.OrderFactory.MarketOrder(_spy, 1)));
            Assert.Throws<ArgumentException>(() => _algorithm.OrderFactory.OneCancelsOther(request, _algorithm.OrderFactory.MarketOrder(_spy, 1)));

            // present twice
            var repeated = _algorithm.OrderFactory.LimitOrder(_spy, 10, 99);
            Assert.Throws<ArgumentException>(() => _algorithm.Order(_algorithm.OrderFactory.LimitOrder(_spy, 10, 99).Triggers(repeated, repeated)));
        }

        [Test]
        public void InvalidRequests()
        {
            Assert.IsEmpty(_algorithm.Order(new List<SubmitOrderRequest>()));
            Assert.Throws<ArgumentException>(() => _algorithm.OrderFactory.OneCancelsOther(_algorithm.OrderFactory.MarketOrder(_spy, 1)));
            Assert.Throws<ArgumentException>(() => _algorithm.OrderFactory.OneCancelsOther());
            Assert.Throws<ArgumentException>(() => _algorithm.OrderFactory.OneUpdatesOther(_algorithm.OrderFactory.MarketOrder(_spy, 1), null));
            Assert.Throws<ArgumentException>(() => _algorithm.OrderFactory.MarketOrder(_spy, 1).Triggers());
            Assert.Throws<ArgumentException>(() => _algorithm.OrderFactory.MarketOrder(_spy, 1).Triggers(null, null));
            Assert.Throws<InvalidOperationException>(() => _algorithm.OrderFactory.ComboMarketOrder(new List<Leg> { Leg.Create(_spy, 1) }, 1)[0].Bracket(1, 2));
            Assert.Throws<ArgumentException>(() => _algorithm.OrderFactory.ComboMarketOrder(new List<Leg>(), 1));
            // a combo market order has no prices, per leg prices are a combo leg limit order
            Assert.Throws<ArgumentException>(() => _algorithm.OrderFactory.ComboMarketOrder(new List<Leg> { Leg.Create(_spy, 1, 100), Leg.Create(_aapl, -1) }, 1));
            // all the legs of a combo order are required
            Assert.Throws<ArgumentException>(() => _algorithm.Order(_algorithm.OrderFactory.ComboMarketOrder(new List<Leg> { Leg.Create(_spy, 1), Leg.Create(_aapl, -1) }, 1)[0]));
            // only options can be exercised
            Assert.Throws<ArgumentException>(() => _algorithm.OrderFactory.ExerciseOption(_spy, 1));
            Assert.Throws<ArgumentException>(() => _algorithm.OneTriggersOtherOrder((SubmitOrderRequest)null, new List<SubmitOrderRequest>()));

            // the legs of a combo order are a single order to relate
            var legs = new List<Leg> { Leg.Create(_spy, 1), Leg.Create(_aapl, -1) };
            Assert.Throws<ArgumentException>(() => _algorithm.OrderFactory.OneCancelsOther(_algorithm.OrderFactory.ComboMarketOrder(legs, 1)));

            // orders can only be related once
            var related = _algorithm.OrderFactory.OneCancelsOther(_algorithm.OrderFactory.LimitOrder(_spy, 10, 99), _algorithm.OrderFactory.LimitOrder(_spy, 10, 98));
            Assert.Throws<ArgumentException>(() => _algorithm.OrderFactory.OneUpdatesOther(related[0], _algorithm.OrderFactory.LimitOrder(_spy, 10, 97)));

            // an exercise is not a working order, it can't be part of a set of contingent orders in any role
            var exercise = _algorithm.OrderFactory.ExerciseOption(Symbols.SPY_C_192_Feb19_2016, 1);
            Assert.Throws<ArgumentException>(() => _algorithm.Order(_algorithm.OrderFactory.LimitOrder(_spy, 10, 99).Triggers(exercise)));
            Assert.Throws<ArgumentException>(() => _algorithm.Order(_algorithm.OrderFactory.ExerciseOption(Symbols.SPY_C_192_Feb19_2016, 1).Triggers(_algorithm.OrderFactory.MarketOrder(_spy, 1))));
            Assert.Throws<ArgumentException>(() => _algorithm.OneCancelsOtherOrder(new List<SubmitOrderRequest> { _algorithm.OrderFactory.ExerciseOption(Symbols.SPY_C_192_Feb19_2016, 1), _algorithm.OrderFactory.LimitOrder(_spy, 10, 99) }));
        }

        [Test]
        public void NothingIsSubmittedIfAnyOrderFailsPreOrderChecks()
        {
            var processor = new FakeOrderProcessor();
            _algorithm.Transactions.SetOrderProcessor(processor);

            // the stop loss has zero quantity
            var invalid = _algorithm.OrderFactory.StopMarketOrder(_spy, 0, 90);
            var entry = _algorithm.OrderFactory.LimitOrder(_spy, 10, 99).Triggers(_algorithm.OrderFactory.OneCancelsOther(_algorithm.OrderFactory.LimitOrder(_spy, -10, 110), invalid));

            var tickets = _algorithm.Order(entry);

            var ticket = tickets.Single();
            Assert.AreEqual(OrderStatus.Invalid, ticket.Status);
            Assert.AreEqual(OrderResponseErrorCode.OrderQuantityZero, ticket.SubmitRequest.Response.ErrorCode);
            Assert.IsEmpty(processor.ProcessedOrdersRequests);
            Assert.IsFalse(entry.OrderId > 0);
            Assert.IsNull(Ticket(entry));
        }

        [Test]
        public void BracketBuildsTheExits()
        {
            var entry = _algorithm.OrderFactory.LimitOrder(_spy, 10, 99).Bracket(110, 90, stopLossLimitPrice: 89);

            var exits = entry.Contingency.Requests.Skip(1).ToList();
            Assert.AreEqual(2, exits.Count);
            var takeProfit = exits[0];
            var stopLoss = exits[1];
            Assert.AreEqual(OrderType.Limit, takeProfit.OrderType);
            Assert.AreEqual(-10, takeProfit.Quantity);
            Assert.AreEqual(110, takeProfit.LimitPrice);
            Assert.AreEqual(OrderType.StopLimit, stopLoss.OrderType);
            Assert.AreEqual(-10, stopLoss.Quantity);
            Assert.AreEqual(90, stopLoss.StopPrice);
            Assert.AreEqual(89, stopLoss.LimitPrice);
            // not submitted yet: composed, without a set id
            Assert.IsTrue(exits.All(exit => exit.OrderId <= 0 && exit.Contingency.Id == 0 && exit.Time == _algorithm.UtcTime));
            Assert.AreEqual(2, entry.Contingency.Links.Count(link => link.Role == ContingencyRole.Parent) + exits.Count(exit => exit.Contingency.Links[0].Role == ContingencyRole.Child) - 1);
        }

        private OrderTicket Ticket(SubmitOrderRequest request)
        {
            return _algorithm.Transactions.GetOrderTicket(request.OrderId);
        }

        private static void AssertBracket(List<SubmitOrderRequest> requests, ContingencyType exitsContingencyType)
        {
            var contingency = requests[0].Contingency;
            Assert.IsNotNull(contingency);
            Assert.Greater(contingency.Id, 0);
            Assert.AreEqual(3, contingency.Count);
            // each order has its own contingency, the set is shared
            Assert.IsTrue(requests.All(x => x.Contingency.Id == contingency.Id && ReferenceEquals(x.Contingency.OrderIds, contingency.OrderIds)));
            Assert.AreEqual(1, contingency.Symbols.Count);
            Assert.AreEqual(2, contingency.Directions.Count);

            var parent = requests[0].Contingency.Links.Single();
            Assert.AreEqual(ContingencyType.OneTriggersOther, parent.Type);
            Assert.AreEqual(ContingencyRole.Parent, parent.Role);

            foreach (var request in requests.Skip(1))
            {
                Assert.AreEqual(2, request.Contingency.Links.Count);
                var child = request.Contingency.Links.Single(x => x.Role == ContingencyRole.Child);
                Assert.AreEqual(parent.Id, child.Id);
                Assert.IsFalse(child.Triggered);
                var member = request.Contingency.Links.Single(x => x.Role == null);
                Assert.AreEqual(exitsContingencyType, member.Type);
                Assert.AreNotEqual(parent.Id, member.Id);
            }
            Assert.AreEqual(requests[1].Contingency.Links.Single(x => x.Role == null).Id,
                requests[2].Contingency.Links.Single(x => x.Role == null).Id);
        }

        private static void AssertContingencies(SubmitOrderRequest request, params (int Id, ContingencyType Type, ContingencyRole? Role)[] expected)
        {
            CollectionAssert.AreEqual(expected, request.Contingency.Links.Select(link => (link.Id, link.Type, link.Role)));
        }

        private Symbol AddEquity(string ticker, decimal price)
        {
            var security = _algorithm.AddEquity(ticker);
            security.SetMarketPrice(new TradeBar(_algorithm.Time, security.Symbol, price, price, price, price, 100));
            return security.Symbol;
        }
    }
}
