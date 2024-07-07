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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class PeriodCountConsolidatorTests
    {
        private static readonly object[] PeriodCases =
        {
            new[] { TimeSpan.FromDays(100), TimeSpan.FromDays(10) },
            new[] { TimeSpan.FromDays(30), TimeSpan.FromDays(1) }, //GH Issue #4915
            new[] { TimeSpan.FromDays(10), TimeSpan.FromDays(1) },
            new[] { TimeSpan.FromDays(1), TimeSpan.FromHours(1) },
            new[] { TimeSpan.FromHours(10), TimeSpan.FromHours(1) },
            new[] { TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1) },
            new[] { TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10) },
            new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.1) }
        };

        [TestCaseSource(nameof(PeriodCases))]
        public void ExpectedConsolidatedTradeBarsInPeriodMode(TimeSpan barSpan, TimeSpan updateSpan)
        {
            TradeBar consolidated = null;
            using var consolidator = new BaseDataConsolidator(barSpan);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                Assert.AreEqual(barSpan, bar.Period); // The period matches our span
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13);
            var dataTime = reference;

            var nextBarTime = reference + barSpan;
            var lastBarTime = reference;

            // First data point
            consolidator.Update(new Tick { Time = dataTime });
            Assert.IsNull(consolidated);

            for (var i = 0; i < 10; i++)
            {
                // Add data on the given interval until we expect a new bar
                while (dataTime < nextBarTime)
                {
                    dataTime = dataTime.Add(updateSpan);
                    consolidator.Update(new Tick { Time = dataTime });
                }

                // Our asserts
                Assert.IsNotNull(consolidated); // We have a bar
                Assert.AreEqual(dataTime, consolidated.EndTime); // New bar time should be dataTime
                Assert.AreEqual(barSpan, consolidated.EndTime - lastBarTime); // The difference between the bars is the span

                nextBarTime = dataTime + barSpan;
                lastBarTime = consolidated.EndTime;
            }
        }

        [TestCaseSource(nameof(PeriodCases))]
        public void ExpectedConsolidatedQuoteBarsInPeriodMode(TimeSpan barSpan, TimeSpan updateSpan)
        {
            QuoteBar consolidated = null;
            using var consolidator = new QuoteBarConsolidator(barSpan);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                Assert.AreEqual(barSpan, bar.Period); // The period matches our span
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13);
            var dataTime = reference;

            var nextBarTime = reference + barSpan;
            var lastBarTime = reference;

            // First data point
            consolidator.Update(new QuoteBar { Time = dataTime, Period = updateSpan });
            Assert.IsNull(consolidated);

            for (var i = 0; i < 10; i++)
            {
                // Add data on the given interval until we expect a new bar
                while (dataTime < nextBarTime)
                {
                    dataTime = dataTime.Add(updateSpan);
                    consolidator.Update(new QuoteBar { Time = dataTime, Period = updateSpan });
                }

                // Our asserts
                Assert.IsNotNull(consolidated); // We have a bar
                Assert.AreEqual(dataTime, consolidated.EndTime); // New bar time should be dataTime
                Assert.AreEqual(barSpan, consolidated.EndTime - lastBarTime); // The difference between the bars is the span

                nextBarTime = dataTime + barSpan;
                lastBarTime = consolidated.EndTime;
            }
        }

        [Test]
        public void ConsolidatorEmitsOffsetBarsCorrectly()
        {
            // This test is to cover an issue seen with the live data stack
            // The consolidator would fail to emit every other bar because of a
            // ms delay in data from a live stream
            var period = TimeSpan.FromHours(2);
            using var consolidator = new TradeBarConsolidator(period);
            var consolidatedBarsCount = 0;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidatedBarsCount++;
            };

            var random = new Random();
            var time = new DateTime(2015, 04, 13);

            // The bars time is accurate, covering the hour perfectly
            // But the emit time is slightly offset (the timeslice that contains the bar)
            // So add a random ms offset to the scan time
            consolidator.Update(new TradeBar { Time = time, Period = Time.OneHour });
            time = time.Add(period);
            consolidator.Scan(time.AddMilliseconds(random.Next(800)));

            consolidator.Update(new TradeBar { Time = time, Period = Time.OneHour });
            time = time.Add(period);
            consolidator.Scan(time.AddMilliseconds(random.Next(800)));

            consolidator.Update(new TradeBar { Time = time, Period = Time.OneHour });
            time = time.Add(period);
            consolidator.Scan(time.AddMilliseconds(random.Next(800)));

            consolidator.Update(new TradeBar { Time = time, Period = Time.OneHour });
            time = time.Add(period);
            consolidator.Scan(time.AddMilliseconds(random.Next(800)));

            // We should expect to see 4 bars emitted from the consolidator
            Assert.AreEqual(4, consolidatedBarsCount);
        }

        [Test]
        public void ConsolidatorEmitsOldBarsUsingUpdate()
        {
            // This test is to ensure that no bars get swallowed by the consolidator
            // even if it doesn't get the data on regular intervals.
            // We will use the PushThrough method which calls update
            var period = TimeSpan.FromHours(1);
            using var consolidator = new TradeBarConsolidator(period);
            TradeBar latestConsolidated = null;
            var consolidatedBarsCount = 0;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestConsolidated = bar;
                consolidatedBarsCount++;
            };

            // Set our starting time 04/13/2015 at 12:00AM
            var time = new DateTime(2015, 04, 13);

            // Update this consolidator with minute tradebars but one less than 60, which would trigger emit
            PushBarsThrough(59, Time.OneMinute, consolidator, ref time);

            // No bars should be emitted, lets assert the current time and count
            Assert.IsTrue(time == new DateTime(2015, 04, 13, 0, 59, 0));
            Assert.AreEqual(0, consolidatedBarsCount);

            // Advance time way past (3 hours) the bar end time of 1AM
            time += TimeSpan.FromHours(3); // Time = 3:59AM now

            // Push one bar through at 3:59AM and check that we still get the 12AM - 1AM Bar emitted
            PushBarsThrough(1, Time.OneMinute, consolidator, ref time);
            Assert.AreEqual(1, consolidatedBarsCount);
            Assert.IsTrue(
                latestConsolidated != null && latestConsolidated.Time == new DateTime(2015, 04, 13)
            );

            // Check the new working bar is 3AM to 4AM, This is because we pushed a bar in at 3:59AM
            Assert.IsTrue(consolidator.WorkingBar.Time == new DateTime(2015, 04, 13, 3, 0, 0));
        }

        [Test]
        public void ConsolidatorEmitsOldBarsUsingScan()
        {
            // This test is to ensure that no bars get swallowed by the consolidator
            // even if it doesn't get the data on regular intervals.
            // We will use Consolidators Scan method to emit bars
            var period = TimeSpan.FromHours(1);
            using var consolidator = new TradeBarConsolidator(period);
            TradeBar latestConsolidated = null;
            var consolidatedBarsCount = 0;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestConsolidated = bar;
                consolidatedBarsCount++;
            };

            var time = new DateTime(2015, 04, 13);

            // Push through one bar at 12:00AM to create the consolidators working bar
            PushBarsThrough(1, Time.OneMinute, consolidator, ref time);

            // There should be no emit, lets assert the current time and count
            Assert.IsTrue(time == new DateTime(2015, 04, 13, 0, 1, 0));
            Assert.AreEqual(0, consolidatedBarsCount);

            // Now advance time way past (3 Hours) the bar end time of 1AM
            time += TimeSpan.FromHours(3); // Time = 3:59AM now

            // Call scan with current time, it should emit the 12AM - 1AM Bar without any update
            consolidator.Scan(time);
            Assert.AreEqual(1, consolidatedBarsCount);
            Assert.IsTrue(
                latestConsolidated != null
                    && latestConsolidated.Time == new DateTime(2015, 04, 13, 0, 0, 0)
            );

            // WorkingBar should be null, ready for whatever data comes through next
            Assert.IsTrue(consolidator.WorkingBar == null);
        }

        [Test]
        public void ConsolidatorEmitsRegularly()
        {
            // This test just pushes through 1000 bars
            // and ensures that the emit time and count are correct
            var period = TimeSpan.FromHours(2);
            using var consolidator = new TradeBarConsolidator(period);
            var consolidatedBarsCount = 0;
            var time = new DateTime(2015, 04, 13);

            consolidator.DataConsolidated += (sender, bar) =>
            {
                Assert.IsTrue(bar.EndTime == time);
                consolidatedBarsCount++;
            };

            PushBarsThrough(1000, Time.OneHour, consolidator, ref time);

            // Scan one last time so we can emit the 1000th bar
            consolidator.Scan(time);
            Assert.AreEqual(500, consolidatedBarsCount);
        }

        [TestCase(14)] // 2PM
        [TestCase(15)] // 3PM
        [TestCase(16)] // 4PM
        public void BarsEmitOnTime(int hour)
        {
            // This test just pushes one full hourly bar into a consolidator
            // and scans to see if it will emit immediately as expected

            using var consolidator = new TradeBarConsolidator(Time.OneHour);
            var consolidatedBarsCount = 0;
            TradeBar latestBar = null;
            var time = new DateTime(2015, 04, 13, hour, 0, 0);

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestBar = bar;
                consolidatedBarsCount++;
            };

            // Update with one tradebar that ends at this time
            // This is to simulate getting a data bar for the last period
            consolidator.Update(
                new TradeBar { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute }
            );

            // Assert that the bar hasn't emitted
            Assert.IsNull(latestBar);
            Assert.AreEqual(0, consolidatedBarsCount);

            // Scan afterwards (Like algorithmManager does)
            consolidator.Scan(time);

            // Assert that the bar emitted
            Assert.IsNotNull(latestBar);
            Assert.IsTrue(latestBar.EndTime == time);
            Assert.AreEqual(1, consolidatedBarsCount);
        }

        private static void PushBarsThrough(
            int barCount,
            TimeSpan period,
            TradeBarConsolidator consolidator,
            ref DateTime time
        )
        {
            TradeBar bar;

            for (int i = 0; i < barCount; i++)
            {
                bar = new TradeBar { Time = time, Period = period };
                consolidator.Update(bar);

                // Advance time
                time += period;
            }
        }
    }
}
