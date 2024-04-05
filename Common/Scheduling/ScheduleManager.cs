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
using QuantConnect.Securities;
using QuantConnect.Logging;
using Python.Runtime;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Provides access to the real time handler's event scheduling feature
    /// </summary>
    public class ScheduleManager : IEventSchedule
    {
        private IEventSchedule _eventSchedule;

        private readonly SecurityManager _securities;
        private readonly object _eventScheduleLock = new object();
        private readonly List<ScheduledEvent> _preInitializedEvents;

        /// <summary>
        /// Gets the date rules helper object to make specifying dates for events easier
        /// </summary>
        public DateRules DateRules { get; }

        /// <summary>
        /// Gets the time rules helper object to make specifying times for events easier
        /// </summary>
        public TimeRules TimeRules { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleManager"/> class
        /// </summary>
        /// <param name="securities">Securities manager containing the algorithm's securities</param>
        /// <param name="timeZone">The algorithm's time zone</param>
        /// <param name="marketHoursDatabase">The market hours database instance to use</param>
        public ScheduleManager(SecurityManager securities, DateTimeZone timeZone, MarketHoursDatabase marketHoursDatabase)
        {
            _securities = securities;
            DateRules = new DateRules(securities, timeZone, marketHoursDatabase);
            TimeRules = new TimeRules(securities, timeZone, marketHoursDatabase);

            // used for storing any events before the event schedule is set
            _preInitializedEvents = new List<ScheduledEvent>();
        }

        /// <summary>
        /// Sets the <see cref="IEventSchedule"/> implementation
        /// </summary>
        /// <param name="eventSchedule">The event schedule implementation to be used. This is the IRealTimeHandler</param>
        internal void SetEventSchedule(IEventSchedule eventSchedule)
        {
            if (eventSchedule == null)
            {
                throw new ArgumentNullException(nameof(eventSchedule));
            }

            lock (_eventScheduleLock)
            {
                _eventSchedule = eventSchedule;

                // load up any events that were added before we were ready to send them to the scheduler
                foreach (var scheduledEvent in _preInitializedEvents)
                {
                    _eventSchedule.Add(scheduledEvent);
                }
            }
        }

        /// <summary>
        /// Adds the specified event to the schedule
        /// </summary>
        /// <param name="scheduledEvent">The event to be scheduled, including the date/times the event fires and the callback</param>
        public void Add(ScheduledEvent scheduledEvent)
        {
            lock (_eventScheduleLock)
            {
                if (_eventSchedule != null)
                {
                    _eventSchedule.Add(scheduledEvent);
                }
                else
                {
                    _preInitializedEvents.Add(scheduledEvent);
                }
            }
        }

        /// <summary>
        /// Removes the specified event from the schedule
        /// </summary>
        /// <param name="scheduledEvent">The event to be removed</param>
        public void Remove(ScheduledEvent scheduledEvent)
        {
            lock (_eventScheduleLock)
            {
                if (_eventSchedule != null)
                {
                    _eventSchedule.Remove(scheduledEvent);
                }
                else
                {
                    _preInitializedEvents.RemoveAll(se => Equals(se, scheduledEvent));
                }
            }
        }

        /// <summary>
        /// Schedules the callback to run using the specified date and time rules
        /// </summary>
        /// <param name="dateRule">Specifies what dates the event should run</param>
        /// <param name="timeRule">Specifies the times on those dates the event should run</param>
        /// <param name="callback">The callback to be invoked</param>
        public ScheduledEvent On(IDateRule dateRule, ITimeRule timeRule, Action callback)
        {
            return On(dateRule, timeRule, (name, time) => callback());
        }

        /// <summary>
        /// Schedules the callback to run using the specified date and time rules
        /// </summary>
        /// <param name="dateRule">Specifies what dates the event should run</param>
        /// <param name="timeRule">Specifies the times on those dates the event should run</param>
        /// <param name="callback">The callback to be invoked</param>
        public ScheduledEvent On(IDateRule dateRule, ITimeRule timeRule, PyObject callback)
        {
            return On(dateRule, timeRule, (name, time) => { using (Py.GIL()) callback.Invoke(); });
        }

        /// <summary>
        /// Schedules the callback to run using the specified date and time rules
        /// </summary>
        /// <param name="dateRule">Specifies what dates the event should run</param>
        /// <param name="timeRule">Specifies the times on those dates the event should run</param>
        /// <param name="callback">The callback to be invoked</param>
        public ScheduledEvent On(IDateRule dateRule, ITimeRule timeRule, Action<string, DateTime> callback)
        {
            var name = $"{dateRule.Name}: {timeRule.Name}";
            return On(name, dateRule, timeRule, callback);
        }

        /// <summary>
        /// Schedules the callback to run using the specified date and time rules
        /// </summary>
        /// <param name="name">The event's unique name</param>
        /// <param name="dateRule">Specifies what dates the event should run</param>
        /// <param name="timeRule">Specifies the times on those dates the event should run</param>
        /// <param name="callback">The callback to be invoked</param>
        public ScheduledEvent On(string name, IDateRule dateRule, ITimeRule timeRule, Action callback)
        {
            return On(name, dateRule, timeRule, (n, d) => callback());
        }

        /// <summary>
        /// Schedules the callback to run using the specified date and time rules
        /// </summary>
        /// <param name="name">The event's unique name</param>
        /// <param name="dateRule">Specifies what dates the event should run</param>
        /// <param name="timeRule">Specifies the times on those dates the event should run</param>
        /// <param name="callback">The callback to be invoked</param>
        public ScheduledEvent On(string name, IDateRule dateRule, ITimeRule timeRule, PyObject callback)
        {
            return On(name, dateRule, timeRule, (n, d) => { using (Py.GIL()) callback.Invoke(); });
        }

        /// <summary>
        /// Schedules the callback to run using the specified date and time rules
        /// </summary>
        /// <param name="name">The event's unique name</param>
        /// <param name="dateRule">Specifies what dates the event should run</param>
        /// <param name="timeRule">Specifies the times on those dates the event should run</param>
        /// <param name="callback">The callback to be invoked</param>
        public ScheduledEvent On(string name, IDateRule dateRule, ITimeRule timeRule, Action<string, DateTime> callback)
        {
            // back the date up to ensure we get all events, the event scheduler will skip past events that whose time has passed
            var dates = GetDatesDeferred(dateRule, _securities);
            var eventTimes = timeRule.CreateUtcEventTimes(dates);
            var scheduledEvent = new ScheduledEvent(name, eventTimes, callback);
            Add(scheduledEvent);

            Log.Trace($"Event Name \"{scheduledEvent.Name}\", scheduled to run.");
            return scheduledEvent;
        }

        #region Fluent Scheduling

        /// <summary>
        /// Entry point for the fluent scheduled event builder
        /// </summary>
        public IFluentSchedulingDateSpecifier Event()
        {
            return new FluentScheduledEventBuilder(this, _securities);
        }

        /// <summary>
        /// Entry point for the fluent scheduled event builder
        /// </summary>
        public IFluentSchedulingDateSpecifier Event(string name)
        {
            return new FluentScheduledEventBuilder(this, _securities, name);
        }

        #endregion

        #region Training Events

        /// <summary>
        /// Schedules the provided training code to execute immediately
        /// </summary>
        public ScheduledEvent TrainingNow(Action trainingCode)
        {
            return On($"Training: Now: {_securities.UtcTime:O}", DateRules.Today, TimeRules.Now, trainingCode);
        }

        /// <summary>
        /// Schedules the provided training code to execute immediately
        /// </summary>
        public ScheduledEvent TrainingNow(PyObject trainingCode)
        {
            return On($"Training: Now: {_securities.UtcTime:O}", DateRules.Today, TimeRules.Now, trainingCode);
        }

        /// <summary>
        /// Schedules the training code to run using the specified date and time rules
        /// </summary>
        /// <param name="dateRule">Specifies what dates the event should run</param>
        /// <param name="timeRule">Specifies the times on those dates the event should run</param>
        /// <param name="trainingCode">The training code to be invoked</param>
        public ScheduledEvent Training(IDateRule dateRule, ITimeRule timeRule, Action trainingCode)
        {
            var name = $"{dateRule.Name}: {timeRule.Name}";
            return On(name, dateRule, timeRule, (n, time) => trainingCode());
        }


        /// <summary>
        /// Schedules the training code to run using the specified date and time rules
        /// </summary>
        /// <param name="dateRule">Specifies what dates the event should run</param>
        /// <param name="timeRule">Specifies the times on those dates the event should run</param>
        /// <param name="trainingCode">The training code to be invoked</param>
        public ScheduledEvent Training(IDateRule dateRule, ITimeRule timeRule, PyObject trainingCode)
        {
            var name = $"{dateRule.Name}: {timeRule.Name}";
            return On(name, dateRule, timeRule, (n, time) => { using (Py.GIL()) trainingCode.Invoke(); });
        }

        /// <summary>
        /// Schedules the training code to run using the specified date and time rules
        /// </summary>
        /// <param name="dateRule">Specifies what dates the event should run</param>
        /// <param name="timeRule">Specifies the times on those dates the event should run</param>
        /// <param name="trainingCode">The training code to be invoked</param>
        public ScheduledEvent Training(IDateRule dateRule, ITimeRule timeRule, Action<DateTime> trainingCode)
        {
            var name = $"{dateRule.Name}: {timeRule.Name}";
            return On(name, dateRule, timeRule, (n, time) => trainingCode(time));
        }

        #endregion

        /// <summary>
        /// Helper methods to defer the evaluation of the current time until the dates are enumerated for the first time.
        /// This allows for correct support for warmup period
        /// </summary>
        internal static IEnumerable<DateTime> GetDatesDeferred(IDateRule dateRule, SecurityManager securities)
        {
            foreach (var item in dateRule.GetDates(securities.UtcTime.Date.AddDays(-1), Time.EndOfTime))
            {
                yield return item;
            }
        }
    }
}
