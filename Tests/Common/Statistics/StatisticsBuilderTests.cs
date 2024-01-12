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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class StatisticsBuilderTests
    {
        /// <summary>
        /// TradingDaysPerYear: Use like backward compatibility
        /// </summary>
        /// <remarks><see cref="Interfaces.IAlgorithmSettings.TradingDaysPerYear"></remarks>
        protected const int _tradingDaysPerYear = 252;

        [Test]
        public void MisalignedValues_ShouldThrow_DuringGeneration()
        {
            var testBenchmarkPoints = new List<ChartPoint>
            {
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 1, 16, 0, 0), DateTimeKind.Utc), 100),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 2, 16, 0, 0), DateTimeKind.Utc), 102),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 3, 16, 0, 0), DateTimeKind.Utc), 110),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 4, 16, 0, 0), DateTimeKind.Utc), 110),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 5, 16, 0, 0), DateTimeKind.Utc), 120),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 6, 16, 0, 0), DateTimeKind.Utc), 130),
            };

            var testEquityPoints = new List<ChartPoint>
            {
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2018, 12, 31, 16, 0, 0), DateTimeKind.Utc), 100000),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 1, 16, 0, 0), DateTimeKind.Utc), 100000),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 2, 16, 0, 0), DateTimeKind.Utc), 102000),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 3, 16, 0, 0), DateTimeKind.Utc), 110000),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 4, 16, 0, 0), DateTimeKind.Utc), 110000),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 5, 16, 0, 0), DateTimeKind.Utc), 120000),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 6, 16, 0, 0), DateTimeKind.Utc), 130000),
            };

            var misalignedTestPerformancePoints = new List<ChartPoint>
            {
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2018, 12, 31), DateTimeKind.Utc), 1000m * 100m),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 1, 16, 0, 0), DateTimeKind.Utc), 0.25m * 100m),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 2, 16, 0, 0), DateTimeKind.Utc), 0.02m * 100m),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 3, 16, 0, 0), DateTimeKind.Utc), 0.0784313725490196m * 100m),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 4, 16, 0, 0), DateTimeKind.Utc), 0 * 100m),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 5, 16, 0, 0), DateTimeKind.Utc), 0.090909090909090m * 100m),
                new ChartPoint(DateTime.SpecifyKind(new DateTime(2019, 1, 6, 16, 0, 0), DateTimeKind.Utc), 0.083333333333333m * 100m)
            };

            Assert.Throws<ArgumentException>(() =>
            {
                StatisticsBuilder.Generate(
                    new List<Trade>(),
                    new SortedDictionary<DateTime, decimal>(),
                    testEquityPoints.Cast<ISeriesPoint>().ToList(),
                    misalignedTestPerformancePoints.Cast<ISeriesPoint>().ToList(),
                    testBenchmarkPoints.Cast<ISeriesPoint>().ToList(),
                    new List<ISeriesPoint>(),
                    100000m,
                    0m,
                    1,
                    null,
                    "$",
                    new QuantConnect.Securities.SecurityTransactionManager(
                        null,
                        new QuantConnect.Securities.SecurityManager(new TimeKeeper(DateTime.UtcNow))),
                    new InterestRateProvider(),
                    _tradingDaysPerYear);
            }, "Misaligned values provided, but we still generate statistics");
        }
    }
}

