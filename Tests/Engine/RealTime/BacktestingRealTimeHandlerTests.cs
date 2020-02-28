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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.AlgorithmFactory.Python.Wrappers;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Packets;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
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

        [TestCase(false)]
        [TestCase(true)]
        public void SetTime(bool oneStep)
        {
            var realTimeHandler = new BacktestingRealTimeHandler();
            var date = new DateTime(2020, 1, 1);

            var count = 0;
            var asserts = 0;
            realTimeHandler.Add(new ScheduledEvent("1",
                new List<DateTime> { date, date.AddMinutes(10) },
                (s, time) =>
                {
                    count++;
                    if (count == 1)
                    {
                        asserts++;
                        Assert.AreEqual(date, time);
                    }
                    else if (oneStep ? count == 2 : count == 4)
                    {
                        asserts++;
                        Assert.AreEqual(date.AddMinutes(10), time);
                    }
                }));

            realTimeHandler.Add(new ScheduledEvent("2",
                new List<DateTime> { date.AddMinutes(1), date.AddMinutes(2) },
                (s, time) =>
                {
                    count++;
                    if (oneStep ? count == 3 : count == 2)
                    {
                        asserts++;
                        Assert.AreEqual(date.AddMinutes(1), time);
                    }
                    else if (oneStep ? count == 4 : count == 3)
                    {
                        asserts++;
                        Assert.AreEqual(date.AddMinutes(2), time);
                    }
                }));

            if (oneStep)
            {
                realTimeHandler.SetTime(date.AddDays(1));
            }
            else
            {
                realTimeHandler.SetTime(date);
                realTimeHandler.SetTime(date.AddMinutes(1));
                realTimeHandler.SetTime(date.AddMinutes(2));
                realTimeHandler.SetTime(date.AddMinutes(10));
            }
            Assert.AreEqual(4, count);
            Assert.AreEqual(4, asserts);
        }

        [Test]
        public void LazySortRespectsOriginalOrder()
        {
            var date = new DateTime(2020, 1, 1);
            var events = new List<ScheduledEvent>
            {
                new ScheduledEvent("1", new List<DateTime> {date.AddMinutes(10)}, (s, time) => { }),
                new ScheduledEvent("2", new List<DateTime> {date.AddMinutes(1)}, (s, time) => { }),
                new ScheduledEvent("3", new List<DateTime> {date.AddMinutes(10)}, (s, time) => { })
            };
            BacktestingRealTimeHandler.SortFirstElement(events);
            Assert.AreEqual(date.AddMinutes(1), events[0].NextEventUtcTime);
            Assert.AreEqual(date.AddMinutes(10), events[1].NextEventUtcTime);
            Assert.AreEqual("1", events[1].Name);
            Assert.AreEqual(date.AddMinutes(10), events[2].NextEventUtcTime);
            Assert.AreEqual("3", events[2].Name);

            events = new List<ScheduledEvent>
            {
                new ScheduledEvent("1", new List<DateTime> {date.AddMinutes(10)}, (s, time) => { }),
                new ScheduledEvent("2", new List<DateTime> {date.AddMinutes(1)}, (s, time) => { }),
                new ScheduledEvent("3", new List<DateTime> {date.AddMinutes(3)}, (s, time) => { }),
                new ScheduledEvent("4", new List<DateTime> {date.AddMinutes(10)}, (s, time) => { }),
                new ScheduledEvent("5", new List<DateTime> {date.AddMinutes(50)}, (s, time) => { })
            };
            BacktestingRealTimeHandler.SortFirstElement(events);
            Assert.AreEqual(date.AddMinutes(1), events[0].NextEventUtcTime);
            Assert.AreEqual(date.AddMinutes(3), events[1].NextEventUtcTime);
            Assert.AreEqual(date.AddMinutes(10), events[2].NextEventUtcTime);
            Assert.AreEqual("1", events[2].Name);
            Assert.AreEqual(date.AddMinutes(10), events[3].NextEventUtcTime);
            Assert.AreEqual("4", events[3].Name);
            Assert.AreEqual(date.AddMinutes(50), events[4].NextEventUtcTime);
        }

        [Test]
        public void LazySort()
        {
            var date = new DateTime(2020, 1, 1);
            var events = new List<ScheduledEvent>
            {
                new ScheduledEvent("1", new List<DateTime> {date.AddMinutes(10)}, (s, time) => { }),
                new ScheduledEvent("2", new List<DateTime> {date.AddMinutes(1)}, (s, time) => { })
            };
            BacktestingRealTimeHandler.SortFirstElement(events);
            Assert.AreEqual(date.AddMinutes(1), events[0].NextEventUtcTime);
            Assert.AreEqual(date.AddMinutes(10), events[1].NextEventUtcTime);

            events = new List<ScheduledEvent>
            {
                new ScheduledEvent("1", new List<DateTime> {date.AddMinutes(10)}, (s, time) => { }),
                new ScheduledEvent("2", new List<DateTime> {date.AddMinutes(1)}, (s, time) => { }),
                new ScheduledEvent("2", new List<DateTime> {date.AddMinutes(3)}, (s, time) => { })
            };
            BacktestingRealTimeHandler.SortFirstElement(events);
            Assert.AreEqual(date.AddMinutes(1), events[0].NextEventUtcTime);
            Assert.AreEqual(date.AddMinutes(3), events[1].NextEventUtcTime);
            Assert.AreEqual(date.AddMinutes(10), events[2].NextEventUtcTime);

            events = new List<ScheduledEvent>
            {
                new ScheduledEvent("1", new List<DateTime> {date.AddMinutes(10)}, (s, time) => { }),
                new ScheduledEvent("2", new List<DateTime> {date.AddMinutes(1)}, (s, time) => { }),
                new ScheduledEvent("2", new List<DateTime> {date.AddMinutes(3)}, (s, time) => { }),
                new ScheduledEvent("2", new List<DateTime> {date.AddMinutes(50)}, (s, time) => { })
            };
            BacktestingRealTimeHandler.SortFirstElement(events);
            Assert.AreEqual(date.AddMinutes(1), events[0].NextEventUtcTime);
            Assert.AreEqual(date.AddMinutes(3), events[1].NextEventUtcTime);
            Assert.AreEqual(date.AddMinutes(10), events[2].NextEventUtcTime);
            Assert.AreEqual(date.AddMinutes(50), events[3].NextEventUtcTime);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ScanPastEvents(bool oneStep)
        {
            var realTimeHandler = new BacktestingRealTimeHandler();
            realTimeHandler.Setup(new AlgorithmStub(),
                new AlgorithmNodePacket(PacketType.AlgorithmNode) { Language = Language.CSharp },
                new TestResultHandler(),
                null,
                new TestTimeLimitManager());

            var date = new DateTime(2020, 1, 1);

            var count = 0;
            var asserts = 0;
            realTimeHandler.Add(new ScheduledEvent("1",
                new List<DateTime> { date, date.AddMinutes(10) },
                (s, time) =>
                {
                    count++;
                    if (count == 1)
                    {
                        asserts++;
                        Assert.AreEqual(date, time);
                    }
                    else if (count == 4)
                    {
                        asserts++;
                        Assert.AreEqual(date.AddMinutes(10), time);
                    }
                }));

            realTimeHandler.Add(new ScheduledEvent("2",
                new List<DateTime> { date.AddMinutes(1), date.AddMinutes(2) },
                (s, time) =>
                {
                    count++;
                    if (count == 2)
                    {
                        asserts++;
                        Assert.AreEqual(date.AddMinutes(1), time);
                    }
                    else if (count == 3)
                    {
                        asserts++;
                        Assert.AreEqual(date.AddMinutes(2), time);
                    }
                }));

            if (oneStep)
            {
                realTimeHandler.ScanPastEvents(date.AddDays(1));
            }
            else
            {
                realTimeHandler.ScanPastEvents(date.AddMilliseconds(1));
                realTimeHandler.ScanPastEvents(date.AddMinutes(1).AddMilliseconds(1));
                realTimeHandler.ScanPastEvents(date.AddMinutes(2).AddMilliseconds(1));
                realTimeHandler.ScanPastEvents(date.AddMinutes(10).AddMilliseconds(1));
            }
            Assert.AreEqual(4, count);
            Assert.AreEqual(4, asserts);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void DoesNotAddOnEndOfDayEventsIfNotImplemented(Language language)
        {
            Security security;
            IAlgorithm algorithm;
            if (language == Language.CSharp)
            {
                algorithm = new AlgorithmStub();
                security = (algorithm as QCAlgorithm).AddEquity("SPY");
            }
            else
            {
                algorithm = new AlgorithmPythonWrapper("Test_CustomDataAlgorithm");
                algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
                security = algorithm.AddSecurity(SecurityType.Equity,
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
            // the generic OnEndOfDay()
            Assert.AreEqual(1, realTimeHandler.GetScheduledEventsCount);

            realTimeHandler.OnSecuritiesChanged(
                new SecurityChanges(new[] { security }, Enumerable.Empty<Security>()));

            Assert.AreEqual(1, realTimeHandler.GetScheduledEventsCount);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void AddsOnEndOfDayEventsIfImplemented(Language language)
        {
            Security security;
            IAlgorithm algorithm;
            if (language == Language.CSharp)
            {
                algorithm = new TestAlgorithm();
                security = (algorithm as QCAlgorithm).AddEquity("SPY");
            }
            else
            {
                algorithm = new AlgorithmPythonWrapper("OnEndOfDayRegressionAlgorithm");
                algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(new MockDataFeed(), algorithm));
                security = algorithm.AddSecurity(SecurityType.Equity,
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
            // the generic OnEndOfDay()
            Assert.AreEqual(1, realTimeHandler.GetScheduledEventsCount);

            realTimeHandler.OnSecuritiesChanged(
                new SecurityChanges(new[] { security }, Enumerable.Empty<Security>()));

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
