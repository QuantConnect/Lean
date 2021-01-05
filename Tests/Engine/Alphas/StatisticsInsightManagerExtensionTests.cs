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
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;
using QuantConnect.Lean.Engine.Alphas;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Engine.Alphas
{
    [TestFixture]
    public class StatisticsInsightManagerExtensionTests
    {
        [Test]
        public void DefaultConstructorHasZeroWarmupPeriodForPopulationAverageScores()
        {
            var stats = new StatisticsInsightManagerExtension(new TestAccountCurrencyProvider());
            Assert.IsTrue(stats.RollingAverageIsReady);
        }

        [Test]
        public void RecordsPopulationAverageScoresOnInsightAnalysisCompleted()
        {
            var time = new DateTime(2000, 01, 01);
            var stats = new StatisticsInsightManagerExtension(new TestAccountCurrencyProvider());
            var insight = Insight.Price(Symbols.SPY, Time.OneDay, InsightDirection.Up, magnitude: 1.0);
            var spySecurityValues = new SecurityValues(insight.Symbol, time, SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), 100m, 1m, 125000, 1m);
            var context = new InsightAnalysisContext(insight, spySecurityValues, insight.Period);
            context.Score.SetScore(InsightScoreType.Direction, .55, time);
            context.Score.SetScore(InsightScoreType.Magnitude, .25, time);
            stats.OnInsightAnalysisCompleted(context);
            Assert.AreEqual(context.Score.Direction, stats.Statistics.RollingAveragedPopulationScore.Direction);
            Assert.AreEqual(context.Score.Magnitude, stats.Statistics.RollingAveragedPopulationScore.Magnitude);
        }

        [TestCase(InsightScoreType.Direction, InsightType.Price, null)]
        [TestCase(InsightScoreType.Magnitude, InsightType.Price, null)]
        [TestCase(InsightScoreType.Direction, InsightType.Volatility, null)]
        [TestCase(InsightScoreType.Magnitude, InsightType.Volatility, null)]
        [TestCase(InsightScoreType.Direction, InsightType.Price, 1.0)]
        [TestCase(InsightScoreType.Magnitude, InsightType.Price, 1.0)]
        [TestCase(InsightScoreType.Direction, InsightType.Volatility, 1.0)]
        [TestCase(InsightScoreType.Magnitude, InsightType.Volatility, 1.0)]
        public void IgnoresFlatInsightsWithScore(InsightScoreType scoreType, InsightType insightType, double? magnitude)
        {
            var time = new DateTime(2000, 01, 01);
            var stats = new StatisticsInsightManagerExtension(new TestAccountCurrencyProvider());
            var insight = new Insight(Symbols.SPY, TimeSpan.FromDays(1), insightType, InsightDirection.Flat, magnitude, null);
            var spySecurityValues = new SecurityValues(insight.Symbol, time, SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), 100m, 1m, 125000, 1m);
            var context = new InsightAnalysisContext(insight, spySecurityValues, insight.Period);
            context.Score.SetScore(InsightScoreType.Direction, .55, time);
            context.Score.SetScore(InsightScoreType.Magnitude, .25, time);
            stats.OnInsightAnalysisCompleted(context);
            Assert.AreEqual(0.0, stats.Statistics.RollingAveragedPopulationScore.Direction);
            Assert.AreEqual(0.0, stats.Statistics.RollingAveragedPopulationScore.Magnitude);
        }
    }
}
