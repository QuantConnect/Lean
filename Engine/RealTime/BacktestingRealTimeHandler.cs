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
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Scheduling;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Pseudo realtime event processing for backtesting to simulate realtime events in fast forward.
    /// </summary>
    public class BacktestingRealTimeHandler : BaseRealTimeHandler
    {
        private bool _sortingScheduledEventsRequired;
        private List<ScheduledEvent> _scheduledEventsSortedByTime = new List<ScheduledEvent>();

        /// <summary>
        /// Flag indicating the hander thread is completely finished and ready to dispose.
        /// this doesn't run as its own thread
        /// </summary>
        public override bool IsActive { get; protected set; }

        /// <summary>
        /// Initializes the real time handler for the specified algorithm and job
        /// </summary>
        public override void Setup(
            IAlgorithm algorithm,
            AlgorithmNodePacket job,
            IResultHandler resultHandler,
            IApi api,
            IIsolatorLimitResultProvider isolatorLimitProvider
        )
        {
            // create events for algorithm's end of tradeable dates
            // set up the events for each security to fire every tradeable date before market close
            base.Setup(algorithm, job, resultHandler, api, isolatorLimitProvider);

            foreach (var scheduledEvent in GetScheduledEventsSortedByTime())
            {
                // zoom past old events
                scheduledEvent.SkipEventsUntil(algorithm.UtcTime);
                // set logging accordingly
                scheduledEvent.IsLoggingEnabled = Log.DebuggingEnabled;
            }
            // after skipping events we should re order
            _sortingScheduledEventsRequired = true;
        }

        /// <summary>
        /// Adds the specified event to the schedule
        /// </summary>
        /// <param name="scheduledEvent">The event to be scheduled, including the date/times the event fires and the callback</param>
        public override void Add(ScheduledEvent scheduledEvent)
        {
            if (Algorithm != null)
            {
                scheduledEvent.SkipEventsUntil(Algorithm.UtcTime);
            }

            ScheduledEvents.AddOrUpdate(scheduledEvent, GetScheduledEventUniqueId());

            if (Log.DebuggingEnabled)
            {
                scheduledEvent.IsLoggingEnabled = true;
            }

            _sortingScheduledEventsRequired = true;
        }

        /// <summary>
        /// Removes the specified event from the schedule
        /// </summary>
        /// <param name="scheduledEvent">The event to be removed</param>
        public override void Remove(ScheduledEvent scheduledEvent)
        {
            int id;
            ScheduledEvents.TryRemove(scheduledEvent, out id);

            _sortingScheduledEventsRequired = true;
        }

        /// <summary>
        /// Set the time for the realtime event handler.
        /// </summary>
        /// <param name="time">Current time.</param>
        public override void SetTime(DateTime time)
        {
            var scheduledEvents = GetScheduledEventsSortedByTime();

            // the first element is always the next
            while (scheduledEvents.Count > 0 && scheduledEvents[0].NextEventUtcTime <= time)
            {
                try
                {
                    IsolatorLimitProvider.Consume(scheduledEvents[0], time, TimeMonitor);
                }
                catch (Exception exception)
                {
                    Algorithm.SetRuntimeError(
                        exception,
                        $"Scheduled event: '{scheduledEvents[0].Name}' at {time}"
                    );
                    break;
                }

                SortFirstElement(scheduledEvents);
            }
        }

        /// <summary>
        /// Scan for past events that didn't fire because there was no data at the scheduled time.
        /// </summary>
        /// <param name="time">Current time.</param>
        public override void ScanPastEvents(DateTime time)
        {
            var scheduledEvents = GetScheduledEventsSortedByTime();

            // the first element is always the next
            while (scheduledEvents.Count > 0 && scheduledEvents[0].NextEventUtcTime < time)
            {
                var scheduledEvent = scheduledEvents[0];
                var nextEventUtcTime = scheduledEvent.NextEventUtcTime;

                Algorithm.SetDateTime(nextEventUtcTime);

                try
                {
                    IsolatorLimitProvider.Consume(scheduledEvent, nextEventUtcTime, TimeMonitor);
                }
                catch (Exception exception)
                {
                    Algorithm.SetRuntimeError(
                        exception,
                        $"Scheduled event: '{scheduledEvent.Name}' at {nextEventUtcTime}"
                    );
                    break;
                }

                SortFirstElement(scheduledEvents);
            }
        }

        private List<ScheduledEvent> GetScheduledEventsSortedByTime()
        {
            if (_sortingScheduledEventsRequired)
            {
                _sortingScheduledEventsRequired = false;
                _scheduledEventsSortedByTime = ScheduledEvents
                    // we order by next event time
                    .OrderBy(x => x.Key.NextEventUtcTime)
                    // then by unique id so that for scheduled events in the same time
                    // respect their creation order, so its deterministic
                    .ThenBy(x => x.Value)
                    .Select(x => x.Key)
                    .ToList();
            }

            return _scheduledEventsSortedByTime;
        }

        /// <summary>
        /// Sorts the first element of the provided list and supposes the rest of the collection is sorted.
        /// Supposes the collection has at least 1 element
        /// </summary>
        public static void SortFirstElement(IList<ScheduledEvent> scheduledEvents)
        {
            var scheduledEvent = scheduledEvents[0];
            var nextEventUtcTime = scheduledEvent.NextEventUtcTime;

            if (
                scheduledEvents.Count > 1
                // if our NextEventUtcTime is after the next event we sort our selves
                && nextEventUtcTime > scheduledEvents[1].NextEventUtcTime
            )
            {
                // remove ourselves and re insert at the correct position, the rest of the items are sorted!
                scheduledEvents.RemoveAt(0);

                var position = scheduledEvents.BinarySearch(
                    nextEventUtcTime,
                    (time, orderEvent) => time.CompareTo(orderEvent.NextEventUtcTime)
                );
                if (position >= 0)
                {
                    // we have to insert after existing position to respect existing order, see ScheduledEventsOrderRegressionAlgorithm
                    var finalPosition = position + 1;
                    if (finalPosition == scheduledEvents.Count)
                    {
                        // bigger than all of them add at the end
                        scheduledEvents.Add(scheduledEvent);
                    }
                    else
                    {
                        // Calling insert isn't that performant but note that we are doing it once
                        // and has better performance than sorting the entire collection
                        scheduledEvents.Insert(finalPosition, scheduledEvent);
                    }
                }
                else
                {
                    var index = ~position;
                    if (index == scheduledEvents.Count)
                    {
                        // bigger than all of them insert in the end
                        scheduledEvents.Add(scheduledEvent);
                    }
                    else
                    {
                        // index + 1 is bigger than us so insert before
                        scheduledEvents.Insert(index, scheduledEvent);
                    }
                }
            }
        }
    }
}
