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
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.Alphas
{
    [TestFixture]
    public class InsightAnalysisContextTests
    {
        [TestCase(InsightScoreType.Direction, false, InsightType.Price, null)]
        [TestCase(InsightScoreType.Magnitude, false, InsightType.Price, null)]
        [TestCase(InsightScoreType.Direction, false, InsightType.Volatility, null)]
        [TestCase(InsightScoreType.Magnitude, false, InsightType.Volatility, null)]
        [TestCase(InsightScoreType.Direction, false, InsightType.Price, 1.0)]
        [TestCase(InsightScoreType.Magnitude, true, InsightType.Price, 1.0)]
        [TestCase(InsightScoreType.Direction, false, InsightType.Volatility, 1.0)]
        [TestCase(InsightScoreType.Magnitude, true, InsightType.Volatility, 1.0)]
        public void ShouldAnalyzeInsight(InsightScoreType scoreType,
            bool result,
            InsightType insightType,
            double? magnitude)
        {
            var context = new InsightAnalysisContext(
                new Insight(Symbols.SPY,
                    TimeSpan.FromDays(1),
                    insightType,
                    InsightDirection.Flat,
                    magnitude,
                    null),
                new SecurityValues(Symbols.SPY,
                    new DateTime(2013, 1, 1),
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    1, 1, 1, 1),
                TimeSpan.FromDays(1));
            Assert.AreEqual(result, context.ShouldAnalyze(scoreType));
        }
    }
}
