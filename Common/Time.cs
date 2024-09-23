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
using System.Globalization;
using Newtonsoft.Json.Converters;
using NodaTime;
using QuantConnect.Logging;
using QuantConnect.Securities;
using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Time helper class collection for working with trading dates
    /// </summary>
    public static class Time
    {
        /// <summary>
        /// Allows specifying an offset to trigger the tradable date event
        /// </summary>
        /// <remarks>Useful for delaying the tradable date event until new auxiliary data is available to refresh map and factor files</remarks>
        public static TimeSpan LiveAuxiliaryDataOffset { get; set; } = TimeSpan.FromHours(8);

        /// <summary>
        /// Provides a value far enough in the future the current computer hardware will have decayed :)
        /// </summary>
        /// <value>
        /// new DateTime(2050, 12, 31)
        /// </value>
        public static readonly DateTime EndOfTime = new DateTime(2050, 12, 31);

        /// <summary>
        /// Provides a time span based on <see cref="EndOfTime"/>
        /// </summary>
        public static TimeSpan EndOfTimeTimeSpan = new TimeSpan(EndOfTime.Ticks);

        /// <summary>
        /// Provides a common and normalized start time for Lean data
        /// </summary>
        public static readonly DateTime Start = new DateTime(1998, 1, 2);

        /// <summary>
        /// Provides a value far enough in the past that can be used as a lower bound on dates, 12/30/1899
        /// </summary>
        /// <value>
        /// DateTime.FromOADate(0)
        /// </value>
        public static readonly DateTime BeginningOfTime = DateTime.FromOADate(0);

        /// <summary>
        /// Provides a value large enough that we won't hit the limit, while small enough
        /// we can still do math against it without checking everywhere for <see cref="TimeSpan.MaxValue"/>
        /// </summary>
        public static readonly TimeSpan MaxTimeSpan = TimeSpan.FromDays(1000*365);

        /// <summary>
        /// One Year TimeSpan Period Constant
        /// </summary>
        /// <remarks>365 days</remarks>
        public static readonly TimeSpan OneYear = TimeSpan.FromDays(365);

        /// <summary>
        /// One Day TimeSpan Period Constant
        /// </summary>
        public static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

        /// <summary>
        /// One Hour TimeSpan Period Constant
        /// </summary>
        public static readonly TimeSpan OneHour = TimeSpan.FromHours(1);

        /// <summary>
        /// One Minute TimeSpan Period Constant
        /// </summary>
        public static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        /// <summary>
        /// One Second TimeSpan Period Constant
        /// </summary>
        public static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        /// <summary>
        /// One Millisecond TimeSpan Period Constant
        /// </summary>
        public static readonly TimeSpan OneMillisecond = TimeSpan.FromMilliseconds(1);

        /// <summary>
        /// Live charting is sensitive to timezone so need to convert the local system time to a UTC and display in browser as UTC.
        /// </summary>
        public struct DateTimeWithZone
        {
            private readonly DateTime utcDateTime;
            private readonly TimeZoneInfo timeZone;

            /// <summary>
            /// Initializes a new instance of the <see cref="QuantConnect.Time.DateTimeWithZone"/> struct.
            /// </summary>
            /// <param name="dateTime">Date time.</param>
            /// <param name="timeZone">Time zone.</param>
            public DateTimeWithZone(DateTime dateTime, TimeZoneInfo timeZone)
            {
                utcDateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, timeZone);
                this.timeZone = timeZone;
            }

            /// <summary>
            /// Gets the universal time.
            /// </summary>
            /// <value>The universal time.</value>
            public DateTime UniversalTime { get { return utcDateTime; } }

            /// <summary>
            /// Gets the time zone.
            /// </summary>
            /// <value>The time zone.</value>
            public TimeZoneInfo TimeZone { get { return timeZone; } }

            /// <summary>
            /// Gets the local time.
            /// </summary>
            /// <value>The local time.</value>
            public DateTime LocalTime
            {
                get
                {
                    return TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
                }
            }
        }

        private static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private const long SecondToMillisecond = 1000;

        /// <summary>
        /// Helper method to get the new live auxiliary data due time
        /// </summary>
        /// <returns>The due time for the new auxiliary data emission</returns>
        public static TimeSpan GetNextLiveAuxiliaryDataDueTime()
        {
            return GetNextLiveAuxiliaryDataDueTime(DateTime.UtcNow);
        }

        /// <summary>
        /// Helper method to get the new live auxiliary data due time
        /// </summary>
        /// <param name="utcNow">The current utc time</param>
        /// <returns>The due time for the new auxiliary data emission</returns>
        public static TimeSpan GetNextLiveAuxiliaryDataDueTime(DateTime utcNow)
        {
            var nowNewYork = utcNow.ConvertFromUtc(TimeZones.NewYork);
            if (nowNewYork.TimeOfDay < LiveAuxiliaryDataOffset)
            {
                return LiveAuxiliaryDataOffset - nowNewYork.TimeOfDay;
            }
            return nowNewYork.Date.AddDays(1).Add(+LiveAuxiliaryDataOffset) - nowNewYork;
        }

        /// <summary>
        /// Helper method to adjust a waiting time, in milliseconds, so it's uneven with the second turn around
        /// </summary>
        /// <param name="waitTimeMillis">The desired wait time</param>
        /// <remarks>This is useful for real time performance in live trading. We want to avoid adding unnecessary cpu usage,
        /// during periods where we know there will be cpu time demand, like a second turn around where data is emitted.</remarks>
        /// <returns>The adjusted wait time</returns>
        public static int GetSecondUnevenWait(int waitTimeMillis)
        {
            return DateTime.UtcNow.GetSecondUnevenWait(waitTimeMillis);
        }

        /// <summary>
        /// Helper method to adjust a waiting time, in milliseconds, so it's uneven with the second turn around
        /// </summary>
        /// <param name="now">The current time</param>
        /// <param name="waitTimeMillis">The desired wait time</param>
        /// <remarks>This is useful for real time performance in live trading. We want to avoid adding unnecessary cpu usage,
        /// during periods where we know there will be cpu time demand, like a second turn around where data is emitted.</remarks>
        /// <returns>The adjusted wait time</returns>
        public static int GetSecondUnevenWait(this DateTime now, int waitTimeMillis)
        {
            var wakeUpTime = now.AddMilliseconds(waitTimeMillis);
            if (wakeUpTime.Millisecond < 100 || wakeUpTime.Millisecond > 900)
            {
                // if we are going to wake before/after the next second we add an offset to avoid it
                var offsetMillis = waitTimeMillis >= 1000 ? 500 : 100;
                return waitTimeMillis + offsetMillis;
            }
            return waitTimeMillis;
        }

        /// <summary>
        /// Create a C# DateTime from a UnixTimestamp
        /// </summary>
        /// <param name="unixTimeStamp">Double unix timestamp (Time since Midnight Jan 1 1970)</param>
        /// <returns>C# date timeobject</returns>
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime time;
            try
            {
                var ticks = unixTimeStamp * TimeSpan.TicksPerSecond;
                time = EpochTime.AddTicks((long)ticks);
            }
            catch (Exception err)
            {
                Log.Error(err, Invariant($"UnixTimeStamp: {unixTimeStamp}"));
                time = DateTime.Now;
            }
            return time;
        }

        /// <summary>
        /// Create a C# DateTime from a UnixTimestamp
        /// </summary>
        /// <param name="unixTimeStamp">Decimal unix timestamp (Time since Midnight Jan 1 1970)</param>
        /// <returns>C# date time object</returns>
        public static DateTime UnixTimeStampToDateTime(decimal unixTimeStamp)
        {
            return UnixMillisecondTimeStampToDateTime(unixTimeStamp * SecondToMillisecond);
        }

        /// <summary>
        /// Create a C# DateTime from a UnixTimestamp
        /// </summary>
        /// <param name="unixTimeStamp">Long unix timestamp (Time since Midnight Jan 1 1970)</param>
        /// <returns>C# date time object</returns>
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            return UnixTimeStampToDateTime(Convert.ToDecimal(unixTimeStamp));
        }

        /// <summary>
        /// Create a C# DateTime from a UnixTimestamp
        /// </summary>
        /// <param name="unixTimeStamp">Decimal unix timestamp (Time since Midnight Jan 1 1970) in milliseconds</param>
        /// <returns>C# date time object</returns>
        public static DateTime UnixMillisecondTimeStampToDateTime(decimal unixTimeStamp)
        {
            DateTime time;
            try
            {
                // Any residual decimal numbers that remain are nanoseconds from [0, 100) nanoseconds.
                // If we cast to (long), only the integer component of the decimal is taken, and can
                // potentially result in look-ahead bias in increments of 100 nanoseconds, i.e. 1 DateTime tick.
                var ticks = Math.Ceiling(unixTimeStamp * TimeSpan.TicksPerMillisecond);
                time = EpochTime.AddTicks((long)ticks);
            }
            catch (Exception err)
            {
                Log.Error(err, Invariant($"UnixTimeStamp: {unixTimeStamp}"));
                time = DateTime.Now;
            }
            return time;
        }

        /// <summary>
        /// Create a C# DateTime from a UnixTimestamp
        /// </summary>
        /// <param name="unixTimeStamp">Int64 unix timestamp (Time since Midnight Jan 1 1970) in nanoseconds</param>
        /// <returns>C# date time object</returns>
        public static DateTime UnixNanosecondTimeStampToDateTime(long unixTimeStamp)
        {
            DateTime time;
            try
            {
                var ticks = unixTimeStamp / 100;
                time = EpochTime.AddTicks(ticks);
            }
            catch (Exception err)
            {
                Log.Error(err, Invariant($"UnixTimeStamp: {unixTimeStamp}"));
                time = DateTime.Now;
            }
            return time;
        }

        /// <summary>
        /// Convert a Datetime to Unix Timestamp
        /// </summary>
        /// <param name="time">C# datetime object</param>
        /// <returns>Double unix timestamp</returns>
        public static double DateTimeToUnixTimeStamp(DateTime time)
        {
            double timestamp = 0;
            try
            {
                timestamp = (time - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
            }
            catch (Exception err)
            {
                Log.Error(err, Invariant($"{time:o}"));
            }
            return timestamp;
        }

        /// <summary>
        /// Convert a Datetime to Unix Timestamp
        /// </summary>
        /// <param name="time">C# datetime object</param>
        /// <returns>Double unix timestamp</returns>
        public static double DateTimeToUnixTimeStampMilliseconds(DateTime time)
        {
            double timestamp = 0;
            try
            {
                timestamp = (time - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds;
            }
            catch (Exception err)
            {
                Log.Error(err, Invariant($"{time:o}"));
            }
            return timestamp;
        }

        /// <summary>
        /// Convert a Datetime to Unix Timestamp
        /// </summary>
        /// <param name="time">C# datetime object</param>
        /// <returns>Int64 unix timestamp</returns>
        public static long DateTimeToUnixTimeStampNanoseconds(DateTime time)
        {
            long timestamp = 0;
            try
            {
                timestamp = (time - new DateTime(1970, 1, 1, 0, 0, 0, 0)).Ticks * 100;
            }
            catch (Exception err)
            {
                Log.Error(err, Invariant($"{time:o}"));
            }
            return timestamp;
        }

        /// <summary>
        /// Get the current time as a unix timestamp
        /// </summary>
        /// <returns>Double value of the unix as UTC timestamp</returns>
        public static double TimeStamp()
        {
            return DateTimeToUnixTimeStamp(DateTime.UtcNow);
        }

        /// <summary>
        /// Returns the timespan with the larger value
        /// </summary>
        public static TimeSpan Max(TimeSpan one, TimeSpan two)
        {
            return TimeSpan.FromTicks(Math.Max(one.Ticks, two.Ticks));
        }

        /// <summary>
        /// Returns the timespan with the smaller value
        /// </summary>
        public static TimeSpan Min(TimeSpan one, TimeSpan two)
        {
            return TimeSpan.FromTicks(Math.Min(one.Ticks, two.Ticks));
        }

        /// <summary>
        /// Returns the larger of two date times
        /// </summary>
        public static DateTime Max(DateTime one, DateTime two)
        {
            return one > two ? one : two;
        }

        /// <summary>
        /// Returns the smaller of two date times
        /// </summary>
        public static DateTime Min(DateTime one, DateTime two)
        {
            return one < two ? one : two;
        }

        /// <summary>
        /// Multiplies the specified interval by the multiplier
        /// </summary>
        /// <param name="interval">The interval to be multiplied, such as TimeSpan.FromSeconds(1)</param>
        /// <param name="multiplier">The number of times to multiply the interval</param>
        /// <returns>The multiplied interval, such as 1s*5 = 5s</returns>
        public static TimeSpan Multiply(this TimeSpan interval, double multiplier)
        {
            return TimeSpan.FromTicks((long) (interval.Ticks * multiplier));
        }

        /// <summary>
        /// Parse a standard YY MM DD date into a DateTime. Attempt common date formats
        /// </summary>
        /// <param name="dateToParse">String date time to parse</param>
        /// <returns>Date time</returns>
        public static DateTime ParseDate(string dateToParse)
        {
            try
            {
                //First try the exact options:
                DateTime date;
                if (DateTime.TryParseExact(dateToParse, DateFormat.SixCharacter, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
                if (DateTime.TryParseExact(dateToParse, DateFormat.EightCharacter, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
                if (DateTime.TryParseExact(dateToParse, DateFormat.TwelveCharacter, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
                if (DateTime.TryParseExact(dateToParse.SafeSubstring(0, 19), DateFormat.JsonFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
                if (DateTime.TryParseExact(dateToParse, DateFormat.USShort, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
                if (DateTime.TryParseExact(dateToParse, DateFormat.USShortDateOnly, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
                if (DateTime.TryParseExact(dateToParse, DateFormat.US, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
                if (DateTime.TryParseExact(dateToParse, DateFormat.USDateOnly, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
                if (DateTime.TryParse(dateToParse, out date))
                {
                    return date;
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            return DateTime.Now;
        }

        /// <summary>
        /// Parse a standard YY MM DD date into a DateTime. Attempt common date formats
        /// </summary>
        /// <param name="dateToParse">String date time to parse</param>
        /// <returns>Date time</returns>
        public static DateTime ParseFIXUtcTimestamp(string dateToParse)
        {
            try
            {
                //First try the exact options:
                DateTime date;
                if (DateTime.TryParseExact(dateToParse, DateFormat.FIX, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
                if (DateTime.TryParseExact(dateToParse, DateFormat.FIXWithMillisecond, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            return DateTime.UtcNow;
        }

        /// <summary>
        /// Define an enumerable date time range using the given time step
        /// </summary>
        /// <param name="from">DateTime start date time</param>
        /// <param name="thru">DateTime end date time</param>
        /// <returns>Enumerable date time range</returns>
        public static IEnumerable<DateTime> DateTimeRange(DateTime from, DateTime thru, TimeSpan step)
        {
            for (var dateTime = from; dateTime <= thru; dateTime = dateTime.Add(step))
                yield return dateTime;
        }

        /// <summary>
        /// Define an enumerable date range and return each date as a datetime object in the date range
        /// </summary>
        /// <param name="from">DateTime start date</param>
        /// <param name="thru">DateTime end date</param>
        /// <returns>Enumerable date range</returns>
        public static IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
        {
            return DateTimeRange(from.Date, thru.Date, TimeSpan.FromDays(1));
        }


        /// <summary>
        /// Define an enumerable date range of tradeable dates - skip the holidays and weekends when securities in this algorithm don't trade.
        /// </summary>
        /// <param name="securities">Securities we have in portfolio</param>
        /// <param name="from">Start date</param>
        /// <param name="thru">End date</param>
        /// <returns>Enumerable date range</returns>
        public static IEnumerable<DateTime> EachTradeableDay(ICollection<Security> securities, DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
            {
                if (TradableDate(securities, day))
                {
                    yield return day;
                }
            }
        }


        /// <summary>
        /// Define an enumerable date range of tradeable dates - skip the holidays and weekends when securities in this algorithm don't trade.
        /// </summary>
        /// <param name="security">The security to get tradeable dates for</param>
        /// <param name="from">Start date</param>
        /// <param name="thru">End date</param>
        /// <param name="extendedMarketHours">True to include days with extended market hours only, like sunday for futures</param>
        /// <returns>Enumerable date range</returns>
        public static IEnumerable<DateTime> EachTradeableDay(Security security, DateTime from, DateTime thru, bool extendedMarketHours = false)
        {
            return EachTradeableDay(security.Exchange.Hours, from, thru, extendedMarketHours);
        }

        /// <summary>
        /// Define an enumerable date range of tradeable dates - skip the holidays and weekends when securities in this algorithm don't trade.
        /// </summary>
        /// <param name="exchange">The security to get tradeable dates for</param>
        /// <param name="from">Start date</param>
        /// <param name="thru">End date</param>
        /// <param name="extendedMarketHours">True to include days with extended market hours only, like sunday for futures</param>
        /// <returns>Enumerable date range</returns>
        public static IEnumerable<DateTime> EachTradeableDay(SecurityExchangeHours exchange, DateTime from, DateTime thru, bool extendedMarketHours = false)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
            {
                if (exchange.IsDateOpen(day, extendedMarketHours))
                {
                    yield return day;
                }
            }
        }

        /// <summary>
        /// Define an enumerable date range of tradeable dates but expressed in a different time zone.
        /// </summary>
        /// <remarks>
        /// This is mainly used to bridge the gap between exchange time zone and data time zone for file written to disk. The returned
        /// enumerable of dates is guaranteed to be the same size or longer than those generated via <see cref="EachTradeableDay(ICollection{Security},DateTime,DateTime)"/>
        /// </remarks>
        /// <param name="exchange">The exchange hours</param>
        /// <param name="from">The start time in the exchange time zone</param>
        /// <param name="thru">The end time in the exchange time zone (inclusive of the final day)</param>
        /// <param name="timeZone">The timezone to project the dates into (inclusive of the final day)</param>
        /// <param name="includeExtendedMarketHours">True to include extended market hours trading in the search, false otherwise</param>
        /// <returns></returns>
        public static IEnumerable<DateTime> EachTradeableDayInTimeZone(SecurityExchangeHours exchange, DateTime from, DateTime thru, DateTimeZone timeZone, bool includeExtendedMarketHours = true)
        {
            var currentExchangeTime = from;
            thru = thru.Date.AddDays(1); // we want to include the full thru date
            while (currentExchangeTime < thru)
            {
                // take steps of max size of one day in the data time zone
                var currentInTimeZone = currentExchangeTime.ConvertTo(exchange.TimeZone, timeZone);
                var currentInTimeZoneEod = currentInTimeZone.Date.AddDays(1);

                var currentExchangeTimeEod = currentInTimeZoneEod.ConvertTo(timeZone, exchange.TimeZone);

                // don't pass the end
                if (currentExchangeTimeEod > thru)
                {
                    currentExchangeTimeEod = thru;
                }

                // perform market open checks in the exchange time zone
                if (exchange.IsOpen(currentExchangeTime, currentExchangeTimeEod, includeExtendedMarketHours))
                {
                    yield return currentInTimeZone.Date;
                }

                currentExchangeTime = currentExchangeTimeEod;
            }
        }

        /// <summary>
        /// Make sure this date is not a holiday, or weekend for the securities in this algorithm.
        /// </summary>
        /// <param name="securities">Security manager from the algorithm</param>
        /// <param name="day">DateTime to check if trade-able.</param>
        /// <returns>True if tradeable date</returns>
        public static bool TradableDate(IEnumerable<Security> securities, DateTime day)
        {
            try
            {
                foreach (var security in securities)
                {
                    if (security.Exchange.DateIsOpen(day.Date)) return true;
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            return false;
        }


        /// <summary>
        /// Could of the number of tradeable dates within this period.
        /// </summary>
        /// <param name="securities">Securities we're trading</param>
        /// <param name="start">Start of Date Loop</param>
        /// <param name="finish">End of Date Loop</param>
        /// <returns>Number of dates</returns>
        public static int TradeableDates(ICollection<Security> securities, DateTime start, DateTime finish)
        {
            var count = 0;
            Log.Trace(Invariant($"Time.TradeableDates(): {Messages.Time.SecurityCount(securities.Count)}"));
            try
            {
                foreach (var day in EachDay(start, finish))
                {
                    if (TradableDate(securities, day))
                    {
                        count++;
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            return count;
        }

        /// <summary>
        /// Determines the start time required to produce the requested number of bars and the given size
        /// </summary>
        /// <param name="exchangeHours">The exchange hours used to test for market open hours</param>
        /// <param name="end">The end time of the last bar over the requested period</param>
        /// <param name="barSize">The length of each bar</param>
        /// <param name="barCount">The number of bars requested</param>
        /// <param name="extendedMarketHours">True to allow extended market hours bars, otherwise false for only normal market hours</param>
        /// <param name="dataTimeZone">Timezone for this data</param>
        /// <param name="dailyPreciseEndTime">True if daily strict end times are enabled</param>
        /// <returns>The start time that would provide the specified number of bars ending at the specified end time, rounded down by the requested bar size</returns>
        public static DateTime GetStartTimeForTradeBars(SecurityExchangeHours exchangeHours, DateTime end, TimeSpan barSize, int barCount,
            bool extendedMarketHours, DateTimeZone dataTimeZone, bool dailyPreciseEndTime = false)
        {
            if (barSize <= TimeSpan.Zero)
            {
                throw new ArgumentException(Messages.Time.InvalidBarSize, nameof(barSize));
            }

            var current = end;
            if (dailyPreciseEndTime && barSize == OneDay)
            {
                if (exchangeHours.IsDateOpen(current) && exchangeHours.GetNextMarketClose(current.Date, extendedMarketHours) > current)
                {
                    // we round down, because data for today isn't ready/wont pass through current time.
                    // for example, for equities, current time is 3pm, 1 bar in daily should be yesterdays, today does not count
                    current = end.RoundDownInTimeZone(barSize, exchangeHours.TimeZone, dataTimeZone);
                }
            }
            else
            {
                // need to round down in data timezone because data is stored in this time zone but only if not doing daily resolution or
                // dailyPreciseEndTime is disabled because if we round down we might include 2 bars when we want 1, for example: say
                // current is monday 8pm NY, if we round down we get minight monday which will return false as open, so we will return
                // friday and monday data for daily equity, when we want only monday.
                current = end.RoundDownInTimeZone(barSize, exchangeHours.TimeZone, dataTimeZone);
            }

            for (int i = 0; i < barCount;)
            {
                var previous = current;
                current = current - barSize;
                if (exchangeHours.IsOpen(current, previous, extendedMarketHours))
                {
                    i++;
                }
            }
            return current;
        }

        /// <summary>
        /// Determines the end time at which the requested number of bars of the given  will have elapsed.
        /// NOTE: The start time is not discretized by barSize units like is done in <see cref="GetStartTimeForTradeBars"/>
        /// </summary>
        /// <param name="exchangeHours">The exchange hours used to test for market open hours</param>
        /// <param name="start">The end time of the last bar over the requested period</param>
        /// <param name="barSize">The length of each bar</param>
        /// <param name="barCount">The number of bars requested</param>
        /// <param name="extendedMarketHours">True to allow extended market hours bars, otherwise false for only normal market hours</param>
        /// <returns>The start time that would provide the specified number of bars ending at the specified end time, rounded down by the requested bar size</returns>
        public static DateTime GetEndTimeForTradeBars(SecurityExchangeHours exchangeHours, DateTime start, TimeSpan barSize, int barCount, bool extendedMarketHours)
        {
            if (barSize <= TimeSpan.Zero)
            {
                throw new ArgumentException(Messages.Time.InvalidBarSize, nameof(barSize));
            }

            var current = start;
            if (barSize == OneDay)
            {
                for (int i = 0; i < barCount;)
                {
                    current = current + OneDay;
                    if (exchangeHours.IsDateOpen(current))
                    {
                        i++;
                    }
                }

                return current;
            }

            for (int i = 0; i < barCount;)
            {
                var previous = current;
                current = current + barSize;
                if (exchangeHours.IsOpen(previous, current, extendedMarketHours))
                {
                    i++;
                }
            }
            return current;
        }

        /// <summary>
        /// Gets the number of trade bars of the specified <paramref name="barSize"/> that fit between the <paramref name="start"/> and <paramref name="end"/>
        /// </summary>
        /// <param name="exchangeHours">The exchange used to test for market open hours</param>
        /// <param name="start">The start time of the interval in the exchange time zone</param>
        /// <param name="end">The end time of the interval in the exchange time zone</param>
        /// <param name="barSize">The step size used to count number of bars between start and end</param>
        /// <returns>The number of bars of the specified size between start and end times</returns>
        public static int GetNumberOfTradeBarsInInterval(SecurityExchangeHours exchangeHours, DateTime start, DateTime end, TimeSpan barSize)
        {
            if (barSize <= TimeSpan.Zero)
            {
                throw new ArgumentException(Messages.Time.InvalidBarSize, nameof(barSize));
            }

            var count = 0;
            var current = start;
            if (barSize == OneDay)
            {
                while (current < end)
                {
                    if (exchangeHours.IsDateOpen(current))
                    {
                        count++;
                    }

                    current = current + OneDay;
                }

                return count;
            }

            while (current < end)
            {
                var previous = current;
                current = current + barSize;
                if (exchangeHours.IsOpen(previous, current, false))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Normalizes the current time within the specified period
        /// time = start => 0
        /// time = start + period => 1
        /// </summary>
        /// <param name="start">The start time of the range</param>
        /// <param name="current">The current time we seek to normalize</param>
        /// <param name="period">The time span of the range</param>
        /// <returns>The normalized time</returns>
        public static double NormalizeInstantWithinRange(DateTime start, DateTime current, TimeSpan period)
        {
            // normalization of a point time only has a value at that specific point
            if (period == TimeSpan.Zero)
            {
                return start == current ? 1 : 0;
            }

            var delta = (current - start).TotalSeconds;
            return delta / period.TotalSeconds;
        }

        /// <summary>
        /// Normalizes the step size as a percentage of the period.
        /// </summary>
        /// <param name="period">The period to normalize against</param>
        /// <param name="stepSize">The step size to be normaized</param>
        /// <returns>The normalized step size as a percentage of the period</returns>
        public static double NormalizeTimeStep(TimeSpan period, TimeSpan stepSize)
        {
            // normalization of a time step for an instantaneous period will always be zero
            if (period == TimeSpan.Zero)
            {
                return 0;
            }

            return stepSize.TotalSeconds / period.TotalSeconds;
        }

        /// <summary>
        /// Gets the absolute value of the specified time span
        /// </summary>
        /// <param name="timeSpan">Time span whose absolute value we seek</param>
        /// <returns>The absolute value of the specified time span</returns>
        public static TimeSpan Abs(this TimeSpan timeSpan)
        {
            return TimeSpan.FromTicks(Math.Abs(timeSpan.Ticks));
        }

        /// <summary>
        /// Helper method to deserialize month/year
        /// </summary>
        public class MonthYearJsonConverter : IsoDateTimeConverter
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public MonthYearJsonConverter()
            {
                DateTimeFormat = @"MM/yy";
            }
        }
    }
}
