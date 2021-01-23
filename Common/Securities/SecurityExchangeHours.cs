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
using System.Runtime.CompilerServices;
using NodaTime;
using QuantConnect.Util;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents the schedule of a security exchange. This includes daily regular and extended market hours
    /// as well as holidays, early closes and late opens.
    /// </summary>
    /// <remarks>
    /// This type assumes that IsOpen will be called with increasingly future times, that is, the calls should never back
    /// track in time. This assumption is required to prevent time zone conversions on every call.
    /// </remarks>
    public class SecurityExchangeHours
    {
        private readonly HashSet<long> _holidays;
        private readonly Dictionary<DateTime, TimeSpan> _earlyCloses;
        private readonly Dictionary<DateTime, TimeSpan> _lateOpens;

        // these are listed individually for speed
        private readonly LocalMarketHours _sunday;
        private readonly LocalMarketHours _monday;
        private readonly LocalMarketHours _tuesday;
        private readonly LocalMarketHours _wednesday;
        private readonly LocalMarketHours _thursday;
        private readonly LocalMarketHours _friday;
        private readonly LocalMarketHours _saturday;
        private readonly Dictionary<DayOfWeek, LocalMarketHours> _openHoursByDay;

        /// <summary>
        /// Gets the time zone this exchange resides in
        /// </summary>
        public DateTimeZone TimeZone { get; }

        /// <summary>
        /// Gets the holidays for the exchange
        /// </summary>
        public HashSet<DateTime> Holidays
        {
            get { return _holidays.ToHashSet(x => new DateTime(x)); }
        }

        /// <summary>
        /// Gets the market hours for this exchange
        /// </summary>
        public IReadOnlyDictionary<DayOfWeek, LocalMarketHours> MarketHours => _openHoursByDay;

        /// <summary>
        /// Gets the early closes for this exchange
        /// </summary>
        public IReadOnlyDictionary<DateTime, TimeSpan> EarlyCloses => _earlyCloses;

        /// <summary>
        /// Gets the late opens for this exchange
        /// </summary>
        public IReadOnlyDictionary<DateTime, TimeSpan> LateOpens => _lateOpens;

        /// <summary>
        /// Gets the most common tradable time during the market week.
        /// For a normal US equity trading day this is 6.5 hours.
        /// This does NOT account for extended market hours and only
        /// considers <see cref="MarketHoursState.Market"/>
        /// </summary>
        public TimeSpan RegularMarketDuration { get; }

        /// <summary>
        /// Gets a <see cref="SecurityExchangeHours"/> instance that is always open
        /// </summary>
        public static SecurityExchangeHours AlwaysOpen(DateTimeZone timeZone)
        {
            var dayOfWeeks = Enum.GetValues(typeof (DayOfWeek)).OfType<DayOfWeek>();
            return new SecurityExchangeHours(timeZone,
                Enumerable.Empty<DateTime>(),
                dayOfWeeks.Select(LocalMarketHours.OpenAllDay).ToDictionary(x => x.DayOfWeek),
                new Dictionary<DateTime, TimeSpan>(),
                new Dictionary<DateTime, TimeSpan>()
                );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityExchangeHours"/> class
        /// </summary>
        /// <param name="timeZone">The time zone the dates and hours are represented in</param>
        /// <param name="holidayDates">The dates this exchange is closed for holiday</param>
        /// <param name="marketHoursForEachDayOfWeek">The exchange's schedule for each day of the week</param>
        /// <param name="earlyCloses">The dates this exchange has an early close</param>
        /// <param name="lateOpens">The dates this exchange has a late open</param>
        public SecurityExchangeHours(
            DateTimeZone timeZone,
            IEnumerable<DateTime> holidayDates,
            IReadOnlyDictionary<DayOfWeek, LocalMarketHours> marketHoursForEachDayOfWeek,
            IReadOnlyDictionary<DateTime, TimeSpan> earlyCloses,
            IReadOnlyDictionary<DateTime, TimeSpan> lateOpens)
        {
            TimeZone = timeZone;
            _holidays = holidayDates.Select(x => x.Date.Ticks).ToHashSet();
            _earlyCloses = earlyCloses.ToDictionary(x => x.Key.Date, x => x.Value);
            _lateOpens = lateOpens.ToDictionary(x => x.Key.Date, x => x.Value);

            // make a copy of the dictionary for internal use
            _openHoursByDay = new Dictionary<DayOfWeek, LocalMarketHours>(marketHoursForEachDayOfWeek.ToDictionary());

            SetMarketHoursForDay(DayOfWeek.Sunday, out _sunday);
            SetMarketHoursForDay(DayOfWeek.Monday, out _monday);
            SetMarketHoursForDay(DayOfWeek.Tuesday, out _tuesday);
            SetMarketHoursForDay(DayOfWeek.Wednesday, out _wednesday);
            SetMarketHoursForDay(DayOfWeek.Thursday, out _thursday);
            SetMarketHoursForDay(DayOfWeek.Friday, out _friday);
            SetMarketHoursForDay(DayOfWeek.Saturday, out _saturday);

            // pick the most common market hours duration, if there's a tie, pick the larger duration
            RegularMarketDuration = _openHoursByDay.Values.GroupBy(lmh => lmh.MarketDuration)
                .OrderByDescending(grp => grp.Count())
                .ThenByDescending(grp => grp.Key)
                .First().Key;
        }

        /// <summary>
        /// Determines if the exchange is open at the specified local date time.
        /// </summary>
        /// <param name="localDateTime">The time to check represented as a local time</param>
        /// <param name="extendedMarket">True to use the extended market hours, false for just regular market hours</param>
        /// <returns>True if the exchange is considered open at the specified time, false otherwise</returns>
        public bool IsOpen(DateTime localDateTime, bool extendedMarket)
        {
            if (_holidays.Contains(localDateTime.Date.Ticks) || IsTimeAfterEarlyClose(localDateTime) || IsTimeBeforeLateOpen(localDateTime))
            {
                return false;
            }

            return GetMarketHours(localDateTime.DayOfWeek).IsOpen(localDateTime.TimeOfDay, extendedMarket);
        }

        /// <summary>
        /// Determines if the exchange is open at any point in time over the specified interval.
        /// </summary>
        /// <param name="startLocalDateTime">The start of the interval in local time</param>
        /// <param name="endLocalDateTime">The end of the interval in local time</param>
        /// <param name="extendedMarket">True to use the extended market hours, false for just regular market hours</param>
        /// <returns>True if the exchange is considered open at the specified time, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOpen(DateTime startLocalDateTime, DateTime endLocalDateTime, bool extendedMarket)
        {
            if (startLocalDateTime == endLocalDateTime)
            {
                // if we're testing an instantaneous moment, use the other function
                return IsOpen(startLocalDateTime, extendedMarket);
            }

            // we must make intra-day requests to LocalMarketHours, so check for a day gap
            var start = startLocalDateTime;
            var end = new DateTime(Math.Min(endLocalDateTime.Ticks, start.Date.Ticks + Time.OneDay.Ticks - 1));
            do
            {
                if (!_holidays.Contains(start.Date.Ticks) && !IsTimeAfterEarlyClose(start) && !IsTimeBeforeLateOpen(start))
                {
                    // check to see if the market is open
                    var marketHours = GetMarketHours(start.DayOfWeek);
                    if (marketHours.IsOpen(start.TimeOfDay, end.TimeOfDay, extendedMarket))
                    {
                        return true;
                    }
                }

                start = start.Date.AddDays(1);
                end = new DateTime(Math.Min(endLocalDateTime.Ticks, end.Ticks + Time.OneDay.Ticks));
            }
            while (end > start);

            return false;
        }

        /// <summary>
        /// Determines if the exchange will be open on the date specified by the local date time
        /// </summary>
        /// <param name="localDateTime">The date time to check if the day is open</param>
        /// <returns>True if the exchange will be open on the specified date, false otherwise</returns>
        public bool IsDateOpen(DateTime localDateTime)
        {
            var marketHours = GetMarketHours(localDateTime.DayOfWeek);
            if (marketHours.IsClosedAllDay)
            {
                // if we don't have hours for this day then we're not open
                return false;
            }

            // if we don't have a holiday then we're open
            return !_holidays.Contains(localDateTime.Date.Ticks);
        }

        /// <summary>
        /// Helper to access the market hours field based on the day of week
        /// </summary>
        /// <param name="localDateTime">The local date time to retrieve market hours for</param>
        public LocalMarketHours GetMarketHours(DateTime localDateTime)
        {
            return GetMarketHours(localDateTime.DayOfWeek);
        }

        /// <summary>
        /// Gets the local date time corresponding to the next market open following the specified time
        /// </summary>
        /// <param name="localDateTime">The time to begin searching for market open (non-inclusive)</param>
        /// <param name="extendedMarket">True to include extended market hours in the search</param>
        /// <returns>The next market opening date time following the specified local date time</returns>
        public DateTime GetNextMarketOpen(DateTime localDateTime, bool extendedMarket)
        {
            var time = localDateTime;
            var oneWeekLater = localDateTime.Date.AddDays(15);
            do
            {
                var marketHours = GetMarketHours(time.DayOfWeek);
                if (!marketHours.IsClosedAllDay && !_holidays.Contains(time.Date.Ticks))
                {
                    TimeSpan lateOpenTime;
                    if (_lateOpens.TryGetValue(time.Date, out lateOpenTime))
                    {
                        var lateOpenDateTime = time.Date.Add(lateOpenTime);
                        if (time < lateOpenDateTime)
                            return lateOpenDateTime;

                        time = time.Date + Time.OneDay;
                        continue;
                    }

                    TimeSpan earlyCloseTime;
                    if (_earlyCloses.TryGetValue(time.Date, out earlyCloseTime))
                    {
                        var earlyCloseDateTime = time.Date.Add(earlyCloseTime);
                        if (time > earlyCloseDateTime)
                        {
                            time = time.Date + Time.OneDay;
                            continue;
                        }
                    }

                    var marketOpenTimeOfDay = marketHours.GetMarketOpen(time.TimeOfDay, extendedMarket);
                    if (marketOpenTimeOfDay.HasValue)
                    {
                        var marketOpen = time.Date + marketOpenTimeOfDay.Value;
                        if (localDateTime < marketOpen)
                        {
                            return marketOpen;
                        }
                    }
                }

                time = time.Date + Time.OneDay;
            }
            while (time < oneWeekLater);

            throw new ArgumentException("Unable to locate next market open within two weeks.");
        }

        /// <summary>
        /// Gets the local date time corresponding to the next market close following the specified time
        /// </summary>
        /// <param name="localDateTime">The time to begin searching for market close (non-inclusive)</param>
        /// <param name="extendedMarket">True to include extended market hours in the search</param>
        /// <returns>The next market closing date time following the specified local date time</returns>
        public DateTime GetNextMarketClose(DateTime localDateTime, bool extendedMarket)
        {
            var time = localDateTime;
            var oneWeekLater = localDateTime.Date.AddDays(15);
            do
            {
                var marketHours = GetMarketHours(time.DayOfWeek);
                if (!marketHours.IsClosedAllDay && !_holidays.Contains(time.Date.Ticks))
                {
                    TimeSpan earlyCloseTime;
                    if (_earlyCloses.TryGetValue(time.Date, out earlyCloseTime))
                    {
                        var earlyCloseDateTime = time.Date.Add(earlyCloseTime);
                        if (time < earlyCloseDateTime)
                            return earlyCloseDateTime;

                        time = time.Date + Time.OneDay;
                        continue;
                    }

                    TimeSpan lateOpenTime;
                    if (_lateOpens.TryGetValue(time.Date, out lateOpenTime))
                    {
                        var lateOpenDateTime = time.Date.Add(lateOpenTime);
                        if (time < lateOpenDateTime)
                        {
                            time = lateOpenDateTime;
                            continue;
                        }
                    }

                    var marketCloseTimeOfDay = marketHours.GetMarketClose(time.TimeOfDay, extendedMarket);
                    if (marketCloseTimeOfDay.HasValue)
                    {
                        var marketClose = time.Date + marketCloseTimeOfDay.Value;
                        if (localDateTime < marketClose)
                        {
                            return marketClose;
                        }
                    }
                }

                time = time.Date + Time.OneDay;
            }
            while (time < oneWeekLater);

            throw new ArgumentException("Unable to locate next market close within two weeks.");
        }

        /// <summary>
        /// Helper to extract market hours from the <see cref="_openHoursByDay"/> dictionary, filling
        /// in Closed instantes when not present
        /// </summary>
        private void SetMarketHoursForDay(DayOfWeek dayOfWeek, out LocalMarketHours localMarketHoursForDay)
        {
            if (!_openHoursByDay.TryGetValue(dayOfWeek, out localMarketHoursForDay))
            {
                // assign to our dictionary that we're closed this day, as well as our local field
                _openHoursByDay[dayOfWeek] = localMarketHoursForDay = LocalMarketHours.ClosedAllDay(dayOfWeek);
            }
        }

        /// <summary>
        /// Helper to access the market hours field based on the day of week
        /// </summary>
        private LocalMarketHours GetMarketHours(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Sunday:
                    return _sunday;
                case DayOfWeek.Monday:
                    return _monday;
                case DayOfWeek.Tuesday:
                    return _tuesday;
                case DayOfWeek.Wednesday:
                    return _wednesday;
                case DayOfWeek.Thursday:
                    return _thursday;
                case DayOfWeek.Friday:
                    return _friday;
                case DayOfWeek.Saturday:
                    return _saturday;
                default:
                    throw new ArgumentOutOfRangeException(nameof(day), day, null);
            }
        }

        /// <summary>
        /// Helper to determine if the current time is after a market early close
        /// </summary>
        private bool IsTimeAfterEarlyClose(DateTime localDateTime)
        {
            TimeSpan earlyCloseTime;
            return _earlyCloses.TryGetValue(localDateTime.Date, out earlyCloseTime) && localDateTime.TimeOfDay >= earlyCloseTime;
        }

        /// <summary>
        /// Helper to determine if the current time is before a market late open
        /// </summary>
        private bool IsTimeBeforeLateOpen(DateTime localDateTime)
        {
            TimeSpan lateOpenTime;
            return _lateOpens.TryGetValue(localDateTime.Date, out lateOpenTime) && localDateTime.TimeOfDay <= lateOpenTime;
        }

        /// <summary>
        /// Gets the previous trading day
        /// </summary>
        /// <param name="localDate">The date to start searching at in this exchange's time zones</param>
        /// <returns>The previous trading day</returns>
        public DateTime GetPreviousTradingDay(DateTime localDate)
        {
            localDate = localDate.AddDays(-1);
            while (!IsDateOpen(localDate))
            {
                localDate = localDate.AddDays(-1);
            }

            return localDate;
        }

        /// <summary>
        /// Gets the next trading day
        /// </summary>
        /// <param name="date">The date to start searching at</param>
        /// <returns>The next trading day</returns>
        public DateTime GetNextTradingDay(DateTime date)
        {
            date = date.AddDays(1);
            while (!IsDateOpen(date))
            {
                date = date.AddDays(1);
            }

            return date;
        }
    }
}