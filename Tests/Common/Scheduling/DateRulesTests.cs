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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Scheduling;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Scheduling
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class DateRulesTests
    {
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

            Assert.AreEqual(52, count);
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

        private static DateRules GetDateRules()
        {
            var timeKeeper = new TimeKeeper(DateTime.Today, new List<DateTimeZone>());
            var manager = new SecurityManager(timeKeeper);

            // Add SPY for Equity testing
            var securityExchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, null, SecurityType.Equity);
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
            securityExchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Bitfinex, Symbols.BTCUSD, SecurityType.Crypto);
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
            securityExchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.FXCM, Symbols.EURUSD, SecurityType.Forex);
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
            securityExchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.CME, Symbols.Fut_SPY_Feb19_2016, SecurityType.Future);
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

            var rules = new DateRules(manager, TimeZones.NewYork);
            return rules;
        }
    }
}
