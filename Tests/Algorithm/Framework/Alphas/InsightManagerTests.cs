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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    [TestFixture]
    public class InsightManagerTests
    {
        private static readonly DateTime _utcNow = new(2019, 1, 1);

        [TestCase(false)]
        [TestCase(true)]
        public void ExpireSameTime(bool useCancelApi)
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetDateTime(_utcNow);
            var insightManager = new InsightManager(algorithm);
            insightManager.AddRange(GetInsights());

            Assert.IsTrue(insightManager.All(insight => insight.IsActive(_utcNow)));

            if (useCancelApi)
            {
                insightManager.Cancel(new[] { Symbols.IBM, Symbols.SPY });
            }
            else
            {
                insightManager.Expire(new[] { Symbols.IBM, Symbols.SPY });
            }

            Assert.IsTrue(
                insightManager[Symbols.IBM]
                    .All(insight =>
                        insight.IsExpired(algorithm.UtcTime)
                        && insight.Period == TimeSpan.FromSeconds(-1)
                    )
            );
            Assert.IsTrue(
                insightManager[Symbols.SPY]
                    .All(insight =>
                        insight.IsExpired(algorithm.UtcTime)
                        && insight.Period == TimeSpan.FromSeconds(-1)
                    )
            );
            Assert.IsTrue(
                insightManager[Symbols.AAPL].All(insight => insight.IsActive(algorithm.UtcTime))
            );
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExpireBySymbol(bool useCancelApi)
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetDateTime(_utcNow);
            var insightManager = new InsightManager(algorithm);
            insightManager.AddRange(GetInsights());

            Assert.IsTrue(insightManager.All(insight => insight.IsActive(_utcNow)));

            algorithm.SetDateTime(algorithm.UtcTime.AddMinutes(1));
            if (useCancelApi)
            {
                insightManager.Cancel(new[] { Symbols.IBM, Symbols.SPY });
            }
            else
            {
                insightManager.Expire(new[] { Symbols.IBM, Symbols.SPY });
            }

            var expectedPeriod = Time.OneMinute.Subtract(Time.OneSecond);
            Assert.IsTrue(
                insightManager[Symbols.IBM]
                    .All(insight =>
                        insight.IsExpired(algorithm.UtcTime) && insight.Period == expectedPeriod
                    )
            );
            Assert.IsTrue(
                insightManager[Symbols.SPY]
                    .All(insight =>
                        insight.IsExpired(algorithm.UtcTime) && insight.Period == expectedPeriod
                    )
            );
            Assert.IsTrue(
                insightManager[Symbols.AAPL].All(insight => insight.IsActive(algorithm.UtcTime))
            );
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExpireByInsight(bool useCancelApi)
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetDateTime(_utcNow);
            var insights = GetInsights();
            var insightManager = new InsightManager(algorithm);
            insightManager.AddRange(insights);

            Assert.IsTrue(insightManager.All(insight => insight.IsActive(_utcNow)));

            algorithm.SetDateTime(algorithm.UtcTime.AddMinutes(1));
            if (useCancelApi)
            {
                insightManager.Cancel(new[] { insights[2], insights[3] });
            }
            else
            {
                insightManager.Expire(new[] { insights[2], insights[3] });
            }

            var expectedPeriod = Time.OneMinute.Subtract(Time.OneSecond);
            Assert.IsTrue(
                insightManager[Symbols.IBM]
                    .All(insight =>
                        insight.IsExpired(algorithm.UtcTime) && insight.Period == expectedPeriod
                    )
            );
            Assert.AreEqual(
                1,
                insightManager[Symbols.SPY]
                    .Count(insight =>
                        insight.IsExpired(algorithm.UtcTime) && insight.Period == expectedPeriod
                    )
            );
            Assert.AreEqual(
                1,
                insightManager[Symbols.SPY].Count(insight => insight.IsActive(algorithm.UtcTime))
            );
            Assert.IsTrue(
                insightManager[Symbols.AAPL].All(insight => insight.IsActive(algorithm.UtcTime))
            );
        }

        private static Insight[] GetInsights()
        {
            return new[]
            {
                new Insight(
                    Symbols.AAPL,
                    new TimeSpan(1, 0, 0, 0),
                    InsightType.Price,
                    InsightDirection.Up
                )
                {
                    GeneratedTimeUtc = _utcNow,
                    CloseTimeUtc = _utcNow.AddDays(1),
                },
                new Insight(
                    Symbols.SPY,
                    new TimeSpan(2, 0, 0, 0),
                    InsightType.Volatility,
                    InsightDirection.Up
                )
                {
                    GeneratedTimeUtc = _utcNow,
                    CloseTimeUtc = _utcNow.AddDays(2),
                },
                new Insight(
                    Symbols.SPY,
                    new TimeSpan(3, 0, 0, 0),
                    InsightType.Volatility,
                    InsightDirection.Up
                )
                {
                    GeneratedTimeUtc = _utcNow,
                    CloseTimeUtc = _utcNow.AddDays(3),
                },
                new Insight(
                    Symbols.IBM,
                    new TimeSpan(4, 0, 0, 0),
                    InsightType.Volatility,
                    InsightDirection.Up
                )
                {
                    GeneratedTimeUtc = _utcNow,
                    CloseTimeUtc = _utcNow.AddDays(4),
                }
            };
        }
    }
}
