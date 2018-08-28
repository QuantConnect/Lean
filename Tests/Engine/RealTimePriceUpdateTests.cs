using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine
{
    [TestFixture]
    public class RealTimePriceUpdateTests
    {
        private TestableLiveTradingDataFeed _liveTradingDataFeed;
        private SecurityExchangeHours _exchangeHours;

        [TestFixtureSetUp]
        public void Setup()
        {
            var sunday = new LocalMarketHours(DayOfWeek.Sunday, new TimeSpan(17, 0, 0), TimeSpan.FromTicks(Time.OneDay.Ticks - 1));
            var monday = LocalMarketHours.OpenAllDay(DayOfWeek.Monday);
            var tuesday = LocalMarketHours.OpenAllDay(DayOfWeek.Tuesday);
            var wednesday = LocalMarketHours.OpenAllDay(DayOfWeek.Wednesday);
            var thursday = LocalMarketHours.OpenAllDay(DayOfWeek.Thursday);
            var friday = new LocalMarketHours(DayOfWeek.Friday, TimeSpan.Zero, new TimeSpan(17, 0, 0));
            var earlyCloses = new Dictionary<DateTime, TimeSpan>();
            _exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, USHoliday.Dates.Select(x => x.Date), new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses);

            _liveTradingDataFeed = new TestableLiveTradingDataFeed();

            var jobPacket = new LiveNodePacket()
            {
                DeployId = "",
                Brokerage = BrokerageName.OandaBrokerage.ToString(),
                DataQueueHandler = "LiveDataQueue"
            };

            var algo = new TestAlgorithm();
            var dataManager = new DataManager();
            algo.SubscriptionManager.SetDataManager(dataManager);

            _liveTradingDataFeed.Initialize(algo, jobPacket, new LiveTradingResultHandler(), new LocalDiskMapFileProvider(),
                                            null, new DefaultDataProvider(), dataManager);

            algo.Initialize();
        }

        /// <summary>
        /// Test algorithm which doesn't consume any feeds for simple testing.
        /// </summary>
        private class TestAlgorithm : QCAlgorithm
        {
            public override void Initialize() { SetBenchmark(time => 0); }
        }


        [TestFixtureTearDown]
        public void TearDown()
        {
            _liveTradingDataFeed.Exit();
        }


        [Test]
        public void NullSubscriptions_DoNotIndicateRealTimePriceUpdates()
        {
            Assert.IsFalse(_liveTradingDataFeed.UpdateRealTimePrice(null, new TimeZoneOffsetProviderNeverOpen()));
        }

        [Test]
        public void ClosedExchanges_DoNotIndicateRealTimePriceUpdates()
        {
            var security = new Security(Symbol.Empty, _exchangeHours, new Cash("USA", 100m, 1m), SymbolProperties.GetDefault("USA"));
            var subscription = new Subscription(null, security, null, null, new TimeZoneOffsetProviderNeverOpen(), DateTime.MinValue, DateTime.MaxValue, false);
            Assert.IsFalse(_liveTradingDataFeed.UpdateRealTimePrice(subscription, new TimeZoneOffsetProviderNeverOpen()));
        }

        [Test]
        public void OpenExchanges_DoIndicateRealTimePriceUpdates()
        {
            var security = new Security(Symbol.Empty, _exchangeHours, new Cash("USA", 100m, 1m), SymbolProperties.GetDefault("USA"));
            var subscription = new Subscription(null, security, null, null, new TimeZoneOffsetProviderAlwaysOpen(), DateTime.MinValue, DateTime.MaxValue, false);
            Assert.IsTrue(_liveTradingDataFeed.UpdateRealTimePrice(subscription, new TimeZoneOffsetProviderAlwaysOpen()));
        }

        class TestableLiveTradingDataFeed : LiveTradingDataFeed
        {
            public bool UpdateRealTimePrice(Subscription subscription, TimeZoneOffsetProvider timeZoneOffsetProvider)
            {
                return SubscriptionShouldUpdateRealTimePrice(subscription, timeZoneOffsetProvider);
            }
        }

        class TimeZoneOffsetProviderNeverOpen : TimeZoneOffsetProvider
        {
            public TimeZoneOffsetProviderNeverOpen()
                : base(TimeZones.NewYork, DateTime.Parse("1/1/2016"), DateTime.Parse("1/1/2018"))
            {
            }

            public override DateTime ConvertFromUtc(DateTime utcTime)
            {
                // return a date that's always closed for equities
                return new DateTime(2017, 3, 18, 23, 0, 0);
            }
        }

        class TimeZoneOffsetProviderAlwaysOpen : TimeZoneOffsetProvider
        {
            public TimeZoneOffsetProviderAlwaysOpen()
                : base(TimeZones.NewYork, DateTime.Parse("1/1/2016"), DateTime.Parse("1/1/2018"))
            {
            }

            public override DateTime ConvertFromUtc(DateTime utcTime)
            {
                // return a date that's always open for equities
                return new DateTime(2017, 3, 20, 13, 0, 0);
            }
        }
    }
}
