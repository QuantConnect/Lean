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
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class SectorWeightingPortfolioConstructionModelTests : BaseWeightingPortfolioConstructionModelTests
    {
        private const double _weight = 0.01;

        public override double? Weight => Algorithm.Securities.Count == 0 ? default(double) : 1d / Algorithm.Securities.Count;

        [TestFixtureSetUp]
        public override void SetUp()
        {
            base.SetUp();

            var prices = new Dictionary<Symbol, Tuple<decimal, string>>
            {
                { Symbol.Create("XXX", SecurityType.Equity, Market.USA), Tuple.Create(55.22m, "") },
                { Symbol.Create("B01", SecurityType.Equity, Market.USA), Tuple.Create(55.22m, "B") },
                { Symbol.Create("B02", SecurityType.Equity, Market.USA), Tuple.Create(55.22m, "B") },
                { Symbol.Create("B03", SecurityType.Equity, Market.USA), Tuple.Create(55.22m, "B") },
                { Symbol.Create("T01", SecurityType.Equity, Market.USA), Tuple.Create(145.17m, "T") },
                { Symbol.Create("T02", SecurityType.Equity, Market.USA), Tuple.Create(145.17m, "T") },
                { Symbol.Create("SPY", SecurityType.Equity, Market.USA), Tuple.Create(281.79m, "X") },
            };

            Func<Symbol, Security> GetSecurity = symbol =>
                new Equity(
                    symbol,
                    SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                    new Cash(Currencies.USD, 0, 1),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                    );

            foreach (var kvp in prices)
            {
                var symbol = kvp.Key;
                var security = GetSecurity(symbol);
                var price = kvp.Value.Item1;
                var sectorCode = kvp.Value.Item2;
                // The first item does not have a valid sector code,
                // This procedure shows that the model ignores the securities without it
                if (!string.IsNullOrEmpty(sectorCode))
                {
                    security.SetMarketPrice(new Fundamentals
                    {
                        Symbol = symbol,
                        Time = Algorithm.Time,
                        Value = price,
                        CompanyReference = new CompanyReference() { IndustryTemplateCode = kvp.Value.Item2 }
                    });
                }
                security.SetMarketPrice(new Tick(Algorithm.Time, symbol, price, price));
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

            // Let's create a position for SPY
            var insights = new[] { GetInsight(Symbols.SPY, direction, Algorithm.UtcTime) };

            foreach (var target in Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights))
            {
                var holding = Algorithm.Portfolio[target.Symbol];
                holding.SetHoldings(holding.Price, target.Quantity);
                Algorithm.Portfolio.SetCash(StartingCash - holding.HoldingsValue);
            }

            SetUtcTime(Algorithm.UtcTime.AddDays(2));

            // Since we have 3 B, 2 T and 1 X, each security in each sector will get 
            // B => .166%, T => .25, X => 0% (removed)
            var sectorCount = 2;
            var groupedBySector = Algorithm.Securities
                .Where(pair => pair.Value.Fundamentals?.CompanyReference?.IndustryTemplateCode != null)
                .GroupBy(pair => pair.Value.Fundamentals.CompanyReference.IndustryTemplateCode);

            var expectedTargets = new List<PortfolioTarget>();

            foreach (var securities in groupedBySector)
            {
                var list = securities.ToList();
                var amount = Algorithm.Portfolio.TotalPortfolioValue / list.Count / sectorCount *
                    (1 - Algorithm.Settings.FreePortfolioValuePercentage);

                expectedTargets.AddRange(list
                    .Select(x => new PortfolioTarget(x.Key, x.Key.Value == "SPY" ? 0
                        : (int)direction * Math.Floor(amount / x.Value.Price))));
            }

            // Do no include SPY in the insights
            insights = Algorithm.Securities.Keys.Where(x => x.Value != "SPY")
                .Select(x => GetInsight(x, direction, Algorithm.UtcTime)).ToArray();

            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights);

            Assert.Greater(Algorithm.Securities.Count, expectedTargets.Count);
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

            var insights = new[] { GetInsight(Symbols.SPY, InsightDirection.Down, Algorithm.UtcTime) };
            var targets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights).ToList();
            Assert.AreEqual(1, targets.Count);

            // Removing SPY should clear the key in the insight collection
            var changes = SecurityChanges.Removed(Algorithm.Securities[Symbols.SPY]);
            Algorithm.PortfolioConstruction.OnSecuritiesChanged(Algorithm, changes);

            // Since we have 3 B, 2 T and 1 X, each security in each sector will get 
            // B => .166%, T => .25, X => 0% (removed)
            var sectorCount = 2;
            var groupedBySector = Algorithm.Securities
                .Where(pair => pair.Value.Fundamentals?.CompanyReference?.IndustryTemplateCode != null)
                .GroupBy(pair => pair.Value.Fundamentals.CompanyReference.IndustryTemplateCode);

            var expectedTargets = new List<PortfolioTarget>();

            foreach (var securities in groupedBySector)
            {
                var list = securities.ToList();
                var amount = Algorithm.Portfolio.TotalPortfolioValue / list.Count / sectorCount *
                    (1 - Algorithm.Settings.FreePortfolioValuePercentage);

                expectedTargets.AddRange(list
                    .Select(x => new PortfolioTarget(x.Key, x.Key.Value == "SPY" ? 0
                        : (int)direction * Math.Floor(amount / x.Value.Price))));
            }

            // Do no include SPY in the insights
            insights = Algorithm.Securities.Keys.Where(x => x.Value != "SPY")
                .Select(x => GetInsight(x, direction, Algorithm.UtcTime)).ToArray();

            // Create target from an empty insights array
            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights);

            Assert.Greater(Algorithm.Securities.Count, expectedTargets.Count);
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

            // Since we have 3 B, 2 T and 1 X, each security in each sector will get 
            // B => .166%, T => .25, X => 0% (removed)
            var sectorCount = 2;
            var groupedBySector = Algorithm.Securities
                .Where(pair => pair.Value.Fundamentals?.CompanyReference?.IndustryTemplateCode != null)
                .GroupBy(pair => pair.Value.Fundamentals.CompanyReference.IndustryTemplateCode);

            var expectedTargets = new List<PortfolioTarget>();

            foreach (var securities in groupedBySector)
            {
                var list = securities.ToList();
                var amount = Algorithm.Portfolio.TotalPortfolioValue / list.Count / sectorCount *
                    (1 - Algorithm.Settings.FreePortfolioValuePercentage);

                expectedTargets.AddRange(list
                    .Select(x => new PortfolioTarget(x.Key, x.Key.Value == "SPY" ? 0
                        : (int)direction * Math.Floor(amount / x.Value.Price))));
            }

            var insights = Algorithm.Securities.Keys.Select(x =>
            {
                // SPY insight direction is flat
                var actualDirection = x.Value == "SPY" ? InsightDirection.Flat : direction;
                return GetInsight(x, actualDirection, Algorithm.UtcTime);
            });
            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights.ToArray());

            Assert.Greater(Algorithm.Securities.Count, expectedTargets.Count);
            AssertTargets(expectedTargets, actualTargets);
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void WeightsBySectorProportionally(Language language)
        {
            SetPortfolioConstruction(language);

            // create two insights whose weights sums up to 2
            var insights = Algorithm.Securities
                .Select(x => GetInsight(x.Key, InsightDirection.Up, Algorithm.UtcTime))
                .ToArray();

            // Since we have 3 B, 2 T and 1 X, each security in each secotr will get B => .11%, T => .165, X => .33%
            var sectorCount = 3;
            var groupedBySector = Algorithm.Securities
                .Where(pair => pair.Value.Fundamentals?.CompanyReference?.IndustryTemplateCode != null)
                .Where(pair => insights.Any(insight => pair.Key == insight.Symbol))
                .GroupBy(pair => pair.Value.Fundamentals.CompanyReference.IndustryTemplateCode);

            var expectedTargets = new List<PortfolioTarget>();

            foreach (var securities in groupedBySector)
            {
                var list = securities.ToList();
                var amount = Algorithm.Portfolio.TotalPortfolioValue / list.Count / sectorCount *
                    (1 - Algorithm.Settings.FreePortfolioValuePercentage);

                expectedTargets.AddRange(list
                    .Select(x => new PortfolioTarget(x.Key, (int)InsightDirection.Up * Math.Floor(amount / x.Value.Price))));
            }

            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights).ToList();
            Assert.AreEqual(6, actualTargets.Count);
            Assert.Greater(Algorithm.Securities.Count, expectedTargets.Count);
            AssertTargets(expectedTargets, actualTargets);
        }

        public override Insight GetInsight(Symbol symbol, InsightDirection direction, DateTime generatedTimeUtc, TimeSpan? period = null, double? weight = _weight)
        {
            period = period ?? TimeSpan.FromDays(1);
            var insight = Insight.Price(symbol, period.Value, direction, weight: weight);
            insight.GeneratedTimeUtc = generatedTimeUtc;
            insight.CloseTimeUtc = generatedTimeUtc.Add(period.Value);
            return insight;
        }

        public override IPortfolioConstructionModel GetPortfolioConstructionModel(Language language, dynamic paramenter = null)
        {
            if (language == Language.CSharp)
            {
                return new SectorWeightingPortfolioConstructionModel(paramenter);
            }

            using (Py.GIL())
            {
                const string name = nameof(SectorWeightingPortfolioConstructionModel);
                var instance = Py.Import(name).GetAttr(name).Invoke(((object)paramenter).ToPython());
                return new PortfolioConstructionModelPythonWrapper(instance);
            }
        }
    }
}
