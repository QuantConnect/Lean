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
using System.Linq;
using QuantConnect.Securities;
using System.Collections.Generic;

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

        [NonParallelizable]
        [TestCaseSource(typeof(ExchangeHoursDataClass), nameof(ExchangeHoursDataClass.TestCases))]
        public void RefreshesMarketHoursCorrectly(SecurityExchangeHours securityExchangeHours, MarketHoursSegment expectedSegment)
        {
            Security security;
            var algorithm = new AlgorithmStub();
            security = algorithm.AddEquity("SPY");

            var realTimeHandler = new TestLiveTradingRealTimeHandler();
            realTimeHandler.Setup(algorithm,
                new AlgorithmNodePacket(PacketType.AlgorithmNode),
                new BacktestingResultHandler(),
                null,
                new TestTimeLimitManager());

            var time = new DateTime(2023, 5, 28).Date;
            var entry = new MarketHoursDatabase.Entry(TimeZones.NewYork, securityExchangeHours);
            var key = new SecurityDatabaseKey(Market.USA, null, SecurityType.Equity);
            var mhdb = new MarketHoursDatabase(new Dictionary<SecurityDatabaseKey, MarketHoursDatabase.Entry>() { { key, entry} });
            realTimeHandler.SetMarketHoursDatabase(mhdb);
            realTimeHandler.TestRefreshMarketHoursToday(security, time, expectedSegment);
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

        public class TestLiveTradingRealTimeHandler: LiveTradingRealTimeHandler
        {
            private static AutoResetEvent OnSecurityUpdated = new AutoResetEvent(false);
            public void SetMarketHoursDatabase(MarketHoursDatabase marketHoursDatabase)
            {
                MarketHoursDatabase = marketHoursDatabase;
            }

            public void TestRefreshMarketHoursToday(Security security, DateTime time, MarketHoursSegment expectedSegment)
            {
                OnSecurityUpdated.Reset();
                RefreshMarketHoursToday(time);
                OnSecurityUpdated.WaitOne();
                AssertMarketHours(security, time, expectedSegment);
            }

            protected override IEnumerable<MarketHoursSegment> GetMarketHours(DateTime time, Symbol symbol)
            {
                var results = base.GetMarketHours(time, symbol);
                OnSecurityUpdated.Set();
                return results;
            }

            public void AssertMarketHours(Security security, DateTime time, MarketHoursSegment expectedSegment)
            {
                var marketHours = security.Exchange.Hours.MarketHours[time.DayOfWeek];
                var segment = marketHours.Segments.SingleOrDefault();

                if (segment == null)
                {
                    Assert.AreEqual(expectedSegment, segment);
                }
                else
                {
                    Assert.AreEqual(expectedSegment.Start, segment.Start);
                    Assert.AreEqual(expectedSegment.End, segment.End);
                    for (var hour = segment.Start; hour < segment.End; hour = hour.Add(TimeSpan.FromHours(1)))
                    {
                        Assert.IsTrue(marketHours.IsOpen(hour, false));
                    }
                    Assert.AreEqual(expectedSegment.End, security.Exchange.Hours.GetNextMarketClose(time.Date, false).TimeOfDay);
                    Assert.AreEqual(expectedSegment.Start, security.Exchange.Hours.GetNextMarketOpen(time.Date, false).TimeOfDay);
                }

                Exit();
            }
        }

        public class ExchangeHoursDataClass
        {
            private static LocalMarketHours _sunday = new LocalMarketHours(DayOfWeek.Sunday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            private static LocalMarketHours _monday = new LocalMarketHours(DayOfWeek.Monday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            private static LocalMarketHours _tuesday = new LocalMarketHours(DayOfWeek.Tuesday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            private static LocalMarketHours _wednesday = new LocalMarketHours(DayOfWeek.Wednesday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            private static LocalMarketHours _thursday = new LocalMarketHours(DayOfWeek.Thursday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            private static LocalMarketHours _friday = new LocalMarketHours(DayOfWeek.Friday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            private static LocalMarketHours _saturday = new LocalMarketHours(DayOfWeek.Saturday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));

            public static IEnumerable<TestCaseData> TestCases
            {
                get
                {
                    yield return new TestCaseData(CreateExchangeHoursWithEarlyCloseAndLateOpen(), new MarketHoursSegment(MarketHoursState.Market,new TimeSpan(10, 0, 0), new TimeSpan(13, 0, 0)));
                    yield return new TestCaseData(CreateExchangeHoursWithEarlyClose(), new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(9, 30, 0), new TimeSpan(13, 0, 0)));
                    yield return new TestCaseData(CreateExchangeHoursWithLateOpen(), new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(10, 0, 0), new TimeSpan(16, 0, 0)));
                    yield return new TestCaseData(CreateExchangeHoursWithHolidays(), null);
                }
            }

            private static SecurityExchangeHours CreateExchangeHoursWithEarlyCloseAndLateOpen()
            {
                var earlyCloses = new Dictionary<DateTime, TimeSpan> { { new DateTime(2023, 5, 28).Date, new TimeSpan(13, 0, 0) } };
                var lateOpens = new Dictionary<DateTime, TimeSpan>() { { new DateTime(2023, 5, 28).Date, new TimeSpan(10, 0, 0) } };
                var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, new List<DateTime>(), new[]
                {
                _sunday, _monday, _tuesday, _wednesday, _thursday, _friday, _saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
                return exchangeHours;
            }

            private static SecurityExchangeHours CreateExchangeHoursWithEarlyClose()
            {
                var earlyCloses = new Dictionary<DateTime, TimeSpan> { { new DateTime(2023, 5, 28).Date, new TimeSpan(13, 0, 0) } };
                var lateOpens = new Dictionary<DateTime, TimeSpan>();
                var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, new List<DateTime>(), new[]
                {
                _sunday, _monday, _tuesday, _wednesday, _thursday, _friday, _saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
                return exchangeHours;
            }

            private static SecurityExchangeHours CreateExchangeHoursWithLateOpen()
            {
                var earlyCloses = new Dictionary<DateTime, TimeSpan>();
                var lateOpens = new Dictionary<DateTime, TimeSpan>() { { new DateTime(2023, 5, 28).Date, new TimeSpan(10, 0, 0) } };
                var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, new List<DateTime>(), new[]
                {
                _sunday, _monday, _tuesday, _wednesday, _thursday, _friday, _saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
                return exchangeHours;
            }

            private static SecurityExchangeHours CreateExchangeHoursWithHolidays()
            {
                var earlyCloses = new Dictionary<DateTime, TimeSpan>();
                var lateOpens = new Dictionary<DateTime, TimeSpan>();
                var holidays = new List<DateTime>() { new DateTime(2023, 5, 28).Date };
                var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, holidays, new[]
                {
                _sunday, _monday, _tuesday, _wednesday, _thursday, _friday, _saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
                return exchangeHours;
            }

        }
    }
}
