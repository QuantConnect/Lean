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
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Tests.Common.Data.UniverseSelection;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class AccumulativeInsightPortfolioConstructionModelTests
    {
        private QCAlgorithm _algorithm;
        private const decimal _startingCash = 100000;
        private const double DefaultPercent = 0.03;

        [SetUp]
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
            SetUtcTime(_algorithm.Time.AddMinutes(1));
            insights = new[] { GetInsight(Symbols.SPY, InsightDirection.Up, _algorithm.UtcTime, Time.OneMinute) };
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();
            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, 0) };
            AssertTargets(expectedTargets, targets);

            // One minute later, emit empty insights array, short term insight expires but should stay -1 since long term insight is still valid
            SetUtcTime(_algorithm.Time.AddMinutes(1.1));

            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, -1m * (decimal)DefaultPercent) };

            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, new Insight[0]);
            AssertTargets(expectedTargets, actualTargets);

            // should stay 0 *after* the long expires
            SetUtcTime(_algorithm.Time.AddYears(1));

            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, 0) };

            actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, new Insight[0]);
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

            // now we should reach 0 percent
            insights = new[] { GetInsight(Symbols.SPY, InsightDirection.Flat, _algorithm.UtcTime) };
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();

            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, 0) };

            AssertTargets(expectedTargets, targets);
        }

        [Test]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        public void InsightExpirationUndoesAccumulationBySteps(Language language, InsightDirection direction)
        {
            SetPortfolioConstruction(language, _algorithm);

            // First emit long term insight
            SetUtcTime(_algorithm.Time);
            var insights = new[] { GetInsight(Symbols.SPY, direction, _algorithm.UtcTime, TimeSpan.FromMinutes(10)) };
            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();
            var expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, (int)direction * 1m * (decimal)DefaultPercent) };
            AssertTargets(expectedTargets, targets);

            // One minute later, emits insight to add to portfolio
            SetUtcTime(_algorithm.Time.AddMinutes(1));
            insights = new[] { GetInsight(Symbols.SPY, direction, _algorithm.UtcTime, TimeSpan.FromMinutes(10)) };
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();

            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, (int)direction * 1m * 2 * (decimal)DefaultPercent) };
            AssertTargets(expectedTargets, targets);

            // the first insight should expire
            SetUtcTime(_algorithm.Time.AddMinutes(10));
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, new Insight[0]).ToList();

            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, (int)direction * 1m * (decimal)DefaultPercent) };

            AssertTargets(expectedTargets, targets);

            // the second insight should expire
            SetUtcTime(_algorithm.Time.AddMinutes(1));
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, new Insight[0]).ToList();

            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, 0) };

            AssertTargets(expectedTargets, targets);
        }

        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        public void RespectsRebalancingPeriod(Language language, InsightDirection direction)
        {
            PortfolioConstructionModel model = new AccumulativeInsightPortfolioConstructionModel(Resolution.Daily);
            if (language == Language.Python)
            {
                using (Py.GIL())
                {
                    var name = nameof(AccumulativeInsightPortfolioConstructionModel);
                    dynamic instance = Py.Import(name).GetAttr(name);
                    model = new PortfolioConstructionModelPythonWrapper(instance(Resolution.Daily));
                }
            }

            model.RebalanceOnSecurityChanges = false;
            model.RebalanceOnInsightChanges = false;

            SetUtcTime(new DateTime(2018, 7, 31));
            // First emit long term insight
            var insights = new[] { GetInsight(Symbols.SPY, direction, _algorithm.UtcTime, TimeSpan.FromDays(10)) };
            AssertTargets(new List<IPortfolioTarget>(), model.CreateTargets(_algorithm, insights).ToList());

            // One minute later, emits insight to add to portfolio
            SetUtcTime(_algorithm.Time.AddMinutes(1));
            insights = new[] { GetInsight(Symbols.SPY, direction, _algorithm.UtcTime, TimeSpan.FromMinutes(10)) };
            AssertTargets(new List<IPortfolioTarget>(), model.CreateTargets(_algorithm, insights));

            // the second insight should expire
            SetUtcTime(_algorithm.Time.AddMinutes(1));
            AssertTargets(new List<IPortfolioTarget>(), model.CreateTargets(_algorithm, insights));

            // the rebalancing period is due and the first insight is still valid
            SetUtcTime(_algorithm.Time.AddDays(1));
            var targets = model.CreateTargets(_algorithm, new Insight[0]);
            var expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, (int)direction * 1m * (decimal)DefaultPercent) };
            AssertTargets(expectedTargets, targets);

            // the rebalancing period is due and no insight is valid
            SetUtcTime(_algorithm.Time.AddDays(10));
            AssertTargets(
                new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, 0) },
                model.CreateTargets(_algorithm, new Insight[0]));

            AssertTargets(new List<IPortfolioTarget>(), model.CreateTargets(_algorithm, new Insight[0]));
        }

        [Test]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        public void InsightExpirationUndoesAccumulation(Language language, InsightDirection direction)
        {
            SetPortfolioConstruction(language, _algorithm);

            // First emit long term insight
            SetUtcTime(_algorithm.Time);
            var insights = new[]
            {
                GetInsight(Symbols.SPY, direction, _algorithm.UtcTime, TimeSpan.FromMinutes(10)),
                GetInsight(Symbols.SPY, direction, _algorithm.UtcTime, TimeSpan.FromMinutes(10))
            };
            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToList();
            var expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, (int)direction * 1m * 2 * (decimal)DefaultPercent) };
            AssertTargets(expectedTargets, targets);

            // both insights should expire
            SetUtcTime(_algorithm.Time.AddMinutes(11));
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, new Insight[0]).ToList();

            expectedTargets = new List<IPortfolioTarget> { PortfolioTarget.Percent(_algorithm, Symbols.SPY, 0) };

            AssertTargets(expectedTargets, targets);

            // we expect no target
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, new Insight[0]).ToList();
            AssertTargets(new List<IPortfolioTarget>(), targets);
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

        [TestCase(Language.CSharp, PortfolioBias.Long)]
        [TestCase(Language.Python, PortfolioBias.Long)]
        [TestCase(Language.CSharp, PortfolioBias.Short)]
        [TestCase(Language.Python, PortfolioBias.Short)]
        public void PortfolioBiasIsRespected(Language language, PortfolioBias bias)
        {
            SetPortfolioConstruction(language, _algorithm, bias);
            var now = new DateTime(2018, 7, 31);
            SetUtcTime(now.ConvertFromUtc(_algorithm.TimeZone));
            var appl = _algorithm.AddEquity("AAPL");
            appl.SetMarketPrice(new Tick(now, appl.Symbol, 10, 10));

            var spy = _algorithm.AddEquity("SPY");
            spy.SetMarketPrice(new Tick(now, spy.Symbol, 20, 20));

            var ibm = _algorithm.AddEquity("IBM");
            ibm.SetMarketPrice(new Tick(now, ibm.Symbol, 30, 30));

            var aig = _algorithm.AddEquity("AIG");
            aig.SetMarketPrice(new Tick(now, aig.Symbol, 30, 30));

            var qqq = _algorithm.AddEquity("QQQ");
            qqq.SetMarketPrice(new Tick(now, qqq.Symbol, 30, 30));

            var insights = new[]
            {
                new Insight(now, appl.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, 0.1d, null),
                new Insight(now, spy.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Down, -0.1d, null),
                new Insight(now, ibm.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Down, 0d, null),
                new Insight(now, aig.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Down, -0.1d, null),
                new Insight(now, qqq.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, 0.1d, null)
            };
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, SecurityChangesTests.AddedNonInternal(appl, spy, ibm, aig, qqq));

            var createdValidTarget = false;
            _algorithm.Insights.AddRange(insights);
            foreach (var target in _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights))
            {
                QuantConnect.Logging.Log.Trace($"{target.Symbol}: {target.Quantity}");
                if (target.Quantity == 0)
                {
                    continue;
                }

                createdValidTarget = true;
                Assert.AreEqual(Math.Sign((int)bias), Math.Sign(target.Quantity));
            }

            Assert.IsTrue(createdValidTarget);
        }

        private Security GetSecurity(Symbol symbol)
        {
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            return new Equity(
                symbol,
                exchangeHours,
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        private Insight GetInsight(Symbol symbol, InsightDirection direction, DateTime generatedTimeUtc, TimeSpan? period = null, double? confidence = DefaultPercent)
        {
            period ??= TimeSpan.FromDays(1);
            var insight = Insight.Price(symbol, period.Value, direction, confidence: confidence);
            insight.GeneratedTimeUtc = generatedTimeUtc;
            insight.CloseTimeUtc = generatedTimeUtc.Add(period.Value);
            _algorithm.Insights.Add(insight);
            return insight;
        }

        private void SetPortfolioConstruction(Language language, QCAlgorithm algorithm, PortfolioBias bias= PortfolioBias.LongShort)
        {
            algorithm.SetPortfolioConstruction(new AccumulativeInsightPortfolioConstructionModel((Func<DateTime,DateTime>)null, bias));
            if (language == Language.Python)
            {
                using (Py.GIL())
                {
                    var name = nameof(AccumulativeInsightPortfolioConstructionModel);
                    var instance = Py.Import(name).GetAttr(name).Invoke(((object)null).ToPython(), ((int)bias).ToPython());
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

            var changes = SecurityChangesTests.AddedNonInternal(_algorithm.Securities.Values.ToArray());
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
