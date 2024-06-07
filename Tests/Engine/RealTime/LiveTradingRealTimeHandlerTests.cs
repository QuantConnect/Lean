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
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.TransactionHandlers;
using Moq;
using QuantConnect.Brokerages.Backtesting;
using static QuantConnect.Tests.Engine.BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests;
using QuantConnect.Orders;
using System.Reflection;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.RealTime
{
    [TestFixture]
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

        [TestCaseSource(typeof(ExchangeHoursDataClass), nameof(ExchangeHoursDataClass.TestCases))]
        public void RefreshesMarketHoursCorrectly(SecurityExchangeHours securityExchangeHours, MarketHoursSegment expectedSegment)
        {
            var algorithm = new AlgorithmStub();
            var security = algorithm.AddEquity("SPY");

            var realTimeHandler = new TestLiveTradingRealTimeHandler();
            realTimeHandler.Setup(algorithm,
                new AlgorithmNodePacket(PacketType.AlgorithmNode),
                new BacktestingResultHandler(),
                null,
                new TestTimeLimitManager());

            var time = new DateTime(2023, 5, 30).Date;
            var entry = new MarketHoursDatabase.Entry(TimeZones.NewYork, securityExchangeHours);
            var key = new SecurityDatabaseKey(Market.USA, null, SecurityType.Equity);
            var mhdb = new MarketHoursDatabase(new Dictionary<SecurityDatabaseKey, MarketHoursDatabase.Entry>() { { key, entry} });
            realTimeHandler.SetMarketHoursDatabase(mhdb);
            realTimeHandler.TestRefreshMarketHoursToday(security, time, expectedSegment);
        }

        [Test]
        public void ResetMarketHoursCorrectly()
        {
            var algorithm = new TestAlgorithm { HistoryProvider = new FakeHistoryProvider() };
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetCash(100000);
            algorithm.SetStartDate(2023, 5, 30);
            algorithm.SetEndDate(2023, 5, 30);
            var security = algorithm.AddEquity("SPY");
            security.Exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            var symbol = security.Symbol;
            algorithm.SetFinishedWarmingUp();

            var handleOptionNotification = typeof(BrokerageTransactionHandler).GetMethod("HandleOptionNotification", BindingFlags.NonPublic | BindingFlags.Instance);

            var transactionHandler = new TestBrokerageTransactionHandler();
            using var broker = new BacktestingBrokerage(algorithm);
            transactionHandler.Initialize(algorithm, broker, new BacktestingResultHandler());

            // Creates a market order
            security.SetMarketPrice(new TradeBar(new DateTime(2023, 5, 30), symbol, 280m, 280m, 280m, 280m, 100));

            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1, 0, 0, new DateTime(2023, 5, 30), "TestTag1");

            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(algorithm.Transactions, orderRequest));
            algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Submitted);
            broker.Scan();
            Assert.IsTrue(orderTicket.Status == OrderStatus.Filled);

            var realTimeHandler = new TestLiveTradingRealTimeHandlerReset();
            realTimeHandler.Setup(algorithm,
                new AlgorithmNodePacket(PacketType.AlgorithmNode),
                new BacktestingResultHandler(),
                null,
                new TestTimeLimitManager());
            realTimeHandler.AddRefreshHoursScheduledEvent();

            orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1, 0, 0, new DateTime(2023, 5, 30), "TestTag2");
            orderRequest.SetOrderId(2);
            orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Submitted);
            broker.Scan();
            Assert.IsTrue(orderTicket.Status != OrderStatus.Filled);

            realTimeHandler.Exit();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("1.00:00:00")]
        [TestCase("2.00:00:00")]
        [TestCase("1.12:00:00")]
        [TestCase("12:00:00")]
        [TestCase("6:00:00")]
        [TestCase("6:30:00")]
        public void RefreshesSymbolProperties(string refreshPeriodStr)
        {
            var refreshPeriod = string.IsNullOrEmpty(refreshPeriodStr) ? TimeSpan.FromDays(1) : TimeSpan.Parse(refreshPeriodStr);
            var step = refreshPeriod / 2;

            using var realTimeHandler = new SPDBTestLiveTradingRealTimeHandler();

            var timeProvider = realTimeHandler.PublicTimeProvider;
            timeProvider.SetCurrentTimeUtc(new DateTime(2023, 5, 30));

            var algorithm = new AlgorithmStub();
            algorithm.Settings.DatabasesRefreshPeriod = refreshPeriod;
            algorithm.AddEquity("SPY");
            algorithm.AddForex("EURUSD");

            realTimeHandler.Setup(algorithm,
                new AlgorithmNodePacket(PacketType.AlgorithmNode),
                new BacktestingResultHandler(),
                null,
                new TestTimeLimitManager());
            realTimeHandler.SpdbRefreshed.Reset();
            realTimeHandler.SecuritySymbolPropertiesUpdated.Reset();

            algorithm.SetFinishedWarmingUp();
            realTimeHandler.SetTime(timeProvider.GetUtcNow());

            var events = new[] { realTimeHandler.SpdbRefreshed, realTimeHandler.SecuritySymbolPropertiesUpdated };
            for (var i = 0; i < 10; i++)
            {
                timeProvider.Advance(step);

                // We only advanced half the time, so we should not have refreshed yet
                if (i % 2 == 0)
                {
                    Assert.IsFalse(WaitHandle.WaitAll(events, 500));
                }
                else
                {
                    Assert.IsTrue(WaitHandle.WaitAll(events, 2000));
                    realTimeHandler.SpdbRefreshed.Reset();
                    realTimeHandler.SecuritySymbolPropertiesUpdated.Reset();
                }
            }
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
            private MarketHoursDatabase newMarketHoursDatabase;
            public void SetMarketHoursDatabase(MarketHoursDatabase marketHoursDatabase)
            {
                newMarketHoursDatabase = marketHoursDatabase;
            }
            protected override void ResetMarketHoursDatabase()
            {
                if (newMarketHoursDatabase != null)
                {
                    MarketHoursDatabase = newMarketHoursDatabase;
                }
                else
                {
                    base.ResetMarketHoursDatabase();
                }
            }

            public void TestRefreshMarketHoursToday(Security security, DateTime time, MarketHoursSegment expectedSegment)
            {
                OnSecurityUpdated.Reset();
                RefreshMarketHours(time);
                OnSecurityUpdated.WaitOne();
                AssertMarketHours(security, time, expectedSegment);
            }

            protected override void UpdateMarketHours(Security security)
            {
                base.UpdateMarketHours(security);
                OnSecurityUpdated.Set();
            }

            public void AssertMarketHours(Security security, DateTime time, MarketHoursSegment expectedSegment)
            {
                var marketHours = security.Exchange.Hours.GetMarketHours(time);
                var segment = marketHours.Segments.SingleOrDefault();

                if (expectedSegment == null)
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

        private class TestLiveTradingRealTimeHandlerReset : LiveTradingRealTimeHandler
        {
            private static AutoResetEvent OnSecurityUpdated = new AutoResetEvent(false);

            public void AddRefreshHoursScheduledEvent()
            {
                Add(new ScheduledEvent("RefreshHours", new[] { new DateTime(2023, 6, 29) }, (name, triggerTime) =>
                {
                    // refresh market hours from api every day
                    RefreshMarketHours((new DateTime(2023, 5, 30)).Date);
                }));
                OnSecurityUpdated.Reset();
                SetTime(DateTime.UtcNow);
                OnSecurityUpdated.WaitOne();
                Exit();
            }

            protected override void UpdateMarketHours(Security security)
            {
                base.UpdateMarketHours(security);
                OnSecurityUpdated.Set();
            }

            protected override void ResetMarketHoursDatabase()
            {
                var entry = new MarketHoursDatabase.Entry(TimeZones.NewYork, ExchangeHoursDataClass.CreateExchangeHoursWithHolidays());
                var key = new SecurityDatabaseKey(Market.USA, null, SecurityType.Equity);
                var mhdb = new MarketHoursDatabase(new Dictionary<SecurityDatabaseKey, MarketHoursDatabase.Entry>() { { key, entry } });
                MarketHoursDatabase = mhdb;
            }
        }

        private class SPDBTestLiveTradingRealTimeHandler : LiveTradingRealTimeHandler, IDisposable
        {
            private int _securitiesUpdated;

            public ManualTimeProvider PublicTimeProvider = new ManualTimeProvider();

            protected override ITimeProvider TimeProvider { get { return PublicTimeProvider; } }

            public ManualResetEvent SpdbRefreshed { get; } = new ManualResetEvent(false);
            public ManualResetEvent SecuritySymbolPropertiesUpdated = new ManualResetEvent(false);

            protected override void RefreshSymbolProperties()
            {
                base.RefreshSymbolProperties();
                SpdbRefreshed.Set();
            }

            protected override void UpdateSymbolProperties(Security security)
            {
                base.UpdateSymbolProperties(security);
                Algorithm.Log($"{Algorithm.Securities.Count}");

                if (++_securitiesUpdated == Algorithm.Securities.Count)
                {
                    SecuritySymbolPropertiesUpdated.Set();
                    _securitiesUpdated = 0;
                }
            }

            public void Dispose()
            {
                SpdbRefreshed.Dispose();
                SecuritySymbolPropertiesUpdated.Dispose();
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
                var earlyCloses = new Dictionary<DateTime, TimeSpan> { { new DateTime(2023, 5, 30).Date, new TimeSpan(13, 0, 0) } };
                var lateOpens = new Dictionary<DateTime, TimeSpan>() { { new DateTime(2023, 5, 30).Date, new TimeSpan(10, 0, 0) } };
                var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, new List<DateTime>(), new[]
                {
                _sunday, _monday, _tuesday, _wednesday, _thursday, _friday, _saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
                return exchangeHours;
            }

            private static SecurityExchangeHours CreateExchangeHoursWithEarlyClose()
            {
                var earlyCloses = new Dictionary<DateTime, TimeSpan> { { new DateTime(2023, 5, 30).Date, new TimeSpan(13, 0, 0) } };
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
                var lateOpens = new Dictionary<DateTime, TimeSpan>() { { new DateTime(2023, 5, 30).Date, new TimeSpan(10, 0, 0) } };
                var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, new List<DateTime>(), new[]
                {
                _sunday, _monday, _tuesday, _wednesday, _thursday, _friday, _saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
                return exchangeHours;
            }

            public static SecurityExchangeHours CreateExchangeHoursWithHolidays()
            {
                var earlyCloses = new Dictionary<DateTime, TimeSpan>();
                var lateOpens = new Dictionary<DateTime, TimeSpan>();
                var holidays = new List<DateTime>() { new DateTime(2023, 5, 30).Date };
                var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, holidays, new[]
                {
                _sunday, _monday, _tuesday, _wednesday, _thursday, _friday, _saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
                return exchangeHours;
            }

        }
    }
}
