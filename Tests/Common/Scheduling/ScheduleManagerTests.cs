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
using QuantConnect.Algorithm;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Packets;
using QuantConnect.Util.RateLimit;

namespace QuantConnect.Tests.Common.Scheduling
{
    [TestFixture]
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

            Assert.AreEqual(timeSteps, count1);
            Assert.AreEqual(timeSteps, count2);
        }
    }
}
