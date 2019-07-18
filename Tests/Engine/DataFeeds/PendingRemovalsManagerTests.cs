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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Orders;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class PendingRemovalsManagerTests
    {
        [Test]
        public void ReturnedRemoved_Add()
        {
            var orderProvider = new FakeOrderProcessor();
            var pendingRemovals = new PendingRemovalsManager(orderProvider);
            var security = SecurityTests.GetSecurity();
            var universe = new TestUniverse();

            var result = pendingRemovals.TryRemoveMember(security, universe);

            Assert.IsTrue(result.Any());
            Assert.AreEqual(universe, result.First().Universe);
            Assert.AreEqual(security, result.First().Security);
            Assert.IsFalse(pendingRemovals.CheckPendingRemovals(new HashSet<Symbol>(), universe).Any());
            Assert.AreEqual(0, pendingRemovals.PendingRemovals.Keys.Count());
            Assert.AreEqual(0, pendingRemovals.PendingRemovals.Values.Count());
        }

        [Test]
        public void ReturnedRemoved_Check()
        {
            var orderProvider = new FakeOrderProcessor();
            var pendingRemovals = new PendingRemovalsManager(orderProvider);
            var security = SecurityTests.GetSecurity();
            var universe = new TestUniverse();
            orderProvider.AddOrder(new LimitOrder(security.Symbol, 1, 1, DateTime.UtcNow));
            pendingRemovals.TryRemoveMember(security, universe);
            orderProvider.Clear();

            var result = pendingRemovals.CheckPendingRemovals(new HashSet<Symbol>(), universe);

            Assert.IsTrue(result.Any());
            Assert.AreEqual(universe, result.First().Universe);
            Assert.AreEqual(security, result.First().Security);
            Assert.IsFalse(pendingRemovals.CheckPendingRemovals(new HashSet<Symbol>(), universe).Any());
            Assert.AreEqual(0, pendingRemovals.PendingRemovals.Keys.Count());
            Assert.AreEqual(0, pendingRemovals.PendingRemovals.Values.Count());
        }

        [Test]
        public void WontRemoveBecauseOpenOrder_Add()
        {
            var orderProvider = new FakeOrderProcessor();
            var pendingRemovals = new PendingRemovalsManager(orderProvider);
            var security = SecurityTests.GetSecurity();
            var universe = new TestUniverse();
            orderProvider.AddOrder(new LimitOrder(security.Symbol, 1, 1, DateTime.UtcNow));

            Assert.IsFalse(pendingRemovals.TryRemoveMember(security, universe).Any());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Keys.Count());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Values.Count());
            Assert.AreEqual(universe, pendingRemovals.PendingRemovals.Keys.First());
            Assert.AreEqual(security, pendingRemovals.PendingRemovals.Values.First().First());
        }

        [Test]
        public void WontRemoveBecauseOpenOrder_Check()
        {
            var orderProvider = new FakeOrderProcessor();
            var pendingRemovals = new PendingRemovalsManager(orderProvider);
            var security = SecurityTests.GetSecurity();
            var universe = new TestUniverse();
            orderProvider.AddOrder(new LimitOrder(security.Symbol, 1, 1, DateTime.UtcNow));

            Assert.IsFalse(pendingRemovals.TryRemoveMember(security, universe).Any());
            Assert.IsFalse(pendingRemovals.CheckPendingRemovals(new HashSet<Symbol>(), universe).Any());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Keys.Count());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Values.Count());
            Assert.AreEqual(universe, pendingRemovals.PendingRemovals.Keys.First());
            Assert.AreEqual(security, pendingRemovals.PendingRemovals.Values.First().First());
        }

        [Test]
        public void WontRemoveBecauseHoldings_Add()
        {
            var orderProvider = new FakeOrderProcessor();
            var pendingRemovals = new PendingRemovalsManager(orderProvider);
            var security = SecurityTests.GetSecurity();
            var universe = new TestUniverse();
            security.Holdings.SetHoldings(10, 10);

            Assert.IsFalse(pendingRemovals.TryRemoveMember(security, universe).Any());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Keys.Count());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Values.Count());
            Assert.AreEqual(universe, pendingRemovals.PendingRemovals.Keys.First());
            Assert.AreEqual(security, pendingRemovals.PendingRemovals.Values.First().First());
        }

        [Test]
        public void WontRemoveBecauseHoldings_Check()
        {
            var orderProvider = new FakeOrderProcessor();
            var pendingRemovals = new PendingRemovalsManager(orderProvider);
            var security = SecurityTests.GetSecurity();
            var universe = new TestUniverse();
            security.Holdings.SetHoldings(10, 10);

            Assert.IsFalse(pendingRemovals.TryRemoveMember(security, universe).Any());
            Assert.IsFalse(pendingRemovals.CheckPendingRemovals(new HashSet<Symbol>(), universe).Any());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Keys.Count());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Values.Count());
            Assert.AreEqual(universe, pendingRemovals.PendingRemovals.Keys.First());
            Assert.AreEqual(security, pendingRemovals.PendingRemovals.Values.First().First());
        }

        [Test]
        public void WontRemoveBecauseTarget_Add()
        {
            var orderProvider = new FakeOrderProcessor();
            var pendingRemovals = new PendingRemovalsManager(orderProvider);
            var security = SecurityTests.GetSecurity();
            var universe = new TestUniverse();
            security.Holdings.Target = new PortfolioTarget(security.Symbol, 10);

            Assert.IsFalse(pendingRemovals.TryRemoveMember(security, universe).Any());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Keys.Count());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Values.Count());
            Assert.AreEqual(universe, pendingRemovals.PendingRemovals.Keys.First());
            Assert.AreEqual(security, pendingRemovals.PendingRemovals.Values.First().First());
        }

        [Test]
        public void WontRemoveBecauseTarget_Check()
        {
            var orderProvider = new FakeOrderProcessor();
            var pendingRemovals = new PendingRemovalsManager(orderProvider);
            var security = SecurityTests.GetSecurity();
            var universe = new TestUniverse();
            security.Holdings.Target = new PortfolioTarget(security.Symbol, 10);

            Assert.IsFalse(pendingRemovals.TryRemoveMember(security, universe).Any());
            Assert.IsFalse(pendingRemovals.CheckPendingRemovals(new HashSet<Symbol>(), universe).Any());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Keys.Count());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Values.Count());
            Assert.AreEqual(universe, pendingRemovals.PendingRemovals.Keys.First());
            Assert.AreEqual(security, pendingRemovals.PendingRemovals.Values.First().First());
        }

        [Test]
        public void WontBeReturnedBecauseReSelected()
        {
            var orderProvider = new FakeOrderProcessor();
            var pendingRemovals = new PendingRemovalsManager(orderProvider);
            var security = SecurityTests.GetSecurity();
            var universe = new TestUniverse();
            orderProvider.AddOrder(new LimitOrder(security.Symbol, 1, 1, DateTime.UtcNow));
            pendingRemovals.TryRemoveMember(security, universe);

            Assert.IsFalse(pendingRemovals.CheckPendingRemovals(
                new HashSet<Symbol> { security.Symbol}, universe).Any());

            // internally it was removed because it was reselected
            Assert.AreEqual(0, pendingRemovals.PendingRemovals.Keys.Count());
            Assert.AreEqual(0, pendingRemovals.PendingRemovals.Values.Count());
        }

        private class TestUniverse : Universe
        {
            public TestUniverse()
                : base(SecurityTests.CreateTradeBarConfig())
            {
            }

            public override UniverseSettings UniverseSettings { get; }
            public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
            {
                throw new NotImplementedException();
            }
        }
    }
}
