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
                Console.WriteLine(date.Day);
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
                Console.WriteLine(date.Day);
            }

            Assert.AreEqual(11, count);
        }

        [Test]
        public void StartOfMonthWithSymbolWithOffset()
        {
            var rules = GetDateRules();
            var rule = rules.MonthStart(Symbols.SPY, 5);
            var dates = rule.GetDates(new DateTime(2000, 01, 04), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreNotEqual(DayOfWeek.Saturday, date.DayOfWeek);
                Assert.AreNotEqual(DayOfWeek.Sunday, date.DayOfWeek);

                //1st + 5 = 6th; Possible weekend means between 6th and 8th
                Assert.IsTrue(date.Day <= 8);
                Assert.IsTrue(date.Day >= 6);
            }
            Assert.AreEqual(12, count);
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
                Console.WriteLine(date + " " + date.DayOfWeek);
            }

            Assert.AreEqual(12, count);
        }

        [Test]
        public void EndOfMonthWithSymbolWithOffset()
        {
            var rules = GetDateRules();
            var rule = rules.MonthEnd(Symbols.SPY, 5);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.AreNotEqual(DayOfWeek.Saturday, date.DayOfWeek);
                Assert.AreNotEqual(DayOfWeek.Sunday, date.DayOfWeek);

                // 28th - 5 = 23rd; 31 - 5 = 27th; Must be between
                Assert.IsTrue(date.Day >= 23);
                Assert.IsTrue(date.Day <= 27);
            }
            Assert.AreEqual(12, count);
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

        [Test]
        public void StartOfWeekNoSymbolWithOffset()
        {
            var rules = GetDateRules();
            var rule = rules.WeekStart(2);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                // Monday offset by 2 days, so Wednesday
                Assert.AreEqual(DayOfWeek.Wednesday, date.DayOfWeek);
            }

            Assert.AreEqual(52, count);
        }

        [Test]
        public void StartOfWeekWithSymbol()
        {
            var rules = GetDateRules();
            var rule = rules.WeekStart(Symbols.SPY);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.IsTrue(date.DayOfWeek == DayOfWeek.Monday || date.DayOfWeek == DayOfWeek.Tuesday);
                Console.WriteLine(date + " " + date.DayOfWeek);
            }

            Assert.AreEqual(52, count);
        }

        [Test]
        public void StartOfWeekWithSymbolWithOffset()
        {
            var rules = GetDateRules();
            var rule = rules.WeekStart(Symbols.SPY, 2);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                // Offset monday by two days so Wednesday
                Assert.IsTrue(date.DayOfWeek == DayOfWeek.Wednesday);
                Console.WriteLine(date + " " + date.DayOfWeek);
            }

            Assert.AreEqual(52, count);
        }

        [TestCase(5)] // Monday + 5 = Saturday
        [TestCase(6)] // Monday + 6 = Sunday
        public void StartOfWeekWithSymbolWithOffsetToWeekend(int offset)
        {
            var rules = GetDateRules();
            var rule = rules.WeekStart(Symbols.SPY, offset);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 3, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;

                // Both test cases are non-tradable days for Spy
                // We expect it to find another available day later in the week that is tradable,
                // meaning Monday in this set of dates
                // Also allow Tuesday because of some holidays where monday is not tradable.
                Assert.IsTrue(date.DayOfWeek == DayOfWeek.Monday || date.DayOfWeek == DayOfWeek.Tuesday);
            }

            Assert.AreEqual(13, count);
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

        [Test]
        public void EndOfWeekNoSymbolWithOffset()
        {
            var rules = GetDateRules();
            var rule = rules.WeekEnd(4);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                // Friday - 4 = Monday
                Assert.AreEqual(DayOfWeek.Monday, date.DayOfWeek);
            }

            Assert.AreEqual(52, count);
        }

        [Test]
        public void EndOfWeekWithSymbol()
        {
            var rules = GetDateRules();
            var rule = rules.WeekEnd(Symbols.SPY);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;
                Assert.IsTrue(date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Thursday);
            }

            Assert.AreEqual(52, count);
        }

        [Test]
        public void EndOfWeekWithSymbolWithOffset()
        {
            var rules = GetDateRules();
            var rule = rules.WeekEnd(Symbols.SPY, 2);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 12, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;

                // Friday - 2 = Wednesday
                Assert.IsTrue(date.DayOfWeek == DayOfWeek.Wednesday);
            }

            Assert.AreEqual(52, count);
        }

        [TestCase(5)] // Friday - 5 = Sunday
        [TestCase(6)] // Friday - 6 = Saturday
        public void EndOfWeekWithSymbolWithOffsetToWeekend(int offset)
        {
            var rules = GetDateRules();
            var rule = rules.WeekEnd(Symbols.SPY, offset);
            var dates = rule.GetDates(new DateTime(2000, 01, 01), new DateTime(2000, 3, 31));

            int count = 0;
            foreach (var date in dates)
            {
                count++;

                // Both test cases are non-tradable days for Spy
                // We expect it to find a previous day in the week that is tradable, meaning
                // Friday in this set of dates
                Assert.IsTrue(date.DayOfWeek == DayOfWeek.Friday);
            }

            Assert.AreEqual(13, count);
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
            var rules = new DateRules(manager, TimeZones.NewYork);
            return rules;
        }
    }
}
