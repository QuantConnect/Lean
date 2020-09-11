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
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class InsightWeightingPortfolioConstructionModelTests : EqualWeightingPortfolioConstructionModelTests
    {
        private const double _weight = 0.01;

        public override double? Weight => _weight;

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void WeightsProportionally(Language language)
        {
            SetPortfolioConstruction(language);

            // create two insights whose weights sums up to 2
            var insights = new[]
            {
                GetInsight(Symbols.SPY, InsightDirection.Up, Algorithm.UtcTime, weight:1),
                GetInsight(Symbol.Create("IBM", SecurityType.Equity, Market.USA),
                    InsightDirection.Up, Algorithm.UtcTime, weight:1)
            };

            // they will each share, proportionally, the total portfolio value
            var amount = Algorithm.Portfolio.TotalPortfolioValue * (decimal)0.5;
            var expectedTargets = Algorithm.Securities.Where(pair => insights.Any(insight => pair.Key == insight.Symbol))
                .Select(x => new PortfolioTarget(x.Key, (int)InsightDirection.Up
                    * Math.Floor(amount * (1 - Algorithm.Settings.FreePortfolioValuePercentage)
                        / x.Value.Price)));

            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights).ToList();
            Assert.AreEqual(2, actualTargets.Count);
            AssertTargets(expectedTargets, actualTargets);
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void GeneratesNoTargetsForInsightsWithNoWeight(Language language)
        {
            SetPortfolioConstruction(language);

            var insights = new[]
            {
                GetInsight(Symbols.SPY, InsightDirection.Down, Algorithm.UtcTime, weight:null)
            };

            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights).ToList();
            Assert.AreEqual(0, actualTargets.Count);
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void GeneratesZeroTargetForZeroInsightWeight(Language language)
        {
            SetPortfolioConstruction(language);

            var insights = new[]
            {
                GetInsight(Symbols.SPY, InsightDirection.Down, Algorithm.UtcTime, weight:0)
            };

            var actualTargets = Algorithm.PortfolioConstruction.CreateTargets(Algorithm, insights).ToList();
            Assert.AreEqual(1, actualTargets.Count);
            AssertTargets(actualTargets, new[] {new PortfolioTarget(Symbols.SPY, 0)});
        }

        public override IPortfolioConstructionModel GetPortfolioConstructionModel(Language language, dynamic paramenter = null)
        {
            if (language == Language.CSharp)
            {
                return new InsightWeightingPortfolioConstructionModel(paramenter);
            }

            using (Py.GIL())
            {
                const string name = nameof(InsightWeightingPortfolioConstructionModel);
                var instance = Py.Import(name).GetAttr(name).Invoke(((object)paramenter).ToPython());
                return new PortfolioConstructionModelPythonWrapper(instance);
            }
        }

        public override Insight GetInsight(Symbol symbol, InsightDirection direction, DateTime generatedTimeUtc, TimeSpan? period = null, double? weight = _weight)
        {
            period = period ?? TimeSpan.FromDays(1);
            var insight = Insight.Price(symbol, period.Value, direction, weight: weight);
            insight.GeneratedTimeUtc = generatedTimeUtc;
            insight.CloseTimeUtc = generatedTimeUtc.Add(period.Value);
            return insight;
        }

        public override List<IPortfolioTarget> GetTargetsForSPY()
        {
            return new List<IPortfolioTarget> { PortfolioTarget.Percent(Algorithm, Symbols.SPY, -_weight) };
        }
    }
}
