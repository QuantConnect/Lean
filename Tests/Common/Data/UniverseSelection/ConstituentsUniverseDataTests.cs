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
*/

using System;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Data.UniverseSelection
{
    [TestFixture]
    public class ConstituentsUniverseDataTests
    {
        private SubscriptionDataConfig _config;
        private SecurityExchangeHours _exchangeHours;

        [SetUp]
        public void SetUp()
        {
            _config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.AAPL,
                Resolution.Second,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false,
                false,
                TickType.Trade,
                false);
            _exchangeHours = MarketHoursDatabase.FromDataFolder()
                 .GetEntry(Symbols.AAPL.ID.Market, Symbols.AAPL, Symbols.AAPL.ID.SecurityType).ExchangeHours;
        }

        [Test]
        public void BacktestSourceForEachTradableDate()
        {
            var reader = new ConstituentsUniverseData();

            var tradableDays = Time.EachTradeableDayInTimeZone(_exchangeHours,
                new DateTime(2019, 06, 9), // sunday
                new DateTime(2019, 06, 16),
                _config.DataTimeZone,
                _config.ExtendedMarketHours);

            foreach (var tradableDay in tradableDays)
            {
                if (tradableDay.DayOfWeek == DayOfWeek.Saturday
                    || tradableDay.DayOfWeek == DayOfWeek.Sunday)
                {
                    Assert.Fail($"Unexpected tradable DayOfWeek {tradableDay.DayOfWeek}");
                }

                var source = reader.GetSource(_config, tradableDay, false);
                // Mon to Friday
                Assert.IsTrue(source.Source.Contains($"{tradableDay:yyyyMMdd}"));
            }
        }

        [Test]
        public void BacktestDataTimeForEachTradableDate()
        {
            var reader = new ConstituentsUniverseData();

            var tradableDays = Time.EachTradeableDayInTimeZone(_exchangeHours,
                new DateTime(2019, 06, 9), // sunday
                new DateTime(2019, 06, 16),
                _config.DataTimeZone,
                _config.ExtendedMarketHours);

            foreach (var tradableDay in tradableDays)
            {
                var dataPoint = reader.Reader(_config, "NONE,NONE 0", tradableDay, false);
                Assert.AreEqual(dataPoint.Time, tradableDay);
                // emitted tomorrow
                Assert.AreEqual(dataPoint.EndTime, tradableDay.AddDays(1));
            }
        }

        [Test]
        public void LiveSourceForCurrentDate()
        {
            var reader = new ConstituentsUniverseData();

            var currentTime = DateTime.UtcNow;
            var source = reader.GetSource(_config, currentTime, true);
            // From Tue to Sat will find files from Mon to Friday
            Assert.IsTrue(source.Source.Contains($"{currentTime.AddDays(-1):yyyyMMdd}"));
        }

        [Test]
        public void LiveDataTimeForCurrentDate()
        {
            var reader = new ConstituentsUniverseData();

            var currentTime = DateTime.UtcNow;

            var dataPoint = reader.Reader(_config, "NONE,NONE 0", currentTime, true);
            Assert.AreEqual(dataPoint.Time, currentTime.AddDays(-1));
            // emitted right away
            Assert.AreEqual(dataPoint.EndTime, currentTime);
        }
    }
}
