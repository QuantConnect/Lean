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
using QuantConnect.AlgorithmFactory.Python.Wrappers;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Packets;
using QuantConnect.Scheduling;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.RealTime
{
    [TestFixture]
    public class BacktestingRealTimeHandlerTests
    {
        [Test]
        public void TriggersScheduledEventsSameTimeInOrder()
        {
            var realTimeHandler = new BacktestingRealTimeHandler();
            var eventTime = DateTime.UtcNow;

            var count = 0;
            for (var i = 0; i < 100; i++)
            {
                var id = i;
                realTimeHandler.Add(new ScheduledEvent($"{id}", eventTime,
                    (s, time) =>
                    {
                        Assert.AreEqual(id, count);
                        count++;
                    }));
            }

            realTimeHandler.SetTime(DateTime.UtcNow);
            Assert.AreEqual(100, count);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void DoesNotAddOnEndOfDayEventsIfNotImplemented(Language language)
        {
            IAlgorithm algorithm;
            if (language == Language.CSharp)
            {
                algorithm = new AlgorithmStub();
                (algorithm as QCAlgorithm).AddEquity("SPY");
            }
            else
            {
                algorithm = new AlgorithmPythonWrapper("Test_CustomDataAlgorithm");
                algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
                algorithm.AddSecurity(SecurityType.Equity,
                    "SPY",
                    Resolution.Daily,
                    Market.USA,
                    false,
                    1,
                    false);
            }

            var realTimeHandler = new TestBacktestingRealTimeHandler();
            realTimeHandler.Setup(algorithm,
                new AlgorithmNodePacket(PacketType.AlgorithmNode) { Language = language },
                new TestResultHandler(),
                null,
                new TestTimeLimitManager());
            Assert.AreEqual(1, realTimeHandler.GetScheduledEventsCount);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void AddsOnEndOfDayEventsIfImplemented(Language language)
        {
            IAlgorithm algorithm;
            if (language == Language.CSharp)
            {
                algorithm = new TestAlgorithm();
                (algorithm as QCAlgorithm).AddEquity("SPY");
            }
            else
            {
                algorithm = new AlgorithmPythonWrapper("OnEndOfDayRegressionAlgorithm");
                algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(new MockDataFeed(), algorithm));
                algorithm.AddSecurity(SecurityType.Equity,
                    "SPY",
                    Resolution.Daily,
                    Market.USA,
                    false,
                    1,
                    false);
            }

            var realTimeHandler = new TestBacktestingRealTimeHandler();
            realTimeHandler.Setup(algorithm,
                new AlgorithmNodePacket(PacketType.AlgorithmNode) { Language = language },
                new TestResultHandler(),
                null,
                new TestTimeLimitManager());
            Assert.AreEqual(2, realTimeHandler.GetScheduledEventsCount);
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

        private class TestBacktestingRealTimeHandler : BacktestingRealTimeHandler
        {
            public int GetScheduledEventsCount => ScheduledEvents.Count;
        }

        private class TestAlgorithm : AlgorithmStub
        {
            public override void OnEndOfDay(Symbol symbol)
            {

            }
        }
    }
}
