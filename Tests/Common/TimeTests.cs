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
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class TimeTests
    {
        [Test]
        public void UnixTimeStampSecondsToDateTimeHasSubMillisecondPrecision()
        {
            const double stamp = 1520711961.00055;
            var expected = new DateTime(2018, 3, 10, 19, 59, 21, 0).AddTicks(5500);
            var time = Time.UnixTimeStampToDateTime(stamp);
            Assert.AreEqual(expected, time);
        }

        [Test]
        public void UnixTimeStampMillisecondsToDateTimeHasSubMillisecondPrecision()
        {
            const double stamp = 1520711961000.55;
            var expected = new DateTime(2018, 3, 10, 19, 59, 21, 0).AddTicks(5500);
            var time = Time.UnixMillisecondTimeStampToDateTime(stamp);
            Assert.AreEqual(expected, time);
        }

        [Test]
        public void GetStartTimeForTradeBarsRoundsDown()
        {
            // 2015.09.01 @ noon
            var end = new DateTime(2015, 09, 01, 12, 0, 1);
            var barSize = TimeSpan.FromMinutes(1);
            var hours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);
            var start = Time.GetStartTimeForTradeBars(hours, end, barSize, 1, false, TimeZones.NewYork);
            // round down and back up a single bar
            Assert.AreEqual(end.RoundDown(barSize).Subtract(barSize), start);
        }

        [Test]
        public void GetStartTimeForTradeBarsHandlesOverNight()
        {
            // 2015.09.01 @ noon
            var end = new DateTime(2015, 09, 01, 12, 0, 0);
            var barSize = TimeSpan.FromHours(1);
            var hours = SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours();
            var start = Time.GetStartTimeForTradeBars(hours, end, barSize, 7, false, hours.TimeZone);
            // from noon, back up to 9am (3 hours) then skip night, so from 4pm, back up to noon, 4 more hours
            Assert.AreEqual(end.AddDays(-1), start);
        }

        [Test]
        public void GetStartTimeForTradeBarsHandlesWeekends()
        {
            // 2015.09.01 @ noon
            var end = new DateTime(2015, 09, 01, 12, 0, 0);
            var expectedStart = new DateTime(2015, 08, 21);
            var barSize = TimeSpan.FromDays(1);
            var hours = SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours();
            var start = Time.GetStartTimeForTradeBars(hours, end, barSize, 7, false, hours.TimeZone);
            // from noon, back up to 9am (3 hours) then skip night, so from 4pm, back up to noon, 4 more hours
            Assert.AreEqual(expectedStart, start);
        }

        [Test, TestCaseSource(nameof(ForexHistoryDates))]
        public void GetStartTimeForForexTradeBars(DateTime end, DateTime expectedStart, DateTimeZone dataTimeZone)
        {
            var barSize = TimeSpan.FromDays(1);
            var hours = SecurityExchangeHoursTests.CreateForexSecurityExchangeHours();
            var start = Time.GetStartTimeForTradeBars(hours, end, barSize, 1, false, dataTimeZone);
            Assert.AreEqual(expectedStart, start);
        }

        [Test, TestCaseSource(nameof(EquityHistoryDates))]
        public void GetStartTimeForEquityTradeBars(DateTime end, DateTime expectedStart, DateTimeZone dataTimeZone)
        {
            var barSize = TimeSpan.FromMinutes(1);
            var hours = SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours();
            var start = Time.GetStartTimeForTradeBars(hours, end, barSize, 10, false, dataTimeZone);
            Assert.AreEqual(expectedStart, start);
        }

        [Test]
        public void EachTradeableDayInTimeZoneIsSameForEqualTimeZones()
        {
            var start = new DateTime(2010, 01, 01);
            var end = new DateTime(2016, 02, 12);
            var entry = MarketHoursDatabase.FromDataFolder().ExchangeHoursListing.First().Value;
            var expected = Time.EachTradeableDay(entry.ExchangeHours, start, end);
            var actual = Time.EachTradeableDayInTimeZone(entry.ExchangeHours, start, end, entry.ExchangeHours.TimeZone, true);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void EachTradeableDayInTimeZoneWithOffsetPlus12()
        {
            var start = new DateTime(2016, 2, 11);
            var end = new DateTime(2016, 2, 12);
            var equityExchange = SecurityExchangeHours.AlwaysOpen(DateTimeZone.ForOffset(Offset.FromHours(-5)));
            var dataTimeZone = DateTimeZone.ForOffset(Offset.FromHours(7));

            // given this arrangement we should still start on the same date and end a day late
            var expected = new[] { start, end, end.AddDays(1) };
            var actual = Time.EachTradeableDayInTimeZone(equityExchange, start, end, dataTimeZone, true);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void EachTradeableDayInTimeZoneWithOffsetMinus12()
        {
            var start = new DateTime(2016, 2, 11);
            var end = new DateTime(2016, 2, 12);
            var exchange = SecurityExchangeHours.AlwaysOpen(DateTimeZone.ForOffset(Offset.FromHours(5)));
            var dataTimeZone = DateTimeZone.ForOffset(Offset.FromHours(-7));

            // given this arrangement we should still start a day early but still end on the same date
            var expected = new[] { start.AddDays(-1), start, end };
            var actual = Time.EachTradeableDayInTimeZone(exchange, start, end, dataTimeZone, true);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void EachTradeableDayInTimeZoneWithOffset25()
        {
            var start = new DateTime(2016, 2, 11);
            var end = new DateTime(2016, 2, 12);
            var exchange = SecurityExchangeHours.AlwaysOpen(DateTimeZone.ForOffset(Offset.FromHours(12)));
            var dataTimeZone = DateTimeZone.ForOffset(Offset.FromHours(-13));

            // given this arrangement we should still start a day early but still end on the same date
            var expected = new[] { start.AddDays(-2), start.AddDays(-1), start };
            var actual = Time.EachTradeableDayInTimeZone(exchange, start, end, dataTimeZone, true);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void MultipliesTimeSpans()
        {
            var interval = TimeSpan.FromSeconds(1);
            var expected = TimeSpan.FromSeconds(5);
            var actual = interval.Multiply(5d);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCase(2, 7, 3)]
        [TestCase(2, 4, 2)]
        [TestCase(6, 7, 1)]
        public void GetNumberOfTradeBarsForIntervalUsingDailyStepSize(int startDay, int endDay, int expected)
        {
            var start = new DateTime(2018, 08, startDay);
            var end = new DateTime(2018, 08, endDay);
            var exchangeHours = CreateUsEquitySecurityExchangeHours();
            var actual = Time.GetNumberOfTradeBarsInInterval(exchangeHours, start, end, Time.OneDay);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCase(2, 7, 21)]
        [TestCase(2, 4, 14)]
        [TestCase(6, 7, 07)]
        public void GetNumberOfTradeBarsForIntervalUsingHourlyStepSize(int startDay, int endDay, int expected)
        {
            var start = new DateTime(2018, 08, startDay);
            var end = new DateTime(2018, 08, endDay);
            var exchangeHours = CreateUsEquitySecurityExchangeHours();
            var actual = Time.GetNumberOfTradeBarsInInterval(exchangeHours, start, end, Time.OneHour);
            Assert.AreEqual(expected, actual);
        }


        private static readonly TimeSpan USEquityPreOpen = new TimeSpan(4, 0, 0);
        private static readonly TimeSpan USEquityOpen = new TimeSpan(9, 30, 0);
        private static readonly TimeSpan USEquityClose = new TimeSpan(16, 0, 0);
        private static readonly TimeSpan USEquityPostClose = new TimeSpan(20, 0, 0);
        private static SecurityExchangeHours CreateUsEquitySecurityExchangeHours()
        {
            var sunday = LocalMarketHours.ClosedAllDay(DayOfWeek.Sunday);
            var monday = new LocalMarketHours(DayOfWeek.Monday, USEquityPreOpen, USEquityOpen, USEquityClose, USEquityPostClose);
            var tuesday = new LocalMarketHours(DayOfWeek.Tuesday, USEquityPreOpen, USEquityOpen, USEquityClose, USEquityPostClose);
            var wednesday = new LocalMarketHours(DayOfWeek.Wednesday, USEquityPreOpen, USEquityOpen, USEquityClose, USEquityPostClose);
            var thursday = new LocalMarketHours(DayOfWeek.Thursday, USEquityPreOpen, USEquityOpen, USEquityClose, USEquityPostClose);
            var friday = new LocalMarketHours(DayOfWeek.Friday, USEquityPreOpen, USEquityOpen, USEquityClose, USEquityPostClose);
            var saturday = LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday);

            var earlyCloses = new Dictionary<DateTime, TimeSpan>();
            var lateOpens = new Dictionary<DateTime, TimeSpan>();
            return new SecurityExchangeHours(TimeZones.NewYork, USHoliday.Dates.Select(x => x.Date), new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday, saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
        }

        [Test]
        [TestCase("190120", 2019, 1, 20)]
        [TestCase("20190120", 2019, 1, 20)]
        [TestCase("20190120 00:00", 2019, 1, 20)]
        [TestCase("2019-01-20T00:00:00.000Z", 2019, 1, 20)]
        [TestCase("1/20/2019 00:00:00 AM", 2019, 1, 20)]
        [TestCase("1/20/19 00:00 AM", 2019, 1, 20)]
        [TestCase("1/20/19", 2019, 1, 20)]
        public void ParseDate(string parseDate, int year, int month, int day)
        {
            Assert.AreEqual(new DateTime(year, month, day), Time.ParseDate(parseDate));
        }

        [Test]
        [TestCase("20190120 02:30", 2019, 1, 20, 2, 30)]
        [TestCase("1/20/2019 2:30:00 AM", 2019, 1, 20, 2, 30)]
        [TestCase("1/20/2019 2:30:00 PM", 2019, 1, 20, 14, 30)]
        [TestCase("1/20/19 2:30 PM", 2019, 1, 20, 14, 30)]
        [TestCase("2019-01-20T02:30:00.000Z", 2019, 1, 20, 2, 30)]
        public void ParseDateAndTime(string parseDate, int year, int month, int day, int hour, int minute)
        {
            Assert.AreEqual(new DateTime(year, month, day, hour, minute, 0), Time.ParseDate(parseDate));
        }

        [Test]
        [TestCase("19981231-23:59:59", 1998, 12, 31, 23, 59, 59)]
        [TestCase("19990101-00:00:00", 1999, 01, 01, 00, 00, 00)]
        [TestCase("20210121-21:32:18", 2021, 01, 21, 21, 32, 18)]
        public void ParseFIXUtcTimestamp(string parseDate, int year, int month, int day, int hour, int minute, int second)
        {
            var expected = new DateTime(year, month, day, hour, minute, second);
            Assert.AreEqual(
                expected,
                Parse.DateTimeExact(parseDate, DateFormat.FIX));

            Assert.AreEqual(
                expected,
                Time.ParseFIXUtcTimestamp(parseDate));
        }

        [Test]
        [TestCase("19981231-23:59:59.000", 1998, 12, 31, 23, 59, 59, 0)]
        [TestCase("19990101-00:00:00.000", 1999, 01, 01, 00, 00, 00, 0)]
        [TestCase("20210121-21:32:18.610", 2021, 01, 21, 21, 32, 18, 610)]
        public void ParseFIXUtcTimestampWithMillisecond(string parseDate, int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            var expected = new DateTime(year, month, day, hour, minute, second, millisecond);
            Assert.AreEqual(
                expected, 
                Parse.DateTimeExact(parseDate, DateFormat.FIXWithMillisecond));

            Assert.AreEqual(
                expected,
                Time.ParseFIXUtcTimestamp(parseDate));
        }

        private static IEnumerable<TestCaseData> ForexHistoryDates => new List<TestCaseData>
        {
            new TestCaseData(new DateTime(2018, 04, 02, 1, 0, 0), new DateTime(2018, 04, 01, 01, 0, 0), DateTimeZone.ForOffset(Offset.FromHours(-5))),
            new TestCaseData(new DateTime(2018, 04, 02, 0, 0, 0), new DateTime(2018, 03, 29, 01, 0, 0), DateTimeZone.ForOffset(Offset.FromHours(-5))),
            new TestCaseData(new DateTime(2018, 04, 04, 0, 0, 0), new DateTime(2018, 04, 02, 01, 0, 0), DateTimeZone.ForOffset(Offset.FromHours(-5))),
            new TestCaseData(new DateTime(2018, 04, 02, 1, 0, 0), new DateTime(2018, 03, 29, 15, 0, 0), DateTimeZone.ForOffset(Offset.FromHours(5))),
            new TestCaseData(new DateTime(2018, 04, 02, 0, 0, 0), new DateTime(2018, 03, 29, 15, 0, 0), DateTimeZone.ForOffset(Offset.FromHours(5))),
            new TestCaseData(new DateTime(2018, 04, 04, 0, 0, 0), new DateTime(2018, 04, 02, 15, 0, 0), DateTimeZone.ForOffset(Offset.FromHours(5))),
            new TestCaseData(new DateTime(2018, 04, 02, 1, 0, 0), new DateTime(2018, 03, 31, 20, 0, 0), DateTimeZone.Utc),
            new TestCaseData(new DateTime(2018, 04, 02, 0, 0, 0), new DateTime(2018, 03, 31, 20, 0, 0), DateTimeZone.Utc),
            new TestCaseData(new DateTime(2018, 04, 04, 0, 0, 0), new DateTime(2018, 04, 02, 20, 0, 0), DateTimeZone.Utc)
        };

        private static IEnumerable<TestCaseData> EquityHistoryDates => new List<TestCaseData>
        {
            new TestCaseData(new DateTime(2013, 10, 08, 17, 0, 0), new DateTime(2013, 10, 08, 15, 50, 0), DateTimeZone.Utc),
            new TestCaseData(new DateTime(2013, 10, 08, 13, 0, 0), new DateTime(2013, 10, 08, 12, 50, 0), DateTimeZone.Utc),
        };
    }
}
