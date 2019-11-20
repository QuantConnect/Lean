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
using NodaTime;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class AccumulativeInsightPortfolioConstructionModelTests
    {
        private QCAlgorithm _algorithm;
        private const decimal _startingCash = 100000;
        private const double DefaultPercent = 0.03;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _algorithm = new QCAlgorithm();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));

            var prices = new Dictionary<Symbol, decimal>
            {
                { Symbol.Create("AIG", SecurityType.Equity, Market.USA), 55.22m },
                { Symbol.Create("IBM", SecurityType.Equity, Market.USA), 145.17m },
                { Symbol.Create("SPY", SecurityType.Equity, Market.USA), 281.79m },
            };

            foreach (var kvp in prices)
            {
                var symbol = kvp.Key;
                var security = GetSecurity(symbol);
                security.SetMarketPrice(new Tick(_algorithm.Time, symbol, kvp.Value, kvp.Value));
                _algorithm.Securities.Add(symbol, security);
            }
        }


        [Test]
        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void EmptyInsightsReturnsEmptyTargets(Language language)
        {
            SetPortfolioConstruction(language, _algorithm);

            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, new Insight[0]);

            Assert.AreEqual(0, actualTargets.Count());
        }

        [Test]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.Python, InsightDirection.Flat)]
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Flat)]
        public void InsightsReturnsTargetsConsistentWithDirection(Language language, InsightDirection direction)
        {
            SetPortfolioConstruction(language, _algorithm);

            var amount = _algorithm.Portfolio.TotalPortfolioValue * (decimal)DefaultPercent;
            var expectedTargets = _algorithm.Securities
                .Select(x => new PortfolioTarget(x.Key, (int)direction
                                                        * Math.Floor(amount / x.Value.Price)));

            var insights = _algorithm.Securities.Keys.Select(x => GetInsight(x, direction, _algorithm.UtcTime));
            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights.ToArray());
            
            AssertTargets( expectedTargets, actualTargets);
        }

        [Test]
        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void LongTermInsightCanceledByNew(Language language)
        {
            SetPortfolioConstruction(language, _algorithm);

            // First emit long term insight
            var insights = new[] { GetInsight(Symbols.SPY, InsightDirection.Down, _algorithm.UtcTime) };
            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();
            var expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, -1m * (decimal)DefaultPercent) };
            AssertTargets(expectedTargets, targets);

            // One minute later, emits short term insight to cancel long
            SetUtcTime(_algorithm.UtcTime.AddMinutes(1));
            insights = new[] { GetInsight(Symbols.SPY, InsightDirection.Up, _algorithm.UtcTime, Time.OneMinute) };
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();
            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, 0) };
            AssertTargets(expectedTargets, targets);

            // One minute later, emit empty insights array, should stay 0 after the long expires
            SetUtcTime(_algorithm.UtcTime.AddMinutes(1.1));

            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, 0) };

            // Create target from an empty insights array
            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, new Insight[0]);

            AssertTargets(expectedTargets, actualTargets);
        }

        [Test]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        public void LongTermInsightAccumulatesByNew(Language language, InsightDirection direction)
        {
            SetPortfolioConstruction(language, _algorithm);

            // First emit long term insight
            var insights = new[] { GetInsight(Symbols.SPY, direction, _algorithm.UtcTime) };
            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();
            var expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, (int)direction * 1m * (decimal)DefaultPercent) };
            AssertTargets(expectedTargets, targets);

            // One minute later, emits short term insight to add long
            SetUtcTime(_algorithm.UtcTime.AddMinutes(1));
            insights = new[] { GetInsight(Symbols.SPY, direction, _algorithm.UtcTime, Time.OneMinute) };
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();

            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, (int)direction * 1m * 2 * (decimal)DefaultPercent) };
            AssertTargets(expectedTargets, targets);

            // One minute later, emit empty insights array, should return to nomral after the long expires
            SetUtcTime(_algorithm.UtcTime.AddMinutes(1.1));

            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, (int)direction * 1m * (decimal)DefaultPercent) };

            // Create target from an empty insights array
            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, new Insight[0]);

            AssertTargets(expectedTargets, actualTargets);
        }

        [Test]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        public void FlatUndoesAccumulation(Language language, InsightDirection direction)
        {
            SetPortfolioConstruction(language, _algorithm);

            // First emit long term insight
            var insights = new[] { GetInsight(Symbols.SPY, direction, _algorithm.UtcTime) };
            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();
            var expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, (int)direction * 1m * (decimal)DefaultPercent) };
            AssertTargets(expectedTargets, targets);

            // One minute later, emits insight to add to portfolio
            SetUtcTime(_algorithm.UtcTime.AddMinutes(1));
            insights = new[] { GetInsight(Symbols.SPY, direction, _algorithm.UtcTime) };
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();

            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, (int)direction * 1m * 2 * (decimal)DefaultPercent) };
            AssertTargets(expectedTargets, targets);

            // One minute later, emits flat insight
            SetUtcTime(_algorithm.UtcTime.AddMinutes(1));
            insights = new[] { GetInsight(Symbols.SPY, InsightDirection.Flat, _algorithm.UtcTime) };
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();

            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, (int)direction * 1m * (decimal)DefaultPercent) };

            AssertTargets(expectedTargets, targets);
        }


        [Test]
        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void WeightsProportionally(Language language)
        {
            SetPortfolioConstruction(language, _algorithm);

            var insights = new[]
            {
                GetInsight(Symbols.SPY, InsightDirection.Down, _algorithm.UtcTime),
                GetInsight(Symbol.Create("IBM", SecurityType.Equity, Market.USA),
                    InsightDirection.Down, _algorithm.UtcTime)
            };

            // they will each share, proportionally, the total portfolio value
            var amount = _algorithm.Portfolio.TotalPortfolioValue * (decimal)DefaultPercent;

            var expectedTargets = _algorithm.Securities.Where(pair => insights.Any(insight => pair.Key == insight.Symbol))
                .Select(x => new PortfolioTarget(x.Key, (int)InsightDirection.Down * Math.Floor(amount / x.Value.Price)));

            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();
            Assert.AreEqual(2, actualTargets.Count);

            AssertTargets(expectedTargets, actualTargets);
        }

        [Test]
        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void GeneratesTargetsForInsightsWithNoConfidence(Language language)
        {
            SetPortfolioConstruction(language, _algorithm);

            var insights = new[]
            {
                GetInsight(Symbols.SPY, InsightDirection.Down, _algorithm.UtcTime, confidence:null)
            };

            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();
            Assert.AreEqual(1, actualTargets.Count);
        }

        [Test]
        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void GeneratesNormalTargetForZeroInsightConfidence(Language language)
        {
            SetPortfolioConstruction(language, _algorithm);

            var insights = new[]
            {
                GetInsight(Symbols.SPY, InsightDirection.Down, _algorithm.UtcTime, confidence:0)
            };

            // they will each share, proportionally, the total portfolio value
            var amount = _algorithm.Portfolio.TotalPortfolioValue * (decimal)DefaultPercent;

            var expectedTargets = _algorithm.Securities.Where(pair => insights.Any(insight => pair.Key == insight.Symbol))
                .Select(x => new PortfolioTarget(x.Key, (int)InsightDirection.Down * Math.Floor(amount / x.Value.Price)));

            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();
            
            AssertTargets(expectedTargets, actualTargets);
        }


        private Security GetSecurity(Symbol symbol)
        {
            var config = SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc);
            return new Equity(
                symbol,
                config,
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        private Insight GetInsight(Symbol symbol, InsightDirection direction, DateTime generatedTimeUtc, TimeSpan? period = null, double? confidence = DefaultPercent)
        {
            period = period ?? TimeSpan.FromDays(1);
            var insight = Insight.Price(symbol, period.Value, direction, confidence: confidence);
            insight.GeneratedTimeUtc = generatedTimeUtc;
            insight.CloseTimeUtc = generatedTimeUtc.Add(period.Value);
            return insight;
        }

        private void SetPortfolioConstruction(Language language, QCAlgorithm algorithm)
        {
            algorithm.SetPortfolioConstruction(new AccumulativeInsightPortfolioConstructionModel());
            if (language == Language.Python)
            {
                using (Py.GIL())
                {
                    var name = nameof(AccumulativeInsightPortfolioConstructionModel);
                    var instance = Py.Import(name).GetAttr(name).Invoke();
                    var model = new PortfolioConstructionModelPythonWrapper(instance);
                    algorithm.SetPortfolioConstruction(model);
                }
            }

            foreach (var kvp in _algorithm.Portfolio)
            {
                kvp.Value.SetHoldings(kvp.Value.Price, 0);
            }
            _algorithm.Portfolio.SetCash(_startingCash);
            SetUtcTime(new DateTime(2018, 7, 31));

            var changes = SecurityChanges.Added(_algorithm.Securities.Values.ToArray());
            algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, changes);
        }

        private void SetUtcTime(DateTime dateTime)
        {
            _algorithm.SetDateTime(dateTime.ConvertToUtc(_algorithm.TimeZone));
        }

        private void AssertTargets(IEnumerable<IPortfolioTarget> expectedTargets, IEnumerable<IPortfolioTarget> actualTargets)
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
    }
}
