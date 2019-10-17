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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Scheduling;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Scheduling
{
    [TestFixture]
    public class TimeRulesTests
    {
        [Test]
        public void AtSpecificTimeFromUtc()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.At(TimeSpan.FromHours(12));
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 01) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(12), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void AtSpecificTimeFromNonUtc()
        {
            var rules = GetTimeRules(TimeZones.NewYork);
            var rule = rules.At(TimeSpan.FromHours(12));
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 01) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(12 + 5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void RegularMarketOpenNoDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.AfterMarketOpen(Symbols.SPY);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(9.5 + 5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void RegularMarketOpenWithDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.AfterMarketOpen(Symbols.SPY, 30);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(9.5 + 5 + .5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void ExtendedMarketOpenNoDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.AfterMarketOpen(Symbols.SPY, 0, true);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(4 + 5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void ExtendedMarketOpenWithDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.AfterMarketOpen(Symbols.SPY, 30, true);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(4 + 5 + .5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void RegularMarketCloseNoDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.BeforeMarketClose(Symbols.SPY);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(16 + 5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void RegularMarketCloseWithDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.BeforeMarketClose(Symbols.SPY, 30);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(16 + 5 - .5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void ExtendedMarketCloseNoDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.BeforeMarketClose(Symbols.SPY, 0, true);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours((20 + 5) % 24), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void ExtendedMarketCloseWithDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.BeforeMarketClose(Symbols.SPY, 30, true);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours((20 + 5 - .5) % 24), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void EveryValidatesTimeSpan(int timeSpanMinutes)
        {
            var rules = GetTimeRules(TimeZones.Utc);
            Assert.Throws<ArgumentException>(() => rules.Every(TimeSpan.FromMinutes(timeSpanMinutes)));
        }

        [Test]
        public void Every()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var every = rules.Every(TimeSpan.FromMilliseconds(500));
            var previous = new DateTime(2019, 6, 28);
            var dateTimes = every.CreateUtcEventTimes(new[] { previous });
            foreach (var dateTime in dateTimes)
            {
                if (previous != dateTime)
                {
                    Assert.Fail("Unexpected Every DateTime");
                }
                previous = previous.AddMilliseconds(500);
            }
        }

        private static TimeRules GetTimeRules(DateTimeZone dateTimeZone)
        {
            var timeKeeper = new TimeKeeper(DateTime.Today, new List<DateTimeZone>());
            var manager = new SecurityManager(timeKeeper);
            var marketHourDbEntry = MarketHoursDatabase.FromDataFolder().GetEntry(Market.USA, (string)null, SecurityType.Equity);
            var securityExchangeHours = marketHourDbEntry.ExchangeHours;
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Daily, marketHourDbEntry.DataTimeZone, securityExchangeHours.TimeZone, true, false, false);
            manager.Add(
                Symbols.SPY,
                new Security(
                    securityExchangeHours,
                    config,
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );
            var rules = new TimeRules(manager, dateTimeZone);
            return rules;
        }
    }
}
