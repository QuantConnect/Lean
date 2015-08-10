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
using QuantConnect.Securities;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Provides access to the real time handler's event scheduling feature
    /// </summary>
    public class ScheduleManager : IEventSchedule
    {
        private IEventSchedule _eventSchedule;

        private readonly SecurityManager _securities;

        /// <summary>
        /// Gets the date rules helper object to make specifying dates for events easier
        /// </summary>
        public DateRules DateRules { get; private set; }

        /// <summary>
        /// Gets the time rules helper object to make specifying times for events easier
        /// </summary>
        public TimeRules TimeRules { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleManager"/> class
        /// </summary>
        /// <param name="securities">Securities manager containing the algorithm's securities</param>
        /// <param name="timeZone">The algorithm's time zone</param>
        public ScheduleManager(SecurityManager securities, DateTimeZone timeZone)
        {
            _securities = securities;
            DateRules = new DateRules(securities);
            TimeRules = new TimeRules(securities, timeZone);
        }

        /// <summary>
        /// Sets the <see cref="IEventSchedule"/> implementation
        /// </summary>
        /// <param name="eventSchedule">The event schedule implementation to be used. This is the IRealTimeHandler</param>
        internal void SetEventSchedule(IEventSchedule eventSchedule)
        {
            if (eventSchedule == null)
            {
                throw new ArgumentNullException("eventSchedule");
            }

            _eventSchedule = eventSchedule;
        }

        /// <summary>
        /// Adds the specified event to the schedule using the <see cref="ScheduledEvent.Name"/> as a key.
        /// </summary>
        /// <param name="scheduledEvent">The event to be scheduled, including the date/times the event fires and the callback</param>
        public void Add(ScheduledEvent scheduledEvent)
        {
            _eventSchedule.Add(scheduledEvent);
        }

        /// <summary>
        /// Removes the event with the specified name from the schedule
        /// </summary>
        /// <param name="name">The name of the event to be removed</param>
        public void Remove(string name)
        {
            _eventSchedule.Remove(name);
        }

        /// <summary>
        /// Schedules the callback to run using the specified date and time rules
        /// </summary>
        /// <param name="dateRule">Specifies what dates the event should run</param>
        /// <param name="timeRule">Specifies the times on those dates the event should run</param>
        /// <param name="callback">The callback to be invoked</param>
        public void On(IDateRule dateRule, ITimeRule timeRule, Action callback)
        {
            var name = dateRule.Name + ": " + timeRule.Name;
            On(name, dateRule, timeRule, callback);
        }

        /// <summary>
        /// Schedules the callback to run using the specified date and time rules
        /// </summary>
        /// <param name="name">The event's unique name</param>
        /// <param name="dateRule">Specifies what dates the event should run</param>
        /// <param name="timeRule">Specifies the times on those dates the event should run</param>
        /// <param name="callback">The callback to be invoked</param>
        public void On(string name, IDateRule dateRule, ITimeRule timeRule, Action callback)
        {
            var dates = dateRule.GetDates(_securities.UtcTime, Time.EndOfTime);
            var eventTimes = timeRule.CreateUtcEventTimes(dates);
            var scheduledEvent = new ScheduledEvent(name, eventTimes, (s, time) => callback());
            _eventSchedule.Add(scheduledEvent);
        }
    }
}
