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
using NUnit.Framework;

using QuantConnect.Lean.Engine.Results;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class BacktestProgressMonitorTests
    {
        [TestCaseSource(nameof(TotalDaysCalculationTestCases))]
        public void CalculatesTotalDays(DateTime start, DateTime end)
        {
            var timeKeeper = new TimeKeeper(start);

            var progressMonitor = new BacktestProgressMonitor(timeKeeper, start, end);

            Assert.AreEqual((end - start).TotalDays + 1, progressMonitor.TotalDays);
        }

        [Test]
        public void CalculatesProgress()
        {
            var start = new DateTime(2020, 1, 2);
            var end = start.AddMonths(10);
            var timeKeeper = new TimeKeeper(start);

            var progressMonitor = new BacktestProgressMonitor(timeKeeper, start, end);

            Assert.AreEqual(0, progressMonitor.ProcessedDays);
            Assert.AreEqual(0m, progressMonitor.Progress);

            var steps = 15;
            for (var i = 0; i < steps; i++)
            {
                timeKeeper.SetUtcDateTime(start.Add((end - start) * i / steps));
                progressMonitor.RecalculateProcessedDays();

                var expectedProcessedDays = (int)(timeKeeper.UtcTime- start).TotalDays;
                Assert.AreEqual(expectedProcessedDays, progressMonitor.ProcessedDays);
                Assert.AreEqual((decimal) expectedProcessedDays / progressMonitor.TotalDays, progressMonitor.Progress);
            }
        }

        private static TestCaseData[] TotalDaysCalculationTestCases => new TestCaseData[]
        {
            new(new DateTime(2020, 1, 2), new DateTime(2020, 1, 2)),
            new(new DateTime(2020, 1, 2), new DateTime(2020, 1, 3)),
            new(new DateTime(2020, 1, 2), new DateTime(2020, 2, 2)),
            new(new DateTime(2020, 1, 2), new DateTime(2021, 1, 2))
        };
    }
}
