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
    /// Provides a builder class to allow for fluent syntax when constructing new events
    /// </summary>
    /// <remarks>
    /// This builder follows the following steps for event creation:
    ///
    /// 1. Specify an event name (optional)
    /// 2. Specify an IDateRule
    /// 3. Specify an ITimeRule
    ///     a. repeat 3. to define extra time rules (optional)
    /// 4. Specify additional where clause (optional)
    /// 5. Register event via call to Run
    /// </remarks>
    public class FluentScheduledEventBuilder : IFluentSchedulingDateSpecifier, IFluentSchedulingRunnable
    {
        private IDateRule _dateRule;
        private ITimeRule _timeRule;
        private Func<DateTime, bool> _predicate;

        private readonly string _name;
        private readonly ScheduleManager _schedule;
        private readonly SecurityManager _securities;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentScheduledEventBuilder"/> class
        /// </summary>
        /// <param name="schedule">The schedule to send created events to</param>
        /// <param name="securities">The algorithm's security manager</param>
        /// <param name="name">A specific name for this event</param>
        public FluentScheduledEventBuilder(ScheduleManager schedule, SecurityManager securities, string name = null)
        {
            _name = name;
            _schedule = schedule;
            _securities = securities;
        }

        private FluentScheduledEventBuilder SetTimeRule(ITimeRule rule)
        {
            // if it's not set, just set it
            if (_timeRule == null)
            {
                _timeRule = rule;
                return this;
            }

            // if it's already a composite, open it up and make a new composite
            // prevent nesting composites
            var compositeTimeRule = _timeRule as CompositeTimeRule;
            if (compositeTimeRule != null)
            {
                var rules = compositeTimeRule.Rules;
                _timeRule = new CompositeTimeRule(rules.Concat(new[] { rule }));
                return this;
            }

            // create a composite from the existing rule and the new rules
            _timeRule = new CompositeTimeRule(_timeRule, rule);
            return this;
        }

        #region DateRules and TimeRules delegation

        /// <summary>
        /// Creates events on each of the specified day of week
        /// </summary>
        IFluentSchedulingTimeSpecifier IFluentSchedulingDateSpecifier.Every(params DayOfWeek[] days)
        {
            _dateRule = _schedule.DateRules.Every(days);
            return this;
        }

        /// <summary>
        /// Creates events on every day of the year
        /// </summary>
        IFluentSchedulingTimeSpecifier IFluentSchedulingDateSpecifier.EveryDay()
        {
            _dateRule = _schedule.DateRules.EveryDay();
            return this;
        }

        /// <summary>
        /// Creates events on every trading day of the year for the symbol
        /// </summary>
        IFluentSchedulingTimeSpecifier IFluentSchedulingDateSpecifier.EveryDay(Symbol symbol)
        {
            _dateRule = _schedule.DateRules.EveryDay(symbol);
            return this;
        }

        /// <summary>
        /// Creates events on the first day of the month
        /// </summary>
        IFluentSchedulingTimeSpecifier IFluentSchedulingDateSpecifier.MonthStart()
        {
            _dateRule = _schedule.DateRules.MonthStart();
            return this;
        }

        /// <summary>
        /// Creates events on the first trading day of the month
        /// </summary>
        IFluentSchedulingTimeSpecifier IFluentSchedulingDateSpecifier.MonthStart(Symbol symbol)
        {
            _dateRule = _schedule.DateRules.MonthStart(symbol);
            return this;
        }

        /// <summary>
        /// Filters the event times using the predicate
        /// </summary>
        IFluentSchedulingTimeSpecifier IFluentSchedulingDateSpecifier.Where(Func<DateTime, bool> predicate)
        {
            _predicate = _predicate == null
                ? predicate
                : (time => _predicate(time) && predicate(time));
            return this;
        }

        /// <summary>
        /// Creates events that fire at the specific time of day in the algorithm's time zone
        /// </summary>
        IFluentSchedulingRunnable IFluentSchedulingTimeSpecifier.At(TimeSpan timeOfDay)
        {
            return SetTimeRule(_schedule.TimeRules.At(timeOfDay));
        }

        /// <summary>
        /// Creates events that fire a specified number of minutes after market open
        /// </summary>
        IFluentSchedulingRunnable IFluentSchedulingTimeSpecifier.AfterMarketOpen(Symbol symbol, double minutesAfterOpen, bool extendedMarketOpen)
        {
            return SetTimeRule(_schedule.TimeRules.AfterMarketOpen(symbol, minutesAfterOpen, extendedMarketOpen));
        }

        /// <summary>
        /// Creates events that fire a specified numer of minutes before market close
        /// </summary>
        IFluentSchedulingRunnable IFluentSchedulingTimeSpecifier.BeforeMarketClose(Symbol symbol, double minuteBeforeClose, bool extendedMarketClose)
        {
            return SetTimeRule(_schedule.TimeRules.BeforeMarketClose(symbol, minuteBeforeClose, extendedMarketClose));
        }

        /// <summary>
        /// Creates events that fire on a period define by the specified interval
        /// </summary>
        IFluentSchedulingRunnable IFluentSchedulingTimeSpecifier.Every(TimeSpan interval)
        {
            return SetTimeRule(_schedule.TimeRules.Every(interval));
        }

        /// <summary>
        /// Filters the event times using the predicate
        /// </summary>
        IFluentSchedulingTimeSpecifier IFluentSchedulingTimeSpecifier.Where(Func<DateTime, bool> predicate)
        {
            _predicate = _predicate == null
                ? predicate
                : (time => _predicate(time) && predicate(time));
            return this;
        }

        /// <summary>
        /// Register the defined event with the callback
        /// </summary>
        ScheduledEvent IFluentSchedulingRunnable.Run(Action callback)
        {
            return ((IFluentSchedulingRunnable)this).Run((name, time) => callback());
        }

        /// <summary>
        /// Register the defined event with the callback
        /// </summary>
        ScheduledEvent IFluentSchedulingRunnable.Run(Action<DateTime> callback)
        {
            return ((IFluentSchedulingRunnable)this).Run((name, time) => callback(time));
        }

        /// <summary>
        /// Register the defined event with the callback
        /// </summary>
        ScheduledEvent IFluentSchedulingRunnable.Run(Action<string, DateTime> callback)
        {
            var name = _name ?? _dateRule.Name + ": " + _timeRule.Name;
            // back the date up to ensure we get all events, the event scheduler will skip past events that whose time has passed
            var dates = _dateRule.GetDates(_securities.UtcTime.Date.AddDays(-1), Time.EndOfTime);
            var eventTimes = _timeRule.CreateUtcEventTimes(dates);
            if (_predicate != null)
            {
                eventTimes = eventTimes.Where(_predicate);
            }
            var scheduledEvent = new ScheduledEvent(name, eventTimes, callback);
            _schedule.Add(scheduledEvent);
            return scheduledEvent;
        }

        /// <summary>
        /// Filters the event times using the predicate
        /// </summary>
        IFluentSchedulingRunnable IFluentSchedulingRunnable.Where(Func<DateTime, bool> predicate)
        {
            _predicate = _predicate == null
                ? predicate
                : (time => _predicate(time) && predicate(time));
            return this;
        }

        /// <summary>
        /// Filters the event times to only include times where the symbol's market is considered open
        /// </summary>
        IFluentSchedulingRunnable IFluentSchedulingRunnable.DuringMarketHours(Symbol symbol, bool extendedMarket)
        {
            var security = GetSecurity(symbol);
            Func<DateTime, bool> predicate = time =>
            {
                var localTime = time.ConvertFromUtc(security.Exchange.TimeZone);
                return security.Exchange.IsOpenDuringBar(localTime, localTime, extendedMarket);
            };
            _predicate = _predicate == null
                ? predicate
                : (time => _predicate(time) && predicate(time));
            return this;
        }

        IFluentSchedulingTimeSpecifier IFluentSchedulingDateSpecifier.On(int year, int month, int day)
        {
            _dateRule = _schedule.DateRules.On(year, month, day);
            return this;
        }

        IFluentSchedulingTimeSpecifier IFluentSchedulingDateSpecifier.On(params DateTime[] dates)
        {
            _dateRule = _schedule.DateRules.On(dates);
            return this;
        }

        IFluentSchedulingRunnable IFluentSchedulingTimeSpecifier.At(int hour, int minute, int second)
        {
            return SetTimeRule(_schedule.TimeRules.At(hour, minute, second));
        }

        IFluentSchedulingRunnable IFluentSchedulingTimeSpecifier.At(int hour, int minute, DateTimeZone timeZone)
        {
            return SetTimeRule(_schedule.TimeRules.At(hour, minute, 0, timeZone));
        }

        IFluentSchedulingRunnable IFluentSchedulingTimeSpecifier.At(int hour, int minute, int second, DateTimeZone timeZone)
        {
            return SetTimeRule(_schedule.TimeRules.At(hour, minute, second, timeZone));
        }

        IFluentSchedulingRunnable IFluentSchedulingTimeSpecifier.At(TimeSpan timeOfDay, DateTimeZone timeZone)
        {
            return SetTimeRule(_schedule.TimeRules.At(timeOfDay, timeZone));
        }

        private Security GetSecurity(Symbol symbol)
        {
            Security security;
            if (!_securities.TryGetValue(symbol, out security))
            {
                throw new KeyNotFoundException($"{symbol} not found in portfolio. Request this data when initializing the algorithm.");
            }

            return security;
        }

        #endregion
    }

    /// <summary>
    /// Specifies the date rule component of a scheduled event
    /// </summary>
    public interface IFluentSchedulingDateSpecifier
    {
        /// <summary>
        /// Filters the event times using the predicate
        /// </summary>
        IFluentSchedulingTimeSpecifier Where(Func<DateTime, bool> predicate);
        /// <summary>
        /// Creates events only on the specified date
        /// </summary>
        IFluentSchedulingTimeSpecifier On(int year, int month, int day);
        /// <summary>
        /// Creates events only on the specified dates
        /// </summary>
        IFluentSchedulingTimeSpecifier On(params DateTime[] dates);
        /// <summary>
        /// Creates events on each of the specified day of week
        /// </summary>
        IFluentSchedulingTimeSpecifier Every(params DayOfWeek[] days);
        /// <summary>
        /// Creates events on every day of the year
        /// </summary>
        IFluentSchedulingTimeSpecifier EveryDay();
        /// <summary>
        /// Creates events on every trading day of the year for the symbol
        /// </summary>
        IFluentSchedulingTimeSpecifier EveryDay(Symbol symbol);
        /// <summary>
        /// Creates events on the first day of the month
        /// </summary>
        IFluentSchedulingTimeSpecifier MonthStart();
        /// <summary>
        /// Creates events on the first trading day of the month
        /// </summary>
        IFluentSchedulingTimeSpecifier MonthStart(Symbol symbol);
    }

    /// <summary>
    /// Specifies the time rule component of a scheduled event
    /// </summary>
    public interface IFluentSchedulingTimeSpecifier
    {
        /// <summary>
        /// Filters the event times using the predicate
        /// </summary>
        IFluentSchedulingTimeSpecifier Where(Func<DateTime, bool> predicate);
        /// <summary>
        /// Creates events that fire at the specified time of day in the specified time zone
        /// </summary>
        IFluentSchedulingRunnable At(int hour, int minute, int second = 0);
        /// <summary>
        /// Creates events that fire at the specified time of day in the specified time zone
        /// </summary>
        IFluentSchedulingRunnable At(int hour, int minute, DateTimeZone timeZone);
        /// <summary>
        /// Creates events that fire at the specified time of day in the specified time zone
        /// </summary>
        IFluentSchedulingRunnable At(int hour, int minute, int second, DateTimeZone timeZone);
        /// <summary>
        /// Creates events that fire at the specified time of day in the specified time zone
        /// </summary>
        IFluentSchedulingRunnable At(TimeSpan timeOfDay, DateTimeZone timeZone);
        /// <summary>
        /// Creates events that fire at the specific time of day in the algorithm's time zone
        /// </summary>
        IFluentSchedulingRunnable At(TimeSpan timeOfDay);
        /// <summary>
        /// Creates events that fire on a period define by the specified interval
        /// </summary>
        IFluentSchedulingRunnable Every(TimeSpan interval);
        /// <summary>
        /// Creates events that fire a specified number of minutes after market open
        /// </summary>
        IFluentSchedulingRunnable AfterMarketOpen(Symbol symbol, double minutesAfterOpen = 0, bool extendedMarketOpen = false);
        /// <summary>
        /// Creates events that fire a specified numer of minutes before market close
        /// </summary>
        IFluentSchedulingRunnable BeforeMarketClose(Symbol symbol, double minuteBeforeClose = 0, bool extendedMarketClose = false);
    }

    /// <summary>
    /// Specifies the callback component of a scheduled event, as well as final filters
    /// </summary>
    public interface IFluentSchedulingRunnable : IFluentSchedulingTimeSpecifier
    {
        /// <summary>
        /// Filters the event times using the predicate
        /// </summary>
        new IFluentSchedulingRunnable Where(Func<DateTime, bool> predicate);
        /// <summary>
        /// Filters the event times to only include times where the symbol's market is considered open
        /// </summary>
        IFluentSchedulingRunnable DuringMarketHours(Symbol symbol, bool extendedMarket = false);
        /// <summary>
        /// Register the defined event with the callback
        /// </summary>
        ScheduledEvent Run(Action callback);
        /// <summary>
        /// Register the defined event with the callback
        /// </summary>
        ScheduledEvent Run(Action<DateTime> callback);
        /// <summary>
        /// Register the defined event with the callback
        /// </summary>
        ScheduledEvent Run(Action<string, DateTime> callback);
    }
}