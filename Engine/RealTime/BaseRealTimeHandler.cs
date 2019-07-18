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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Base class for the real time handler <see cref="LiveTradingRealTimeHandler"/>
    /// and <see cref="BacktestingRealTimeHandler"/> implementations
    /// </summary>
    public abstract class BaseRealTimeHandler : IEventSchedule
    {
        private int _scheduledEventUniqueId;

        /// <summary>
        /// Keep track of this event so we can remove it when we need to update it
        /// </summary>
        private ScheduledEvent _algorithmOnEndOfDay;

        /// <summary>
        /// Keep a separate track of these scheduled events so we can remove them
        /// if the security gets removed
        /// </summary>
        private readonly ConcurrentDictionary<Symbol, ScheduledEvent> _securityOnEndOfDay
            = new ConcurrentDictionary<Symbol, ScheduledEvent>();

        /// <summary>
        /// The scheduled events container
        /// </summary>
        /// <remarks>Initialize this immediately since the Initialize method gets
        /// called after IAlgorithm.Initialize, so we want to be ready to accept
        /// events as soon as possible</remarks>
        protected readonly ConcurrentDictionary<ScheduledEvent, int> ScheduledEvents
            = new ConcurrentDictionary<ScheduledEvent, int>();

        /// <summary>
        /// The algorithm instance
        /// </summary>
        protected IAlgorithm Algorithm;

        /// <summary>
        /// The result handler instance
        /// </summary>
        protected IResultHandler ResultHandler;

        /// <summary>
        /// Adds the specified event to the schedule
        /// </summary>
        /// <param name="scheduledEvent">The event to be scheduled, including the date/times
        /// the event fires and the callback</param>
        public abstract void Add(ScheduledEvent scheduledEvent);

        /// <summary>
        /// Removes the specified event from the schedule
        /// </summary>
        /// <param name="scheduledEvent">The event to be removed</param>
        public abstract void Remove(ScheduledEvent scheduledEvent);

        /// <summary>
        /// Initializes the real time handler for the specified algorithm and job.
        /// Adds EndOfDayEvents
        /// </summary>
        protected void Setup(DateTime start, DateTime end, DateTime? currentUtcTime = null)
        {
            AddAlgorithmEndOfDayEvent(start, end, currentUtcTime);

            // add end of trading day events for each security
            AddSecurityDependentEndOfDayEvents(Algorithm.Securities.Values, start, end, currentUtcTime);
        }

        /// <summary>
        /// Gets a new scheduled event unique id
        /// </summary>
        /// <remarks>This value is used to order scheduled events in a deterministic way</remarks>
        protected int GetScheduledEventUniqueId()
        {
            return Interlocked.Increment(ref _scheduledEventUniqueId);
        }

        /// <summary>
        /// Creates a new <see cref="ScheduledEvent"/> that will fire before market close by the specified time
        /// </summary>
        /// <param name="start">The date to start the events</param>
        /// <param name="end">The date to end the events</param>
        /// <param name="currentUtcTime">Specifies the current time in UTC, before which,
        /// no events will be scheduled. Specify null to skip this filter.</param>
        [Obsolete("This method is deprecated. It will add ScheduledEvents for the deprecated IAlgorithm.OnEndOfDay()")]
        protected void AddAlgorithmEndOfDayEvent(DateTime start, DateTime end, DateTime? currentUtcTime = null)
        {
            if (_algorithmOnEndOfDay != null)
            {
                // if we already set it once we remove the previous and
                // add a new one, we don't want to keep both
                Remove(_algorithmOnEndOfDay);
            }

            // add end of day events for each tradeable day
            _algorithmOnEndOfDay = ScheduledEventFactory.EveryAlgorithmEndOfDay(
                Algorithm,
                ResultHandler,
                start,
                end,
                ScheduledEvent.AlgorithmEndOfDayDelta,
                currentUtcTime);

            Add(_algorithmOnEndOfDay);
        }

        /// <summary>
        /// Creates a new <see cref="ScheduledEvent"/> that will fire before market
        /// close by the specified time for each provided securities.
        /// </summary>
        /// <param name="securities">The securities for which we want to add the OnEndOfDay event</param>
        /// <param name="start">The date to start the events</param>
        /// <param name="end">The date to end the events</param>
        /// <param name="currentUtcTime">Specifies the current time in UTC, before which,
        /// no events will be scheduled. Specify null to skip this filter.</param>
        protected void AddSecurityDependentEndOfDayEvents(
            IEnumerable<Security> securities,
            DateTime start,
            DateTime end,
            DateTime? currentUtcTime = null)
        {
            // add end of trading day events for each security
            foreach (var security in securities)
            {
                if (!security.IsInternalFeed())
                {
                    var scheduledEvent = ScheduledEventFactory.EverySecurityEndOfDay(
                        Algorithm, ResultHandler, security, start, end, ScheduledEvent.SecurityEndOfDayDelta, currentUtcTime);

                    // we keep separate track so we can remove it later
                    _securityOnEndOfDay[security.Symbol] = scheduledEvent;

                    // assumes security.Exchange has been updated with today's hours via RefreshMarketHoursToday
                    Add(scheduledEvent);
                }
            }
        }

        /// <summary>
        /// Event fired each time that we add/remove securities from the data feed
        /// </summary>
        public void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (changes != SecurityChanges.None)
            {
                AddSecurityDependentEndOfDayEvents(changes.AddedSecurities,
                    Algorithm.UtcTime,
                    Algorithm.EndDate,
                    Algorithm.UtcTime);

                foreach (var security in changes.RemovedSecurities)
                {
                    ScheduledEvent scheduledEvent;
                    if (_securityOnEndOfDay.TryRemove(security.Symbol, out scheduledEvent))
                    {
                        // we remove the schedule events of the securities that were removed
                        Remove(scheduledEvent);
                    }
                }

                // we re add the algorithm end of day event because it depends on the securities
                // tradable dates
                AddAlgorithmEndOfDayEvent(Algorithm.UtcTime, Algorithm.EndDate, Algorithm.UtcTime);
            }
        }
    }
}
