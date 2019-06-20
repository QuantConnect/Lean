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

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Pseudo realtime event processing for backtesting to simulate realtime events in fast forward.
    /// </summary>
    public class BacktestingRealTimeHandler : BaseRealTimeHandler, IRealTimeHandler
    {
        private bool _sortingScheduledEventsRequired;
        private List<ScheduledEvent> _scheduledEventsSortedByTime = new List<ScheduledEvent>();

        /// <summary>
        /// Flag indicating the hander thread is completely finished and ready to dispose.
        /// this doesn't run as its own thread
        /// </summary>
        public bool IsActive => false;

        /// <summary>
        /// Initializes the real time handler for the specified algorithm and job
        /// </summary>
        public void Setup(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IApi api)
        {
            //Initialize:
            Algorithm = algorithm;
            ResultHandler =  resultHandler;

            // create events for algorithm's end of tradeable dates
            // set up the events for each security to fire every tradeable date before market close
            base.Setup(Algorithm.StartDate, Algorithm.EndDate);

            foreach (var scheduledEvent in GetScheduledEventsSortedByTime())
            {
                // zoom past old events
                scheduledEvent.SkipEventsUntil(algorithm.UtcTime);
                // set logging accordingly
                scheduledEvent.IsLoggingEnabled = Log.DebuggingEnabled;
            }
        }

        /// <summary>
        /// Normally this would run the realtime event monitoring. Backtesting is in fastforward so the realtime is linked to the backtest clock.
        /// This thread does nothing. Wait until the job is over.
        /// </summary>
        public void Run()
        {
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
        public void SetTime(DateTime time)
        {
            // poke each event to see if it has fired, be sure to invoke these in time order
            foreach (var scheduledEvent in GetScheduledEventsSortedByTime())
            {
                scheduledEvent.Scan(time);
            }
        }

        /// <summary>
        /// Scan for past events that didn't fire because there was no data at the scheduled time.
        /// </summary>
        /// <param name="time">Current time.</param>
        public void ScanPastEvents(DateTime time)
        {
            foreach (var scheduledEvent in GetScheduledEventsSortedByTime())
            {
                while (scheduledEvent.NextEventUtcTime < time)
                {
                    Algorithm.SetDateTime(scheduledEvent.NextEventUtcTime);

                    try
                    {
                        scheduledEvent.Scan(scheduledEvent.NextEventUtcTime);
                    }
                    catch (ScheduledEventException scheduledEventException)
                    {
                        var errorMessage = $"BacktestingRealTimeHandler.Run(): There was an error in a scheduled event {scheduledEvent.Name}. The error was {scheduledEventException.Message}";

                        Log.Error(scheduledEventException, errorMessage);

                        ResultHandler.RuntimeError(errorMessage);

                        // Errors in scheduled event should be treated as runtime error
                        // Runtime errors should end Lean execution
                        Algorithm.RunTimeError = scheduledEventException;
                    }
                }
            }
        }

        /// <summary>
        /// Stop the real time thread
        /// </summary>
        public void Exit()
        {
            // this doesn't run as it's own thread, so nothing to exit
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
                    .Select(x => x.Key).ToList();
            }

            return _scheduledEventsSortedByTime;
        }
    }
}