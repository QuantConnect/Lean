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
using System.Threading;
using QuantConnect.Packets;
using QuantConnect.Scheduling;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.RealTime
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class LiveTradingRealTimeHandlerTests
    {
        [Test]
        public void ThreadSafety()
        {
            var realTimeHandler = new LiveTradingRealTimeHandler();
            var algo = new AlgorithmStub();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.SetFinishedWarmingUp();

            realTimeHandler.Setup(algo,
                new AlgorithmNodePacket(PacketType.AlgorithmNode),
                new BacktestingResultHandler(),
                null,
                new TestTimeLimitManager());

            realTimeHandler.SetTime(DateTime.UtcNow);
            // wait for the internal thread to start
            Thread.Sleep(500);
            var scheduledEvent = new ScheduledEvent("1", new []{ Time.EndOfTime }, (_, _) => { });
            var scheduledEvent2 = new ScheduledEvent("2", new []{ Time.EndOfTime }, (_, _) => { });
            Assert.DoesNotThrow(() =>
            {
                for (var i = 0; i < 100000; i++)
                {
                    realTimeHandler.Add(scheduledEvent);
                    realTimeHandler.Add(scheduledEvent2);
                    realTimeHandler.Add(scheduledEvent);
                    realTimeHandler.Remove(scheduledEvent);
                    realTimeHandler.Remove(scheduledEvent2);
                    realTimeHandler.Remove(scheduledEvent);
                }
            });

            realTimeHandler.Exit();
        }

        private class TestTimeLimitManager : IIsolatorLimitResultProvider
        {
            public IsolatorLimitResult IsWithinLimit()
            {
                throw new NotImplementedException();
            }
            public void RequestAdditionalTime(int minutes)
            {
                throw new NotImplementedException();
            }
            public bool TryRequestAdditionalTime(int minutes)
            {
                throw new NotImplementedException();
            }
        }
    }
}
