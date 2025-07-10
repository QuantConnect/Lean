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
    internal class MaxDradownRecoveryTests
    {
        [Test, TestCaseSource(nameof(TestCases))]
        public void DrawdownMetrics_MaxRecoveryTime_Tests(List<decimal> data, decimal expectedRecoveryTime, string description)
        {
            var startDate = new DateTime(2025, 1, 1);
            var equity = new SortedDictionary<DateTime, decimal>();

            for (int i = 0; i < data.Count; i++)
            {
                var value = data[i];
                equity[startDate.AddDays(i)] = value;
            }

            var result = QuantConnect.Statistics.Statistics.CalculateDrawdownMetrics(equity).MaxRecoveryTime;
            Assert.AreEqual(expectedRecoveryTime, result, description);
        }

        private static IEnumerable<TestCaseData> TestCases()
        {
            // Basic recovery cases
            yield return new TestCaseData(new List<decimal> { 100, 90, 100 }, 2m, "RecoveryAfterOneDip_2Days");
            yield return new TestCaseData(new List<decimal> { 100, 90, 95, 100 }, 3m, "RecoveryAfterPartialThenFull_3Days");

            // Multiple drawdown cases
            yield return new TestCaseData(new List<decimal> { 100, 90, 100, 90, 100 }, 2m, "RecoveryFromTwoEqualDips_2DaysEach");
            yield return new TestCaseData(new List<decimal> { 100, 90, 95, 90, 100 }, 4m, "RecoveryFromNestedDrawdowns_4Days");

            // No recovery cases
            yield return new TestCaseData(new List<decimal> { 100, 90, 80, 70 }, 0m, "NoRecoveryContinuousDecline");
            yield return new TestCaseData(new List<decimal> { 100, 90, 95, 90 }, 0m, "NoRecoveryPartialButNoNewHigh");

            // Edge cases
            yield return new TestCaseData(new List<decimal> { 50, 100, 98, 99, 100 }, 3m, "RecoveryFromSecondaryPeak_3Days");
            yield return new TestCaseData(new List<decimal> { 100, 100, 100 }, 0m, "NoDrawdownFlatLine");
            yield return new TestCaseData(new List<decimal> { 100 }, 0m, "NoDrawdownSingleValue");
            yield return new TestCaseData(new List<decimal>(), 0m, "NoDrawdownEmptyList");

            // Complex scenarios
            yield return new TestCaseData(new List<decimal> { 100, 98, 100, 101, 100, 99 }, 2m, "RecoveryBeforeNewHigh_2Days");
            yield return new TestCaseData(new List<decimal> { 100, 97, 99, 97, 100 }, 4m, "RecoveryWithMultipleDips_4Days");
        }
    }
}
