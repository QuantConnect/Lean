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

namespace QuantConnect.Tests.Engine.RealTime
{
    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
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
            using var scheduledEvent = new ScheduledEvent("1", new []{ Time.EndOfTime }, (_, _) => { });
            using var scheduledEvent2 = new ScheduledEvent("2", new []{ Time.EndOfTime }, (_, _) => { });
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

            algorithm.SetFinishedWarmingUp();
            realTimeHandler.SetTime(timeProvider.GetUtcNow());

            // wait for the internal thread to start
            WaitUntilActive(realTimeHandler);

            Assert.IsTrue(realTimeHandler.SpdbRefreshed.IsSet);
            Assert.IsTrue(realTimeHandler.SecuritySymbolPropertiesUpdated.IsSet);

            realTimeHandler.SpdbRefreshed.Reset();
            realTimeHandler.SecuritySymbolPropertiesUpdated.Reset();

            var events = new[] { realTimeHandler.SpdbRefreshed.WaitHandle, realTimeHandler.SecuritySymbolPropertiesUpdated.WaitHandle };
            for (var i = 0; i < 10; i++)
            {
                timeProvider.Advance(step);

                // We only advanced half the time, so we should not have refreshed yet
                if (i % 2 == 0)
                {
                    Assert.IsFalse(WaitHandle.WaitAll(events, 5000));
                }
                else
                {
                    Assert.IsTrue(WaitHandle.WaitAll(events, 5000));
                    realTimeHandler.SpdbRefreshed.Reset();
                    realTimeHandler.SecuritySymbolPropertiesUpdated.Reset();
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

            Assert.IsTrue(realTimeHandler.SpdbRefreshed.IsSet);
            Assert.IsTrue(realTimeHandler.SecuritySymbolPropertiesUpdated.IsSet);

            realTimeHandler.SpdbRefreshed.Reset();
            realTimeHandler.SecuritySymbolPropertiesUpdated.Reset();

            var previousSymbolProperties = security.SymbolProperties;

            // Refresh the spdb
            timeProvider.Advance(refreshPeriod);
            Assert.IsTrue(realTimeHandler.SpdbRefreshed.Wait(5000));
            Assert.IsTrue(realTimeHandler.SecuritySymbolPropertiesUpdated.Wait(5000));

            // Access the symbol properties again
            // The instance must have been changed
            Assert.AreNotSame(security.SymbolProperties, previousSymbolProperties);
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
                Thread.Sleep(5);
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
                using var scheduledEvent = new ScheduledEvent("RefreshHours", new[] { new DateTime(2023, 6, 29) }, (name, triggerTime) =>
                {
                    // refresh market hours from api every day
                    RefreshMarketHours((new DateTime(2023, 5, 30)).Date);
                });
                Add(scheduledEvent);
                OnSecurityUpdated.Reset();
                SetTime(DateTime.UtcNow);
                WaitUntilActive(this);
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
            private bool _disposed;
            private int _securitiesUpdated;

            public ManualTimeProvider PublicTimeProvider = new ManualTimeProvider();

            protected override ITimeProvider TimeProvider { get { return PublicTimeProvider; } }

            public ManualResetEventSlim SpdbRefreshed = new ManualResetEventSlim(false);
            public ManualResetEventSlim SecuritySymbolPropertiesUpdated = new ManualResetEventSlim(false);

            protected override void RefreshSymbolProperties()
            {
                if (_disposed) return;

                base.RefreshSymbolProperties();
                SpdbRefreshed.Set();
            }

            protected override void UpdateSymbolProperties(Security security)
            {
                if (_disposed) return;

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
                if (_disposed) return;
                Exit();
                SpdbRefreshed.Dispose();
                SecuritySymbolPropertiesUpdated.Dispose();
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

















        private class TestRealTimeHandler : LiveTradingRealTimeHandler
        {
            private MarketHoursDatabase newMarketHoursDatabase;

            public ManualTimeProvider PublicTimeProvider { get; set; } = new ManualTimeProvider();

            protected override ITimeProvider TimeProvider => PublicTimeProvider;

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

            public void ResetMarketHoursPublic(DateTime time)
            {
                RefreshMarketHours(time);
            }
        }

        [Test]
        [NonParallelizable]
        public void DateTimeRulesPickUpMarketHoursDataBaseUpdates([Values] bool addedSecurity)
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetStartDate(2024, 12, 02);

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
                algorithm.Schedule.On(algorithm.DateRules.EveryDay(symbol), algorithm.TimeRules.BeforeMarketClose(symbol, 0), () =>
                {
                    firedEventTimes.Add(realTimeHandler.PublicTimeProvider.GetUtcNow().ConvertFromUtc(exchangeTimeZone));
                    fireEvent.Set();
                });

                // Events should be fired every week day at 16:00 (market close)
                var expectedEventsFireTimes = new List<DateTime>
                {
                    new(2024, 12, 02, 16, 0, 0),
                    new(2024, 12, 03, 16, 0, 0),
                    new(2024, 12, 04, 16, 0, 0),
                    new(2024, 12, 05, 16, 0, 0),
                    new(2024, 12, 06, 16, 0, 0),
                    new(2024, 12, 09, 16, 0, 0),
                    new(2024, 12, 10, 16, 0, 0)
                };

                var utcNow = default(DateTime);
                var AssertScheduledEvents = () =>
                {
                    while (firedEventTimes.Count < expectedEventsFireTimes.Count)
                    {
                        var currentEventsCount = firedEventTimes.Count;
                        utcNow = realTimeHandler.PublicTimeProvider.GetUtcNow();
                        var nextTimeUtc = utcNow.AddMinutes(60);

                        realTimeHandler.PublicTimeProvider.SetCurrentTimeUtc(nextTimeUtc);

                        if (currentEventsCount < expectedEventsFireTimes.Count &&
                            nextTimeUtc.ConvertFromUtc(exchangeTimeZone) >= expectedEventsFireTimes[currentEventsCount])
                        {
                            Assert.IsTrue(fireEvent.Wait(1000));
                            fireEvent.Reset();

                            Assert.AreEqual(currentEventsCount + 1, firedEventTimes.Count);
                            Assert.AreEqual(expectedEventsFireTimes[currentEventsCount], firedEventTimes.Last());
                        }
                    }
                };

                AssertScheduledEvents();

                // Update market hours db: change market close from 16:00 to 13:00
                marketHoursEntry.ExchangeHours.Update(new SecurityExchangeHours(
                    marketHoursEntry.ExchangeHours.TimeZone,
                    marketHoursEntry.ExchangeHours.Holidays,
                    new()
                    {
                        { DayOfWeek.Sunday, new LocalMarketHours(DayOfWeek.Sunday, []) },
                        { DayOfWeek.Monday, new LocalMarketHours(DayOfWeek.Monday, new TimeSpan(9, 30, 0), new TimeSpan(13, 0, 0)) },
                        { DayOfWeek.Tuesday, new LocalMarketHours(DayOfWeek.Tuesday, new TimeSpan(9, 30, 0), new TimeSpan(13, 0, 0)) },
                        { DayOfWeek.Wednesday, new LocalMarketHours(DayOfWeek.Wednesday, new TimeSpan(9, 30, 0), new TimeSpan(13, 0, 0)) },
                        { DayOfWeek.Thursday, new LocalMarketHours(DayOfWeek.Thursday, new TimeSpan(9, 30, 0), new TimeSpan(13, 0, 0)) },
                        { DayOfWeek.Friday, new LocalMarketHours(DayOfWeek.Friday, new TimeSpan(9, 30, 0), new TimeSpan(13, 0, 0)) },
                        { DayOfWeek.Saturday, new LocalMarketHours(DayOfWeek.Saturday, []) },
                    },
                    marketHoursEntry.ExchangeHours.EarlyCloses,
                    marketHoursEntry.ExchangeHours.LateOpens));
                realTimeHandler.SetMarketHoursDatabase(mhdb);
                realTimeHandler.ResetMarketHoursPublic(realTimeHandler.PublicTimeProvider.GetUtcNow().ConvertFromUtc(exchangeTimeZone));

                marketHoursEntry = mhdb.GetEntry(symbol.ID.Market, symbol, symbol.SecurityType);
                foreach (var hours in marketHoursEntry.ExchangeHours.MarketHours.Values.Where(x => x.DayOfWeek != DayOfWeek.Saturday && x.DayOfWeek != DayOfWeek.Sunday))
                {
                    Assert.AreEqual(1, hours.Segments.Count);
                    Assert.AreEqual(new TimeSpan(13, 0, 0), hours.Segments[0].End);
                }

                firedEventTimes.Clear();
                expectedEventsFireTimes = new List<DateTime>
                {
                    new(2024, 12, 11, 16, 0, 0),
                    new(2024, 12, 12, 13, 0, 0),
                    new(2024, 12, 13, 13, 0, 0),
                    new(2024, 12, 16, 13, 0, 0),
                    new(2024, 12, 17, 13, 0, 0),
                    new(2024, 12, 18, 13, 0, 0)
                };

                AssertScheduledEvents();
            }
            finally
            {
                realTimeHandler.Exit();
            }
        }
    }
}
