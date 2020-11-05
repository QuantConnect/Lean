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
        /// <param name="day">The day the event should fire</param>
        /// <returns>A date rule that fires on every specified day of week</returns>
        public IDateRule Every(DayOfWeek day) => Every(new[] { day });

        /// <summary>
        /// Specifies an event should fire on each of the specified days of week
        /// </summary>
        /// <param name="days">The days the event should fire</param>
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
        /// <param name="symbol">The symbol whose exchange is used to determine tradable dates</param>
        /// <returns>A date rule that fires every day the specified symbol trades</returns>
        public IDateRule EveryDay(Symbol symbol)
        {
            var security = GetSecurity(symbol);
            return new FuncDateRule($"{symbol.Value}: EveryDay", (start, end) => Time.EachTradeableDay(security, start, end));
        }

        /// <summary>
        /// Specifies an event should fire on the first of each month
        /// </summary>
        /// <param name="daysOffset"> The amount of days to offset the schedule by; must
        /// be between 0 and 15. Use MonthEnd for unreachable dates</param>
        /// <returns>A date rule that fires on the first of each month + offset</returns>
        public IDateRule MonthStart(int daysOffset = 0)
        {
            return MonthStart(null, daysOffset);
        }

        /// <summary>
        /// Specifies an event should fire on the first tradable date for the specified
        /// symbol of each month
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the first
        /// tradable date of the month</param>
        /// <param name="daysOffset"> The amount of days to offset the schedule by; must
        /// be between 0 and 15. Use MonthEnd for unreachable dates</param>
        /// <returns>A date rule that fires on the first tradable date + offset for the
        /// specified security each month</returns>
        public IDateRule MonthStart(Symbol symbol, int daysOffset = 0)
        {
            // Check that our offset is allowed
            if (daysOffset < 0 || 15 < daysOffset)
            {
                throw new ArgumentOutOfRangeException("daysOffset", "DateRules.MonthStart() : Offset must be between 0 and 15");
            }

            // Create the new DateRule and return it
            return new FuncDateRule(GetName(symbol, "MonthStart", daysOffset), (start, end) => MonthIterator(GetSecurity(symbol), start, end, daysOffset, true));
        }

        /// <summary>
        /// Specifies an event should fire on the last of each month
        /// </summary>
        /// <param name="daysOffset"> The amount of days to offset the schedule by; must
        /// be between -15 and 0. Use MonthStart for unreachable dates</param>
        /// <returns>A date rule that fires on the last of each month + offset</returns>
        public IDateRule MonthEnd(int daysOffset = 0)
        {
            return MonthEnd(null, daysOffset);
        }

        /// <summary>
        /// Specifies an event should fire on the last tradable date for the specified
        /// symbol of each month
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the last
        /// tradable date of the month</param>
        /// <param name="daysOffset"> The amount of days to offset the schedule by; must
        /// be between -15 and 0. Use MonthStart for unreachable dates</param>
        /// <returns>A date rule that fires on the last tradable date + offset for the specified security each month</returns>
        public IDateRule MonthEnd(Symbol symbol, int daysOffset = 0)
        {
            // Check that our offset is allowed
            if (daysOffset < -15 || 0 < daysOffset)
            {
                throw new ArgumentOutOfRangeException("daysOffset", "DateRules.MonthEnd() : Offset must be between -15 and 0");
            }

            // Create the new DateRule and return it
            return new FuncDateRule(GetName(symbol, "MonthEnd", daysOffset), (start, end) => MonthIterator(GetSecurity(symbol), start, end, daysOffset, false));
        }

        /// <summary>
        /// Specifies an event should fire on Monday each week; can be offset
        /// </summary>
        /// <param name="daysOffset"> The amount of days to offset monday by; because offset 0 = monday value must be between -1 and 5 </param>
        /// <returns>A date rule that fires on Monday + offset each week</returns>
        public IDateRule WeekStart(int daysOffset = 0)
        {
            return WeekStart(null, daysOffset);
        }

        /// <summary>
        /// Specifies an event should fire on the first tradable date for the specified
        /// symbol of each week; First day is defined by Monday, schedule will adjust according to tradable days
        /// and offset
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the first
        /// tradeable date of the week</param>
        /// <param name="daysOffset"> The amount of days to offset monday by; must be between -1 and 5 </param>
        /// <returns>A date rule that fires on the first tradable date + offset for the specified security each week</returns>
        public IDateRule WeekStart(Symbol symbol, int daysOffset = 0)
        {
            // Check that our offset is allowed
            if (daysOffset < -1 || 5 < daysOffset)
            {
                throw new ArgumentOutOfRangeException("daysOffset", "DateRules.WeekStart() : Offset must be between -1 and 5");
            }

            // Create the new DateRule and return it
            return new FuncDateRule(GetName(symbol, "WeekStart", daysOffset), (start, end) => WeekIterator(GetSecurity(symbol), start, end, daysOffset, true));
        }

        /// <summary>
        /// Specifies an event should fire on Friday; can be offset
        /// </summary>
        /// <param name="daysOffset"> The amount of days to offset Friday by; must be between -5 and 1 </param>
        /// <returns>A date rule that fires on Friday each week</returns>
        public IDateRule WeekEnd(int daysOffset = 0)
        {
            return WeekEnd(null, daysOffset);
        }

        /// <summary>
        /// Specifies an event should fire on the last tradable date for the specified
        /// symbol of each week; last day is defined by Friday, schedule will adjust according to tradable days
        /// and offset
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the last
        /// tradable date of the week</param>
        /// <param name="daysOffset"> The amount of days to offset Friday by; must be between -5 and 1 </param>
        /// <returns>A date rule that fires on the last tradable + offset date for the specified security each week</returns>
        public IDateRule WeekEnd(Symbol symbol, int daysOffset = 0)
        {
            // Check that our offset is allowed
            if (daysOffset < -5 || 1 < daysOffset)
            {
                throw new ArgumentOutOfRangeException("daysOffset", "DateRules.WeekStart() : Offset must be between -1 and 5");
            }

            // Create the new DateRule and return it
            return new FuncDateRule(GetName(symbol, "WeekEnd", daysOffset), (start, end) => WeekIterator(GetSecurity(symbol), start, end, daysOffset, false));
        }

        /// <summary>
        /// Gets the security with the specified symbol, or throws an exception if the symbol is not found
        /// </summary>
        /// <param name="symbol">The security's symbol to search for</param>
        /// <returns>The security object matching the given symbol</returns>
        private Security GetSecurity(Symbol symbol)
        {
            // We use this for the rules without a symbol
            if (symbol == null)
            {
                return null;
            }

            Security security;
            if (!_securities.TryGetValue(symbol, out security))
            {
                throw new KeyNotFoundException(symbol.Value + " not found in portfolio. Request this data when initializing the algorithm.");
            }
            return security;
        }

        private string GetName(Symbol symbol, string ruleType, int offset)
        {
            // Convert our offset to +#, -#, or empty string if 0
            var offsetString = offset.ToString("+#;-#;''", CultureInfo.InvariantCulture);
            var name = symbol == null ? $"{ruleType}{offsetString}" : $"{symbol.Value}: {ruleType}{offsetString}";

            return name;
        }

        private static IEnumerable<DateTime> MonthIterator(Security security, DateTime start, DateTime end, int offset, bool searchForward)
        {
            foreach (var date in Time.EachDay(start, end))
            {
                // For MonthStart we are searching forward so dates is : 1st + offset
                // For MonthEnd we are searching backward so date is : Last of the Month + offset
                var scheduledDayOfMonth = searchForward ? 1 + offset : DateTime.DaysInMonth(date.Year, date.Month) + offset;

                // On the day we expect to trigger the event lets find the appropriate day
                if (date.Day == scheduledDayOfMonth)
                {
                    if (security == null)
                    {
                        // fire on the scheduled day of each month
                        yield return date;
                    }
                    else
                    {
                        // find the next appropriate date when market is open
                        var currentDate = date;
                        while (!security.Exchange.Hours.IsDateOpen(currentDate))
                        {
                            // Search in the appropriate direction
                            currentDate = currentDate.AddDays(searchForward ? 1 : -1);
                        }
                        yield return currentDate;
                    }
                }
            }
        }

        private static IEnumerable<DateTime> WeekIterator(Security security, DateTime start, DateTime end, int offset, bool searchForward)
        {
            // For WeekStart we are searching forward so day of the week is : Monday + offset
            // For WeekEnd we are searching backward so day of the week is : Friday + offset
            var scheduledDayOfWeek = searchForward ? DayOfWeek.Monday + offset : DayOfWeek.Friday + offset;

            foreach (var date in Time.EachDay(start, end))
            {
                // On the day we expect to trigger the event lets find the appropriate day
                if (date.DayOfWeek == scheduledDayOfWeek)
                {
                    if (security == null)
                    {
                        // fire on scheduled day of the week
                        yield return date;
                    }
                    else
                    {
                        // find the next appropriate date when market is open
                        var currentDate = date;
                        while (!security.Exchange.Hours.IsDateOpen(currentDate))
                        {
                            // Search in the appropriate direction
                            currentDate = currentDate.AddDays(searchForward ? 1 : -1);
                        }
                        yield return currentDate;
                    }
                }
            }
        }
    }
}
