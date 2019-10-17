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
                Console.WriteLine(date + " " + date.DayOfWeek);
            }

            Assert.AreEqual(52, count);
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
