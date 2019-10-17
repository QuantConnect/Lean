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
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Helper class used to provide better syntax when defining date rules
    /// </summary>
    public class DateRules
    {
        private readonly DateTimeZone _timeZone;
        private readonly SecurityManager _securities;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateRules"/> helper class
        /// </summary>
        /// <param name="securities">The security manager</param>
        /// <param name="timeZone">The algorithm's default time zone</param>
        public DateRules(SecurityManager securities, DateTimeZone timeZone)
        {
            _timeZone = timeZone;
            _securities = securities;
        }

        /// <summary>
        /// Specifies an event should fire only on the specified day
        /// </summary>
        /// <param name="year">The year</param>
        /// <param name="month">The month</param>
        /// <param name="day">The day</param>
        /// <returns></returns>
        public IDateRule On(int year, int month, int day)
        {
            // make sure they're date objects
            var dates = new[] {new DateTime(year, month, day)};
            return new FuncDateRule(string.Join(",", dates.Select(x => x.ToShortDateString())), (start, end) => dates);
        }

        /// <summary>
        /// Specifies an event should fire only on the specified days
        /// </summary>
        /// <param name="dates">The dates the event should fire</param>
        public IDateRule On(params DateTime[] dates)
        {
            // make sure they're date objects
            dates = dates.Select(x => x.Date).ToArray();
            return new FuncDateRule(string.Join(",", dates.Select(x => x.ToShortDateString())), (start, end) => dates);
        }

        /// <summary>
        /// Specifies an event should only fire today in the algorithm's time zone
        /// using _securities.UtcTime instead of 'start' since ScheduleManager backs it up a day
        /// </summary>
        public IDateRule Today => new FuncDateRule("TodayOnly",
            (start, e) => new[] {_securities.UtcTime.ConvertFromUtc(_timeZone).Date}
        );

        /// <summary>
        /// Specifies an event should only fire tomorrow in the algorithm's time zone
        /// using _securities.UtcTime instead of 'start' since ScheduleManager backs it up a day
        /// </summary>
        public IDateRule Tomorrow => new FuncDateRule("TomorrowOnly",
            (start, e) => new[] {_securities.UtcTime.ConvertFromUtc(_timeZone).Date.AddDays(1)}
        );

        /// <summary>
        /// Specifies an event should fire on each of the specified days of week
        /// </summary>
        /// <param name="day">The day the event shouls fire</param>
        /// <returns>A date rule that fires on every specified day of week</returns>
        public IDateRule Every(DayOfWeek day) => Every(new[] { day });

        /// <summary>
        /// Specifies an event should fire on each of the specified days of week
        /// </summary>
        /// <param name="days">The days the event shouls fire</param>
        /// <returns>A date rule that fires on every specified day of week</returns>
        public IDateRule Every(params DayOfWeek[] days)
        {
            var hash = days.ToHashSet();
            return new FuncDateRule(string.Join(",", days), (start, end) => Time.EachDay(start, end).Where(date => hash.Contains(date.DayOfWeek)));
        }

        /// <summary>
        /// Specifies an event should fire every day
        /// </summary>
        /// <returns>A date rule that fires every day</returns>
        public IDateRule EveryDay()
        {
            return new FuncDateRule("EveryDay", Time.EachDay);
        }

        /// <summary>
        /// Specifies an event should fire every day the symbol is trading
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine tradeable dates</param>
        /// <returns>A date rule that fires every day the specified symbol trades</returns>
        public IDateRule EveryDay(Symbol symbol)
        {
            var security = GetSecurity(symbol);
            return new FuncDateRule($"{symbol.Value}: EveryDay", (start, end) => Time.EachTradeableDay(security, start, end));
        }

        /// <summary>
        /// Specifies an event should fire on the first of each month
        /// </summary>
        /// <returns>A date rule that fires on the first of each month</returns>
        public IDateRule MonthStart()
        {
            return new FuncDateRule("MonthStart", (start, end) => MonthStartIterator(null, start, end));
        }

        /// <summary>
        /// Specifies an event should fire on the first tradeable date for the specified
        /// symbol of each month
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the first
        /// tradeable date of the month</param>
        /// <returns>A date rule that fires on the first tradeable date for the specified security each month</returns>
        public IDateRule MonthStart(Symbol symbol)
        {
            return new FuncDateRule($"{symbol.Value}: MonthStart", (start, end) => MonthStartIterator(GetSecurity(symbol), start, end));
        }

        /// <summary>
        /// Specifies an event should fire on the last of each month
        /// </summary>
        /// <returns>A date rule that fires on the last of each month</returns>
        public IDateRule MonthEnd()
        {
            return new FuncDateRule("MonthEnd", (start, end) => MonthEndIterator(null, start, end));
        }

        /// <summary>
        /// Specifies an event should fire on the last tradeable date for the specified
        /// symbol of each month
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the last
        /// tradeable date of the month</param>
        /// <returns>A date rule that fires on the last tradeable date for the specified security each month</returns>
        public IDateRule MonthEnd(Symbol symbol)
        {
            return new FuncDateRule($"{symbol.Value}: MonthEnd", (start, end) => MonthEndIterator(GetSecurity(symbol), start, end));
        }

        /// <summary>
        /// Specifies an event should fire on Monday each week
        /// </summary>
        /// <returns>A date rule that fires on Monday each week</returns>
        public IDateRule WeekStart()
        {
            return new FuncDateRule("WeekStart", (start, end) => WeekStartIterator(null, start, end));
        }

        /// <summary>
        /// Specifies an event should fire on the first tradeable date for the specified
        /// symbol of each week
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the first
        /// tradeable date of the week</param>
        /// <returns>A date rule that fires on the first tradeable date for the specified security each week</returns>
        public IDateRule WeekStart(Symbol symbol)
        {
            return new FuncDateRule($"{symbol.Value}: WeekStart", (start, end) => WeekStartIterator(GetSecurity(symbol), start, end));
        }

        /// <summary>
        /// Specifies an event should fire on Friday each week
        /// </summary>
        /// <returns>A date rule that fires on Friday each week</returns>
        public IDateRule WeekEnd()
        {
            return new FuncDateRule("WeekEnd", (start, end) => WeekEndIterator(null, start, end));
        }

        /// <summary>
        /// Specifies an event should fire on the last tradeable date for the specified
        /// symbol of each week
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the last
        /// tradeable date of the week</param>
        /// <returns>A date rule that fires on the last tradeable date for the specified security each week</returns>
        public IDateRule WeekEnd(Symbol symbol)
        {
            return new FuncDateRule($"{symbol.Value}: WeekEnd", (start, end) => WeekEndIterator(GetSecurity(symbol), start, end));
        }

        /// <summary>
        /// Gets the security with the specified symbol, or throws an exception if the symbol is not found
        /// </summary>
        /// <param name="symbol">The security's symbol to search for</param>
        /// <returns>The security object matching the given symbol</returns>
        private Security GetSecurity(Symbol symbol)
        {
            Security security;
            if (!_securities.TryGetValue(symbol, out security))
            {
                throw new KeyNotFoundException(symbol.Value + " not found in portfolio. Request this data when initializing the algorithm.");
            }
            return security;
        }

        private static IEnumerable<DateTime> MonthStartIterator(Security security, DateTime start, DateTime end)
        {
            if (security == null)
            {
                foreach (var date in Time.EachDay(start, end))
                {
                    // fire on the first of each month
                    if (date.Day == 1) yield return date;
                }
                yield break;
            }

            // start a month back so we can properly resolve the first event (we may have passed it)
            var aMonthBeforeStart = start.AddMonths(-1);
            int lastMonth = aMonthBeforeStart.Month;
            foreach (var date in Time.EachTradeableDay(security, aMonthBeforeStart, end))
            {
                if (date.Month != lastMonth)
                {
                    if (date >= start)
                    {
                        // only emit if the date is on or after the start
                        // the date may be before here because we backed up a month
                        // to properly resolve the first tradeable date
                        yield return date;
                    }
                    lastMonth = date.Month;
                }
            }
        }

        private static IEnumerable<DateTime> MonthEndIterator(Security security, DateTime start, DateTime end)
        {
            foreach (var date in Time.EachDay(start, end))
            {
                if (date.Day == DateTime.DaysInMonth(date.Year, date.Month))
                {
                    if (security == null)
                    {
                        // fire on the last of each month
                        yield return date;
                    }
                    else
                    {
                        // find previous date when market is open
                        var currentDate = date;
                        while (!security.Exchange.Hours.IsDateOpen(currentDate))
                        {
                            currentDate = currentDate.AddDays(-1);
                        }
                        yield return currentDate;
                    }
                }
            }
        }

        private static IEnumerable<DateTime> WeekStartIterator(Security security, DateTime start, DateTime end)
        {
            var skippedMarketClosedDay = false;

            foreach (var date in Time.EachDay(start, end))
            {
                if (security == null)
                {
                    // fire on Monday
                    if (date.DayOfWeek == DayOfWeek.Monday)
                    {
                        yield return date;
                    }
                }
                else
                {
                    // skip Mondays and following days when market is closed
                    if (date.DayOfWeek == DayOfWeek.Monday || skippedMarketClosedDay)
                    {
                        if (security.Exchange.Hours.IsDateOpen(date))
                        {
                            skippedMarketClosedDay = false;
                            yield return date;
                        }
                        else
                        {
                            skippedMarketClosedDay = true;
                        }
                    }
                }
            }
        }

        private static IEnumerable<DateTime> WeekEndIterator(Security security, DateTime start, DateTime end)
        {
            foreach (var date in Time.EachDay(start, end))
            {
                if (date.DayOfWeek == DayOfWeek.Friday)
                {
                    if (security == null)
                    {
                        // fire on Friday
                        yield return date;
                    }
                    else
                    {
                        // find previous date when market is open
                        var currentDate = date;
                        while (!security.Exchange.Hours.IsDateOpen(currentDate))
                        {
                            currentDate = currentDate.AddDays(-1);
                        }
                        yield return currentDate;
                    }
                }
            }
        }
    }
}
