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
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Psuedo realtime event processing for backtesting to simulate realtime events in fast forward.
    /// </summary>
    public class BacktestingRealTimeHandler : IRealTimeHandler
    {
        private IAlgorithm _algorithm;
        private IResultHandler _resultHandler;
        private ConcurrentDictionary<string, ScheduledEvent> _scheduledEvents;

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
        public void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IApi api) 
        {
            //Initialize:
            _algorithm = algorithm;
            _resultHandler =  resultHandler;
            _scheduledEvents = new ConcurrentDictionary<string, ScheduledEvent>();

            // create events for algorithm's end of tradeable dates
            AddEvent(ScheduledEvent.EveryAlgorithmEndOfDay(_algorithm, _resultHandler, _algorithm.StartDate, _algorithm.EndDate, ScheduledEvent.AlgorithmEndOfDayDelta));

            // set up the events for each security to fire every tradeable date before market close
            foreach (var security in _algorithm.Securities.Values)
            {
                AddEvent(ScheduledEvent.EverySecurityEndOfDay(_algorithm, _resultHandler, security, algorithm.StartDate, _algorithm.EndDate, ScheduledEvent.SecurityEndOfDayDelta));
            }

            if (Log.DebuggingEnabled)
            {
                foreach (var scheduledEvent in _scheduledEvents)
                {
                    scheduledEvent.Value.IsLoggingEnabled = true;
                }
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
        /// Add a new event to our list of events to scan.
        /// </summary>
        /// <param name="newEvent">Event object to montitor daily.</param>
        public void AddEvent(ScheduledEvent newEvent)
        {
            _scheduledEvents[newEvent.Name] = newEvent;
            if (Log.DebuggingEnabled)
            {
                newEvent.IsLoggingEnabled = true;
            }
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
        /// Stop the real time thread
        /// </summary>
        public void Exit()
        {
            // this doesn't run as it's own thread, so nothing to exit
        }
    }
}