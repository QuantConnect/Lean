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
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class ContingentOrderProcessorTests
    {
        private static readonly DateTime Time = new DateTime(2024, 1, 2, 15, 0, 0);
        private Dictionary<int, Order> _orders;
        private Dictionary<int, decimal> _filledQuantity;
        private Dictionary<Symbol, Security> _securities;
        private ContingentOrderProcessor _processor;

        [SetUp]
        public void SetUp()
        {
            _orders = new();
            _filledQuantity = new();
            _securities = new();
            _processor = new ContingentOrderProcessor(id => _filledQuantity.GetValueOrDefault(id), new TestSecurityProvider(_securities));
        }

        [Test]
        public void IgnoresNonContingentOrders()
        {
            var order = Add(new MarketOrder(Symbols.SPY, 1, Time) { Id = 1 });
            Assert.IsNull(Process(new[] { Fill(order) }));
            Assert.IsTrue(IsWorking(order, Time));
        }

        [Test]
        public void ParentFillTriggersItsChildren()
        {
            var bracket = Add(ContingentOrderTests.CreateBracket());

            var actions = Process(new[] { Fill(bracket[0]) });

            CollectionAssert.AreEqual(new[] { bracket[1], bracket[2] }, actions.ToTrigger);
            Assert.IsEmpty(actions.ToCancel);
            Assert.IsEmpty(actions.ToUpdateQuantity);
        }

        [Test]
        public void ParentPartialFillDoesNotTriggerItsChildren()
        {
            var bracket = Add(ContingentOrderTests.CreateBracket());
            Assert.IsNull(Process(new[] { Fill(bracket[0], 40) }));
        }

        [TestCase(OrderStatus.Canceled)]
        [TestCase(OrderStatus.Invalid)]
        public void ParentClosedCancelsItsHeldChildren(OrderStatus status)
        {
            var bracket = Add(ContingentOrderTests.CreateBracket());
            bracket[0].Status = status;

            var actions = Process(new[] { new OrderEvent(bracket[0], Time, OrderFee.Zero) { Status = status } });

            CollectionAssert.AreEqual(new[] { bracket[1], bracket[2] }, actions.ToCancel.Select(x => x.Key));
            Assert.IsTrue(actions.ToCancel.All(x => x.Value.Contains($"Contingent parent order 1 was {status.ToString().ToLowerInvariant()}", StringComparison.InvariantCulture)));
            Assert.IsEmpty(actions.ToTrigger);
        }

        [Test]
        public void AlreadyClosedChildrenAreIgnored()
        {
            var bracket = Add(ContingentOrderTests.CreateBracket());
            bracket[1].Status = OrderStatus.Canceled;

            var actions = Process(new[] { Fill(bracket[0]) });
            CollectionAssert.AreEqual(new[] { bracket[2] }, actions.ToTrigger);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void OneCancelsOtherFillCancelsSiblings(bool partialFill)
        {
            var bracket = Add(ContingentOrderTests.CreateBracket());

            var actions = Process(new[] { Fill(bracket[1], partialFill ? -40 : null) });

            CollectionAssert.AreEqual(new[] { bracket[2] }, actions.ToCancel.Select(x => x.Key));
            StringAssert.Contains("Contingent sibling order 2 was filled", actions.ToCancel[0].Value);
            Assert.IsEmpty(actions.ToTrigger);
            Assert.IsEmpty(actions.ToUpdateQuantity);
        }

        [TestCase(OrderStatus.Canceled)]
        [TestCase(OrderStatus.Invalid)]
        public void ClosedSiblingCancelsTheRest(OrderStatus status)
        {
            // the contingency is canceled as a whole, like brokerages do, whether the members are held or working
            var bracket = Add(ContingentOrderTests.CreateBracket());
            bracket[1].Status = status;

            var actions = Process(new[] { new OrderEvent(bracket[1], Time, OrderFee.Zero) { Status = status } });

            CollectionAssert.AreEqual(new[] { bracket[2] }, actions.ToCancel.Select(x => x.Key));
            StringAssert.Contains($"Contingent sibling order 2 was {status.ToString().ToLowerInvariant()}", actions.ToCancel[0].Value);
            Assert.IsEmpty(actions.ToTrigger);

            // the parent is not affected
            Assert.AreEqual(OrderStatus.Submitted, bracket[0].Status);
        }

        [Test]
        public void OneUpdatesOtherPartialFillReducesSiblingsProportionally()
        {
            var bracket = Add(ContingentOrderTests.CreateBracket(exitsContingencyType: ContingencyType.OneUpdatesOther));
            // the stop loss has twice the size
            bracket[2].Quantity = -200;

            // 40 out of 100
            var actions = Process(new[] { Fill(bracket[1], -40) });
            Assert.IsEmpty(actions.ToCancel);
            var update = actions.ToUpdateQuantity.Single();
            Assert.AreSame(bracket[2], update.Key);
            Assert.AreEqual(-120, update.Value);
            bracket[2].Quantity = update.Value;

            // 40 out of the remaining 60
            actions = Process(new[] { Fill(bracket[1], -40) });
            Assert.AreEqual(-40, actions.ToUpdateQuantity.Single().Value);

            // completely filled: cancels the sibling
            actions = Process(new[] { Fill(bracket[1]) });
            Assert.IsEmpty(actions.ToUpdateQuantity);
            CollectionAssert.AreEqual(new[] { bracket[2] }, actions.ToCancel.Select(x => x.Key));
        }

        [Test]
        public void OneUpdatesOtherTakesSiblingFillsIntoAccount()
        {
            var bracket = Add(ContingentOrderTests.CreateBracket(exitsContingencyType: ContingencyType.OneUpdatesOther));
            // the stop loss already filled 20, 80 remaining
            _filledQuantity[bracket[2].Id] = -20;

            // take profit fills half => the stop loss remaining is halved too: 20 filled + 40 remaining
            var actions = Process(new[] { Fill(bracket[1], -50) });
            Assert.AreEqual(-60, actions.ToUpdateQuantity.Single().Value);
        }

        [Test]
        public void OneUpdatesOtherRespectsLotSize()
        {
            var lotSize = 0.001m;
            CreateSecurity(Symbols.BTCUSD, lotSize);

            var manager = new OrderContingency(1, 2, []);
            var first = Add(new LimitOrder(Symbols.BTCUSD, -1m, 100, Time)
            {
                Contingency = manager.WithLinks([new(1, ContingencyType.OneUpdatesOther)]), Status = OrderStatus.Submitted, Id = 1
            });
            var second = Add(new StopMarketOrder(Symbols.BTCUSD, -1m, 50, Time)
            {
                Contingency = manager.WithLinks([new(1, ContingencyType.OneUpdatesOther)]), Status = OrderStatus.Submitted, Id = 2
            });

            var actions = Process(new[] { Fill(first, -1m / 3) });

            var newQuantity = actions.ToUpdateQuantity.Single().Value;
            Assert.AreEqual(0, newQuantity % lotSize);
            Assert.AreEqual((double)(-2m / 3), (double)newQuantity, (double)lotSize);
        }

        [Test]
        public void ComboParentRequiresAllLegsFilled()
        {
            var manager = new OrderContingency(1, 3, []);
            var combo = new GroupOrderManager(1, 2, 1);
            var firstLeg = Add(new ComboMarketOrder(Symbols.SPY, 1, Time, combo)
            {
                Contingency = manager.WithLinks([new(1, ContingencyType.OneTriggersOther, ContingencyRole.Parent)]), Status = OrderStatus.Submitted, Id = 1
            });
            var secondLeg = Add(new ComboMarketOrder(Symbols.AAPL, -1, Time, combo)
            {
                Contingency = manager.WithLinks([new(1, ContingencyType.OneTriggersOther, ContingencyRole.Parent)]), Status = OrderStatus.Submitted, Id = 2
            });
            var child = Add(new MarketOrder(Symbols.SPY, -1, Time)
            {
                Contingency = manager.WithLinks([new(1, ContingencyType.OneTriggersOther, ContingencyRole.Child)]), Status = OrderStatus.Submitted, Id = 3
            });

            // a single leg filled
            Assert.IsNull(Process(new[] { Fill(firstLeg) }));

            // both legs filled
            var actions = Process(new[] { Fill(firstLeg), Fill(secondLeg) });
            CollectionAssert.AreEqual(new[] { child }, actions.ToTrigger);
        }

        [Test]
        public void HeldOrdersAreNotWorking()
        {
            var bracket = Add(ContingentOrderTests.CreateBracket());

            Assert.IsTrue(IsWorking(bracket[0], Time));
            Assert.IsFalse(IsWorking(bracket[1], Time));
            Assert.IsFalse(IsWorking(bracket[2], Time.AddDays(10)));
        }

        [Test]
        public void TriggeredOrdersRequireNewDataToBeWorking()
        {
            var security = CreateSecurity(Symbols.SPY, 1);
            var bracket = Add(ContingentOrderTests.CreateBracket());

            var triggeredTime = Time.AddMinutes(1);
            foreach (var order in bracket.Skip(1))
            {
                var child = order.GetContingencyLink(ContingencyRole.Child);
                child.TriggeredTime = triggeredTime;
                child.Triggered = true;
            }

            // no data at all
            Assert.IsFalse(IsWorking(bracket[1], triggeredTime.AddMinutes(1)));

            // data from before being triggered
            var exchangeTimeZone = security.Exchange.TimeZone;
            security.SetMarketPrice(new TradeBar(triggeredTime.ConvertFromUtc(exchangeTimeZone).AddMinutes(-1), Symbols.SPY, 100, 100, 100, 100, 1, TimeSpan.FromMinutes(1)));
            Assert.IsFalse(IsWorking(bracket[1], triggeredTime.AddMinutes(1)));

            // new data but same time step it was triggered
            security.SetMarketPrice(new TradeBar(triggeredTime.ConvertFromUtc(exchangeTimeZone), Symbols.SPY, 100, 100, 100, 100, 1, TimeSpan.FromMinutes(1)));
            Assert.IsFalse(IsWorking(bracket[1], triggeredTime));

            Assert.IsTrue(IsWorking(bracket[1], triggeredTime.AddMinutes(1)));
            Assert.IsTrue(IsWorking(bracket[2], triggeredTime.AddMinutes(1)));
        }

        [Test]
        public void TriggeredMarketOrdersAreWorkingRightAway()
        {
            var manager = new OrderContingency(1, 1, []);
            var order = new MarketOrder(Symbols.SPY, 1, Time)
            {
                Contingency = manager.WithLinks([new(1, ContingencyType.OneTriggersOther, ContingencyRole.Child, true, Time.AddMinutes(1))]),
                Id = 1
            };

            Assert.IsTrue(IsWorking(order, Time.AddMinutes(1)));
        }

        private bool IsWorking(Order order, DateTime utcTime)
        {
            return BacktestingBrokerage.IsWorking(order, utcTime, new TestSecurityProvider(_securities));
        }

        private Actions Process(IReadOnlyList<OrderEvent> orderEvents)
        {
            var (updates, cancels) = _processor.Process(orderEvents, id => _orders.GetValueOrDefault(id), Time);
            if (updates == null && cancels == null)
            {
                return null;
            }
            return new Actions(
                updates?.Where(x => x.ContingencyTriggered).Select(x => _orders[x.OrderId]).ToList() ?? new(),
                cancels?.Select(x => KeyValuePair.Create(_orders[x.OrderId], x.Message)).ToList() ?? new(),
                updates?.Where(x => x.Quantity.HasValue).Select(x => KeyValuePair.Create(_orders[x.OrderId], x.Quantity.Value)).ToList() ?? new());
        }

        private record Actions(List<Order> ToTrigger, List<KeyValuePair<Order, string>> ToCancel, List<KeyValuePair<Order, decimal>> ToUpdateQuantity);

        private OrderEvent Fill(Order order, decimal? partialQuantity = null)
        {
            var fillQuantity = partialQuantity ?? order.Quantity - _filledQuantity.GetValueOrDefault(order.Id);
            _filledQuantity[order.Id] = _filledQuantity.GetValueOrDefault(order.Id) + fillQuantity;
            order.Status = partialQuantity.HasValue ? OrderStatus.PartiallyFilled : OrderStatus.Filled;
            return new OrderEvent(order, Time, OrderFee.Zero) { Status = order.Status, FillQuantity = fillQuantity, FillPrice = 100 };
        }

        private T Add<T>(T order) where T : Order
        {
            _orders[order.Id] = order;
            return order;
        }

        private List<Order> Add(List<Order> orders)
        {
            foreach (var order in orders)
            {
                Add(order);
            }
            return orders;
        }

        private Security CreateSecurity(Symbol symbol, decimal lotSize)
        {
            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
            var security = new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), config, new Cash(Currencies.USD, 0, 1m),
                new SymbolProperties(symbol.Value, Currencies.USD, 1, 0.01m, lotSize, symbol.Value), ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null, new SecurityCache());
            _securities[symbol] = security;
            return security;
        }

        private class TestSecurityProvider : ISecurityProvider
        {
            private readonly Dictionary<Symbol, Security> _securities;
            public TestSecurityProvider(Dictionary<Symbol, Security> securities)
            {
                _securities = securities;
            }
            public Security GetSecurity(Symbol symbol)
            {
                return _securities.GetValueOrDefault(symbol);
            }
        }
    }
}
