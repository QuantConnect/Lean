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
using QuantConnect.Securities;
using System.Collections.Generic;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Helper class used to provide better syntax when defining time rules
    /// </summary>
    public class TimeRules : BaseScheduleRules
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeRules"/> helper class
        /// </summary>
        /// <param name="securities">The security manager</param>
        /// <param name="timeZone">The algorithm's default time zone</param>
        /// <param name="marketHoursDatabase">The market hours database instance to use</param>
        public TimeRules(SecurityManager securities, DateTimeZone timeZone, MarketHoursDatabase marketHoursDatabase)
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
        /// Specifies an event should fire at the current time
        /// </summary>
        public ITimeRule Now => new FuncTimeRule("Now", dates => {
            return dates.Select(date =>
            {
                // we ignore the given date and just use the current time, why? if the algorithm used 'DateRules.Today'
                // we get the algorithms first 'Date', which during warmup might not be a complete date, depending on the warmup period
                // and since Today returns dates we might get a time in the past which get's ignored. See 'WarmupTrainRegressionAlgorithm'
                // which reproduces GH issue #6410
                return Securities.UtcTime;
            });
        });

        /// <summary>
        /// Convenience property for running a scheduled event at midnight in the algorithm time zone
        /// </summary>
        public ITimeRule Midnight => new FuncTimeRule("Midnight", dates => dates.Select(date => date.ConvertToUtc(TimeZone)));

        /// <summary>
        /// Convenience property for running a scheduled event at noon in the algorithm time zone
        /// </summary>
        public ITimeRule Noon => new FuncTimeRule("Noon", dates => dates.Select(date => date.ConvertToUtc(TimeZone).AddHours(12)));

        /// <summary>
        /// Specifies an event should fire at the specified time of day in the algorithm's time zone
        /// </summary>
        /// <param name="timeOfDay">The time of day in the algorithm's time zone the event should fire</param>
        /// <returns>A time rule that fires at the specified time in the algorithm's time zone</returns>
        public ITimeRule At(TimeSpan timeOfDay)
        {
            return At(timeOfDay, TimeZone);
        }

        /// <summary>
        /// Specifies an event should fire at the specified time of day in the algorithm's time zone
        /// </summary>
        /// <param name="hour">The hour</param>
        /// <param name="minute">The minute</param>
        /// <param name="second">The second</param>
        /// <returns>A time rule that fires at the specified time in the algorithm's time zone</returns>
        public ITimeRule At(int hour, int minute, int second = 0)
        {
            return At(new TimeSpan(hour, minute, second), TimeZone);
        }

        /// <summary>
        /// Specifies an event should fire at the specified time of day in the specified time zone
        /// </summary>
        /// <param name="hour">The hour</param>
        /// <param name="minute">The minute</param>
        /// <param name="timeZone">The time zone the event time is represented in</param>
        /// <returns>A time rule that fires at the specified time in the algorithm's time zone</returns>
        public ITimeRule At(int hour, int minute, DateTimeZone timeZone)
        {
            return At(new TimeSpan(hour, minute, 0), timeZone);
        }

        /// <summary>
        /// Specifies an event should fire at the specified time of day in the specified time zone
        /// </summary>
        /// <param name="hour">The hour</param>
        /// <param name="minute">The minute</param>
        /// <param name="second">The second</param>
        /// <param name="timeZone">The time zone the event time is represented in</param>
        /// <returns>A time rule that fires at the specified time in the algorithm's time zone</returns>
        public ITimeRule At(int hour, int minute, int second, DateTimeZone timeZone)
        {
            return At(new TimeSpan(hour, minute, second), timeZone);
        }

        /// <summary>
        /// Specifies an event should fire at the specified time of day in the specified time zone
        /// </summary>
        /// <param name="timeOfDay">The time of day in the algorithm's time zone the event should fire</param>
        /// <param name="timeZone">The time zone the date time is expressed in</param>
        /// <returns>A time rule that fires at the specified time in the algorithm's time zone</returns>
        public ITimeRule At(TimeSpan timeOfDay, DateTimeZone timeZone)
        {
            var name = string.Join(",", timeOfDay.TotalHours.ToStringInvariant("0.##"));
            Func<IEnumerable<DateTime>, IEnumerable<DateTime>> applicator = dates =>
                from date in dates
                let localEventTime = date + timeOfDay
                let utcEventTime = localEventTime.ConvertToUtc(timeZone)
                select utcEventTime;

            return new FuncTimeRule(name, applicator);
        }

        /// <summary>
        /// Specifies an event should fire periodically on the requested interval
        /// </summary>
        /// <param name="interval">The frequency with which the event should fire, can not be zero or less</param>
        /// <returns>A time rule that fires after each interval passes</returns>
        public ITimeRule Every(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentException("TimeRules.Every(): time span interval can not be zero or less");
            }
            var name = Invariant($"Every {interval.TotalMinutes:0.##} min");
            Func<IEnumerable<DateTime>, IEnumerable<DateTime>> applicator = dates => EveryIntervalIterator(dates, interval, TimeZone);
            return new FuncTimeRule(name, applicator);
        }

        /// <summary>
        /// Specifies an event should fire at market open +- <paramref name="minutesAfterOpen"/>
        /// </summary>
        /// <param name="symbol">The symbol whose market open we want an event for</param>
        /// <param name="minutesAfterOpen">The minutes after market open that the event should fire</param>
        /// <param name="extendedMarketOpen">True to use extended market open, false to use regular market open</param>
        /// <returns>A time rule that fires the specified number of minutes after the symbol's market open</returns>
        public ITimeRule AfterMarketOpen(Symbol symbol, double minutesAfterOpen = 0, bool extendedMarketOpen = false)
        {
            var type = extendedMarketOpen ? "ExtendedMarketOpen" : "MarketOpen";
            var name = Invariant($"{symbol}: {minutesAfterOpen:0.##} min after {type}");
            var exchangeHours = GetSecurityExchangeHours(symbol);

            var timeAfterOpen = TimeSpan.FromMinutes(minutesAfterOpen);
            Func<IEnumerable<DateTime>, IEnumerable<DateTime>> applicator = dates =>
                from date in dates
                let marketOpen = exchangeHours.GetNextMarketOpen(date, extendedMarketOpen)
                // make sure the market open is of this date
                where exchangeHours.IsDateOpen(date) && marketOpen.Date == date.Date
                let localEventTime = marketOpen + timeAfterOpen
                let utcEventTime = localEventTime.ConvertToUtc(exchangeHours.TimeZone)
                select utcEventTime;

            return new FuncTimeRule(name, applicator);
        }

        /// <summary>
        /// Specifies an event should fire at the market close +- <paramref name="minutesBeforeClose"/>
        /// </summary>
        /// <param name="symbol">The symbol whose market close we want an event for</param>
        /// <param name="minutesBeforeClose">The time before market close that the event should fire</param>
        /// <param name="extendedMarketClose">True to use extended market close, false to use regular market close</param>
        /// <returns>A time rule that fires the specified number of minutes before the symbol's market close</returns>
        public ITimeRule BeforeMarketClose(Symbol symbol, double minutesBeforeClose = 0, bool extendedMarketClose = false)
        {
            var type = extendedMarketClose ? "ExtendedMarketClose" : "MarketClose";
            var name = Invariant($"{symbol}: {minutesBeforeClose:0.##} min before {type}");
            var exchangeHours = GetSecurityExchangeHours(symbol);

            var timeBeforeClose = TimeSpan.FromMinutes(minutesBeforeClose);
            Func<IEnumerable<DateTime>, IEnumerable<DateTime>> applicator = dates =>
                from date in dates
                let marketClose = exchangeHours.GetNextMarketClose(date, extendedMarketClose)
                // make sure the market open is of this date
                where exchangeHours.IsDateOpen(date) && marketClose.Date == date.Date
                let localEventTime = marketClose - timeBeforeClose
                let utcEventTime = localEventTime.ConvertToUtc(exchangeHours.TimeZone)
                select utcEventTime;

            return new FuncTimeRule(name, applicator);
        }

        /// <summary>
        /// For each provided date will yield all the time intervals based on the supplied time span
        /// </summary>
        /// <param name="dates">The dates for which we want to create the different intervals</param>
        /// <param name="interval">The interval value to use, can not be zero or less</param>
        /// <param name="timeZone">The time zone the date time is expressed in</param>
        private static IEnumerable<DateTime> EveryIntervalIterator(IEnumerable<DateTime> dates, TimeSpan interval, DateTimeZone timeZone)
        {
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentException("TimeRules.EveryIntervalIterator(): time span interval can not be zero or less");
            }
            foreach (var date in dates)
            {
                for (var time = TimeSpan.Zero; time < Time.OneDay; time += interval)
                {
                    yield return (date + time).ConvertToUtc(timeZone);
                }
            }
        }
    }
}
