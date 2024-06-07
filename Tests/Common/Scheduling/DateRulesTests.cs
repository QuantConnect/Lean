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
using System.Globalization;
using System.Linq;
using NodaTime;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Scheduling
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class DateRulesTests
    {
        private static DateTime _utcNow = new DateTime(2021, 07, 27, 1, 10, 10, 500);

        [Test]
        public void EveryDayDateRuleEmitsEveryDay()
        {
            var rules = GetDateRules();
            var rule = rules.EveryDay();
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            DateTime previous = DateTime.MinValue;
            foreach (var date in dates)
            {
                count++;
                if (previous != DateTime.MinValue)
                {
                    Assert.AreEqual(Time.OneDay, date - previous);
                }
                previous = date;
            }
            // leap year
            Assert.AreEqual(366, count);
        }

        [Test]
        public void EverySymbolDayRuleEmitsOnTradeableDates()
        {
            var rules = GetDateRules();
            var rule = rules.EveryDay(Symbols.SPY);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreNotEqual(DayOfWeek.Saturday, date.DayOfWeek);
                Assert.AreNotEqual(DayOfWeek.Sunday, date.DayOfWeek);
            }

            Assert.AreEqual(252, count);
        }

        [Test]
        public void StartOfMonthNoSymbol()
        {
            var rules = GetDateRules();
            var rule = rules.MonthStart();
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreEqual(1, date.Day);
            }

            Assert.AreEqual(12, count);
        }

        [Test]
        public void StartOfMonthNoSymbolMidMonthStart()
        {
            var rules = GetDateRules();
            var rule = rules.MonthStart();
            var dates = rule.GetDates(new DateTime(2000, 01, 04), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreEqual(1, date.Day);
            }

            Assert.AreEqual(11, count);
        }

        [Test]
        public void StartOfMonthNoSymbolWithOffset()
        {
            var rules = GetDateRules();
            var rule = rules.MonthStart(5);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreEqual(new DateTime(2000, count, 6), date);
                Assert.AreEqual(6, date.Day);
            }
            Assert.AreEqual(12, count);
        }

        [TestCase(2, false)]       // Before 11th
        [TestCase(4, false)]
        [TestCase(8, false)]
        [TestCase(12, true)]      // After 11th
        [TestCase(16, true)]
        [TestCase(20, true)]
        public void StartOfMonthSameMonthSchedule(int startingDateDay, bool expectNone)
        {
            // Reproduces issue #5678, Assert that even though start is not first of month,
            // we still schedule for that month.
            var startingDate = new DateTime(2000, 12, startingDateDay);
            var endingDate = new DateTime(2000, 12, 31);

            var rules = GetDateRules();
            var rule = rules.MonthStart(10); // 12/11/2000
            var dates = rule.GetDates(startingDate, endingDate);

            Assert.AreEqual(expectNone, dates.IsNullOrEmpty());

            if (!expectNone)
            {
                Assert.AreEqual(new DateTime(2000, 12, 11), dates.First());
            }
        }

        [Test]
        public void StartOfMonthWithSymbol()
        {
            var rules = GetDateRules();
            var rule = rules.MonthStart(Symbols.SPY);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreNotEqual(DayOfWeek.Saturday, date.DayOfWeek);
                Assert.AreNotEqual(DayOfWeek.Sunday, date.DayOfWeek);
                Assert.IsTrue(date.Day <= 3);
                Log.Trace(date.Day.ToString(CultureInfo.InvariantCulture));
            }

            Assert.AreEqual(12, count);
        }

        [Test]
        public void StartOfMonthWithSymbolMidMonthStart()
        {
            var rules = GetDateRules();
            var rule = rules.MonthStart(Symbols.SPY);
            var dates = rule.GetDates(new DateTime(2000, 01, 04), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreNotEqual(DayOfWeek.Saturday, date.DayOfWeek);
                Assert.AreNotEqual(DayOfWeek.Sunday, date.DayOfWeek);
                Assert.IsTrue(date.Day <= 3);
                Log.Trace(date.Day.ToString(CultureInfo.InvariantCulture));
            }

            Assert.AreEqual(11, count);
        }

        [TestCase(Symbols.SymbolsKey.SPY, new[] { 10, 8, 8, 10, 8, 8 }, 5)]
        [TestCase(Symbols.SymbolsKey.SPY, new[] { 20, 17, 17, 19, 17, 19 }, 12)] // Contains holiday 1/17
        [TestCase(Symbols.SymbolsKey.SPY, new[] { 31, 29, 31, 28, 31, 30 }, 25)] // Always last trading day of the month (25 > than trading days)
        [TestCase(Symbols.SymbolsKey.BTCUSD, new[] { 6, 6, 6, 6, 6, 6 }, 5)]
        [TestCase(Symbols.SymbolsKey.EURUSD, new[] { 7, 7, 7, 7, 7, 7 }, 5)]
        public void StartOfMonthWithSymbolWithOffset(Symbols.SymbolsKey symbolKey, int[] expectedDays, int offset)
        {
            var rules = GetDateRules();
            var rule = rules.MonthStart(Symbols.Lookup(symbolKey), offset);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 6, 30)).ToList();

            // Assert we have as many dates as expected
            Assert.AreEqual(expectedDays.Length, dates.Count);

            // Verify the days match up
            var datesAndExpectedDays = dates.Zip(expectedDays, (date, expectedDay) => new {date, expectedDay});
            foreach (var pair in datesAndExpectedDays)
            {
                Assert.AreEqual(pair.expectedDay, pair.date.Day);
            }
        }

        [Test]
        public void EndOfMonthNoSymbol()
        {
            var rules = GetDateRules();
            var rule = rules.MonthEnd();
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreEqual(DateTime.DaysInMonth(date.Year, date.Month), date.Day);
            }

            Assert.AreEqual(12, count);
        }

        [Test]
        public void EndOfMonthNoSymbolWithOffset()
        {
            var rules = GetDateRules();
            var rule = rules.MonthEnd(5);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreEqual(DateTime.DaysInMonth(date.Year, date.Month) - 5, date.Day);
            }
            Assert.AreEqual(12, count);
        }

        [TestCase(5, true)]       // Before 21th
        [TestCase(10, true)]
        [TestCase(15, true)]
        [TestCase(21, false)]      // After 21th
        [TestCase(25, false)]
        [TestCase(30, false)]
        public void EndOfMonthSameMonthSchedule(int endingDateDay, bool expectNone)
        {
            // Related to issue #5678, Assert that even though end date is not end of month,
            // we still schedule for that month.
            var startingDate = new DateTime(2000, 12, 1);
            var endingDate = new DateTime(2000, 12, endingDateDay);

            var rules = GetDateRules();
            var rule = rules.MonthEnd(10); // 12/21/2000
            var dates = rule.GetDates(startingDate, endingDate);

            Assert.AreEqual(expectNone, dates.IsNullOrEmpty());

            if (!expectNone)
            {
                Assert.AreEqual(new DateTime(2000, 12, 21), dates.First());
            }
        }

        [Test]
        public void EndOfMonthWithSymbol()
        {
            var rules = GetDateRules();
            var rule = rules.MonthEnd(Symbols.SPY);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreNotEqual(DayOfWeek.Saturday, date.DayOfWeek);
                Assert.AreNotEqual(DayOfWeek.Sunday, date.DayOfWeek);
                Assert.IsTrue(date.Day >= 28);
                Log.Trace(date + " " + date.DayOfWeek);
            }

            Assert.AreEqual(12, count);
        }

        [TestCase(Symbols.SymbolsKey.SPY, new[] { 24, 22, 24, 20, 23, 23 }, 5)] // This case contains two Holidays 4/21 & 5/29
        [TestCase(Symbols.SymbolsKey.SPY, new[] { 12, 10, 15, 11, 12, 14 }, 12)] // Contains holiday 1/17
        [TestCase(Symbols.SymbolsKey.SPY, new[] { 3, 1, 1, 3, 1, 1 }, 25)] // Always first trading day of the month (25 > than trading days)
        [TestCase(Symbols.SymbolsKey.BTCUSD, new[] { 26, 24, 26, 25, 26, 25 }, 5)]
        [TestCase(Symbols.SymbolsKey.EURUSD, new[] { 25, 23, 26, 24, 25, 25 }, 5)]
        public void EndOfMonthWithSymbolWithOffset(Symbols.SymbolsKey symbolKey, int[] expectedDays, int offset)
        {
            var rules = GetDateRules();
            var rule = rules.MonthEnd(Symbols.Lookup(symbolKey), offset);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 6, 30)).ToList();

            // Assert we have as many dates as expected
            Assert.AreEqual(expectedDays.Length, dates.Count);

            // Verify the days match up
            var datesAndExpectedDays = dates.Zip(expectedDays, (date, expectedDay) => new { date, expectedDay });
            foreach (var pair in datesAndExpectedDays)
            {
                Assert.AreEqual(pair.expectedDay, pair.date.Day);
            }
        }

        [Test]
        public void StartOfYearNoSymbol()
        {
            var rules = GetDateRules();
            var rule = rules.YearStart();
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2010, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreEqual(1, date.Day);
                Assert.AreEqual(1, date.Month);
            }

            Assert.AreEqual(11, count);
        }

        [Test]
        public void StartOfYearNoSymbolMidYearStart()
        {
            var rules = GetDateRules();
            var rule = rules.YearStart();
            var dates = rule.GetDates(new DateTime(2000, 06, 01), new DateTime(2010, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreNotEqual(2000, date.Year);
                Assert.AreEqual(1, date.Month);
                Assert.AreEqual(1, date.Day);
            }

            Assert.AreEqual(10, count);
        }

        [Test]
        public void StartOfYearNoSymbolWithOffset()
        {
            var rules = GetDateRules();
            var rule = rules.YearStart(5);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2010, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreEqual(1, date.Month);
                Assert.AreEqual(6, date.Day);
            }
            Assert.AreEqual(11, count);
        }

        [TestCase(2)]       // Before 11th
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(12)]      // After 11th
        [TestCase(16)]
        [TestCase(20)]
        public void StartOfYearSameYearSchedule(int startingDateDay)
        {
            var startingDate = new DateTime(2000, 1, startingDateDay);
            var endingDate = new DateTime(2000, 12, 31);

            var rules = GetDateRules();
            var rule = rules.YearStart(10); // 11/1/2000
            var dates = rule.GetDates(startingDate, endingDate);

            Assert.AreEqual(startingDateDay > 11, dates.IsNullOrEmpty());

            if (startingDateDay <= 11)
            {
                Assert.AreEqual(new DateTime(2000, 1, 11), dates.Single());
            }
        }

        [Test]
        public void StartOfYearWithSymbol()
        {
            var rules = GetDateRules();
            var rule = rules.YearStart(Symbols.SPY);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2010, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreNotEqual(DayOfWeek.Saturday, date.DayOfWeek);
                Assert.AreNotEqual(DayOfWeek.Sunday, date.DayOfWeek);
                Assert.IsTrue(date.Day <= 4);
                Log.Debug(date.Day.ToString(CultureInfo.InvariantCulture));
            }

            Assert.AreEqual(11, count);
        }

        [Test]
        public void StartOfYearWithSymbolMidYearStart()
        {
            var rules = GetDateRules();
            var rule = rules.YearStart(Symbols.SPY);
            var dates = rule.GetDates(new DateTime(2000, 06, 01), new DateTime(2010, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreNotEqual(2000, date.Year);
                Assert.AreNotEqual(DayOfWeek.Saturday, date.DayOfWeek);
                Assert.AreNotEqual(DayOfWeek.Sunday, date.DayOfWeek);
                Assert.AreEqual(1, date.Month);
                Assert.IsTrue(date.Day <= 4);
                Log.Debug(date.Day.ToString(CultureInfo.InvariantCulture));
            }

            Assert.AreEqual(10, count);
        }

        [TestCase(Symbols.SymbolsKey.SPY, new[] { 10, 9, 9, 9, 9}, 5)]
        [TestCase(Symbols.SymbolsKey.SPY, new[] { 20, 19, 18, 21, 21}, 12)] // Contains holiday 1/17
        [TestCase(Symbols.SymbolsKey.SPY, new[] { 29, 31, 31, 31, 31}, 348)] // Always last trading day of the year
        [TestCase(Symbols.SymbolsKey.BTCUSD, new[] { 6, 6, 6, 6, 6}, 5)]
        [TestCase(Symbols.SymbolsKey.EURUSD, new[] { 7, 7, 7, 7, 7}, 5)]
        public void StartOfYearWithSymbolWithOffset(Symbols.SymbolsKey symbolKey, int[] expectedDays, int offset)
        {
            var rules = GetDateRules();
            var rule = rules.YearStart(Symbols.Lookup(symbolKey), offset);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2004, 12, 31)).ToList();

            // Assert we have as many dates as expected
            Assert.AreEqual(expectedDays.Length, dates.Count);

            // Verify the days match up
            var datesAndExpectedDays = dates.Zip(expectedDays, (date, expectedDay) => new { date, expectedDay });
            foreach (var pair in datesAndExpectedDays)
            {
                Assert.AreEqual(pair.expectedDay, pair.date.Day);
            }
        }

        [Test]
        public void EndOfYearNoSymbol()
        {
            var rules = GetDateRules();
            var rule = rules.YearEnd();
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2010, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreEqual(12, date.Month);
                Assert.AreEqual(DateTime.DaysInMonth(date.Year, date.Month), date.Day);
            }

            Assert.AreEqual(11, count);
        }

        [Test]
        public void EndOfYearNoSymbolWithOffset()
        {
            var rules = GetDateRules();
            var rule = rules.YearEnd(5);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2010, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreEqual(12, date.Month);
                Assert.AreEqual(DateTime.DaysInMonth(date.Year, date.Month) - 5, date.Day);
            }
            Assert.AreEqual(11, count);
        }

        [TestCase(5)]       // Before 21th
        [TestCase(10)]
        [TestCase(15)]
        [TestCase(21)]      // After 21th
        [TestCase(25)]
        [TestCase(30)]
        public void EndOfYearSameMonthSchedule(int endingDateDay)
        {
            var startingDate = new DateTime(2000, 1, 1);
            var endingDate = new DateTime(2000, 12, endingDateDay);

            var rules = GetDateRules();
            var rule = rules.YearEnd(10); // 12/21/2000
            var dates = rule.GetDates(startingDate, endingDate);

            Assert.AreEqual(endingDateDay < 21, dates.IsNullOrEmpty());

            if (endingDateDay >= 21)
            {
                Assert.AreEqual(new DateTime(2000, 12, 21), dates.Single());
            }
        }

        [Test]
        public void EndOfYearWithSymbol()
        {
            var rules = GetDateRules();
            var rule = rules.YearEnd(Symbols.SPY);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2010, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreNotEqual(DayOfWeek.Saturday, date.DayOfWeek);
                Assert.AreNotEqual(DayOfWeek.Sunday, date.DayOfWeek);
                Assert.AreEqual(12, date.Month);
                Assert.IsTrue(date.Day >= 28);
                Log.Debug(date + " " + date.DayOfWeek);
            }

            Assert.AreEqual(11, count);
        }

        [TestCase(Symbols.SymbolsKey.SPY, new[] { 21, 21, 23, 23, 23 }, 5)] // This case contains two Holidays 12/25 & 12/29
        [TestCase(Symbols.SymbolsKey.SPY, new[] { 12, 12, 12, 12, 14 }, 12)] // Contains holiday 1/25
        [TestCase(Symbols.SymbolsKey.SPY, new[] { 1, 3, 3, 3, 3 }, 19)] // Always first trading day of the month (25 > than trading days)
        [TestCase(Symbols.SymbolsKey.BTCUSD, new[] { 26, 26, 26, 26, 26 }, 5)]
        [TestCase(Symbols.SymbolsKey.EURUSD, new[] { 25, 25, 25, 25, 26 }, 5)]
        public void EndOfYearWithSymbolWithOffset(Symbols.SymbolsKey symbolKey, int[] expectedDays, int offset)
        {
            var rules = GetDateRules();
            var rule = rules.YearEnd(Symbols.Lookup(symbolKey), offset);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2004, 12, 31)).ToList();

            // Assert we have as many dates as expected
            Assert.AreEqual(expectedDays.Length, dates.Count);

            // Verify the days match up
            var datesAndExpectedDays = dates.Zip(expectedDays, (date, expectedDay) => new { date, expectedDay });
            foreach (var pair in datesAndExpectedDays)
            {
                Assert.AreEqual(pair.expectedDay, pair.date.Day);
            }
        }

        [Test]
        public void StartOfWeekNoSymbol()
        {
            var rules = GetDateRules();
            var rule = rules.WeekStart();
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreEqual(DayOfWeek.Monday, date.DayOfWeek);
            }

            Assert.AreEqual(52, count);
        }

        [TestCase(0, DayOfWeek.Monday)]
        [TestCase(1, DayOfWeek.Tuesday)]
        [TestCase (2, DayOfWeek.Wednesday)]
        [TestCase(3, DayOfWeek.Thursday)]
        [TestCase(4, DayOfWeek.Friday)]
        [TestCase(5, DayOfWeek.Saturday)]
        [TestCase(6, DayOfWeek.Sunday)]
        public void StartOfWeekNoSymbolWithOffset(int offset, DayOfWeek expectedDayOfWeek)
        {
            var rules = GetDateRules();
            var rule = rules.WeekStart(offset);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreEqual(expectedDayOfWeek, date.DayOfWeek);
            }

            // There are 53 saturday and sundays in 2000, otherwise we expect only 52
            int expected = expectedDayOfWeek == DayOfWeek.Saturday || expectedDayOfWeek == DayOfWeek.Sunday ? 53 : 52;
            Assert.AreEqual(expected, count);
        }

        [TestCase(Symbols.SymbolsKey.SPY, new[] { 3, 10, 18, 24, 31 })]
        [TestCase(Symbols.SymbolsKey.BTCUSD, new[] { 2, 9, 16, 23, 30 })]
        [TestCase(Symbols.SymbolsKey.EURUSD, new[] { 2, 9, 16, 23, 30 })]
        [TestCase(Symbols.SymbolsKey.Fut_SPY_Feb19_2016, new int[] { 2, 9, 16, 23, 30 })]
        public void StartOfWeekWithSymbol(Symbols.SymbolsKey symbolKey, int[] expectedDays)
        {
            var rules = GetDateRules();
            var rule = rules.WeekStart(Symbols.Lookup(symbolKey));
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 1, 31)).ToList();

            // Assert we have as many dates as expected
            Assert.AreEqual(expectedDays.Length, dates.Count);

            // Verify the days match up
            var datesAndExpectedDays = dates.Zip(expectedDays, (date, expectedDay) => new { date, expectedDay });
            foreach (var pair in datesAndExpectedDays)
            {
                Assert.AreEqual(pair.expectedDay, pair.date.Day);
            }
        }

        [TestCase(Symbols.SymbolsKey.SPY, new[] { 5, 12, 20, 26 })] // Set contains holiday on 1/17
        [TestCase(Symbols.SymbolsKey.BTCUSD, new[] { 4, 11, 18, 25 })]
        [TestCase(Symbols.SymbolsKey.EURUSD, new[] { 4, 11, 18, 25 })]
        public void StartOfWeekWithSymbolWithOffset(Symbols.SymbolsKey symbolKey, int[] expectedDays)
        {
            var rules = GetDateRules();
            var rule = rules.WeekStart(Symbols.Lookup(symbolKey), 2);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 1, 31)).ToList();

            // Assert we have as many dates as expected
            Assert.AreEqual(expectedDays.Length, dates.Count);

            // Verify the days match up
            var datesAndExpectedDays = dates.Zip(expectedDays, (date, expectedDay) => new { date, expectedDay });
            foreach (var pair in datesAndExpectedDays)
            {
                Assert.AreEqual(pair.expectedDay, pair.date.Day);
            }
        }

        [TestCase(3, false)]    // Start before the 6th
        [TestCase(4, false)]    
        [TestCase(5, false)]
        [TestCase(6, false)]
        [TestCase(7, true)]     // Start after the 6th
        [TestCase(8, true)]
        [TestCase(9, true)]
        public void StartOfWeekSameWeekSchedule(int startingDateDay, bool expectNone)
        {
            // Related to issue #5678, Assert that even though starting date may not be
            // not monday we still schedule for that week.

            // For this test and the EndOfWeek counterpart we will use the week of
            // 1/3/2000 Monday to 1/9/2000 Sunday; with our variable date applied.
            var startingDate = new DateTime(2000, 1, startingDateDay);
            var endingDate = new DateTime(2000, 1, 9);

            var rules = GetDateRules();
            var rule = rules.WeekStart(3); // 1/6/2000
            var dates = rule.GetDates(startingDate, endingDate);

            Assert.AreEqual(expectNone, dates.IsNullOrEmpty());

            if (!expectNone)
            {
                Assert.AreEqual(new DateTime(2000, 1, 6), dates.First());
            }
        }

        [TestCase(5)] // Monday + 5 = Saturday
        [TestCase(6)] // Monday + 6 = Sunday
        public void StartOfWeekWithSymbolOffsetToNonTradableDay(int offset)
        {
            var rules = GetDateRules();

            // We expect it to throw because Spy does not trade on the weekends
            Assert.Throws<ArgumentOutOfRangeException>(() => rules.WeekStart(Symbols.SPY, offset));
        }

        [Test]
        public void EndOfWeekNoSymbol()
        {
            var rules = GetDateRules();
            var rule = rules.WeekEnd();
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreEqual(DayOfWeek.Friday, date.DayOfWeek);
            }

            Assert.AreEqual(52, count);
        }

        [TestCase(0, DayOfWeek.Friday)]
        [TestCase(1, DayOfWeek.Thursday)]
        [TestCase(2, DayOfWeek.Wednesday)]
        [TestCase(3, DayOfWeek.Tuesday)]
        [TestCase(4, DayOfWeek.Monday)]
        [TestCase(5, DayOfWeek.Sunday)]
        [TestCase(6, DayOfWeek.Saturday)]
        public void EndOfWeekNoSymbolWithOffset(int offset, DayOfWeek expectedDayOfWeek)
        {
            var rules = GetDateRules();
            var rule = rules.WeekEnd(offset);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreEqual(expectedDayOfWeek, date.DayOfWeek);
            }

            Assert.AreEqual(52, count);
        }

        [TestCase(Symbols.SymbolsKey.SPY, new[] { 7, 14, 21, 28 })]
        [TestCase(Symbols.SymbolsKey.BTCUSD, new[] { 1, 8, 15, 22, 29 })]
        [TestCase(Symbols.SymbolsKey.EURUSD, new[] { 7, 14, 21, 28 })]
        [TestCase(Symbols.SymbolsKey.Fut_SPY_Feb19_2016, new int[] { 7, 14, 21, 28 })]
        public void EndOfWeekWithSymbol(Symbols.SymbolsKey symbolKey, int[] expectedDays)
        {
            var rules = GetDateRules();
            var rule = rules.WeekEnd(Symbols.Lookup(symbolKey));
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 1, 31)).ToList();


            // Assert we have as many dates as expected
            Assert.AreEqual(expectedDays.Length, dates.Count);

            // Verify the days match up
            var datesAndExpectedDays = dates.Zip(expectedDays, (date, expectedDay) => new { date, expectedDay });
            foreach (var pair in datesAndExpectedDays)
            {
                Assert.AreEqual(pair.expectedDay, pair.date.Day);
            }
        }

        [TestCase(Symbols.SymbolsKey.SPY, new[] { 5, 12, 19, 26 })]
        [TestCase(Symbols.SymbolsKey.BTCUSD, new[] { 6, 13, 20, 27 })]
        [TestCase(Symbols.SymbolsKey.EURUSD, new[] { 5, 12, 19, 26 })]
        public void EndOfWeekWithSymbolWithOffset(Symbols.SymbolsKey symbolKey, int[] expectedDays)
        {
            var rules = GetDateRules();
            var rule = rules.WeekEnd(Symbols.Lookup(symbolKey), 2);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 1, 31)).ToList();

            // Assert we have as many dates as expected
            Assert.AreEqual(expectedDays.Length, dates.Count);

            // Verify the days match up
            var datesAndExpectedDays = dates.Zip(expectedDays, (date, expectedDay) => new { date, expectedDay });
            foreach (var pair in datesAndExpectedDays)
            {
                Assert.AreEqual(pair.expectedDay, pair.date.Day);
            }
        }

        [TestCase(3, true)]     // End before the 6th
        [TestCase(4, true)]
        [TestCase(5, true)]
        [TestCase(6, false)]    // End after the 6th
        [TestCase(7, false)]     
        [TestCase(8, false)]
        [TestCase(9, false)]
        public void EndOfWeekSameWeekSchedule(int endDateDay, bool expectNone)
        {
            // Related to issue #5678, Assert that even though starting date may not be
            // not monday we still schedule for that week.

            // For this test and the EndOfWeek counterpart we will use the week of
            // 1/3/2000 Monday to 1/9/2000 Sunday; with our variable date applied.
            var startingDate = new DateTime(2000, 1, 3);
            var endingDate = new DateTime(2000, 1, endDateDay);

            var rules = GetDateRules();
            var rule = rules.WeekEnd(1); // 1/6/2000 (For weekEnd, Friday is the base day)
            var dates = rule.GetDates(startingDate, endingDate);

            Assert.AreEqual(expectNone, dates.IsNullOrEmpty());

            if (!expectNone)
            {
                Assert.AreEqual(new DateTime(2000, 1, 6), dates.First());
            }
        }

        [TestCase(5)] // Friday - 5 = Sunday
        [TestCase(6)] // Friday - 6 = Saturday
        public void EndOfWeekWithSymbolOffsetToNonTradableDay(int offset)
        {
            var rules = GetDateRules();

            // We expect it to throw because Spy does not trade on the weekends
            Assert.Throws<ArgumentOutOfRangeException>(() => rules.WeekEnd(Symbols.SPY, offset));
        }

        [Test]
        public void DateRulesExpectedNames()
        {
            var rules = GetDateRules();
            IDateRule rule;

            // WeekEnd Rules
            rule = rules.WeekEnd();
            Assert.AreEqual("WeekEnd", rule.Name);

            rule = rules.WeekEnd(1);
            Assert.AreEqual("WeekEnd-1", rule.Name);

            rule = rules.WeekEnd(Symbols.SPY);
            Assert.AreEqual("SPY: WeekEnd", rule.Name);

            rule = rules.WeekEnd(Symbols.SPY, 2);
            Assert.AreEqual("SPY: WeekEnd-2", rule.Name);

            // WeekStart rules
            rule = rules.WeekStart();
            Assert.AreEqual("WeekStart", rule.Name);

            rule = rules.WeekStart(1);
            Assert.AreEqual("WeekStart+1", rule.Name);

            rule = rules.WeekStart(Symbols.SPY);
            Assert.AreEqual("SPY: WeekStart", rule.Name);

            rule = rules.WeekStart(Symbols.SPY, 3);
            Assert.AreEqual("SPY: WeekStart+3", rule.Name);
        }

        [Test]
        public void SetTimeZone()
        {
            var rules = GetDateRules();
            var nowNewYork = rules.Today.GetDates(_utcNow, _utcNow).Single();

            rules.SetDefaultTimeZone(TimeZones.Utc);

            var nowUtc = rules.Today.GetDates(_utcNow, _utcNow).Single();

            Assert.AreEqual(_utcNow.Date, nowUtc);
            Assert.AreEqual(nowUtc.Date, nowNewYork.AddDays(1));
        }

        [Test]
        public void SetFuncDateRuleInPythonWorksAsExpected()
        {
            using (Py.GIL())
            {
                var pythonModule = PyModule.FromString("testModule", @"
from AlgorithmImports import *

def CustomDateRule(start, end):
    return [start + (end - start)/2]
");
                dynamic pythonCustomDateRule = pythonModule.GetAttr("CustomDateRule");
                var funcDateRule = new FuncDateRule("PythonFuncDateRule", pythonCustomDateRule);
                Assert.AreEqual("PythonFuncDateRule", funcDateRule.Name);
                Assert.AreEqual(new DateTime(2023, 1, 16, 12, 0, 0), funcDateRule.GetDates(new DateTime(2023, 1, 1), new DateTime(2023, 2, 1)).First());
            }
        }

        [Test]
        public void SetFuncDateRuleInPythonWorksAsExpectedWithCSharpFunc()
        {
            using (Py.GIL())
            {
                var pythonModule = PyModule.FromString("testModule", @"
from AlgorithmImports import *

def GetFuncDateRule(csharpFunc):
    return FuncDateRule(""CSharp"", csharpFunc)
");
                dynamic getFuncDateRule = pythonModule.GetAttr("GetFuncDateRule");
                Func<DateTime, DateTime, IEnumerable<DateTime>> csharpFunc = (start, end) => { return new List<DateTime>() { new DateTime(2001, 3, 18) }; };
                var funcDateRule = getFuncDateRule(csharpFunc);
                Assert.AreEqual("CSharp", (funcDateRule.Name as PyObject).GetAndDispose<string>());
                Assert.AreEqual(new DateTime(2001, 3, 18),
                    (funcDateRule.GetDates(new DateTime(2023, 1, 1), new DateTime(2023, 2, 1)) as PyObject).GetAndDispose<List<DateTime>>().First());
            }
        }

        [Test]
        public void SetFuncDateRuleInPythonFailsWhenDateRuleIsInvalid()
        {
            using (Py.GIL())
            {
                var pythonModule = PyModule.FromString("testModule", @"
from AlgorithmImports import *

wrongCustomDateRule = 1
");
                dynamic pythonCustomDateRule = pythonModule.GetAttr("wrongCustomDateRule");
                Assert.Throws<ArgumentException>(() => new FuncDateRule("PythonFuncDateRule", pythonCustomDateRule));
            }
        }

        private static DateRules GetDateRules()
        {
            var mhdb = MarketHoursDatabase.FromDataFolder();
            var timeKeeper = new TimeKeeper(_utcNow, new List<DateTimeZone>());
            var manager = new SecurityManager(timeKeeper);

            // Add SPY for Equity testing
            var securityExchangeHours = mhdb.GetExchangeHours(Market.USA, null, SecurityType.Equity);
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, true, false, false);
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

            // Add BTC for Crypto testing
            securityExchangeHours = mhdb.GetExchangeHours(Market.Bitfinex, Symbols.BTCUSD, SecurityType.Crypto);
            config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.BTCUSD, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, true, false, false);
            manager.Add(
                Symbols.BTCUSD,
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

            // Add EURUSD for Forex testing
            securityExchangeHours = mhdb.GetExchangeHours(Market.FXCM, Symbols.EURUSD, SecurityType.Forex);
            config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.EURUSD, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, true, false, false);
            manager.Add(
                Symbols.EURUSD,
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

            // Add Fut_SPY_Feb19_2016 for testing
            securityExchangeHours = mhdb.GetExchangeHours(Market.CME, Symbols.Fut_SPY_Feb19_2016, SecurityType.Future);
            config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.Fut_SPY_Feb19_2016, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, true, false, false);
            manager.Add(
                Symbols.Fut_SPY_Feb19_2016,
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

            var rules = new DateRules(manager, TimeZones.NewYork, mhdb);
            return rules;
        }
    }
}
