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
using QuantConnect.Algorithm;
using QuantConnect.AlgorithmFactory.Python.Wrappers;
using QuantConnect.Interfaces;
using QuantConnect.Tests.Common.Data.UniverseSelection;
using System.Linq;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;
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

        [Test]
        public void RefreshesMarketHoursCorrectly()
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

            // Because neither implement EOD() deprecated it should be zero
            var segments = security.Exchange.Hours.MarketHours[DateTime.UtcNow.AddDays(1).DayOfWeek].Segments.Count;
            Assert.AreEqual(3, segments);
            var securityExchangeHours = CreateExchangeHours();
            var entry = new MarketHoursDatabase.Entry(TimeZones.NewYork, securityExchangeHours);
            var key = new SecurityDatabaseKey(Market.USA, null, SecurityType.Equity);
            var mhdb = new MarketHoursDatabase(new Dictionary<SecurityDatabaseKey, MarketHoursDatabase.Entry>() { { key, entry} });
            realTimeHandler.SetMarketHoursDatabase(mhdb);
            realTimeHandler.ScanPastEvents(DateTime.UtcNow.AddDays(1));
            segments = security.Exchange.Hours.MarketHours[DateTime.UtcNow.AddDays(1).DayOfWeek].Segments.Count;
            Assert.AreEqual(1, segments);
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
            public void SetMarketHoursDatabase(MarketHoursDatabase marketHoursDatabase)
            {
                MarketHoursDatabase = marketHoursDatabase;
            }
        }

        public static SecurityExchangeHours CreateExchangeHours()
        {
            var sunday = LocalMarketHours.ClosedAllDay(DayOfWeek.Sunday);
            var monday = new LocalMarketHours(DayOfWeek.Monday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var tuesday = new LocalMarketHours(DayOfWeek.Tuesday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var wednesday = new LocalMarketHours(DayOfWeek.Wednesday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var thursday = new LocalMarketHours(DayOfWeek.Thursday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var friday = new LocalMarketHours(DayOfWeek.Friday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var saturday = LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday);

            var earlyCloses = new Dictionary<DateTime, TimeSpan> { { DateTime.UtcNow.AddDays(1), new TimeSpan(13, 0, 0) } };
            var lateOpens = new Dictionary<DateTime, TimeSpan>() { { DateTime.UtcNow.AddDays(1), new TimeSpan(10, 0, 0) } };
            var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, new List<DateTime>(), new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday, saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
            return exchangeHours;
        }
    }
}
