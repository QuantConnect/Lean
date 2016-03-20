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

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Helper class used to provide better syntax when defining time rules
    /// </summary>
    public class TimeRules
    {
        private DateTimeZone _timeZone;

        private readonly SecurityManager _securities;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeRules"/> helper class
        /// </summary>
        /// <param name="securities">The security manager</param>
        /// <param name="timeZone">The algorithm's default time zone</param>
        public TimeRules(SecurityManager securities, DateTimeZone timeZone)
        {
            _securities = securities;
            _timeZone = timeZone;
        }

        /// <summary>
        /// Sets the default time zone
        /// </summary>
        /// <param name="timeZone">The time zone to use for helper methods that can't resolve a time zone</param>
        public void SetDefaultTimeZone(DateTimeZone timeZone)
        {
            _timeZone = timeZone;
        }

        /// <summary>
        /// Specifies an event should fire at the specified time of day in the algorithm's time zone
        /// </summary>
        /// <param name="timeOfDay">The time of day in the algorithm's time zone the event should fire</param>
        /// <returns>A time rule that fires at the specified time in the algorithm's time zone</returns>
        public ITimeRule At(TimeSpan timeOfDay)
        {
            return At(timeOfDay, _timeZone);
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
            return At(new TimeSpan(hour, minute, second), _timeZone);
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
            var name = string.Join(",", timeOfDay.TotalHours.ToString("0.##"));
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
        /// <param name="interval">The frequency with which the event should fire</param>
        /// <returns>A time rule that fires after each interval passes</returns>
        public ITimeRule Every(TimeSpan interval)
        {
            var name = "Every " + interval.TotalMinutes.ToString("0.##") + " min";
            Func<IEnumerable<DateTime>, IEnumerable<DateTime>> applicator = dates => EveryIntervalIterator(dates, interval);
            return new FuncTimeRule(name, applicator);
        }

        /// <summary>
        /// Specifies an event should fire at market open +- <paramref name="minutesAfterOpen"/>
        /// </summary>
        /// <param name="symbol">The symbol whose market open we want an event for</param>
        /// <param name="minutesAfterOpen">The time after market open that the event should fire</param>
        /// <param name="extendedMarketOpen">True to use extended market open, false to use regular market open</param>
        /// <returns>A time rule that fires the specified number of minutes after the symbol's market open</returns>
        public ITimeRule AfterMarketOpen(Symbol symbol, double minutesAfterOpen = 0, bool extendedMarketOpen = false)
        {
            var security = GetSecurity(symbol);

            var type = extendedMarketOpen ? "ExtendedMarketOpen" : "MarketOpen";
            var name = string.Format("{0}: {1} min after {2}", symbol, minutesAfterOpen.ToString("0.##"), type);

            var timeAfterOpen = TimeSpan.FromMinutes(minutesAfterOpen);
            Func<IEnumerable<DateTime>, IEnumerable<DateTime>> applicator = dates =>
                from date in dates
                where security.Exchange.DateIsOpen(date)
                let marketOpen = security.Exchange.Hours.GetNextMarketOpen(date, extendedMarketOpen)
                let localEventTime = marketOpen + timeAfterOpen
                let utcEventTime = localEventTime.ConvertToUtc(security.Exchange.TimeZone)
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
            var security = GetSecurity(symbol);

            var type = extendedMarketClose ? "ExtendedMarketClose" : "MarketClose";
            var name = string.Format("{0}: {1} min before {2}", security.Symbol, minutesBeforeClose.ToString("0.##"), type);

            var timeBeforeClose = TimeSpan.FromMinutes(minutesBeforeClose);
            Func<IEnumerable<DateTime>, IEnumerable<DateTime>> applicator = dates =>
                from date in dates
                where security.Exchange.DateIsOpen(date)
                let marketClose = security.Exchange.Hours.GetNextMarketClose(date, extendedMarketClose)
                let localEventTime = marketClose - timeBeforeClose
                let utcEventTime = localEventTime.ConvertToUtc(security.Exchange.TimeZone)
                select utcEventTime;

            return new FuncTimeRule(name, applicator);
        }

        private Security GetSecurity(Symbol symbol)
        {
            Security security;
            if (!_securities.TryGetValue(symbol, out security))
            {
                throw new Exception(symbol.ToString() + " not found in portfolio. Request this data when initializing the algorithm.");
            }
            return security;
        }

        private static IEnumerable<DateTime> EveryIntervalIterator(IEnumerable<DateTime> dates, TimeSpan interval)
        {
            foreach (var date in dates)
            {
                for (var time = TimeSpan.Zero; time < Time.OneDay; time += interval)
                {
                    yield return date + time;
                }
            }
        }
    }
}