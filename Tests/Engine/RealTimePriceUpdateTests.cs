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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine
{
    [TestFixture]
    public class RealTimePriceUpdateTests
    {
        private TestableLiveTradingDataFeed _liveTradingDataFeed;
        private SecurityExchangeHours _exchangeHours;
        private SubscriptionDataConfig _config;

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
            var lateOpens = new Dictionary<DateTime, TimeSpan>();
            _exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, USHoliday.Dates.Select(x => x.Date), new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);

            _liveTradingDataFeed = new TestableLiveTradingDataFeed();

            var jobPacket = new LiveNodePacket()
            {
                DeployId = "",
                Brokerage = BrokerageName.OandaBrokerage.ToString(),
                DataQueueHandler = "LiveDataQueue"
            };

            var algo = new TestAlgorithm();
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var dataManager = new DataManager(_liveTradingDataFeed,
                new UniverseSelection(
                    algo,
                    new SecurityService(algo.Portfolio.CashBook, marketHoursDatabase, symbolPropertiesDataBase, algo, RegisteredSecurityDataTypesProvider.Null, new SecurityCacheProvider(algo.Portfolio))),
                algo,
                algo.TimeKeeper,
                marketHoursDatabase,
                true,
                RegisteredSecurityDataTypesProvider.Null);
            algo.SubscriptionManager.SetDataManager(dataManager);
            var synchronizer = new LiveSynchronizer();
            synchronizer.Initialize(algo, dataManager);
            _liveTradingDataFeed.Initialize(algo, jobPacket, new LiveTradingResultHandler(), new LocalDiskMapFileProvider(),
                                            null, new DefaultDataProvider(), dataManager, synchronizer);
            algo.Initialize();

            _config = SecurityTests.CreateTradeBarConfig();
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
            Assert.IsFalse(_liveTradingDataFeed.UpdateRealTimePrice(null, new TimeZoneOffsetProviderNeverOpen(), _exchangeHours));
        }

        [Test]
        public void ClosedExchanges_DoNotIndicateRealTimePriceUpdates()
        {
            var security = new Security(
                Symbols.AAPL,
                _exchangeHours,
                new Cash("USA", 100m, 1m),
                SymbolProperties.GetDefault("USA"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var subscriptionRequest = new SubscriptionRequest(false, null, security, _config, DateTime.MinValue, DateTime.MaxValue);
            var subscription = new Subscription(subscriptionRequest, null, new TimeZoneOffsetProviderNeverOpen());
            Assert.IsFalse(_liveTradingDataFeed.UpdateRealTimePrice(subscription, new TimeZoneOffsetProviderNeverOpen(), _exchangeHours));
        }

        [Test]
        public void OpenExchanges_DoIndicateRealTimePriceUpdates()
        {
            var security = new Security(
                Symbols.AAPL,
                _exchangeHours,
                new Cash("USA", 100m, 1m),
                SymbolProperties.GetDefault("USA"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var subscriptionRequest = new SubscriptionRequest(false, null, security, _config, DateTime.MinValue, DateTime.MaxValue);
            var subscription = new Subscription(subscriptionRequest, null, new TimeZoneOffsetProviderNeverOpen());
            Assert.IsTrue(_liveTradingDataFeed.UpdateRealTimePrice(subscription, new TimeZoneOffsetProviderAlwaysOpen(), _exchangeHours));
        }

        class TimeZoneOffsetProviderNeverOpen : TimeZoneOffsetProvider
        {
            public TimeZoneOffsetProviderNeverOpen()
                : base(TimeZones.NewYork,
                    Parse.DateTime("1/1/2016"),
                    Parse.DateTime("1/1/2018")
                )
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
                : base(TimeZones.NewYork,
                    Parse.DateTime("1/1/2016"),
                    Parse.DateTime("1/1/2018")
                )
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
