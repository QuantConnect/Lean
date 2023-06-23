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

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Tests.Common.Data.UniverseSelection;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class EqualWeightingPortfolioConstructionModelTests : BaseWeightingPortfolioConstructionModelTests
    {
        public override double? Weight => Algorithm.Securities.Count == 0 ? default(double) : 1d / Algorithm.Securities.Count;

        public virtual PortfolioBias PortfolioBias => PortfolioBias.LongShort;

        [OneTimeSetUp]
        public override void SetUp()
        {
            base.SetUp();

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
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Flat)]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.Python, InsightDirection.Flat)]
        public override void AutomaticallyRemoveInvestedWithNewInsights(Language language, InsightDirection direction)
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
                Algorithm.Portfolio.SetCash(StartingCash - holding.HoldingsValue);
            }
            SetUtcTime(Algorithm.UtcTime.AddDays(2));

            // Equity will be divided by all securities minus 1, since SPY is already invested and we want to remove it
            var amount = Algorithm.Portfolio.TotalPortfolioValue / (decimal)(1 / Weight - 1) *
                         (1 - Algorithm.Settings.FreePortfolioValuePercentage);
            var expectedTargets = Algorithm.Securities.Select(x =>
            {
                // Expected target quantity for SPY is zero, since it will be removed
                var quantity = x.Key.Value == "SPY" ? 0 : (int)direction * Math.Floor(amount / x.Value.Price);
                return new PortfolioTarget(x.Key, quantity);
            });

            // Do no include SPY in the insights
            insights = Algorithm.Securities.Keys.Where(x => x.Value != "SPY")
                .Select(x => GetInsight(x, direction, Algorithm.UtcTime)).ToArray();

            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights);

            AssertTargets(expectedTargets, actualTargets);
        }

        [Test]
        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Flat)]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.Python, InsightDirection.Flat)]
        public override void DelistedSecurityEmitsFlatTargetWithNewInsights(Language language, InsightDirection direction)
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
            var changes = SecurityChangesTests.RemovedNonInternal(Algorithm.Securities[Symbols.SPY]);
            Algorithm.PortfolioConstruction.OnSecuritiesChanged(Algorithm, changes);

            // Equity will be divided by all securities minus 1, since SPY is already invested and we want to remove it
            var amount = Algorithm.Portfolio.TotalPortfolioValue / (decimal)(1 / Weight - 1) *
                (1 - Algorithm.Settings.FreePortfolioValuePercentage);

            var expectedTargets = Algorithm.Securities.Select(x =>
            {
                // Expected target quantity for SPY is zero, since it will be removed
                var quantity = x.Key.Value == "SPY" ? 0 : (int)direction * Math.Floor(amount / x.Value.Price);
                return new PortfolioTarget(x.Key, quantity);
            });

            // Do no include SPY in the insights
            insights = Algorithm.Securities.Keys.Where(x => x.Value != "SPY")
                .Select(x => GetInsight(x, direction, Algorithm.UtcTime)).ToArray();

            // Create target from an empty insights array
            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights);

            AssertTargets(expectedTargets, actualTargets);
        }

        [TestCase(Language.CSharp, InsightDirection.Up)]
        [TestCase(Language.CSharp, InsightDirection.Down)]
        [TestCase(Language.CSharp, InsightDirection.Flat)]
        [TestCase(Language.Python, InsightDirection.Up)]
        [TestCase(Language.Python, InsightDirection.Down)]
        [TestCase(Language.Python, InsightDirection.Flat)]
        public override void FlatDirectionNotAccountedToAllocation(Language language, InsightDirection direction)
        {
            SetPortfolioConstruction(language);

            if (PortfolioBias != PortfolioBias.LongShort && (int)direction != (int)PortfolioBias)
            {
                direction = InsightDirection.Flat;
            }

            // Equity, minus $1 for fees, will be divided by all securities minus 1, since its insight will have flat direction
            var amount = (Algorithm.Portfolio.TotalPortfolioValue - 1 * (Algorithm.Securities.Count - 1)) * 1 /
                         (decimal)((1 / Weight) - 1) * (1 - Algorithm.Settings.FreePortfolioValuePercentage);

            var expectedTargets = Algorithm.Securities.Select(x =>
            {
                // Expected target quantity for SPY is zero, since its insight will have flat direction
                var quantity = x.Key.Value == "SPY" ? 0 : (int)direction * Math.Floor(amount / x.Value.Price);
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
        [TestCase(Language.CSharp, InsightDirection.Up, 1)]
        [TestCase(Language.CSharp, InsightDirection.Up, -1)]
        [TestCase(Language.CSharp, InsightDirection.Down, 1)]
        [TestCase(Language.CSharp, InsightDirection.Down, -1)]
        [TestCase(Language.CSharp, InsightDirection.Flat, 1)]
        [TestCase(Language.CSharp, InsightDirection.Flat, -1)]
        [TestCase(Language.Python, InsightDirection.Up, 1)]
        [TestCase(Language.Python, InsightDirection.Up, -1)]
        [TestCase(Language.Python, InsightDirection.Down, 1)]
        [TestCase(Language.Python, InsightDirection.Down, -1)]
        [TestCase(Language.Python, InsightDirection.Flat, 1)]
        [TestCase(Language.Python, InsightDirection.Flat, -1)]
        public virtual void InsightsReturnsTargetsConsistentWithDirection(Language language, InsightDirection direction, int weightSign)
        {
            SetPortfolioConstruction(language);

            if (PortfolioBias != PortfolioBias.LongShort && (int)direction != (int)PortfolioBias)
            {
                direction = InsightDirection.Flat;
            }
            // Equity will be divided by all securities
            var amount = Algorithm.Portfolio.TotalPortfolioValue * (decimal)Weight *
                (1 - Algorithm.Settings.FreePortfolioValuePercentage);
            var expectedTargets = Algorithm.Securities
                .Select(x => new PortfolioTarget(x.Key, (int)direction * Math.Floor(amount / x.Value.Price)));

            var insights = Algorithm.Securities.Keys.Select(x => GetInsight(x, direction, Algorithm.UtcTime, weight: weightSign * Weight));
            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights.ToArray());

            AssertTargets(expectedTargets, actualTargets);
        }

        public override Insight GetInsight(Symbol symbol, InsightDirection direction, DateTime generatedTimeUtc, TimeSpan? period = null, double? weight = 0.01)
        {
            period ??= TimeSpan.FromDays(1);
            var insight = Insight.Price(symbol, period.Value, direction, weight: Math.Max(0.01, Algorithm.Securities.Count));
            insight.GeneratedTimeUtc = generatedTimeUtc;
            insight.CloseTimeUtc = generatedTimeUtc.Add(period.Value);
            Algorithm.Insights.Add(insight);
            return insight;
        }

        public override IPortfolioConstructionModel GetPortfolioConstructionModel(Language language, dynamic paramenter = null)
        {
            if (language == Language.CSharp)
            {
                return new EqualWeightingPortfolioConstructionModel(paramenter);
            }

            using (Py.GIL())
            {
                const string name = nameof(EqualWeightingPortfolioConstructionModel);
                var instance = Py.Import(name).GetAttr(name).Invoke(((object)paramenter).ToPython());
                return new PortfolioConstructionModelPythonWrapper(instance);
            }
        }
    }
}
