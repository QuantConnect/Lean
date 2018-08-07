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
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class EqualWeightingPortfolioConstructionModelTests
    {
        private QCAlgorithmFramework _algorithm;
        private const decimal _startingCash = 100000;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _algorithm = new QCAlgorithmFramework();
            _algorithm.SetCash(_startingCash);
            _algorithm.SetDateTime(new DateTime(2018, 7, 31));

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
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void EmptyInsightsReturnsEmptyTargets(Language language)
        {
            SetPortfolioConstruction(language);

            var insights = Enumerable.Empty<Insight>();
            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights.ToArray());

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

            // Equity will be divided by all securities
            var amount = _algorithm.Portfolio.TotalPortfolioValue / _algorithm.Securities.Count;
            var expectedTargets = _algorithm.Securities
                .Select(x => new PortfolioTarget(x.Key, (int)direction * Math.Floor(amount / x.Value.Price)));

            var insights = _algorithm.Securities.Keys.Select(x => GetInsight(x, direction, _algorithm.UtcTime));
            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights.ToArray());

            Assert.AreEqual(expectedTargets.Count(), actualTargets.Count());

            foreach (var expected in expectedTargets)
            {
                var actual = actualTargets.FirstOrDefault(x => x.Symbol == expected.Symbol);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Quantity, actual.Quantity);
            }
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

            // Modifying fee model for a constant one so numbers are simplified
            foreach (var security in _algorithm.Securities)
            {
                security.Value.FeeModel = new ConstantFeeModel(1);
            }

            // Equity, minus $1 for fees, will be divided by all securities minus 1, since its insight will have flat direction
            var amount = (_algorithm.Portfolio.TotalPortfolioValue - 1 * (_algorithm.Securities.Count - 1))
                         / (_algorithm.Securities.Count - 1);
            var expectedTargets = _algorithm.Securities.Select(x =>
            {
                // Expected target quantity for SPY is zero, since its insight will have flat direction
                var quantity = x.Key.Value == "SPY" ? 0 : (int)direction * Math.Floor(amount / x.Value.Price);
                return new PortfolioTarget(x.Key, quantity);
            });

            var insights = _algorithm.Securities.Keys.Select(x =>
            {
                // SPY insight direction is flat
                var actualDirection = x.Value == "SPY" ? InsightDirection.Flat : direction;
                return GetInsight(x, actualDirection, _algorithm.UtcTime);
            });
            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights.ToArray());

            Assert.AreEqual(expectedTargets.Count(), actualTargets.Count());

            foreach (var expected in expectedTargets)
            {
                var actual = actualTargets.FirstOrDefault(x => x.Symbol == expected.Symbol);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Quantity, actual.Quantity);
            }
        }

        [Test]
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Flat)]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.Python, InsightDirection.Flat)]
        public void AutomaticallyRemoveInvested(Language language, InsightDirection direction)
        {
            SetPortfolioConstruction(language);

            var spyHolding = _algorithm.Portfolio[Symbols.SPY];
            spyHolding.SetHoldings(spyHolding.Price, 100);
            _algorithm.Portfolio.SetCash(_startingCash - spyHolding.HoldingsValue);

            // Equity will be divided by all securities minus 1, since SPY is already invested and we want to remove it
            var amount = _algorithm.Portfolio.TotalPortfolioValue / (_algorithm.Securities.Count - 1);
            var expectedTargets = _algorithm.Securities.Select(x =>
            {
                // Expected target quantity for SPY is zero, since it will be removed
                var quantity = x.Key.Value == "SPY" ? 0 : (int)direction * Math.Floor(amount / x.Value.Price);
                return new PortfolioTarget(x.Key, quantity);
            });

            // Do no include SPY in the insights
            var insights = _algorithm.Securities.Keys
                .Where(x=> x.Value != "SPY")
                .Select(x => GetInsight(x, direction, _algorithm.UtcTime));

            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights.ToArray());

            Assert.AreEqual(expectedTargets.Count(), actualTargets.Count());

            foreach (var expected in expectedTargets)
            {
                var actual = actualTargets.FirstOrDefault(x => x.Symbol == expected.Symbol);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Quantity, actual.Quantity);
            }
        }



        private Security GetSecurity(Symbol symbol)
        {
            var config = SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc);
            return new Equity(symbol, config, new Cash("USD", 0, 1), SymbolProperties.GetDefault("USD"));
        }

        private Insight GetInsight(Symbol symbol, InsightDirection direction, DateTime generatedTimeUtc)
        {
            var period = TimeSpan.FromDays(1);
            var insight = Insight.Price(symbol, period, direction);
            insight.GeneratedTimeUtc = generatedTimeUtc;
            insight.CloseTimeUtc = generatedTimeUtc.Add(period);
            return insight;
        }

        private void SetPortfolioConstruction(Language language)
        {
            _algorithm.SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            if (language == Language.Python)
            {
                using (Py.GIL())
                {
                    var name = nameof(EqualWeightingPortfolioConstructionModel);
                    var instance = Py.Import(name).GetAttr(name).Invoke();
                    var model = new PortfolioConstructionModelPythonWrapper(instance);
                    _algorithm.SetPortfolioConstruction(model);
                }
            }

            var changes = SecurityChanges.Added(_algorithm.Securities.Values.ToArray());
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, changes);
        }
    }
}