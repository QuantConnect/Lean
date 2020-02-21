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

using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Tests.Engine.DataFeeds;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public abstract class BaseWeightingPortfolioConstructionModelTests
    {
        protected decimal StartingCash => 100000;

        protected QCAlgorithm Algorithm { get; set; }

        public virtual double? Weight => Algorithm.Securities.Count == 0 ? default(double) : 1d / Algorithm.Securities.Count;

        [TestFixtureSetUp]
        public virtual void SetUp()
        {
            Algorithm = new QCAlgorithm();
            Algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(Algorithm));
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void AutomaticallyRemoveInvestedWithoutNewInsights(Language language)
        {
            SetPortfolioConstruction(language);

            // Let's create a position for SPY
            var insights = new[] { GetInsight(Symbols.SPY, InsightDirection.Up, Algorithm.UtcTime) };

            foreach (var target in Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights))
            {
                var holding = Algorithm.Portfolio[target.Symbol];
                holding.SetHoldings(holding.Price, target.Quantity);
                Algorithm.Portfolio.SetCash(StartingCash - holding.HoldingsValue);
            }

            SetUtcTime(Algorithm.UtcTime.AddDays(2));

            var expectedTargets = new List<IPortfolioTarget> { new PortfolioTarget(Symbols.SPY, 0) };

            // Create target from an empty insights array
            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, new Insight[0]);

            AssertTargets(expectedTargets, actualTargets);
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void DelistedSecurityEmitsFlatTargetWithoutNewInsights(Language language)
        {
            SetPortfolioConstruction(language);

            var insights = new[] { GetInsight(Symbols.SPY, InsightDirection.Down, Algorithm.UtcTime) };
            var targets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights).ToList();
            Assert.AreEqual(1, targets.Count);

            var changes = SecurityChanges.Removed(Algorithm.Securities[Symbols.SPY]);
            Algorithm.PortfolioConstruction.OnSecuritiesChanged(Algorithm, changes);

            var expectedTargets = new List<IPortfolioTarget> { new PortfolioTarget(Symbols.SPY, 0) };

            // Create target from an empty insights array
            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, new Insight[0]);

            AssertTargets(expectedTargets, actualTargets);
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void DoesNotReturnTargetsIfSecurityPriceIsZero(Language language)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.AddEquity(Symbols.SPY.Value);
            algorithm.SetDateTime(DateTime.MinValue.ConvertToUtc(Algorithm.TimeZone));

            SetPortfolioConstruction(language);

            var insights = new[] { GetInsight(Symbols.SPY, InsightDirection.Up, algorithm.UtcTime) };
            var actualTargets = algorithm.PortfolioConstruction.CreateTargets(algorithm, insights);

            Assert.AreEqual(0, actualTargets.Count());
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void DoesNotThrowWithAlternativeOverloads(Language language)
        {
            Assert.DoesNotThrow(() => SetPortfolioConstruction(language, Resolution.Minute));
            Assert.DoesNotThrow(() => SetPortfolioConstruction(language, TimeSpan.FromDays(1)));
            Assert.DoesNotThrow(() => SetPortfolioConstruction(language, Expiry.EndOfWeek));
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void EmptyInsightsReturnsEmptyTargets(Language language)
        {
            SetPortfolioConstruction(language);
            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, new Insight[0]);
            Assert.AreEqual(0, actualTargets.Count());
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void LongTermInsightPreservesPosition(Language language)
        {
            SetPortfolioConstruction(language);

            // First emit long term insight
            var insights = new[] { GetInsight(Symbols.SPY, InsightDirection.Down, Algorithm.UtcTime) };
            var targets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights).ToList();
            Assert.AreEqual(1, targets.Count);

            // One minute later, emits short term insight
            SetUtcTime(Algorithm.UtcTime.AddMinutes(1));
            insights = new[] { GetInsight(Symbols.SPY, InsightDirection.Up, Algorithm.UtcTime, Time.OneMinute) };
            targets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights).ToList();
            Assert.AreEqual(1, targets.Count);

            // One minute later, emit empty insights array
            SetUtcTime(Algorithm.UtcTime.AddMinutes(1.1));

            var expectedTargets = GetTargetsForSPY();

            // Create target from an empty insights array
            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, new Insight[0]);

            AssertTargets(expectedTargets, actualTargets);
        }

        [Test]
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Flat)]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.Python, InsightDirection.Flat)]
        public abstract void AutomaticallyRemoveInvestedWithNewInsights(Language language, InsightDirection direction);

        [Test]
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Flat)]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.Python, InsightDirection.Flat)]
        public abstract void DelistedSecurityEmitsFlatTargetWithNewInsights(Language language, InsightDirection direction);

        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Flat)]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.Python, InsightDirection.Flat)]
        public abstract void FlatDirectionNotAccountedToAllocation(Language language, InsightDirection direction);

        public abstract IPortfolioConstructionModel GetPortfolioConstructionModel(Language language, dynamic paramenter = null);

        public abstract Insight GetInsight(Symbol symbol, InsightDirection direction, DateTime generatedTimeUtc, TimeSpan? period = null, double? weight = 0.01);

        public void AssertTargets(IEnumerable<IPortfolioTarget> expectedTargets, IEnumerable<IPortfolioTarget> actualTargets)
        {
            var list = actualTargets.ToList();
            Assert.AreEqual(expectedTargets.Count(), list.Count);

            foreach (var expected in expectedTargets)
            {
                var actual = list.FirstOrDefault(x => x.Symbol == expected.Symbol);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Quantity, actual.Quantity);
            }
        }

        protected Security GetSecurity(Symbol symbol) =>
            new Equity(
                symbol,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
                );

        public virtual List<IPortfolioTarget> GetTargetsForSPY()
        {
            return new List<IPortfolioTarget> { PortfolioTarget.Percent(Algorithm, Symbols.SPY, -1m) };
        }

        protected void SetPortfolioConstruction(Language language, dynamic paramenter = null)
        {
            var model = GetPortfolioConstructionModel(language, paramenter ?? Resolution.Daily);
            Algorithm.SetPortfolioConstruction(model);

            foreach (var kvp in Algorithm.Portfolio)
            {
                kvp.Value.SetHoldings(kvp.Value.Price, 0);
            }
            Algorithm.Portfolio.SetCash(StartingCash);
            SetUtcTime(new DateTime(2018, 7, 31));

            var changes = SecurityChanges.Added(Algorithm.Securities.Values.ToArray());
            Algorithm.PortfolioConstruction.OnSecuritiesChanged(Algorithm, changes);
        }

        protected void SetUtcTime(DateTime dateTime) => Algorithm.SetDateTime(dateTime.ConvertToUtc(Algorithm.TimeZone));
    }
}