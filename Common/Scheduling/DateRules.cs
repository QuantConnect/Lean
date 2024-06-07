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
using NodaTime;
using System.Linq;
using System.Globalization;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Helper class used to provide better syntax when defining date rules
    /// </summary>
    public class DateRules : BaseScheduleRules
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateRules"/> helper class
        /// </summary>
        /// <param name="securities">The security manager</param>
        /// <param name="timeZone">The algorithm's default time zone</param>
        /// <param name="marketHoursDatabase">The market hours database instance to use</param>
        public DateRules(SecurityManager securities, DateTimeZone timeZone, MarketHoursDatabase marketHoursDatabase)
            : base(securities, timeZone, marketHoursDatabase)
        {
        }

        /// <summary>
        /// Sets the default time zone
        /// </summary>
        /// <param name="timeZone">The time zone to use for helper methods that can't resolve a time zone</param>
        public void SetDefaultTimeZone(DateTimeZone timeZone)
        {
            TimeZone = timeZone;
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
            return new FuncDateRule(string.Join(",", dates.Select(x => x.ToShortDateString())), (start, end) => dates.Where(x => x >= start && x <= end));
        }

        /// <summary>
        /// Specifies an event should fire only on the specified days
        /// </summary>
        /// <param name="dates">The dates the event should fire</param>
        public IDateRule On(params DateTime[] dates)
        {
            // make sure they're date objects
            dates = dates.Select(x => x.Date).ToArray();
            return new FuncDateRule(string.Join(",", dates.Select(x => x.ToShortDateString())), (start, end) => dates.Where(x => x >= start && x <= end));
        }

        /// <summary>
        /// Specifies an event should only fire today in the algorithm's time zone
        /// using _securities.UtcTime instead of 'start' since ScheduleManager backs it up a day
        /// </summary>
        public IDateRule Today => new FuncDateRule("TodayOnly",
            (start, e) => {
                return new[] { Securities.UtcTime.ConvertFromUtc(TimeZone).Date };
            }
        );

        /// <summary>
        /// Specifies an event should only fire tomorrow in the algorithm's time zone
        /// using _securities.UtcTime instead of 'start' since ScheduleManager backs it up a day
        /// </summary>
        public IDateRule Tomorrow => new FuncDateRule("TomorrowOnly",
            (start, e) => new[] {Securities.UtcTime.ConvertFromUtc(TimeZone).Date.AddDays(1)}
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
            var securitySchedule = GetSecurityExchangeHours(symbol);
            return new FuncDateRule($"{symbol.Value}: EveryDay", (start, end) => Time.EachTradeableDay(securitySchedule, start, end));
        }

        /// <summary>
        /// Specifies an event should fire on the first of each year + offset
        /// </summary>
        /// <param name="daysOffset"> The amount of days to offset the schedule by; must be between 0 and 365.</param>
        /// <returns>A date rule that fires on the first of each year + offset</returns>
        public IDateRule YearStart(int daysOffset = 0)
        {
            return YearStart(null, daysOffset);
        }

        /// <summary>
        /// Specifies an event should fire on the first tradable date + offset for the specified symbol of each year
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the first tradable date of the year</param>
        /// <param name="daysOffset"> The amount of tradable days to offset the schedule by; must be between 0 and 365</param>
        /// <returns>A date rule that fires on the first tradable date + offset for the
        /// specified security each year</returns>
        public IDateRule YearStart(Symbol symbol, int daysOffset = 0)
        {
            // Check that our offset is allowed
            if (daysOffset < 0 || 365 < daysOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(daysOffset), "DateRules.YearStart() : Offset must be between 0 and 365");
            }

            SecurityExchangeHours securityExchangeHours = null;
            if (symbol != null)
            {
                securityExchangeHours = GetSecurityExchangeHours(symbol);
            }

            // Create the new DateRule and return it
            return new FuncDateRule(GetName(symbol, "YearStart", daysOffset), (start, end) => YearIterator(securityExchangeHours, start, end, daysOffset, true));
        }

        /// <summary>
        /// Specifies an event should fire on the last of each year
        /// </summary>
        /// <param name="daysOffset"> The amount of days to offset the schedule by; must be between 0 and 365</param>
        /// <returns>A date rule that fires on the last of each year - offset</returns>
        public IDateRule YearEnd(int daysOffset = 0)
        {
            return YearEnd(null, daysOffset);
        }

        /// <summary>
        /// Specifies an event should fire on the last tradable date - offset for the specified symbol of each year
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the last tradable date of the year</param>
        /// <param name="daysOffset">The amount of tradable days to offset the schedule by; must be between 0 and 365.</param>
        /// <returns>A date rule that fires on the last tradable date - offset for the specified security each year</returns>
        public IDateRule YearEnd(Symbol symbol, int daysOffset = 0)
        {
            // Check that our offset is allowed
            if (daysOffset < 0 || 365 < daysOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(daysOffset), "DateRules.YearEnd() : Offset must be between 0 and 365");
            }

            SecurityExchangeHours securityExchangeHours = null;
            if (symbol != null)
            {
                securityExchangeHours = GetSecurityExchangeHours(symbol);
            }

            // Create the new DateRule and return it
            return new FuncDateRule(GetName(symbol, "YearEnd", -daysOffset), (start, end) => YearIterator(securityExchangeHours, start, end, daysOffset, false));
        }

        /// <summary>
        /// Specifies an event should fire on the first of each month + offset
        /// </summary>
        /// <param name="daysOffset"> The amount of days to offset the schedule by; must be between 0 and 30.</param>
        /// <returns>A date rule that fires on the first of each month + offset</returns>
        public IDateRule MonthStart(int daysOffset = 0)
        {
            return new FuncDateRule(GetName(null, "MonthStart", daysOffset), (start, end) => MonthIterator(null, start, end, daysOffset, true));
        }

        /// <summary>
        /// Specifies an event should fire on the first tradable date + offset for the specified symbol of each month
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the first tradable date of the month</param>
        /// <param name="daysOffset"> The amount of tradable days to offset the schedule by; must be between 0 and 30</param>
        /// <returns>A date rule that fires on the first tradable date + offset for the
        /// specified security each month</returns>
        public IDateRule MonthStart(Symbol symbol, int daysOffset = 0)
        {
            // Check that our offset is allowed
            if (daysOffset < 0 || 30 < daysOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(daysOffset), "DateRules.MonthStart() : Offset must be between 0 and 30");
            }

            // Create the new DateRule and return it
            return new FuncDateRule(GetName(symbol, "MonthStart", daysOffset), (start, end) => MonthIterator(GetSecurityExchangeHours(symbol), start, end, daysOffset, true));
        }

        /// <summary>
        /// Specifies an event should fire on the last of each month
        /// </summary>
        /// <param name="daysOffset"> The amount of days to offset the schedule by; must be between 0 and 30</param>
        /// <returns>A date rule that fires on the last of each month - offset</returns>
        public IDateRule MonthEnd(int daysOffset = 0)
        {
            return new FuncDateRule(GetName(null, "MonthEnd", -daysOffset), (start, end) => MonthIterator(null, start, end, daysOffset, false));
        }

        /// <summary>
        /// Specifies an event should fire on the last tradable date - offset for the specified symbol of each month
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the last tradable date of the month</param>
        /// <param name="daysOffset">The amount of tradable days to offset the schedule by; must be between 0 and 30.</param>
        /// <returns>A date rule that fires on the last tradable date - offset for the specified security each month</returns>
        public IDateRule MonthEnd(Symbol symbol, int daysOffset = 0)
        {
            // Check that our offset is allowed
            if (daysOffset < 0 || 30 < daysOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(daysOffset), "DateRules.MonthEnd() : Offset must be between 0 and 30");
            }

            // Create the new DateRule and return it
            return new FuncDateRule(GetName(symbol, "MonthEnd", -daysOffset), (start, end) => MonthIterator(GetSecurityExchangeHours(symbol), start, end, daysOffset, false));
        }

        /// <summary>
        /// Specifies an event should fire on Monday + offset each week
        /// </summary>
        /// <param name="daysOffset">The amount of days to offset monday by; must be between 0 and 6</param>
        /// <returns>A date rule that fires on Monday + offset each week</returns>
        public IDateRule WeekStart(int daysOffset = 0)
        {
            // Check that our offset is allowed
            if (daysOffset < 0 || 6 < daysOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(daysOffset), "DateRules.WeekStart() : Offset must be between 0 and 6");
            }

            return new FuncDateRule(GetName(null, "WeekStart", daysOffset), (start, end) => WeekIterator(null, start, end, daysOffset, true));
        }

        /// <summary>
        /// Specifies an event should fire on the first tradable date + offset for the specified
        /// symbol each week
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the first
        /// tradeable date of the week</param>
        /// <param name="daysOffset">The amount of tradable days to offset the first tradable day by</param>
        /// <returns>A date rule that fires on the first + offset tradable date for the specified
        /// security each week</returns>
        public IDateRule WeekStart(Symbol symbol, int daysOffset = 0)
        {
            var securitySchedule = GetSecurityExchangeHours(symbol);
            var tradingDays = securitySchedule.MarketHours.Values
                .Where(x => x.IsClosedAllDay == false).OrderBy(x => x.DayOfWeek).ToList();

            // Limit offsets to securities weekly schedule
            if (daysOffset > tradingDays.Count - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(daysOffset),
                    $"DateRules.WeekStart() : {tradingDays.First().DayOfWeek}+{daysOffset} is out of range for {symbol}'s schedule," +
                    $" please use an offset between 0 - {tradingDays.Count - 1}; Schedule : {string.Join(", ", tradingDays.Select(x => x.DayOfWeek))}");
            }

            // Create the new DateRule and return it
            return new FuncDateRule(GetName(symbol, "WeekStart", daysOffset), (start, end) => WeekIterator(securitySchedule, start, end, daysOffset, true));
        }

        /// <summary>
        /// Specifies an event should fire on Friday - offset
        /// </summary>
        /// <param name="daysOffset"> The amount of days to offset Friday by; must be between 0 and 6 </param>
        /// <returns>A date rule that fires on Friday each week</returns>
        public IDateRule WeekEnd(int daysOffset = 0)
        {
            // Check that our offset is allowed
            if (daysOffset < 0 || 6 < daysOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(daysOffset), "DateRules.WeekEnd() : Offset must be between 0 and 6");
            }

            return new FuncDateRule(GetName(null, "WeekEnd", -daysOffset), (start, end) => WeekIterator(null, start, end, daysOffset, false));
        }

        /// <summary>
        /// Specifies an event should fire on the last - offset tradable date for the specified
        /// symbol of each week
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the last
        /// tradable date of the week</param>
        /// <param name="daysOffset"> The amount of tradable days to offset the last tradable day by each week</param>
        /// <returns>A date rule that fires on the last - offset tradable date for the specified security each week</returns>
        public IDateRule WeekEnd(Symbol symbol, int daysOffset = 0)
        {
            var securitySchedule = GetSecurityExchangeHours(symbol);
            var tradingDays = securitySchedule.MarketHours.Values
                .Where(x => x.IsClosedAllDay == false).OrderBy(x => x.DayOfWeek).ToList();

            // Limit offsets to securities weekly schedule
            if (daysOffset > tradingDays.Count - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(daysOffset),
                    $"DateRules.WeekEnd() : {tradingDays.Last().DayOfWeek}-{daysOffset} is out of range for {symbol}'s schedule," +
                    $" please use an offset between 0 - {tradingDays.Count - 1}; Schedule : {string.Join(", ", tradingDays.Select(x => x.DayOfWeek))}");
            }

            // Create the new DateRule and return it
            return new FuncDateRule(GetName(symbol, "WeekEnd", -daysOffset), (start, end) => WeekIterator(securitySchedule, start, end, daysOffset, false));
        }

        /// <summary>
        /// Determine the string representation for a given rule 
        /// </summary>
        /// <param name="symbol">Symbol for the rule</param>
        /// <param name="ruleType">Rule type in string form</param>
        /// <param name="offset">The amount of offset on this rule</param>
        /// <returns></returns>
        private static string GetName(Symbol symbol, string ruleType, int offset)
        {
            // Convert our offset to +#, -#, or empty string if 0
            var offsetString = offset.ToString("+#;-#;''", CultureInfo.InvariantCulture);
            var name = symbol == null ? $"{ruleType}{offsetString}" : $"{symbol.Value}: {ruleType}{offsetString}";

            return name;
        }


        /// <summary>
        /// Get the closest trading day to a given DateTime for a given <see cref="SecurityExchangeHours"/>.
        /// </summary>
        /// <param name="securityExchangeHours"><see cref="SecurityExchangeHours"/> object with schedule for this Security</param>
        /// <param name="baseDay">The day to base our search from</param>
        /// <param name="offset">Amount to offset the schedule by tradable days</param>
        /// <param name="searchForward">Search into the future for the closest day if true; into the past if false</param>
        /// <param name="boundary">The boundary DateTime on the resulting day</param>
        /// <returns></returns>
        private static DateTime GetScheduledDay(SecurityExchangeHours securityExchangeHours, DateTime baseDay, int offset, bool searchForward, DateTime? boundary = null)
        {
            // By default the scheduled date is the given day
            var scheduledDate = baseDay;

            // If its not open on this day find the next trading day by searching in the given direction
            if (!securityExchangeHours.IsDateOpen(scheduledDate))
            {
                scheduledDate = searchForward
                    ? securityExchangeHours.GetNextTradingDay(scheduledDate)
                    : securityExchangeHours.GetPreviousTradingDay(scheduledDate);
            }

            // Offset the scheduled day accordingly
            for (var i = 0; i < offset; i++)
            {
                scheduledDate = searchForward
                    ? securityExchangeHours.GetNextTradingDay(scheduledDate)
                    : securityExchangeHours.GetPreviousTradingDay(scheduledDate);
            }

            // If there is a boundary ensure we enforce it
            if (boundary.HasValue)
            {
                // If we are searching forward and the resulting date is after this boundary we
                // revert to the last tradable day equal to or less than boundary
                if (searchForward && scheduledDate > boundary)
                {
                    scheduledDate = GetScheduledDay(securityExchangeHours, (DateTime)boundary, 0, false);
                }

                // If we are searching backward and the resulting date is after this boundary we
                // revert to the last tradable day equal to or greater than boundary
                if (!searchForward && scheduledDate < boundary)
                {
                    scheduledDate = GetScheduledDay(securityExchangeHours, (DateTime)boundary, 0, true);
                }
            }

            return scheduledDate;
        }

        private static IEnumerable<DateTime> BaseIterator(
            SecurityExchangeHours securitySchedule,
            DateTime start,
            DateTime end,
            int offset,
            bool searchForward,
            DateTime periodBegin,
            DateTime periodEnd,
            Func<DateTime, DateTime> baseDateFunc,
            Func<DateTime, DateTime> boundaryDateFunc)
        {
            // No schedule means no security, set to open everyday
            if (securitySchedule == null)
            {
                securitySchedule = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);
            }

            foreach (var date in Time.EachDay(periodBegin, periodEnd))
            {
                var baseDate = baseDateFunc(date);
                var boundaryDate = boundaryDateFunc(date);

                // Determine the scheduled day for this period
                if (date == baseDate)
                {
                    var scheduledDay = GetScheduledDay(securitySchedule, baseDate, offset, searchForward, boundaryDate);

                    // Ensure the date is within our schedules range
                    if (scheduledDay >= start && scheduledDay <= end)
                    {
                        yield return scheduledDay;
                    }
                }
            }
        }

        private static IEnumerable<DateTime> MonthIterator(SecurityExchangeHours securitySchedule, DateTime start, DateTime end, int offset, bool searchForward)
        {
            // Iterate all days between the beginning of "start" month, through end of "end" month.
            // Necessary to ensure we schedule events in the month we start and end.
            var beginningOfStartMonth = new DateTime(start.Year, start.Month, 1);
            var endOfEndMonth = new DateTime(end.Year, end.Month, DateTime.DaysInMonth(end.Year, end.Month));

            // Searching forward the first of the month is baseDay, with boundary being the last
            // Searching backward the last of the month is baseDay, with boundary being the first
            Func<DateTime, DateTime> baseDateFunc = date => searchForward ? new DateTime(date.Year, date.Month, 1) : new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
            Func<DateTime, DateTime> boundaryDateFunc = date => searchForward ? new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)) : new DateTime(date.Year, date.Month, 1);

            return BaseIterator(securitySchedule, start, end, offset, searchForward, beginningOfStartMonth, endOfEndMonth, baseDateFunc, boundaryDateFunc);
        }

        private static IEnumerable<DateTime> YearIterator(SecurityExchangeHours securitySchedule, DateTime start, DateTime end, int offset, bool searchForward)
        {
            // Iterate all days between the beginning of "start" year, through end of "end" year
            // Necessary to ensure we schedule events in the year we start and end.
            var beginningOfStartOfYear = new DateTime(start.Year, start.Month, 1);
            var endOfEndYear = new DateTime(end.Year, end.Month, DateTime.DaysInMonth(end.Year, end.Month));

            // Searching forward the first of the year is baseDay, with boundary being the last
            // Searching backward the last of the year is baseDay, with boundary being the first
            Func<DateTime, DateTime> baseDateFunc = date => searchForward ? new DateTime(date.Year, 1, 1) : new DateTime(date.Year, 12, 31);
            Func<DateTime, DateTime> boundaryDateFunc = date => searchForward ? new DateTime(date.Year, 12, 31) : new DateTime(date.Year, 1, 1);

            return BaseIterator(securitySchedule, start, end, offset, searchForward, beginningOfStartOfYear, endOfEndYear, baseDateFunc, boundaryDateFunc);
        }

        private static IEnumerable<DateTime> WeekIterator(SecurityExchangeHours securitySchedule, DateTime start, DateTime end, int offset, bool searchForward)
        {
            // Determine the weekly base day and boundary to schedule off of
            DayOfWeek weeklyBaseDay;
            DayOfWeek weeklyBoundaryDay;
            if (securitySchedule == null)
            {
                // No schedule means no security, set to open everyday
                securitySchedule = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);

                // Searching forward Monday is baseDay, with boundary being the following Sunday
                // Searching backward Friday is baseDay, with boundary being the previous Saturday
                weeklyBaseDay = searchForward ? DayOfWeek.Monday : DayOfWeek.Friday;
                weeklyBoundaryDay = searchForward ? DayOfWeek.Saturday + 1 : DayOfWeek.Sunday - 1;
            }
            else
            {
                // Fetch the securities schedule 
                var weeklySchedule = securitySchedule.MarketHours.Values
                    .Where(x => x.IsClosedAllDay == false).OrderBy(x => x.DayOfWeek).ToList();

                // Determine our weekly base day and boundary for this security
                weeklyBaseDay = searchForward ? weeklySchedule.First().DayOfWeek : weeklySchedule.Last().DayOfWeek;
                weeklyBoundaryDay = searchForward ? weeklySchedule.Last().DayOfWeek : weeklySchedule.First().DayOfWeek;
            }

            // Iterate all days between the beginning of "start" week, through end of "end" week.
            // Necessary to ensure we schedule events in the week we start and end. 
            // Also if we have a sunday for start/end we need to adjust for it being the front of the week when we want it as the end of the week.
            var startAdjustment = start.DayOfWeek == DayOfWeek.Sunday ? -7 : 0;
            var beginningOfStartWeek = start.AddDays(-(int)start.DayOfWeek + 1 + startAdjustment); // Date - DayOfWeek + 1

            var endAdjustment = end.DayOfWeek == DayOfWeek.Sunday ? -7 : 0;
            var endOfEndWeek = end.AddDays(-(int)end.DayOfWeek + 7 + endAdjustment); // Date - DayOfWeek + 7 

            // Determine the schedule for each week in this range
            foreach (var date in Time.EachDay(beginningOfStartWeek, endOfEndWeek).Where(x => x.DayOfWeek == weeklyBaseDay))
            {
                var boundary = date.AddDays(weeklyBoundaryDay - weeklyBaseDay);
                var scheduledDay = GetScheduledDay(securitySchedule, date, offset, searchForward, boundary);

                // Ensure the date is within our schedules range
                if (scheduledDay >= start && scheduledDay <= end)
                {
                    yield return scheduledDay;
                }
            }
        }
    }
}
