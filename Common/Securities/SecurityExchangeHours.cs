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
        private HashSet<long> _holidays;
        private HashSet<long> _bankHolidays;
        private IReadOnlyDictionary<DateTime, TimeSpan> _earlyCloses;
        private IReadOnlyDictionary<DateTime, TimeSpan> _lateOpens;

        // these are listed individually for speed
        private LocalMarketHours _sunday;
        private LocalMarketHours _monday;
        private LocalMarketHours _tuesday;
        private LocalMarketHours _wednesday;
        private LocalMarketHours _thursday;
        private LocalMarketHours _friday;
        private LocalMarketHours _saturday;
        private Dictionary<DayOfWeek, LocalMarketHours> _openHoursByDay;
        private static List<DayOfWeek> daysOfWeek = new List<DayOfWeek>() {
                DayOfWeek.Sunday,
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday
        };

        /// <summary>
        /// Gets the time zone this exchange resides in
        /// </summary>
        public DateTimeZone TimeZone { get; private set; }

        /// <summary>
        /// Gets the holidays for the exchange
        /// </summary>
        public HashSet<DateTime> Holidays
        {
            get { return _holidays.ToHashSet(x => new DateTime(x)); }
        }

        /// <summary>
        /// Gets the bank holidays for the exchange
        /// </summary>
        /// <remarks>In some markets and assets, like CME futures, there are tradable dates (market open) which
        /// should not be considered for expiration rules due to banks being closed</remarks>
        public HashSet<DateTime> BankHolidays
        {
            get { return _bankHolidays.ToHashSet(x => new DateTime(x)); }
        }

        /// <summary>
        /// Gets the market hours for this exchange
        /// </summary>
        /// <remarks>
        /// This returns the regular schedule for each day, without taking into account special cases
        /// such as holidays, early closes, or late opens.
        /// In order to get the actual market hours for a specific date, use <see cref="GetMarketHours(DateTime)"/>
        /// </remarks>
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
        public TimeSpan RegularMarketDuration { get; private set; }

        /// <summary>
        /// Checks whether the market is always open or not
        /// </summary>
        public bool IsMarketAlwaysOpen { private set; get; }

        /// <summary>
        /// Gets a <see cref="SecurityExchangeHours"/> instance that is always open
        /// </summary>
        public static SecurityExchangeHours AlwaysOpen(DateTimeZone timeZone)
        {
            var dayOfWeeks = Enum.GetValues(typeof(DayOfWeek)).OfType<DayOfWeek>();
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
            Dictionary<DayOfWeek, LocalMarketHours> marketHoursForEachDayOfWeek,
            IReadOnlyDictionary<DateTime, TimeSpan> earlyCloses,
            IReadOnlyDictionary<DateTime, TimeSpan> lateOpens,
            IEnumerable<DateTime> bankHolidayDates = null)
        {
            TimeZone = timeZone;
            _holidays = holidayDates.Select(x => x.Date.Ticks).ToHashSet();
            _bankHolidays = (bankHolidayDates ?? Enumerable.Empty<DateTime>()).Select(x => x.Date.Ticks).ToHashSet();
            _earlyCloses = earlyCloses;
            _lateOpens = lateOpens;
            _openHoursByDay = marketHoursForEachDayOfWeek;

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

            IsMarketAlwaysOpen = CheckIsMarketAlwaysOpen();
        }

        /// <summary>
        /// Determines if the exchange is open at the specified local date time.
        /// </summary>
        /// <param name="localDateTime">The time to check represented as a local time</param>
        /// <param name="extendedMarketHours">True to use the extended market hours, false for just regular market hours</param>
        /// <returns>True if the exchange is considered open at the specified time, false otherwise</returns>
        public bool IsOpen(DateTime localDateTime, bool extendedMarketHours)
        {
            return GetMarketHours(localDateTime).IsOpen(localDateTime.TimeOfDay, extendedMarketHours);
        }

        /// <summary>
        /// Determines if the exchange is open at any point in time over the specified interval.
        /// </summary>
        /// <param name="startLocalDateTime">The start of the interval in local time</param>
        /// <param name="endLocalDateTime">The end of the interval in local time</param>
        /// <param name="extendedMarketHours">True to use the extended market hours, false for just regular market hours</param>
        /// <returns>True if the exchange is considered open at the specified time, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOpen(DateTime startLocalDateTime, DateTime endLocalDateTime, bool extendedMarketHours)
        {
            if (startLocalDateTime == endLocalDateTime)
            {
                // if we're testing an instantaneous moment, use the other function
                return IsOpen(startLocalDateTime, extendedMarketHours);
            }

            // we must make intra-day requests to LocalMarketHours, so check for a day gap
            var start = startLocalDateTime;
            var end = new DateTime(Math.Min(endLocalDateTime.Ticks, start.Date.Ticks + Time.OneDay.Ticks - 1));
            do
            {
                if (!_holidays.Contains(start.Date.Ticks))
                {
                    // check to see if the market is open
                    var marketHours = GetMarketHours(start);
                    if (marketHours.IsOpen(start.TimeOfDay, end.TimeOfDay, extendedMarketHours))
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
        /// <param name="extendedMarketHours">True to consider days with extended market hours only as open</param>
        /// <returns>True if the exchange will be open on the specified date, false otherwise</returns>
        public bool IsDateOpen(DateTime localDateTime, bool extendedMarketHours = false)
        {
            var marketHours = GetMarketHours(localDateTime);
            if (marketHours.IsClosedAllDay)
            {
                // if we don't have hours for this day then we're not open
                return false;
            }

            if (marketHours.MarketDuration == TimeSpan.Zero)
            {
                // this date only has extended market hours, like sunday for futures, so we only return true if 'extendedMarketHours'
                return extendedMarketHours;
            }
            return true;
        }

        /// <summary>
        /// Gets the local date time corresponding to the first market open to the specified previous date
        /// </summary>
        /// <param name="localDateTime">The time to begin searching for the last market open (non-inclusive)</param>
        /// <param name="extendedMarketHours">True to include extended market hours in the search</param>
        /// <returns>The previous market opening date time to the specified local date time</returns>
        public DateTime GetFirstDailyMarketOpen(DateTime localDateTime, bool extendedMarketHours)
        {
            return GetPreviousMarketOpen(localDateTime, extendedMarketHours, firstOpen: true);
        }

        /// <summary>
        /// Gets the local date time corresponding to the previous market open to the specified time
        /// </summary>
        /// <param name="localDateTime">The time to begin searching for the last market open (non-inclusive)</param>
        /// <param name="extendedMarketHours">True to include extended market hours in the search</param>
        /// <returns>The previous market opening date time to the specified local date time</returns>
        public DateTime GetPreviousMarketOpen(DateTime localDateTime, bool extendedMarketHours)
        {
            return GetPreviousMarketOpen(localDateTime, extendedMarketHours, firstOpen: false);
        }

        /// <summary>
        /// Gets the local date time corresponding to the previous market open to the specified time
        /// </summary>
        /// <param name="localDateTime">The time to begin searching for the last market open (non-inclusive)</param>
        /// <param name="extendedMarketHours">True to include extended market hours in the search</param>
        /// <returns>The previous market opening date time to the specified local date time</returns>
        public DateTime GetPreviousMarketOpen(DateTime localDateTime, bool extendedMarketHours, bool firstOpen)
        {
            var time = localDateTime;
            var marketHours = GetMarketHours(time);
            var nextMarketOpen = GetNextMarketOpen(time, extendedMarketHours);

            if (localDateTime == nextMarketOpen)
            {
                return localDateTime;
            }

            // let's loop for a week
            for (int i = 0; i < 7; i++)
            {
                DateTime? potentialResult = null;
                foreach (var segment in marketHours.Segments.Reverse())
                {
                    if ((time.Date + segment.Start <= localDateTime) &&
                        (segment.State == MarketHoursState.Market || extendedMarketHours))
                    {
                        var timeOfDay = time.Date + segment.Start;
                        if (firstOpen)
                        {
                            potentialResult = timeOfDay;
                        }
                        // Check the current segment is not part of another segment before
                        else if (GetNextMarketOpen(timeOfDay.AddTicks(-1), extendedMarketHours) == timeOfDay)
                        {
                            return timeOfDay;
                        }
                    }
                }

                if (potentialResult.HasValue)
                {
                    return potentialResult.Value;
                }

                time = time.AddDays(-1);
                marketHours = GetMarketHours(time);
            }

            throw new InvalidOperationException(Messages.SecurityExchangeHours.LastMarketOpenNotFound(localDateTime, IsMarketAlwaysOpen));
        }

        /// <summary>
        /// Gets the local date time corresponding to the next market open following the specified time
        /// </summary>
        /// <param name="localDateTime">The time to begin searching for market open (non-inclusive)</param>
        /// <param name="extendedMarketHours">True to include extended market hours in the search</param>
        /// <returns>The next market opening date time following the specified local date time</returns>
        public DateTime GetNextMarketOpen(DateTime localDateTime, bool extendedMarketHours)
        {
            var time = localDateTime;
            var oneWeekLater = localDateTime.Date.AddDays(15);

            var lastDay = time.Date.AddDays(-1);
            var lastDayMarketHours = GetMarketHours(lastDay);
            var lastDaySegment = lastDayMarketHours.Segments.LastOrDefault();
            do
            {
                var marketHours = GetMarketHours(time);
                if (!marketHours.IsClosedAllDay && !_holidays.Contains(time.Date.Ticks))
                {
                    var marketOpenTimeOfDay = marketHours.GetMarketOpen(time.TimeOfDay, extendedMarketHours, lastDaySegment?.End);
                    if (marketOpenTimeOfDay.HasValue)
                    {
                        var marketOpen = time.Date + marketOpenTimeOfDay.Value;
                        if (localDateTime < marketOpen)
                        {
                            return marketOpen;
                        }
                    }

                    // If there was an early close the market opens until next day first segment,
                    // so we don't take into account continuous segments between days, then
                    // lastDaySegment should be null
                    if (_earlyCloses.ContainsKey(time.Date))
                    {
                        lastDaySegment = null;
                    }
                    else
                    {
                        lastDaySegment = marketHours.Segments.LastOrDefault();
                    }
                }
                else
                {
                    lastDaySegment = null;
                }

                time = time.Date + Time.OneDay;
            }
            while (time < oneWeekLater);

            throw new ArgumentException(Messages.SecurityExchangeHours.UnableToLocateNextMarketOpenInTwoWeeks(IsMarketAlwaysOpen));
        }

        /// <summary>
        /// Gets the local date time corresponding to the last market close following the specified date
        /// </summary>
        /// <param name="localDateTime">The time to begin searching for market close (non-inclusive)</param>
        /// <param name="extendedMarketHours">True to include extended market hours in the search</param>
        /// <returns>The next market closing date time following the specified local date time</returns>
        public DateTime GetLastDailyMarketClose(DateTime localDateTime, bool extendedMarketHours)
        {
            return GetNextMarketClose(localDateTime, extendedMarketHours, lastClose: true);
        }

        /// <summary>
        /// Gets the local date time corresponding to the next market close following the specified time
        /// </summary>
        /// <param name="localDateTime">The time to begin searching for market close (non-inclusive)</param>
        /// <param name="extendedMarketHours">True to include extended market hours in the search</param>
        /// <returns>The next market closing date time following the specified local date time</returns>
        public DateTime GetNextMarketClose(DateTime localDateTime, bool extendedMarketHours)
        {
            return GetNextMarketClose(localDateTime, extendedMarketHours, lastClose: false);
        }

        /// <summary>
        /// Gets the local date time corresponding to the next market close following the specified time
        /// </summary>
        /// <param name="localDateTime">The time to begin searching for market close (non-inclusive)</param>
        /// <param name="extendedMarketHours">True to include extended market hours in the search</param>
        /// <param name="lastClose">True if the last available close of the date should be returned, else the first will be used</param>
        /// <returns>The next market closing date time following the specified local date time</returns>
        public DateTime GetNextMarketClose(DateTime localDateTime, bool extendedMarketHours, bool lastClose)
        {
            var time = localDateTime;
            var oneWeekLater = localDateTime.Date.AddDays(15);
            do
            {
                var marketHours = GetMarketHours(time);
                if (!marketHours.IsClosedAllDay && !_holidays.Contains(time.Date.Ticks))
                {
                    // Get next day first segment. This is made because we need to check the segment returned
                    // by GetMarketClose() ends at segment.End and not continues in the next segment. We get
                    // the next day first segment for the case in which the next market close is the last segment
                    // of the current day
                    var nextSegment = GetNextOrPreviousSegment(time, isNextDay: true);
                    var marketCloseTimeOfDay = marketHours.GetMarketClose(time.TimeOfDay, extendedMarketHours, lastClose, nextSegment?.Start);
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

            throw new ArgumentException(Messages.SecurityExchangeHours.UnableToLocateNextMarketCloseInTwoWeeks(IsMarketAlwaysOpen));
        }

        /// <summary>
        /// Returns next day first segment or previous day last segment
        /// </summary>
        /// <param name="time">Time of reference</param>
        /// <param name="isNextDay">True to get next day first segment. False to get previous day last segment</param>
        /// <returns>Next day first segment or previous day last segment</returns>
        private MarketHoursSegment GetNextOrPreviousSegment(DateTime time, bool isNextDay)
        {
            var nextOrPrevious = isNextDay ? 1 : -1;
            var nextOrPreviousDay = time.Date.AddDays(nextOrPrevious);
            if (_earlyCloses.ContainsKey(nextOrPreviousDay.Date))
            {
                return null;
            }

            var segments = GetMarketHours(nextOrPreviousDay).Segments;
            return isNextDay ? segments.FirstOrDefault() : segments.LastOrDefault();
        }

        /// <summary>
        /// Check whether the market is always open or not
        /// </summary>
        /// <returns>True if the market is always open, false otherwise</returns>
        private bool CheckIsMarketAlwaysOpen()
        {
            LocalMarketHours marketHours = null;
            for (var i = 0; i < daysOfWeek.Count; i++)
            {
                var day = daysOfWeek[i];
                switch (day)
                {
                    case DayOfWeek.Sunday:
                        marketHours = _sunday;
                        break;
                    case DayOfWeek.Monday:
                        marketHours = _monday;
                        break;
                    case DayOfWeek.Tuesday:
                        marketHours = _tuesday;
                        break;
                    case DayOfWeek.Wednesday:
                        marketHours = _wednesday;
                        break;
                    case DayOfWeek.Thursday:
                        marketHours = _thursday;
                        break;
                    case DayOfWeek.Friday:
                        marketHours = _friday;
                        break;
                    case DayOfWeek.Saturday:
                        marketHours = _saturday;
                        break;
                }

                if (!marketHours.IsOpenAllDay)
                {
                    return false;
                }
            }

            return true;
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
        /// <param name="localDateTime">The local date time to retrieve market hours for</param>
        /// <remarks>
        /// This method will return an adjusted instance of <see cref="LocalMarketHours"/> for the specified date,
        /// that is, it will account for holidays, early closes, and late opens (e.g. if the security trades regularly on Mondays,
        /// but a specific Monday is a holiday, this method will return a <see cref="LocalMarketHours"/> that is closed all day).
        /// In order to get the regular schedule, use the <see cref="MarketHours"/> property.
        /// </remarks>
        public LocalMarketHours GetMarketHours(DateTime localDateTime)
        {
            if (_holidays.Contains(localDateTime.Date.Ticks))
            {
                return LocalMarketHours.ClosedAllDay(localDateTime.DayOfWeek);
            }

            LocalMarketHours marketHours;
            switch (localDateTime.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    marketHours = _sunday;
                    break;
                case DayOfWeek.Monday:
                    marketHours = _monday;
                    break;
                case DayOfWeek.Tuesday:
                    marketHours = _tuesday;
                    break;
                case DayOfWeek.Wednesday:
                    marketHours = _wednesday;
                    break;
                case DayOfWeek.Thursday:
                    marketHours = _thursday;
                    break;
                case DayOfWeek.Friday:
                    marketHours = _friday;
                    break;
                case DayOfWeek.Saturday:
                    marketHours = _saturday;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(localDateTime), localDateTime, null);
            }

            var hasEarlyClose = _earlyCloses.TryGetValue(localDateTime.Date, out var earlyCloseTime);
            var hasLateOpen = _lateOpens.TryGetValue(localDateTime.Date, out var lateOpenTime);
            if (!hasEarlyClose && !hasLateOpen)
            {
                return marketHours;
            }

            IReadOnlyList<MarketHoursSegment> marketHoursSegments = marketHours.Segments;

            // If the earlyCloseTime is between a segment, change the close time with it
            // and add it after the segments prior to the earlyCloseTime
            // Otherwise, just take the segments prior to the earlyCloseTime
            List<MarketHoursSegment> segmentsEarlyClose = null;
            if (hasEarlyClose)
            {
                var index = marketHoursSegments.Count;
                MarketHoursSegment newSegment = null;
                for (var i = 0; i < marketHoursSegments.Count; i++)
                {
                    var segment = marketHoursSegments[i];
                    if (segment.Start <= earlyCloseTime && earlyCloseTime <= segment.End)
                    {
                        newSegment = new MarketHoursSegment(segment.State, segment.Start, earlyCloseTime);
                        index = i;
                        break;
                    }
                    else if (earlyCloseTime < segment.Start)
                    {
                        // we will drop any remaining segment starting by this one
                        index = i - 1;
                        break;
                    }
                }

                segmentsEarlyClose = new List<MarketHoursSegment>(marketHoursSegments.Take(index));
                if (newSegment != null)
                {
                    segmentsEarlyClose.Add(newSegment);
                }
            }

            // It could be the case we have a late open after an early close (the market resumes after the early close), in that case, we should take
            // the segments before the early close and the the segments after the late opens and append them. Therefore, if that's not the case, this is,
            // if there was an early close but there is not a late open or it's before the early close, we need to update the variable marketHours with
            // the value of newMarketHours, so that it contains the segments before the early close
            if (segmentsEarlyClose != null && (!hasLateOpen || earlyCloseTime >= lateOpenTime))
            {
                marketHoursSegments = segmentsEarlyClose;
            }

            // If the lateOpenTime is between a segment, change the start time with it
            // and add it before the segments previous to the lateOpenTime
            // Otherwise, just take the segments previous to the lateOpenTime
            List<MarketHoursSegment> segmentsLateOpen = null;
            if (hasLateOpen)
            {
                var index = 0;
                segmentsLateOpen = new List<MarketHoursSegment>();
                for (var i = 0; i < marketHoursSegments.Count; i++)
                {
                    var segment = marketHoursSegments[i];
                    if (segment.Start <= lateOpenTime && lateOpenTime <= segment.End)
                    {
                        segmentsLateOpen.Add(new(segment.State, lateOpenTime, segment.End));
                        index = i + 1;
                        break;
                    }
                    else if (lateOpenTime < segment.Start)
                    {
                        index = i;
                        break;
                    }
                }

                segmentsLateOpen.AddRange(marketHoursSegments.TakeLast(marketHoursSegments.Count - index));
                marketHoursSegments = segmentsLateOpen;
            }

            // Since it could be the case we have a late open after an early close (the market resumes after the early close), we need to take
            // the segments before the early close and the segments after the late open and append them to obtain the expected market hours
            if (segmentsEarlyClose != null && hasLateOpen && earlyCloseTime <= lateOpenTime)
            {
                segmentsEarlyClose.AddRange(segmentsLateOpen);
                marketHoursSegments = segmentsEarlyClose;
            }

            return new LocalMarketHours(localDateTime.DayOfWeek, marketHoursSegments);
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

        /// <summary>
        /// Sets the exchange hours to be the same as the given exchange hours without changing the reference
        /// </summary>
        /// <param name="other">The hours to set</param>
        internal void Update(SecurityExchangeHours other)
        {
            if (other == null)
            {
                return;
            }

            _holidays = other._holidays;
            _earlyCloses = other._earlyCloses;
            _lateOpens = other._lateOpens;
            _sunday = other._sunday;
            _monday = other._monday;
            _tuesday = other._tuesday;
            _wednesday = other._wednesday;
            _thursday = other._thursday;
            _friday = other._friday;
            _saturday = other._saturday;
            _openHoursByDay = other._openHoursByDay;
            TimeZone = other.TimeZone;
            RegularMarketDuration = other.RegularMarketDuration;
            IsMarketAlwaysOpen = other.IsMarketAlwaysOpen;
        }
    }
}
