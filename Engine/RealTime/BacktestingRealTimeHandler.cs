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
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Scheduling;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Psuedo realtime event processing for backtesting to simulate realtime events in fast forward.
    /// </summary>
    public class BacktestingRealTimeHandler : IRealTimeHandler
    {
        private IAlgorithm _algorithm;
        private IResultHandler _resultHandler;
        // initialize this immediately since the Initialzie method gets called after IAlgorithm.Initialize,
        // so we want to be ready to accept events as soon as possible
        private readonly ConcurrentDictionary<string, ScheduledEvent> _scheduledEvents = new ConcurrentDictionary<string, ScheduledEvent>();

        /// <summary>
        /// Flag indicating the hander thread is completely finished and ready to dispose.
        /// </summary>
        public bool IsActive
        {
            // this doesn't run as its own thread
            get { return false; }
        }

        /// <summary>
        /// Intializes the real time handler for the specified algorithm and job
        /// </summary>
        public void Setup(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IApi api)
        {
            //Initialize:
            _algorithm = algorithm;
            _resultHandler =  resultHandler;

            // create events for algorithm's end of tradeable dates
            Add(ScheduledEventFactory.EveryAlgorithmEndOfDay(_algorithm, _resultHandler, _algorithm.StartDate, _algorithm.EndDate, ScheduledEvent.AlgorithmEndOfDayDelta));

            // set up the events for each security to fire every tradeable date before market close
            foreach (var kvp in _algorithm.Securities)
            {
                var security = kvp.Value;

                if (!security.IsInternalFeed())
                {
                    Add(ScheduledEventFactory.EverySecurityEndOfDay(_algorithm, _resultHandler, security, algorithm.StartDate, _algorithm.EndDate, ScheduledEvent.SecurityEndOfDayDelta));
                }
            }

            foreach (var scheduledEvent in _scheduledEvents)
            {
                // zoom past old events
                scheduledEvent.Value.SkipEventsUntil(algorithm.UtcTime);
                // set logging accordingly
                scheduledEvent.Value.IsLoggingEnabled = Log.DebuggingEnabled;
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
        public void Add(ScheduledEvent scheduledEvent)
        {
            if (_algorithm != null)
            {
                scheduledEvent.SkipEventsUntil(_algorithm.UtcTime);
            }

            _scheduledEvents[scheduledEvent.Name] = scheduledEvent;
            if (Log.DebuggingEnabled)
            {
                scheduledEvent.IsLoggingEnabled = true;
            }
        }

        /// <summary>
        /// Removes the specified event from the schedule
        /// </summary>
        /// <param name="name">The name of the event to remove</param>
        public void Remove(string name)
        {
            ScheduledEvent scheduledEvent;
            _scheduledEvents.TryRemove(name, out scheduledEvent);
        }

        /// <summary>
        /// Set the time for the realtime event handler.
        /// </summary>
        /// <param name="time">Current time.</param>
        public void SetTime(DateTime time)
        {
            // poke each event to see if it has fired, be sure to invoke these in time order
            foreach (var scheduledEvent in _scheduledEvents)//.OrderBy(x => x.Value.NextEventUtcTime))
            {
                scheduledEvent.Value.Scan(time);
            }
        }

        /// <summary>
        /// Scan for past events that didn't fire because there was no data at the scheduled time.
        /// </summary>
        /// <param name="time">Current time.</param>
        public void ScanPastEvents(DateTime time)
        {
            foreach (var scheduledEvent in _scheduledEvents)
            {
                while (scheduledEvent.Value.NextEventUtcTime < time)
                {
                    _algorithm.SetDateTime(scheduledEvent.Value.NextEventUtcTime);

                    try
                    {
                        scheduledEvent.Value.Scan(scheduledEvent.Value.NextEventUtcTime);
                    }
                    catch (ScheduledEventException scheduledEventException)
                    {
                        var errorMessage = $"BacktestingRealTimeHandler.Run(): There was an error in a scheduled event {scheduledEvent.Key}. The error was {scheduledEventException.ScheduledEventExceptionMessage}";

                        Log.Error(errorMessage);

                        _resultHandler.RuntimeError(errorMessage);

                        // Errors in scheduled event should be treated as runtime error
                        // Runtime errors should end Lean execution
                        _algorithm.RunTimeError = new Exception(errorMessage);
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
    }
}