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
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.RealTime
{
    [TestFixture]
    public class BacktestingRealTimeHandlerTests
    {
        private IResultHandler _resultHandler;

        [SetUp]
        public void SetUp()
        {
            _resultHandler = new TestResultHandler();
        }

        [TearDown]
        public void TearDown()
        {
            _resultHandler.Exit();
        }

        [Test]
        public void SortsEventsAfterSetup()
        {
            var realTimeHandler = new BacktestingRealTimeHandler();
            var algo = new TestAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.AddEquity("SPY");
            var startDate = new DateTime(2019, 1, 1);
            algo.SetStartDate(startDate);
            algo.SetDateTime(startDate);
            algo.SetEndDate(2020, 1, 1);

            var firstCalled = false;
            var secondCalled = false;
            var events = new List<ScheduledEvent>
            {
                new ScheduledEvent("1", new List<DateTime> { startDate.AddMinutes(-10), startDate.AddMinutes(5)},
                    (s, time) => { firstCalled = true; }),
                new ScheduledEvent("2", new List<DateTime> { startDate.AddMinutes(1)},
                    (s, time) => { secondCalled = true; }),
                new ScheduledEvent("3", new List<DateTime> { startDate.AddMinutes(10)}, (s, time) => { })
            };
            foreach (var scheduledEvent in events)
            {
                realTimeHandler.Add(scheduledEvent);
            }

            realTimeHandler.Setup(algo,
                new AlgorithmNodePacket(PacketType.AlgorithmNode),
                new BacktestingResultHandler(),
                null,
                null);

            realTimeHandler.SetTime(startDate.AddMinutes(1));
            realTimeHandler.Exit();

            Assert.IsTrue(secondCalled);
            // 'first' should of been called and should be moved behind 'second' after setup
            Assert.IsFalse(firstCalled);
        }

        [Test]
        public void SingleScheduledEventFires_SetTime()
        {
            var realTimeHandler = new BacktestingRealTimeHandler();
            var algo = new TestAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.AddEquity("SPY");
            algo.SetStartDate(2019, 1, 1);
            algo.SetDateTime(new DateTime(2019, 1, 1));
            algo.SetEndDate(2020, 1, 1);
            realTimeHandler.Setup(algo,
                new AlgorithmNodePacket(PacketType.AlgorithmNode),
                new BacktestingResultHandler(),
                null,
                null);

            realTimeHandler.SetTime(DateTime.UtcNow);
            realTimeHandler.Exit();
            Assert.IsTrue(algo.OnEndOfDayFired);
        }

        [Test]
        public void SingleScheduledEventFires_ScanPastEvents()
        {
            var realTimeHandler = new BacktestingRealTimeHandler();
            var algo = new TestAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.AddEquity("SPY");
            algo.SetStartDate(2019, 1, 1);
            algo.SetDateTime(new DateTime(2019, 1, 1));
            algo.SetEndDate(2020, 1, 1);
            realTimeHandler.Setup(algo,
                new AlgorithmNodePacket(PacketType.AlgorithmNode),
                new BacktestingResultHandler(),
                null,
                null);

            realTimeHandler.ScanPastEvents(DateTime.UtcNow);
            realTimeHandler.Exit();
            Assert.IsTrue(algo.OnEndOfDayFired);
        }

        [Test]
        public void TriggersScheduledEventsSameTimeInOrder()
        {
            var realTimeHandler = new BacktestingRealTimeHandler();
            realTimeHandler.Setup(new AlgorithmStub(new NullDataFeed()),
                new AlgorithmNodePacket(PacketType.AlgorithmNode),
                new BacktestingResultHandler(),
                null,
                null);
            var eventTime = DateTime.UtcNow;

            var count = 0;
            for (var i = 0; i < 100; i++)
            {
                var id = i;
                realTimeHandler.Add(new ScheduledEvent($"{id}", eventTime,
                    (s, time) =>
                    {
                        Assert.AreEqual(id, count);
                        Assert.AreEqual(s, $"{id}");
                        count++;
                    }));
            }

            realTimeHandler.SetTime(DateTime.UtcNow);
            realTimeHandler.Exit();
            Assert.AreEqual(100, count);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SetTime(bool oneStep)
        {
            var realTimeHandler = new BacktestingRealTimeHandler();
            realTimeHandler.Setup(new AlgorithmStub(new NullDataFeed()),
                new AlgorithmNodePacket(PacketType.AlgorithmNode),
                new BacktestingResultHandler(),
                null,
                null);
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
            realTimeHandler.Exit();
            Assert.AreEqual(4, count);
            Assert.AreEqual(4, asserts);
        }

        [Test]
        public void SortRespectsOriginalOrderSameTime()
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
            // scheduled event 3 and 1 have the same time, 3 should still be next else it would mean 1 executed twice when 3 once
            Assert.AreEqual("3", events[1].Name);
            Assert.AreEqual(date.AddMinutes(10), events[2].NextEventUtcTime);
            Assert.AreEqual("1", events[2].Name);

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
            // scheduled event 4 and 1 have the same time, 4 should still be next else it would mean 1 executed twice when 4 once
            Assert.AreEqual("4", events[2].Name);
            Assert.AreEqual(date.AddMinutes(10), events[3].NextEventUtcTime);
            Assert.AreEqual("1", events[3].Name);
            Assert.AreEqual(date.AddMinutes(50), events[4].NextEventUtcTime);
        }

        [Test]
        public void Sort()
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
                _resultHandler,
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
            realTimeHandler.Exit();
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
                _resultHandler,
                null,
                new TestTimeLimitManager());
            // the generic OnEndOfDay()
            Assert.AreEqual(1, realTimeHandler.GetScheduledEventsCount);

            realTimeHandler.OnSecuritiesChanged(
                new SecurityChanges(new[] { security }, Enumerable.Empty<Security>()));

            Assert.AreEqual(1, realTimeHandler.GetScheduledEventsCount);

            realTimeHandler.Exit();
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
                _resultHandler,
                null,
                new TestTimeLimitManager());
            // the generic OnEndOfDay()
            Assert.AreEqual(1, realTimeHandler.GetScheduledEventsCount);

            realTimeHandler.OnSecuritiesChanged(
                new SecurityChanges(new[] { security }, Enumerable.Empty<Security>()));

            Assert.AreEqual(2, realTimeHandler.GetScheduledEventsCount);
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

        private class TestBacktestingRealTimeHandler : BacktestingRealTimeHandler
        {
            public int GetScheduledEventsCount => ScheduledEvents.Count;
        }

        private class TestAlgorithm : AlgorithmStub
        {
            public bool OnEndOfDayFired { get; set; }

            public override void OnEndOfDay()
            {
                OnEndOfDayFired = true;
            }

            public override void OnEndOfDay(Symbol symbol)
            {
            }
        }
    }
}
