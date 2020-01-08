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
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.Orders.Fees;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class EqualWeightingPortfolioConstructionModelTests
    {
        private const double _weight = 0.01;
        private const decimal _startingCash = 100000;

        public QCAlgorithm Algorithm { get; set; }

        public virtual double? Weight => Algorithm.Securities.Count == 0 ? default(double) : 1d / Algorithm.Securities.Count;

        public virtual PortfolioBias PortfolioBias => PortfolioBias.LongShort;

        [TestFixtureSetUp]
        public void SetUp()
        {
            Algorithm = new QCAlgorithm();
            Algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(Algorithm));

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
                security.SetMarketPrice(new Tick(Algorithm.Time, symbol, kvp.Value, kvp.Value));
                Algorithm.Securities.Add(symbol, security);
            }
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
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Flat)]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.Python, InsightDirection.Flat)]
        public void InsightsReturnsTargetsConsistentWithDirection(Language language, InsightDirection direction)
        {
            SetPortfolioConstruction(language);

            if (PortfolioBias != PortfolioBias.LongShort && (int)direction != (int)PortfolioBias)
            {
                direction = InsightDirection.Flat;
            }

            // Equity will be divided by all securities
            var amount = Algorithm.Portfolio.TotalPortfolioValue * (decimal) Weight;
            var expectedTargets = Algorithm.Securities
                .Select(x => new PortfolioTarget(x.Key, (int)direction
                                                        * Math.Floor(amount * (1 - Algorithm.Settings.FreePortfolioValuePercentage)
                                                                     / x.Value.Price)));

            var insights = Algorithm.Securities.Keys.Select(x => GetInsight(x, direction, Algorithm.UtcTime));
            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights.ToArray());

            AssertTargets(expectedTargets, actualTargets);
        }

        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Flat)]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.Python, InsightDirection.Flat)]
        public void FlatDirectionNotAccountedToAllocation(Language language, InsightDirection direction)
        {
            SetPortfolioConstruction(language);

            if (PortfolioBias != PortfolioBias.LongShort && (int)direction != (int)PortfolioBias)
            {
                direction = InsightDirection.Flat;
            }

            // Modifying fee model for a constant one so numbers are simplified
            foreach (var security in Algorithm.Securities)
            {
                security.Value.FeeModel = new ConstantFeeModel(1);
            }

            // Equity, minus $1 for fees, will be divided by all securities minus 1, since its insight will have flat direction
            var amount = (Algorithm.Portfolio.TotalPortfolioValue - 1 * (Algorithm.Securities.Count - 1)) * 1 /
                         (decimal)((1 / Weight) - 1);

            var expectedTargets = Algorithm.Securities.Select(x =>
            {
                // Expected target quantity for SPY is zero, since its insight will have flat direction
                var quantity = x.Key.Value == "SPY" ? 0 : (int)direction
                                                          * Math.Floor(amount * (1 - Algorithm.Settings.FreePortfolioValuePercentage)
                                                                       / x.Value.Price);
                return new PortfolioTarget(x.Key, quantity);
            });

            var insights = Algorithm.Securities.Keys.Select(x =>
            {
                // SPY insight direction is flat
                var actualDirection = x.Value == "SPY" ? InsightDirection.Flat : direction;
                return GetInsight(x, actualDirection, Algorithm.UtcTime);
            });
            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights.ToArray());

            AssertTargets(expectedTargets, actualTargets);
        }

        [Test]
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Flat)]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.Python, InsightDirection.Flat)]
        public void AutomaticallyRemoveInvestedWithNewInsights(Language language, InsightDirection direction)
        {
            SetPortfolioConstruction(language);

            if (PortfolioBias != PortfolioBias.LongShort && (int)direction != (int)PortfolioBias)
            {
                direction = InsightDirection.Flat;
            }

            // Let's create a position for SPY
            var insights = new[] { GetInsight(Symbols.SPY, direction, Algorithm.UtcTime) };

            foreach (var target in Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights))
            {
                var holding = Algorithm.Portfolio[target.Symbol];
                holding.SetHoldings(holding.Price, target.Quantity);
                Algorithm.Portfolio.SetCash(_startingCash - holding.HoldingsValue);
            }

            SetUtcTime(Algorithm.UtcTime.AddDays(2));

            // Equity will be divided by all securities minus 1, since SPY is already invested and we want to remove it
            var amount = Algorithm.Portfolio.TotalPortfolioValue / (decimal) (1 / Weight - 1);
            var expectedTargets = Algorithm.Securities.Select(x =>
            {
                // Expected target quantity for SPY is zero, since it will be removed
                var quantity = x.Key.Value == "SPY" ? 0 : (int)direction
                                                          * Math.Floor(amount * (1 - Algorithm.Settings.FreePortfolioValuePercentage)
                                                                       / x.Value.Price);
                return new PortfolioTarget(x.Key, quantity);
            });

            // Do no include SPY in the insights
            insights = Algorithm.Securities.Keys.Where(x=> x.Value != "SPY")
                .Select(x => GetInsight(x, direction, Algorithm.UtcTime)).ToArray();

            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights);

            AssertTargets(expectedTargets, actualTargets);
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
                Algorithm.Portfolio.SetCash(_startingCash - holding.HoldingsValue);
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

        public virtual List<IPortfolioTarget> GetTargetsForSPY()
        {
            return new List<IPortfolioTarget> { PortfolioTarget.Percent(Algorithm, Symbols.SPY, -1m) };
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
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Flat)]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.Python, InsightDirection.Flat)]
        public void DelistedSecurityEmitsFlatTargetWithNewInsights(Language language, InsightDirection direction)
        {
            SetPortfolioConstruction(language);

            if (PortfolioBias != PortfolioBias.LongShort && (int)direction != (int)PortfolioBias)
            {
                direction = InsightDirection.Flat;
            }

            var insights = new[] { GetInsight(Symbols.SPY, InsightDirection.Down, Algorithm.UtcTime) };
            var targets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights).ToList();
            Assert.AreEqual(1, targets.Count);

            // Removing SPY should clear the key in the insight collection
            var changes = SecurityChanges.Removed(Algorithm.Securities[Symbols.SPY]);
            Algorithm.PortfolioConstruction.OnSecuritiesChanged(Algorithm, changes);

            // Equity will be divided by all securities minus 1, since SPY is already invested and we want to remove it
            var amount = Algorithm.Portfolio.TotalPortfolioValue / (decimal) (1 / Weight - 1);
            var expectedTargets = Algorithm.Securities.Select(x =>
            {
                // Expected target quantity for SPY is zero, since it will be removed
                var quantity = x.Key.Value == "SPY" ? 0 : (int)direction
                                                          * Math.Floor(amount * (1 - Algorithm.Settings.FreePortfolioValuePercentage)
                                                                       / x.Value.Price);
                return new PortfolioTarget(x.Key, quantity);
            });

            // Do no include SPY in the insights
            insights = Algorithm.Securities.Keys.Where(x => x.Value != "SPY")
                .Select(x => GetInsight(x, direction, Algorithm.UtcTime)).ToArray();

            // Create target from an empty insights array
            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights);

            AssertTargets(expectedTargets, actualTargets);
        }

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

        public virtual IPortfolioConstructionModel GetPortfolioConstructionModel(Language language, dynamic paramenter = null)
        {
            if (language == Language.CSharp)
            {
                return new EqualWeightingPortfolioConstructionModel(paramenter);
            }

            using (Py.GIL())
            {
                const string name = nameof(EqualWeightingPortfolioConstructionModel);
                var instance = Py.Import(name).GetAttr(name).Invoke(((object) paramenter).ToPython());
                return new PortfolioConstructionModelPythonWrapper(instance);
            }
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

        public virtual Insight GetInsight(Symbol symbol, InsightDirection direction, DateTime generatedTimeUtc, TimeSpan? period = null, double? weight = _weight)
        {
            period = period ?? TimeSpan.FromDays(1);
            var insight = Insight.Price(symbol, period.Value, direction, weight: Math.Max(_weight, Algorithm.Securities.Count));
            insight.GeneratedTimeUtc = generatedTimeUtc;
            insight.CloseTimeUtc = generatedTimeUtc.Add(period.Value);
            return insight;
        }

        public void SetPortfolioConstruction(Language language, dynamic paramenter = null)
        {
            var model = GetPortfolioConstructionModel(language, paramenter ?? Resolution.Daily);
            Algorithm.SetPortfolioConstruction(model);

            foreach (var kvp in Algorithm.Portfolio)
            {
                kvp.Value.SetHoldings(kvp.Value.Price, 0);
            }
            Algorithm.Portfolio.SetCash(_startingCash);
            SetUtcTime(new DateTime(2018, 7, 31));

            var changes = SecurityChanges.Added(Algorithm.Securities.Values.ToArray());
            Algorithm.PortfolioConstruction.OnSecuritiesChanged(Algorithm, changes);
        }

        private void SetUtcTime(DateTime dateTime)
        {
            Algorithm.SetDateTime(dateTime.ConvertToUtc(Algorithm.TimeZone));
        }
    }
}