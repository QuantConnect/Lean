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
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Packets;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Util.RateLimit;

namespace QuantConnect.Tests.Common.Scheduling
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class ScheduleManagerTests
    {
        [Test]
        public void DuplicateScheduledEventsAreBothFired()
        {
            var algorithm = new QCAlgorithm();

            var handler = new BacktestingRealTimeHandler();
            var timeLimitManager = new AlgorithmTimeLimitManager(TokenBucket.Null, TimeSpan.MaxValue);
            handler.Setup(algorithm, new AlgorithmNodePacket(PacketType.BacktestNode), null, null, timeLimitManager);

            algorithm.Schedule.SetEventSchedule(handler);

            var time = new DateTime(2018, 1, 1);
            algorithm.SetDateTime(time);

            var count1 = 0;
            var count2 = 0;
            algorithm.Schedule.On(algorithm.Schedule.DateRules.EveryDay(), algorithm.Schedule.TimeRules.Every(TimeSpan.FromHours(1)), () => { count1++; });
            algorithm.Schedule.On(algorithm.Schedule.DateRules.EveryDay(), algorithm.Schedule.TimeRules.Every(TimeSpan.FromHours(1)), () => { count2++; });

            const int timeSteps = 12;

            for (var i = 0; i < timeSteps; i++)
            {
                handler.SetTime(time);
                time = time.AddHours(1);
            }

            handler.Exit();
            Assert.AreEqual(timeSteps, count1);
            Assert.AreEqual(timeSteps, count2);
        }

        [Test]
        public void TriggersWeeklyScheduledEventsEachWeekBacktesting()
        {
            var algorithm = new AlgorithmStub();

            var handler = new BacktestingRealTimeHandler();
            var time = new DateTime(2024, 02, 10);
            handler.SetTime(time);
            var timeLimitManager = new AlgorithmTimeLimitManager(TokenBucket.Null, TimeSpan.FromMinutes(20));
            handler.Setup(algorithm, new AlgorithmNodePacket(PacketType.BacktestNode), null, null, timeLimitManager);

            algorithm.Schedule.SetEventSchedule(handler);

            algorithm.SetDateTime(time);

            var spy = algorithm.AddEquity("SPY").Symbol;

            var eventTriggerTimes = new List<DateTime>();
            var scheduledEvent = algorithm.Schedule.On(algorithm.Schedule.DateRules.WeekStart(spy),
                algorithm.Schedule.TimeRules.BeforeMarketClose(spy, 3),
                () =>
                {
                    eventTriggerTimes.Add(time);
                });

            while (time.Month < 4)
            {
                handler.SetTime(time);
                time = time.AddMinutes(1);
            }

            handler.Exit();

            var expectedEventTriggerTimes = new List<DateTime>()
            {
                new DateTime(2024, 02, 12, 20, 57, 0),
                new DateTime(2024, 02, 20, 20, 57, 0),  // Monday is 19th but it's a holiday
                new DateTime(2024, 02, 26, 20, 57, 0),
                new DateTime(2024, 03, 04, 20, 57, 0),
                // Daylight saving adjustment
                new DateTime(2024, 03, 11, 19, 57, 0),
                new DateTime(2024, 03, 18, 19, 57, 0),
                new DateTime(2024, 03, 25, 19, 57, 0),
            };
            CollectionAssert.AreEqual(expectedEventTriggerTimes, eventTriggerTimes);
        }

        [Test]
        public void TriggersWeeklyScheduledEventsEachWeekLive()
        {
            var algorithm = new AlgorithmStub();

            var handler = new  TestableLiveTradingRealTimeHandler();
            var time = new DateTime(2024, 02, 10);
            handler.ManualTimeProvider.SetCurrentTime(time);
            var timeLimitManager = new AlgorithmTimeLimitManager(TokenBucket.Null, TimeSpan.FromMinutes(20));
            handler.Setup(algorithm, new LiveNodePacket(), null, null, timeLimitManager);

            algorithm.Schedule.SetEventSchedule(handler);

            algorithm.SetDateTime(time);

            var spy = algorithm.AddEquity("SPY").Symbol;

            var eventTriggerTimes = new List<DateTime>();
            var scheduledEvent = algorithm.Schedule.On(algorithm.Schedule.DateRules.WeekStart(spy),
                algorithm.Schedule.TimeRules.BeforeMarketClose(spy, 60),
                () =>
                {
                    eventTriggerTimes.Add(handler.ManualTimeProvider.GetUtcNow());
                });

            algorithm.SetFinishedWarmingUp();

            using var finished = new ManualResetEventSlim(false);

            // Schedule a task to advance time
            var timeStep = TimeSpan.FromMinutes(60);
            algorithm.Schedule.On(algorithm.Schedule.DateRules.EveryDay(),
                algorithm.Schedule.TimeRules.Every(timeStep),
                () =>
                {
                    handler.ManualTimeProvider.Advance(timeStep);
                    var now = handler.ManualTimeProvider.GetUtcNow();
                    if (now.Month >= 4)
                    {
                        finished.Set();
                    }
                });

            // Start
            handler.SetTime(time);

            finished.Wait(TimeSpan.FromSeconds(15));

            handler.Exit();

            var expectedEventTriggerTimes = new List<DateTime>()
            {
                new DateTime(2024, 02, 12, 20, 0, 0),
                new DateTime(2024, 02, 20, 20, 0, 0),   // Monday is 19th but it's a holiday
                new DateTime(2024, 02, 26, 20, 0, 0),
                new DateTime(2024, 03, 04, 20, 0, 0),
                // Daylight saving adjustment
                new DateTime(2024, 03, 11, 19, 0, 0),
                new DateTime(2024, 03, 18, 19, 0, 0),
                new DateTime(2024, 03, 25, 19, 0, 0),
            };
            CollectionAssert.AreEqual(expectedEventTriggerTimes, eventTriggerTimes);
        }

        private class TestableLiveTradingRealTimeHandler : LiveTradingRealTimeHandler
        {
            public ManualTimeProvider ManualTimeProvider = new ManualTimeProvider();

            protected override ITimeProvider TimeProvider => ManualTimeProvider;
        }
    }
}
