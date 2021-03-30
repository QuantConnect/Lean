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
            new [] { TimeSpan.FromDays(100), TimeSpan.FromDays(10) },
            new [] { TimeSpan.FromDays(30), TimeSpan.FromDays(1) },     //GH Issue #4915
            new [] { TimeSpan.FromDays(10), TimeSpan.FromDays(1) },
            new [] { TimeSpan.FromDays(1), TimeSpan.FromHours(1) },
            new [] { TimeSpan.FromHours(10), TimeSpan.FromHours(1) },
            new [] { TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1) },
            new [] { TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10) },
            new [] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.1) },
        };

        [TestCaseSource(nameof(PeriodCases))]
        public void ExpectedConsolidatedTradeBarsInPeriodMode(TimeSpan barSpan, TimeSpan updateSpan)
        {
            TradeBar consolidated = null;
            var consolidator = new BaseDataConsolidator(barSpan);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                Assert.AreEqual(barSpan, bar.Period);              // The period matches our span
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
                Assert.IsNotNull(consolidated);                                 // We have a bar
                Assert.AreEqual(dataTime, consolidated.EndTime);    // New bar time should be dataTime
                Assert.AreEqual(barSpan, consolidated.EndTime - lastBarTime);      // The difference between the bars is the span

                nextBarTime = dataTime + barSpan;
                lastBarTime = consolidated.EndTime;
            }
        }

        [TestCaseSource(nameof(PeriodCases))]
        public void ExpectedConsolidatedQuoteBarsInPeriodMode(TimeSpan barSpan, TimeSpan updateSpan)
        {
            QuoteBar consolidated = null;
            var consolidator = new QuoteBarConsolidator(barSpan);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                Assert.AreEqual(barSpan, bar.Period);                  // The period matches our span
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13);
            var dataTime = reference;

            var nextBarTime = reference + barSpan;
            var lastBarTime = reference;

            // First data point
            consolidator.Update(new QuoteBar { Time = dataTime });
            Assert.IsNull(consolidated);

            for (var i = 0; i < 10; i++)
            {
                // Add data on the given interval until we expect a new bar
                while (dataTime < nextBarTime)
                {
                    dataTime = dataTime.Add(updateSpan);
                    consolidator.Update(new QuoteBar { Time = dataTime });
                }

                // Our asserts
                Assert.IsNotNull(consolidated);                                 // We have a bar
                Assert.AreEqual(dataTime, consolidated.EndTime);    // New bar time should be dataTime
                Assert.AreEqual(barSpan, consolidated.EndTime - lastBarTime);      // The difference between the bars is the span

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
            var period = TimeSpan.FromHours(1);
            var consolidator = new TradeBarConsolidator(period);
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
            Assert.AreEqual(4,consolidatedBarsCount);
        }

        [Test]
        public void ConsolidatorEmitsAllWorkingBars()
        {
            // This test is to ensure that no bars get swallowed by the consolidator
            // even if it doesn't get the data on regular intervals.
            var period = TimeSpan.FromHours(1);
            var consolidator = new TradeBarConsolidator(period);
            TradeBar latestConsolidated = null;
            var consolidatedBarsCount = 0;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestConsolidated = bar;
                consolidatedBarsCount++;
            };

            var time = new DateTime(2015, 04, 13);

            // ---- TEST UPDATE WILL EMIT OLD BARS ----
            // Update this consolidator with minute tradebars but one less than 60, which would trigger emit
            PushBarsThrough( 59, Time.OneMinute, consolidator, ref time);

            // Should be zero since it is 12:59AM
            Assert.IsTrue(time.Minute == 59);
            Assert.AreEqual(0, consolidatedBarsCount);

            // Advance time way past (3 hours) the the next expected data time (1 min)
            // Time = 3:59AM
            time += TimeSpan.FromHours(3);

            // Push one bar through and check that we have the 12AM - 1AM Bar emitted
            PushBarsThrough(1, Time.OneMinute, consolidator, ref time);
            Assert.AreEqual(1, consolidatedBarsCount);
            Assert.IsTrue(latestConsolidated != null && latestConsolidated.Time == new DateTime(2015, 04, 13));

            // Then check the new working bar is 3AM to 4AM
            Assert.IsTrue(consolidator.WorkingBar.Time == new DateTime(2015, 04, 13, 3, 0, 0));


            // ---- TEST SCAN WILL EMIT OLD BARS ----
            // Now advance time way past the bar end time of 4AM (3 Hours)
            // Time = 6:59AM
            time += TimeSpan.FromHours(3);

            // Scan the time, it should emit the 3AM - 4AM Bar
            consolidator.Scan(time);
            Assert.AreEqual(2, consolidatedBarsCount);
            Assert.IsTrue(latestConsolidated != null && latestConsolidated.Time == new DateTime(2015, 04, 13, 3, 0, 0));

            // WorkingBar should be null, ready for whatever data comes through next
            Assert.IsTrue(consolidator.WorkingBar == null);
        }

        [Test]
        public void ConsolidatorEmitsRegularly()
        {
            // This test just pushes through 1000 bars
            // and ensures that the emit time and count are correct
            var period = TimeSpan.FromHours(1);
            var consolidator = new TradeBarConsolidator(period);
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
            Assert.AreEqual(1000, consolidatedBarsCount);
        }

        private void PushBarsThrough (int barCount, TimeSpan period, TradeBarConsolidator consolidator, ref DateTime time)
        {
            TradeBar bar;
            
            for (int i = 0; i < barCount; i++)
            {
                bar = new TradeBar { Time = time, Period = Time.OneMinute };
                consolidator.Update(bar);
                consolidator.Scan(time);

                // Advance time
                time += period;
            }
        }
    }
}
