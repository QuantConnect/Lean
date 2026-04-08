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
using NUnit.Framework;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    internal class DrawdownRecoveryTests
    {
        [Test, TestCaseSource(nameof(TestCases))]
        public void DrawdownMetricsMaximumRecoveryTimeTests(List<decimal> data, decimal expectedRecoveryTime)
        {
            var startDate = new DateTime(2025, 1, 1);
            var points = new List<ISeriesPoint>();

            for (int i = 0; i < data.Count; i++)
            {
                points.Add(new ChartPoint(startDate.AddDays(i), data[i]));
            }

            var result = QuantConnect.Statistics.Statistics.CalculateDrawdownMetrics(points).DrawdownRecovery;
            Assert.AreEqual(expectedRecoveryTime, result);
        }

        [Test]
        public void CandlestickUsesHighForPeakAndLowForDrawdown()
        {
            // Day 2 High=105 sets the peak: Low=80 -> drawdown = (80/105) - 1 --> 0.24
            var startDate = new DateTime(2025, 1, 1);

            var points = new List<ISeriesPoint>
            {
                new Candlestick(startDate, 100m, 100m, 100m, 100m),
                new Candlestick(startDate.AddDays(1), 95m, 105m, 80m, 95m),
                new Candlestick(startDate.AddDays(2), 95m, 106m, 100m, 100m),
            };

            var metrics = QuantConnect.Statistics.Statistics.CalculateDrawdownMetrics(points);

            Assert.AreEqual(0.24m, metrics.Drawdown);
        }

        [Test]
        public void CandlestickWithNullHighLowFallsBackToClose()
        {
            // When High/Low are null, Close is used for both peak and drawdown
            var startDate = new DateTime(2025, 1, 1);
            var points = new List<ISeriesPoint>
            {
                new Candlestick(startDate, null, null, null, 100m),
                new Candlestick(startDate.AddDays(1), null, null, null, 90m),
                new Candlestick(startDate.AddDays(2), null, null, null, 100m),
            };

            var metrics = QuantConnect.Statistics.Statistics.CalculateDrawdownMetrics(points);

            Assert.AreEqual(0.1m, metrics.Drawdown);
            Assert.AreEqual(2, metrics.DrawdownRecovery);
        }

        private static IEnumerable<TestCaseData> TestCases()
        {
            yield return new TestCaseData(new List<decimal> { 100, 90, 100 }, 2m).SetName("RecoveryAfterOneDip2Days");
            yield return new TestCaseData(new List<decimal> { 100, 90, 95, 100 }, 3m).SetName("RecoveryAfterPartialThenFull3Days");

            yield return new TestCaseData(new List<decimal> { 100, 90, 100, 90, 100 }, 2m).SetName("RecoveryFromTwoEqualDips2DaysEach");
            yield return new TestCaseData(new List<decimal> { 100, 90, 100, 90, 80, 100 }, 3m).SetName("TakesLongestRecoveryAmongMultipleDrawdowns");
            yield return new TestCaseData(new List<decimal> { 100, 90, 95, 90, 100 }, 4m).SetName("RecoveryFromNestedDrawdowns4Days");

            yield return new TestCaseData(new List<decimal> { 100, 90, 80, 70 }, 0m).SetName("NoRecoveryContinuousDecline");
            yield return new TestCaseData(new List<decimal> { 100, 90, 95, 90 }, 0m).SetName("NoRecoveryPartialButNoNewHigh");

            yield return new TestCaseData(new List<decimal> { 50, 100, 98, 99, 100 }, 3m).SetName("RecoveryFromSecondaryPeak3Days");
            yield return new TestCaseData(new List<decimal> { 100, 100, 100 }, 0m).SetName("NoDrawdownFlatLine");
            yield return new TestCaseData(new List<decimal> { 100 }, 0m).SetName("NoDrawdownSingleValue");
            yield return new TestCaseData(new List<decimal>(), 0m).SetName("NoDrawdownEmptyList");

            yield return new TestCaseData(new List<decimal> { 100, 98, 100, 101, 100, 99 }, 2m).SetName("RecoveryBeforeNewHigh2Days");
            yield return new TestCaseData(new List<decimal> { 100, 97, 99, 97, 100 }, 4m).SetName("RecoveryWithMultipleDips4Days");
        }
    }
}
