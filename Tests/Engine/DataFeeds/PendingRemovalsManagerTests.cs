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
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class PendingRemovalsManagerTests
    {
        private static readonly DateTime Noon = new DateTime(2015, 11, 2, 12, 0, 0);
        private static readonly TimeKeeper TimeKeeper = new TimeKeeper(Noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });

        [Test]
        public void ReturnedRemoved_Add()
        {
            var orderProvider = new FakeOrderProcessor();
            var pendingRemovals = new PendingRemovalsManager(orderProvider);
            var security = SecurityTests.GetSecurity();
            using var universe = new TestUniverse();

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
            using var universe = new TestUniverse();
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
        public void WontRemoveBecauseOfUnderlying()
        {
            var orderProvider = new FakeOrderProcessor();
            var pendingRemovals = new PendingRemovalsManager(orderProvider);
            var equity = CreateSecurity(Symbols.SPY);
            var equityOption = CreateSecurity(Symbols.SPY_C_192_Feb19_2016);

            // we add an order of the equity option
            orderProvider.AddOrder(new LimitOrder(equityOption.Symbol, 1, 1, DateTime.UtcNow));
            using var universe = new TestUniverse();
            universe.AddMember(DateTime.UtcNow, equity, false);
            universe.AddMember(DateTime.UtcNow, equityOption, false);

            // we try to remove the equity
            Assert.IsNull(pendingRemovals.TryRemoveMember(equity, universe));
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Keys.Count());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Values.Count());
            Assert.AreEqual(universe, pendingRemovals.PendingRemovals.Keys.First());
            Assert.AreEqual(equity, pendingRemovals.PendingRemovals.Values.First().First());
        }

        [Test]
        public void WontRemoveBecauseOpenOrder_Add()
        {
            var orderProvider = new FakeOrderProcessor();
            var pendingRemovals = new PendingRemovalsManager(orderProvider);
            var security = SecurityTests.GetSecurity();
            using var universe = new TestUniverse();
            orderProvider.AddOrder(new LimitOrder(security.Symbol, 1, 1, DateTime.UtcNow));

            Assert.IsNull(pendingRemovals.TryRemoveMember(security, universe));
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
            using var universe = new TestUniverse();
            orderProvider.AddOrder(new LimitOrder(security.Symbol, 1, 1, DateTime.UtcNow));

            Assert.IsNull(pendingRemovals.TryRemoveMember(security, universe));
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
            using var universe = new TestUniverse();
            security.Holdings.SetHoldings(10, 10);

            Assert.IsNull(pendingRemovals.TryRemoveMember(security, universe));
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
            using var universe = new TestUniverse();
            security.Holdings.SetHoldings(10, 10);

            Assert.IsNull(pendingRemovals.TryRemoveMember(security, universe));
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
            using var universe = new TestUniverse();
            security.Holdings.Target = new PortfolioTarget(security.Symbol, 10);

            Assert.IsNull(pendingRemovals.TryRemoveMember(security, universe));
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
            using var universe = new TestUniverse();
            security.Holdings.Target = new PortfolioTarget(security.Symbol, 10);

            Assert.IsNull(pendingRemovals.TryRemoveMember(security, universe));
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
            using var universe = new TestUniverse();
            orderProvider.AddOrder(new LimitOrder(security.Symbol, 1, 1, DateTime.UtcNow));
            pendingRemovals.TryRemoveMember(security, universe);

            Assert.IsFalse(pendingRemovals.CheckPendingRemovals(
                new HashSet<Symbol> { security.Symbol}, universe).Any());

            // internally it was removed because it was reselected
            Assert.AreEqual(0, pendingRemovals.PendingRemovals.Keys.Count());
            Assert.AreEqual(0, pendingRemovals.PendingRemovals.Values.Count());
        }

        [Test]
        public void WontRemoveBecauseUnsettledFunds()
        {
            var orderProvider = new FakeOrderProcessor();
            var pendingRemovals = new PendingRemovalsManager(orderProvider);
            using var universe = new TestUniverse();
            var security = SecurityTests.GetSecurity();

            security.SetSettlementModel(new DelayedSettlementModel(1, TimeSpan.FromHours(8)));
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(null, securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions, new AlgorithmSettings());
            security.SettlementModel.ApplyFunds(new ApplyFundsSettlementModelParameters(portfolio, security, TimeKeeper.UtcTime.Date.AddDays(1),
                new CashAmount(1000, Currencies.USD), null));

            Assert.IsNull(pendingRemovals.TryRemoveMember(security, universe));
            Assert.IsFalse(pendingRemovals.CheckPendingRemovals(new HashSet<Symbol>(), universe).Any());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Keys.Count());
            Assert.AreEqual(1, pendingRemovals.PendingRemovals.Values.Count());
            Assert.AreEqual(universe, pendingRemovals.PendingRemovals.Keys.First());
            Assert.AreEqual(security, pendingRemovals.PendingRemovals.Values.First().First());
        }

        private class TestUniverse : Universe
        {
            public TestUniverse()
                : base(SecurityTests.CreateTradeBarConfig())
            {
            }
            public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
            {
                throw new NotImplementedException();
            }
        }

        private static Security CreateSecurity(Symbol symbol)
        {
            return new Security(symbol,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }
    }
}
