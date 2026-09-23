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
using QuantConnect.Lean.Engine.Results.Analysis;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class AlgorithmSpeedTrackerTests
    {
        [Test]
        public void RatesAreNotMeasurableWithASingleSample()
        {
            var tracker = new AlgorithmSpeedTracker();
            tracker.AddSample(new(TimeSpan.FromSeconds(30), 1000, 0, 1, 100));

            Assert.IsNull(tracker.DataPointsPerSecond);
            Assert.IsNull(tracker.RecentDataPointsPerSecond());
            Assert.IsNull(tracker.RecentDaysPerSecond());
            Assert.IsNull(tracker.RecentHistoryDataPointsShare());
            Assert.IsNull(tracker.EstimatedRemainingTime());
            Assert.AreEqual(TimeSpan.Zero, tracker.SampledSpan);
        }

        [Test]
        public void ComputesCumulativeInitialAndRecentRates()
        {
            // 7 samples 30s apart: 3 fast intervals at 100k dp/s followed by 3 slow ones at 10k dp/s
            var tracker = new AlgorithmSpeedTracker();
            var dataPoints = 0L;
            for (var i = 0; i < 7; i++)
            {
                tracker.AddSample(new(TimeSpan.FromSeconds(30 * i), dataPoints, 0, i, 100));
                dataPoints += i < 3 ? 3_000_000 : 300_000;
            }

            // (3 * 3M + 3 * 300k) / 180s
            Assert.AreEqual(55_000, tracker.DataPointsPerSecond.Value, 1);
            // First 5 samples: (3 * 3M + 1 * 300k) / 120s
            Assert.AreEqual(77_500, tracker.InitialDataPointsPerSecond.Value, 1);
            // Last 5 samples: (1 * 3M + 3 * 300k) / 120s
            Assert.AreEqual(32_500, tracker.RecentDataPointsPerSecond().Value, 1);
            // Skipping the last sample: (2 * 3M + 2 * 300k) / 120s
            Assert.AreEqual(55_000, tracker.RecentDataPointsPerSecond(skipLast: 1).Value, 1);
        }

        [Test]
        public void IncludesHistoryDataPointsInRatesAndShare()
        {
            // 100k loop + 200k history data points every 30s
            var tracker = BuildUniformTracker(samples: 6, stepSeconds: 30, dataPointsPerStep: 100_000,
                historyDataPointsPerStep: 200_000, daysPerStep: 1, totalDays: 100);

            Assert.AreEqual(10_000, tracker.DataPointsPerSecond.Value, 1);
            Assert.AreEqual(10_000, tracker.RecentDataPointsPerSecond().Value, 1);
            Assert.AreEqual(2.0 / 3.0, tracker.RecentHistoryDataPointsShare().Value, 0.001);
            Assert.AreEqual(4 * 200_000, tracker.RecentHistoryDataPoints());
        }

        [Test]
        public void EstimatesRemainingTimeFromTheRecentPace()
        {
            // One backtest day every 30s, 95 days to go after the last sample
            var tracker = BuildUniformTracker(samples: 6, stepSeconds: 30, dataPointsPerStep: 100_000,
                historyDataPointsPerStep: 0, daysPerStep: 1, totalDays: 100);

            Assert.AreEqual(1.0 / 30, tracker.RecentDaysPerSecond().Value, 0.0001);
            Assert.AreEqual(95 * 30, tracker.EstimatedRemainingTime().Value.TotalSeconds, 0.1);
        }

        [Test]
        public void RemainingTimeIsZeroWhenTheEndDateIsReached()
        {
            var tracker = BuildUniformTracker(samples: 6, stepSeconds: 30, dataPointsPerStep: 100_000,
                historyDataPointsPerStep: 0, daysPerStep: 1, totalDays: 5);

            Assert.AreEqual(TimeSpan.Zero, tracker.EstimatedRemainingTime());
        }

        [Test]
        public void RemainingTimeIsNotMeasurableWithoutCalendarProgress()
        {
            var tracker = BuildUniformTracker(samples: 6, stepSeconds: 30, dataPointsPerStep: 100_000,
                historyDataPointsPerStep: 0, daysPerStep: 0, totalDays: 100);

            Assert.AreEqual(0, tracker.RecentDaysPerSecond());
            Assert.IsNull(tracker.EstimatedRemainingTime());
        }

        [Test]
        public void IgnoresSamplesWithNonIncreasingElapsedTime()
        {
            var tracker = new AlgorithmSpeedTracker();
            tracker.AddSample(new(TimeSpan.FromSeconds(30), 1000, 0, 1, 100));
            tracker.AddSample(new(TimeSpan.FromSeconds(30), 2000, 0, 1, 100));
            tracker.AddSample(new(TimeSpan.FromSeconds(20), 3000, 0, 1, 100));

            Assert.AreEqual(1, tracker.SampleCount);
        }

        [Test]
        public void TracksProgressAndDataPointCountsAvailability()
        {
            var tracker = new AlgorithmSpeedTracker();
            Assert.AreEqual(0, tracker.Progress);
            Assert.IsFalse(tracker.HasDataPointCounts);

            tracker.AddSample(new(TimeSpan.FromSeconds(30), 0, 1000, 25, 100));
            Assert.AreEqual(0.25m, tracker.Progress);
            Assert.IsFalse(tracker.HasDataPointCounts);

            tracker.AddSample(new(TimeSpan.FromSeconds(60), 500, 2000, 50, 100));
            Assert.AreEqual(0.5m, tracker.Progress);
            Assert.IsTrue(tracker.HasDataPointCounts);
        }

        public static AlgorithmSpeedTracker BuildUniformTracker(int samples, int stepSeconds, long dataPointsPerStep,
            long historyDataPointsPerStep, int daysPerStep, int totalDays)
        {
            var tracker = new AlgorithmSpeedTracker();
            for (var i = 0; i < samples; i++)
            {
                tracker.AddSample(new(TimeSpan.FromSeconds(stepSeconds * i), dataPointsPerStep * i,
                    historyDataPointsPerStep * i, daysPerStep * i, totalDays));
            }
            return tracker;
        }
    }
}
