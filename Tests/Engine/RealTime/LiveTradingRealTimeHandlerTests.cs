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
using QuantConnect.Securities.Option;
using QuantConnect.Securities.IndexOption;
using QuantConnect.Configuration;
using NodaTime;

namespace QuantConnect.Tests.Engine.RealTime
{
    [TestFixture]
    [NonParallelizable]
    public class LiveTradingRealTimeHandlerTests
    {
        [SetUp]
        public void SetUp()
        {
            MarketHoursDatabase.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            MarketHoursDatabase.Reset();
        }

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
            WaitUntilActive(realTimeHandler);
            using var scheduledEvent = new ScheduledEvent("1", new[] { Time.EndOfTime }, (_, _) => { });
            using var scheduledEvent2 = new ScheduledEvent("2", new[] { Time.EndOfTime }, (_, _) => { });
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
            var mhdb = new MarketHoursDatabase(new Dictionary<SecurityDatabaseKey, MarketHoursDatabase.Entry>() { { key, entry } });
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
            MarketHoursDatabase.FromDataFolder().SetEntry(Market.USA, null, SecurityType.Equity, SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            var security = algorithm.AddEquity("SPY");
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

            algorithm.SetFinishedWarmingUp();
            realTimeHandler.SetTime(timeProvider.GetUtcNow());

            // wait for the internal thread to start
            WaitUntilActive(realTimeHandler);

            for (var i = 0; i < 5; i++)
            {
                timeProvider.Advance(step);

                // We only advanced half the time, so we should not have refreshed yet
                if (i % 2 == 0)
                {
                    Assert.IsFalse(realTimeHandler.SpdbRefreshed.Wait(100));
                }
                else
                {
                    Assert.IsTrue(realTimeHandler.SpdbRefreshed.Wait(2000));
                    realTimeHandler.SpdbRefreshed.Reset();
                }
            }
        }

        [TestCase(SecurityType.Equity, typeof(SymbolProperties))]
        [TestCase(SecurityType.Forex, typeof(SymbolProperties))]
        [TestCase(SecurityType.Future, typeof(SymbolProperties))]
        [TestCase(SecurityType.FutureOption, typeof(SymbolProperties))]
        [TestCase(SecurityType.Cfd, typeof(SymbolProperties))]
        [TestCase(SecurityType.Crypto, typeof(SymbolProperties))]
        [TestCase(SecurityType.CryptoFuture, typeof(SymbolProperties))]
        [TestCase(SecurityType.Index, typeof(SymbolProperties))]
        [TestCase(SecurityType.Option, typeof(OptionSymbolProperties))]
        [TestCase(SecurityType.IndexOption, typeof(IndexOptionSymbolProperties))]
        public void SecuritySymbolPropertiesTypeIsRespectedAfterRefresh(SecurityType securityType, Type expectedSymbolPropertiesType)
        {
            using var realTimeHandler = new SPDBTestLiveTradingRealTimeHandler();

            var timeProvider = realTimeHandler.PublicTimeProvider;
            timeProvider.SetCurrentTimeUtc(new DateTime(2023, 5, 30));

            var algorithm = new AlgorithmStub();
            var refreshPeriod = TimeSpan.FromDays(1);
            algorithm.Settings.DatabasesRefreshPeriod = refreshPeriod;

            var symbol = GetSymbol(securityType);
            var security = algorithm.AddSecurity(symbol);

            Assert.IsInstanceOf(expectedSymbolPropertiesType, security.SymbolProperties);

            realTimeHandler.Setup(algorithm,
                new AlgorithmNodePacket(PacketType.AlgorithmNode),
                new BacktestingResultHandler(),
                null,
                new TestTimeLimitManager());

            algorithm.SetFinishedWarmingUp();
            realTimeHandler.SetTime(timeProvider.GetUtcNow());

            // wait for the internal thread to start
            WaitUntilActive(realTimeHandler);

            var previousSymbolProperties = security.SymbolProperties;

            // Refresh the spdb
            timeProvider.Advance(refreshPeriod);
            Assert.IsTrue(realTimeHandler.SpdbRefreshed.Wait(5000));

            // Access the symbol properties again
            // The instance must have not been changed
            Assert.AreSame(security.SymbolProperties, previousSymbolProperties);
            Assert.IsInstanceOf(expectedSymbolPropertiesType, security.SymbolProperties);
        }

