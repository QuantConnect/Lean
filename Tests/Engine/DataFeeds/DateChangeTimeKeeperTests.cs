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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class DateChangeTimeKeeperTests
    {
        private static DateTime _start = new DateTime(2024, 10, 01);
        private static DateTime _end = new DateTime(2024, 10, 11);

        private static TestCaseData[] TimeZonesTestCases => new TestCaseData[]
        {
            new(TimeZones.Utc, DateTimeZone.ForOffset(Offset.FromHours(-1))),
            new(TimeZones.Utc, DateTimeZone.ForOffset(Offset.FromHours(-2))),
            new(TimeZones.Utc, DateTimeZone.ForOffset(Offset.FromHours(-3))),
            new(TimeZones.Utc, DateTimeZone.ForOffset(Offset.FromHours(-4))),
            new(TimeZones.Utc, DateTimeZone.ForOffset(Offset.FromHours(1))),
            new(TimeZones.Utc, DateTimeZone.ForOffset(Offset.FromHours(2))),
            new(TimeZones.Utc, DateTimeZone.ForOffset(Offset.FromHours(3))),
            new(TimeZones.Utc, DateTimeZone.ForOffset(Offset.FromHours(4))),
            new(TimeZones.Utc, TimeZones.Utc),
            new(TimeZones.NewYork, TimeZones.NewYork),
            new(TimeZones.HongKong, TimeZones.HongKong),
        };

        [TestCaseSource(nameof(TimeZonesTestCases))]
        public void EmitsFirstExchangeDateEvent(DateTimeZone dataTimeZone, DateTimeZone exchangeTimeZone)
        {
            using var timeKeeper = GetTimeKeeper(dataTimeZone, exchangeTimeZone, true, null, out var tradableDates, out var config, out var exchangeHours);

            var emittedExchangeDates = new List<DateTime>();
            void HandleNewTradableDate(object sender, DateTime date)
            {
                emittedExchangeDates.Add(date);
            }

            timeKeeper.NewExchangeDate += HandleNewTradableDate;

            var firstDataDate = tradableDates[0];
            var firstDataDateInExchangeTimeZone = firstDataDate.ConvertTo(dataTimeZone, exchangeTimeZone);
            var firstExchangeDateIsBeforeFirstDataDate = firstDataDateInExchangeTimeZone < firstDataDate;

            try
            {
                Assert.IsTrue(timeKeeper.TryAdvanceUntilNextDataDate());
                Assert.AreEqual(1, emittedExchangeDates.Count);

                if (firstExchangeDateIsBeforeFirstDataDate)
                {
                    // The first exchange date is the day before the first data date when the exchange is behind the data
                    var expectedFirstExchangeDate = firstDataDate.AddDays(-1);
                    Assert.AreEqual(expectedFirstExchangeDate, emittedExchangeDates[0]);
                    Assert.AreEqual(expectedFirstExchangeDate.ConvertTo(exchangeTimeZone, dataTimeZone), timeKeeper.DataTime);
                }
                else
                {
                    // If exchange is ahead of the data, even though technically at the first data date the exchange date hasn't changed,
                    // we emit the first one so that first daily actions are performed (mappings, delistings, etc).
                    Assert.AreEqual(firstDataDate, emittedExchangeDates[0]);
                    Assert.AreEqual(firstDataDate, timeKeeper.DataTime);
                }
            }
            finally
            {
                timeKeeper.NewExchangeDate -= HandleNewTradableDate;
            }
        }

        [TestCaseSource(nameof(TimeZonesTestCases))]
        public void ExchangeDatesAreEmittedByAdvancingToNextDataDate(DateTimeZone dataTimeZone, DateTimeZone exchangeTimeZone)
        {
            using var timeKeeper = GetTimeKeeper(dataTimeZone, exchangeTimeZone, false, null, out var tradableDates, out var config, out var exchangeHours);

            var exchangeDateEmitted = false;
            var emittedExchangeDates = new List<DateTime>();
            void HandleNewTradableDate(object sender, DateTime date)
            {
                emittedExchangeDates.Add(date);
                exchangeDateEmitted = true;
            }

            timeKeeper.NewExchangeDate += HandleNewTradableDate;

            var expectedExchangeDates = new List<DateTime>()
            {
                new(2024, 10, 1),
                new(2024, 10, 2),
                new(2024, 10, 3),
                new(2024, 10, 4),
                new(2024, 10, 7),
                new(2024, 10, 8),
                new(2024, 10, 9),
                new(2024, 10, 10),
                new(2024, 10, 11),
            };

            var firstDataDate = tradableDates[0];
            var firstDataDateInExchangeTimeZone = firstDataDate.ConvertTo(dataTimeZone, exchangeTimeZone);
            var exchangeIsBehindData = firstDataDateInExchangeTimeZone < firstDataDate;
            if (exchangeIsBehindData)
            {
                expectedExchangeDates.Insert(0, new(2024, 9, 30));
                expectedExchangeDates.RemoveAt(expectedExchangeDates.Count - 1);
            }

            try
            {
                // Flush first date:
                Assert.IsTrue(timeKeeper.TryAdvanceUntilNextDataDate());
                Assert.IsTrue(exchangeDateEmitted);

                for (var i = 1; i < tradableDates.Count; i++)
                {
                    exchangeDateEmitted = false;
                    Assert.IsTrue(timeKeeper.TryAdvanceUntilNextDataDate());

                    if (timeKeeper.DataTime == timeKeeper.ExchangeTime)
                    {
                        Assert.AreEqual(tradableDates[i], timeKeeper.DataTime);
                        Assert.AreEqual(tradableDates[i], timeKeeper.ExchangeTime);
                        Assert.IsTrue(exchangeDateEmitted);
                        Assert.AreEqual(tradableDates[i], emittedExchangeDates[^1]);
                    }
                    else
                    {
                        if (exchangeIsBehindData)
                        {
                            Assert.IsFalse(exchangeDateEmitted);
                            Assert.AreEqual(tradableDates[i - 1], timeKeeper.DataTime);
                        }
                        else
                        {
                            Assert.IsTrue(exchangeDateEmitted);
                            Assert.AreEqual(emittedExchangeDates[^1], timeKeeper.ExchangeTime);
                        }

                        // Move again to the next data date or next exchange date
                        exchangeDateEmitted = false;
                        Assert.IsTrue(timeKeeper.TryAdvanceUntilNextDataDate());

                        if (exchangeIsBehindData)
                        {
                            Assert.IsTrue(exchangeDateEmitted);
                            Assert.AreEqual(emittedExchangeDates[^1], timeKeeper.ExchangeTime);
                        }
                        else
                        {
                            Assert.IsFalse(exchangeDateEmitted);
                            Assert.AreEqual(tradableDates[i], timeKeeper.DataTime);
                        }
                    }
                }

                CollectionAssert.AreEqual(expectedExchangeDates, emittedExchangeDates);
            }
            finally
            {
                timeKeeper.NewExchangeDate -= HandleNewTradableDate;
            }
        }

        [TestCaseSource(nameof(TimeZonesTestCases))]
        public void ExchangeDatesAreEmittedByAdvancingToNextGivenExchangeTime(DateTimeZone dataTimeZone, DateTimeZone exchangeTimeZone)
        {
            using var timeKeeper = GetTimeKeeper(dataTimeZone, exchangeTimeZone, false, null, out var tradableDates, out var config, out var exchangeHours);

            var exchangeDateEmitted = false;
            var emittedExchangeDates = new List<DateTime>();
            void HandleNewTradableDate(object sender, DateTime date)
            {
                emittedExchangeDates.Add(date);
                exchangeDateEmitted = true;
            }

            timeKeeper.NewExchangeDate += HandleNewTradableDate;

            var expectedExchangeDates = new List<DateTime>()
            {
                new(2024, 10, 1),
                new(2024, 10, 2),
                new(2024, 10, 3),
                new(2024, 10, 4),
                new(2024, 10, 7),
                new(2024, 10, 8),
                new(2024, 10, 9),
                new(2024, 10, 10),
                new(2024, 10, 11),
            };

            var firstDataDate = tradableDates[0];
            var firstDataDateInExchangeTimeZone = firstDataDate.ConvertTo(dataTimeZone, exchangeTimeZone);
            var exchangeIsBehindData = firstDataDateInExchangeTimeZone < firstDataDate;
            if (exchangeIsBehindData)
            {
                expectedExchangeDates.Insert(0, new(2024, 9, 30));
                expectedExchangeDates.RemoveAt(expectedExchangeDates.Count - 1);
            }

            try
            {
                // Flush first date:
                Assert.IsTrue(timeKeeper.TryAdvanceUntilNextDataDate());
                Assert.IsTrue(exchangeDateEmitted);

                // Using multiple step sizes to simulate data coming in with gaps
                var steps = new Queue<TimeSpan>();
                steps.Enqueue(TimeSpan.FromMinutes(1));
                steps.Enqueue(TimeSpan.FromMinutes(5));
                steps.Enqueue(TimeSpan.FromMinutes(30));
                steps.Enqueue(TimeSpan.FromHours(1));

                while (emittedExchangeDates.Count != expectedExchangeDates.Count)
                {
                    exchangeDateEmitted = false;
                    var currentExchangeTime = timeKeeper.ExchangeTime;
                    var step = steps.Dequeue();
                    steps.Enqueue(step);
                    var nextExchangeTime = currentExchangeTime + step;
                    timeKeeper.AdvanceUntilExchangeTime(nextExchangeTime);

                    if (nextExchangeTime.Date != currentExchangeTime.Date &&
                        exchangeHours.IsDateOpen(nextExchangeTime.Date, config.ExtendedMarketHours))
                    {
                        Assert.IsTrue(exchangeDateEmitted);
                        Assert.AreEqual(emittedExchangeDates[^1], nextExchangeTime.Date);
                        Assert.AreEqual(timeKeeper.ExchangeTime, nextExchangeTime.Date);
                    }
                    else
                    {
                        Assert.IsFalse(exchangeDateEmitted);
                        Assert.AreEqual(timeKeeper.ExchangeTime, nextExchangeTime);
                    }
                }

                CollectionAssert.AreEqual(expectedExchangeDates, emittedExchangeDates);
            }
            finally
            {
                timeKeeper.NewExchangeDate -= HandleNewTradableDate;
            }
        }

        [TestCaseSource(nameof(TimeZonesTestCases))]
        public void DoesNotAdvancePastDelistingDateWhenAdvancingToNextDataDate(DateTimeZone dataTimeZone, DateTimeZone exchangeTimeZone)
        {
            var delistingDate = _start.AddDays(7);
            using var timeKeeper = GetTimeKeeper(dataTimeZone, exchangeTimeZone, false, delistingDate, out var _, out var _, out var _);

            while (timeKeeper.TryAdvanceUntilNextDataDate())
            {
                Assert.LessOrEqual(timeKeeper.ExchangeTime.Date, delistingDate.AddDays(1));
            }
        }

        private static DateChangeTimeKeeper GetTimeKeeper(DateTimeZone dataTimeZone,
            DateTimeZone exchangeTimeZone,
            bool exchangeAlwaysOpen,
            DateTime? delistingDate,
            out List<DateTime> tradableDates,
            out SubscriptionDataConfig config,
            out SecurityExchangeHours exchangeHours)
        {
            var symbol = Symbols.SPY;
            exchangeHours = GetMarketHours(symbol, exchangeTimeZone, exchangeAlwaysOpen);
            config = new SubscriptionDataConfig(typeof(TradeBar),
                symbol,
                Resolution.Minute,
                dataTimeZone,
                exchangeTimeZone,
                false,
                true,
                false);

            tradableDates = Time.EachTradeableDayInTimeZone(exchangeHours,
                _start,
                _end,
                config.DataTimeZone,
                config.ExtendedMarketHours).ToList();
            return new DateChangeTimeKeeper(tradableDates, config, exchangeHours, delistingDate ?? symbol.GetDelistingDate());
        }

        private static SecurityExchangeHours GetMarketHours(Symbol symbol, DateTimeZone exchangeTimeZone, bool alwaysOpen)
        {
            SecurityExchangeHours exchangeHours;
            if (alwaysOpen)
            {
                exchangeHours = SecurityExchangeHours.AlwaysOpen(exchangeTimeZone);
            }
            else
            {
                var databaseExchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
                exchangeHours = new SecurityExchangeHours(exchangeTimeZone,
                    databaseExchangeHours.Holidays,
                    databaseExchangeHours.MarketHours.ToDictionary(),
                    databaseExchangeHours.EarlyCloses,
                    databaseExchangeHours.LateOpens);
            }

            return exchangeHours;
        }
    }
}