        private static Symbol GetSymbol(SecurityType securityType)
        {
            return securityType switch
            {
                SecurityType.Equity => Symbols.SPY,
                SecurityType.Forex => Symbols.USDJPY,
                SecurityType.Future => Symbols.Future_ESZ18_Dec2018,
                SecurityType.FutureOption => Symbol.CreateOption(
                    Symbols.Future_ESZ18_Dec2018,
                    Market.CME,
                    OptionStyle.American,
                    OptionRight.Call,
                    4000m,
                    new DateTime(2023, 6, 16)),
                SecurityType.Cfd => Symbols.DE10YBEUR,
                SecurityType.Crypto => Symbols.BTCUSD,
                SecurityType.CryptoFuture => Symbol.Create("BTCUSD", securityType, Market.Binance),
                SecurityType.Index => Symbols.SPX,
                SecurityType.Option => Symbols.SPY_C_192_Feb19_2016,
                SecurityType.IndexOption => Symbol.Create("SPX", securityType, Market.USA),
                _ => throw new ArgumentOutOfRangeException(nameof(securityType), securityType, null)
            };
        }

        private static void WaitUntilActive(LiveTradingRealTimeHandler realTimeHandler)
        {
            while (!realTimeHandler.IsActive)
            {
                Thread.Sleep(2);
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

        public class TestLiveTradingRealTimeHandler : LiveTradingRealTimeHandler
        {
            private MarketHoursDatabase newMarketHoursDatabase;
            public void SetMarketHoursDatabase(MarketHoursDatabase marketHoursDatabase)
            {
                newMarketHoursDatabase = marketHoursDatabase;
            }
            protected override void ResetMarketHoursDatabase()
            {
                if (newMarketHoursDatabase != null)
                {
                    MarketHoursDatabase.Merge(newMarketHoursDatabase, resetCustomEntries: false);
                }
                else
                {
                    base.ResetMarketHoursDatabase();
                }
            }

            public void TestRefreshMarketHoursToday(Security security, DateTime time, MarketHoursSegment expectedSegment)
            {
                ResetMarketHoursDatabase();
                AssertMarketHours(security, time, expectedSegment);
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
                using var scheduledEvent = new ScheduledEvent("RefreshHours", new[] { new DateTime(2023, 6, 29) }, (name, triggerTime) =>
                {
                    // refresh market hours from api every day
                    ResetMarketHoursDatabase();
                });
                Add(scheduledEvent);
                OnSecurityUpdated.Reset();
                SetTime(DateTime.UtcNow);
                WaitUntilActive(this);
                OnSecurityUpdated.WaitOne();
                Exit();
            }

            protected override void ResetMarketHoursDatabase()
            {
                var entry = new MarketHoursDatabase.Entry(TimeZones.NewYork, ExchangeHoursDataClass.CreateExchangeHoursWithHolidays());
                var key = new SecurityDatabaseKey(Market.USA, null, SecurityType.Equity);
                var mhdb = new MarketHoursDatabase(new Dictionary<SecurityDatabaseKey, MarketHoursDatabase.Entry>() { { key, entry } });
                MarketHoursDatabase.Merge(mhdb, resetCustomEntries: true);
                OnSecurityUpdated.Set();
            }
        }

        private class SPDBTestLiveTradingRealTimeHandler : LiveTradingRealTimeHandler, IDisposable
        {
            private bool _disposed;

            public ManualTimeProvider PublicTimeProvider = new ManualTimeProvider();

            protected override ITimeProvider TimeProvider { get { return PublicTimeProvider; } }

            public ManualResetEventSlim SpdbRefreshed = new ManualResetEventSlim(false);

            protected override void ResetSymbolPropertiesDatabase()
            {
                base.ResetSymbolPropertiesDatabase();
                SpdbRefreshed.Set();
            }

            protected override void WaitTillNextSecond(DateTime time)
            {
                Thread.Sleep(2);
            }

            public void Dispose()
            {
                if (_disposed) return;
                Exit();
                SpdbRefreshed.Dispose();
                _disposed = true;
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
                    yield return new TestCaseData(CreateExchangeHoursWithEarlyCloseAndLateOpen(), new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(10, 0, 0), new TimeSpan(13, 0, 0)));
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

        [TestFixture]
        [NonParallelizable]
        public class DateTimeRulesPickUpMarketHoursUpdates
        {
            private string _originalCacheDataFolder;

            [SetUp]
            public void SetUp()
            {
                _originalCacheDataFolder = Config.Get("cache-location");
                Config.Set("cache-location", "TestData/dynamic-market-hours/original");
                Globals.Reset();
                MarketHoursDatabase.Reset();
            }

            [TearDown]
            public void TearDown()
            {
                Config.Set("cache-location", _originalCacheDataFolder);
                Globals.Reset();
                MarketHoursDatabase.Reset();
            }

            private static IEnumerable<TestCaseData> TestCases()
            {
                // For this test case, market close will be updated from 4pm to 1pm.
                // So we will schedule an event to be fired on market close
                var expectedEventsFireTimesBeforeUpdate = new List<DateTime>
                {
                    new(2024, 12, 02, 16, 0, 0),
                    new(2024, 12, 03, 16, 0, 0),
                    new(2024, 12, 04, 16, 0, 0),
                    new(2024, 12, 05, 16, 0, 0),
                    new(2024, 12, 06, 16, 0, 0),
                    new(2024, 12, 09, 16, 0, 0),
                    new(2024, 12, 10, 16, 0, 0)
                };
                var expectedEventsFireTimesAfterUpdate = new List<DateTime>
                {
                    // Move next will already happen, so this first event will still be fired on the old market close time
                    new(2024, 12, 11, 16, 0, 0),
                    new(2024, 12, 12, 13, 0, 0),
                    new(2024, 12, 13, 13, 0, 0),
                    new(2024, 12, 16, 13, 0, 0),
                    new(2024, 12, 17, 13, 0, 0),
                    new(2024, 12, 18, 13, 0, 0)
                };
                var updatedMhdbFile = "TestData/dynamic-market-hours/modified-close";

                foreach (var withAddedSecurity in new[] { true, false })
                {
                    yield return new TestCaseData(updatedMhdbFile,
                        expectedEventsFireTimesBeforeUpdate,
                        expectedEventsFireTimesAfterUpdate,
                        false,
                        withAddedSecurity);
                }

                // For this test case a holiday will be added, so we will schedule an event to be fired every day at noon.
                expectedEventsFireTimesBeforeUpdate = new List<DateTime>
                {
                    new(2024, 12, 02, 12, 0, 0),
                    new(2024, 12, 03, 12, 0, 0),
                    new(2024, 12, 04, 12, 0, 0),
                    new(2024, 12, 05, 12, 0, 0),
                    new(2024, 12, 06, 12, 0, 0),
                    new(2024, 12, 09, 12, 0, 0),
                    new(2024, 12, 10, 12, 0, 0)
                };
                expectedEventsFireTimesAfterUpdate = new List<DateTime>
                {
                    new(2024, 12, 11, 12, 0, 0),
                    new(2024, 12, 12, 12, 0, 0),
                    // 13th is a holiday, and 14th and 15th are weekend days
                    new(2024, 12, 16, 12, 0, 0),
                    new(2024, 12, 17, 12, 0, 0),
                    new(2024, 12, 18, 12, 0, 0)
                };
                updatedMhdbFile = "TestData/dynamic-market-hours/modified-holidays";

                foreach (var withAddedSecurity in new[] { true, false })
                {
                    yield return new TestCaseData(updatedMhdbFile,
                        expectedEventsFireTimesBeforeUpdate,
                        expectedEventsFireTimesAfterUpdate,
                        true,
                        withAddedSecurity);
                }
            }

            [TestCaseSource(nameof(TestCases))]
            public void EventsAreFiredOnUpdatedRules(string updatedMhdbFile,
                List<DateTime> expectedEventsFireTimesBeforeUpdate,
                List<DateTime> expectedEventsFireTimesAfterUpdate,
                bool updatedHolidays,
                bool addedSecurity)
            {
                var algorithm = new AlgorithmStub();
                algorithm.SetStartDate(2024, 12, 02);

                // "Disable" mhdb automatic refresh to avoid interference with the test
                algorithm.Settings.DatabasesRefreshPeriod = TimeSpan.FromDays(30);
                algorithm.SetFinishedWarmingUp();

                var symbol = addedSecurity ? algorithm.AddEquity("SPY").Symbol : Symbols.SPY;

                var realTimeHandler = new TestRealTimeHandler();
                realTimeHandler.PublicTimeProvider.SetCurrentTimeUtc(algorithm.StartDate.ConvertToUtc(algorithm.TimeZone));
                realTimeHandler.Setup(algorithm,
                    new AlgorithmNodePacket(PacketType.AlgorithmNode),
                    new BacktestingResultHandler(),
                    null,
                    new TestTimeLimitManager());

                algorithm.Schedule.SetEventSchedule(realTimeHandler);

                // Start the real time handler thread
                realTimeHandler.SetTime(realTimeHandler.PublicTimeProvider.GetUtcNow());

                WaitUntilActive(realTimeHandler);

                try
                {
                    var mhdb = MarketHoursDatabase.FromDataFolder();
                    var marketHoursEntry = mhdb.GetEntry(symbol.ID.Market, symbol, symbol.SecurityType);
                    var exchangeTimeZone = marketHoursEntry.ExchangeHours.TimeZone;

                    // Schedule an event every day at market close
                    var firedEventTimes = new List<DateTime>();
                    using var fireEvent = new ManualResetEventSlim();

                    if (updatedHolidays)
                    {
                        algorithm.Schedule.On(algorithm.DateRules.EveryDay(symbol), algorithm.TimeRules.Noon, () =>
                        {
                            firedEventTimes.Add(realTimeHandler.PublicTimeProvider.GetUtcNow().ConvertFromUtc(algorithm.TimeZone));
                            fireEvent.Set();
                        });
                    }
                    else
                    {
                        algorithm.Schedule.On(algorithm.DateRules.EveryDay(symbol), algorithm.TimeRules.BeforeMarketClose(symbol, 0), () =>
                        {
                            firedEventTimes.Add(realTimeHandler.PublicTimeProvider.GetUtcNow().ConvertFromUtc(exchangeTimeZone));
                            fireEvent.Set();
                        });
                    }

                    // Events should be fired every week day at 16:00 (market close)

                    AssertScheduledEvents(realTimeHandler, exchangeTimeZone, fireEvent, firedEventTimes, expectedEventsFireTimesBeforeUpdate);

                    Config.Set("cache-location", updatedMhdbFile);
                    Globals.Reset();
                    realTimeHandler.ResetMarketHoursPublic();

                    firedEventTimes.Clear();

                    AssertScheduledEvents(realTimeHandler, exchangeTimeZone, fireEvent, firedEventTimes, expectedEventsFireTimesAfterUpdate);

                    // Just a final check: directly check for the market hours update in the data base
                    marketHoursEntry = mhdb.GetEntry(symbol.ID.Market, symbol, symbol.SecurityType);
                    if (updatedHolidays)
                    {
                        CollectionAssert.Contains(marketHoursEntry.ExchangeHours.Holidays, new DateTime(2024, 12, 13));
                    }
                    else
                    {
                        foreach (var hours in marketHoursEntry.ExchangeHours.MarketHours.Values.Where(x => x.DayOfWeek != DayOfWeek.Saturday && x.DayOfWeek != DayOfWeek.Sunday))
                        {
                            Assert.AreEqual(1, hours.Segments.Count);
                            Assert.AreEqual(new TimeSpan(13, 0, 0), hours.Segments[0].End);
                        }
                    }
                }
                finally
                {
                    realTimeHandler.Exit();
                }
            }

            private static void AssertScheduledEvents(TestRealTimeHandler realTimeHandler, DateTimeZone timeZone,
                ManualResetEventSlim fireEvent, List<DateTime> firedEventTimes, List<DateTime> expectedEventsFireTimes)
            {
                while (firedEventTimes.Count < expectedEventsFireTimes.Count)
                {
                    var currentEventsCount = firedEventTimes.Count;
                    var utcNow = realTimeHandler.PublicTimeProvider.GetUtcNow();
                    var nextTimeUtc = utcNow.AddMinutes(60);

                    realTimeHandler.PublicTimeProvider.SetCurrentTimeUtc(nextTimeUtc);

                    if (currentEventsCount < expectedEventsFireTimes.Count &&
                        nextTimeUtc.ConvertFromUtc(timeZone) >= expectedEventsFireTimes[currentEventsCount])
                    {
                        Assert.IsTrue(fireEvent.Wait(1000));
                        fireEvent.Reset();

                        Assert.AreEqual(currentEventsCount + 1, firedEventTimes.Count);
                        Assert.AreEqual(expectedEventsFireTimes[currentEventsCount], firedEventTimes.Last());
                    }
                }
            }

            private class TestRealTimeHandler : LiveTradingRealTimeHandler
            {
                public ManualTimeProvider PublicTimeProvider { get; set; } = new ManualTimeProvider();

                protected override ITimeProvider TimeProvider => PublicTimeProvider;

                public void ResetMarketHoursPublic()
                {
                    ResetMarketHoursDatabase();
                }
            }
        }

        [TestFixture]
        [NonParallelizable]
        public class SymbolPropertiesAreUpdated
        {
            private string _originalCacheDataFolder;

            [SetUp]
            public void SetUp()
            {
                _originalCacheDataFolder = Config.Get("cache-location");
                Config.Set("cache-location", "TestData/dynamic-symbol-properties/original");
                Globals.Reset();
                SymbolPropertiesDatabase.Reset();
            }

            [TearDown]
            public void TearDown()
            {
                Config.Set("cache-location", _originalCacheDataFolder);
                Globals.Reset();
                SymbolPropertiesDatabase.Reset();
            }

            [Test]
            public void SecurityGetsSymbolPropertiesUpdates()
            {
                var algorithm = new AlgorithmStub();
                algorithm.SetStartDate(2024, 12, 02);

                // "Disable" automatic refresh to avoid interference with the test
                algorithm.Settings.DatabasesRefreshPeriod = TimeSpan.FromDays(30);
                algorithm.SetFinishedWarmingUp();

                var security = algorithm.AddEquity("SPY");
                var symbol = security.Symbol;

                var realTimeHandler = new TestRealTimeHandler();
                realTimeHandler.PublicTimeProvider.SetCurrentTimeUtc(algorithm.StartDate.ConvertToUtc(algorithm.TimeZone));
                realTimeHandler.Setup(algorithm,
                    new AlgorithmNodePacket(PacketType.AlgorithmNode),
                    new BacktestingResultHandler(),
                    null,
                    new TestTimeLimitManager());

                algorithm.Schedule.SetEventSchedule(realTimeHandler);

                // Start the real time handler thread
                realTimeHandler.SetTime(realTimeHandler.PublicTimeProvider.GetUtcNow());
                WaitUntilActive(realTimeHandler);

                try
                {
                    var spdb = SymbolPropertiesDatabase.FromDataFolder();
                    var entry = spdb.GetSymbolProperties(Market.USA, symbol, symbol.SecurityType, "USD");
                    var securityEntry = security.SymbolProperties;

                    Assert.AreEqual(entry.Description, securityEntry.Description);
                    Assert.AreEqual(entry.QuoteCurrency, securityEntry.QuoteCurrency);
                    Assert.AreEqual(entry.ContractMultiplier, securityEntry.ContractMultiplier);
                    Assert.AreEqual(entry.MinimumPriceVariation, securityEntry.MinimumPriceVariation);
                    Assert.AreEqual(entry.LotSize, securityEntry.LotSize);
                    Assert.AreEqual(entry.MarketTicker, securityEntry.MarketTicker);
                    Assert.AreEqual(entry.MinimumOrderSize, securityEntry.MinimumOrderSize);
                    Assert.AreEqual(entry.PriceMagnifier, securityEntry.PriceMagnifier);
                    Assert.AreEqual(entry.StrikeMultiplier, securityEntry.StrikeMultiplier);

                    // Back up entry
                    entry = new SymbolProperties(entry.Description, entry.QuoteCurrency, entry.ContractMultiplier, entry.MinimumPriceVariation, entry.LotSize, entry.MarketTicker, entry.MinimumOrderSize, entry.PriceMagnifier, entry.StrikeMultiplier);

                    Config.Set("cache-location", "TestData/dynamic-symbol-properties/modified");
                    Globals.Reset();
                    realTimeHandler.ResetSymbolPropertiesDatabasePublic();

                    var newEntry = spdb.GetSymbolProperties(Market.USA, symbol, symbol.SecurityType, "USD");

                    Assert.AreEqual(newEntry.Description, securityEntry.Description);
                    Assert.AreEqual(newEntry.QuoteCurrency, securityEntry.QuoteCurrency);
                    Assert.AreEqual(newEntry.ContractMultiplier, securityEntry.ContractMultiplier);
                    Assert.AreEqual(newEntry.MinimumPriceVariation, securityEntry.MinimumPriceVariation);
                    Assert.AreEqual(newEntry.LotSize, securityEntry.LotSize);
                    Assert.AreEqual(newEntry.MarketTicker, securityEntry.MarketTicker);
                    Assert.AreEqual(newEntry.MinimumOrderSize, securityEntry.MinimumOrderSize);
                    Assert.AreEqual(newEntry.PriceMagnifier, securityEntry.PriceMagnifier);
                    Assert.AreEqual(newEntry.StrikeMultiplier, securityEntry.StrikeMultiplier);

                    // The old entry must be outdated
                    Assert.IsTrue(entry.Description != securityEntry.Description ||
                        entry.QuoteCurrency != securityEntry.QuoteCurrency ||
                        entry.ContractMultiplier != securityEntry.ContractMultiplier ||
                        entry.MinimumPriceVariation != securityEntry.MinimumPriceVariation ||
                        entry.LotSize != securityEntry.LotSize ||
                        entry.MarketTicker != securityEntry.MarketTicker ||
                        entry.MinimumOrderSize != securityEntry.MinimumOrderSize ||
                        entry.PriceMagnifier != securityEntry.PriceMagnifier ||
                        entry.StrikeMultiplier != securityEntry.StrikeMultiplier);
                }
                finally
                {
                    realTimeHandler.Exit();
                }
            }

            private class TestRealTimeHandler : LiveTradingRealTimeHandler
            {
                public ManualTimeProvider PublicTimeProvider { get; set; } = new ManualTimeProvider();

                protected override ITimeProvider TimeProvider => PublicTimeProvider;

                public void ResetSymbolPropertiesDatabasePublic()
                {
                    ResetSymbolPropertiesDatabase();
                }
            }
        }
    }
}
