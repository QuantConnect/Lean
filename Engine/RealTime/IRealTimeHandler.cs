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
using System.ComponentModel.Composition;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Real time event handler, trigger functions at regular or pretimed intervals
    /// </summary>
    [InheritedExport(typeof(IRealTimeHandler))]
    public interface IRealTimeHandler
    {
        /// <summary>
        /// The real time handlers internal record of current time used to scan the events.
        /// </summary>
        DateTime Time 
        { 
            get;
        }

        /// <summary>
        /// List of events we're monitoring.
        /// </summary>
        List<RealTimeEvent> Events
        {
            get;
        }

        /// <summary>
        /// Thread status flag.
        /// </summary>
        bool IsActive
        {
            get;
        }

        /// <summary>
        /// Data for the Market Open Hours Today
        /// </summary>
        Dictionary<SecurityType, MarketToday> MarketToday
        {
            get;
        }

        /// <summary>
        /// Intializes the real time handler for the specified algorithm and job
        /// </summary>
        void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job);

        /// <summary>
        /// Main entry point to scan and trigger the realtime events.
        /// </summary>
        void Run();

        /// <summary>
        /// Given a list of events, set it up for this day.
        /// </summary>
        void SetupEvents(DateTime day);

        /// <summary>
        /// Add a new event to the processing list
        /// </summary>
        /// <param name="newEvent">Event information</param>
        void AddEvent(RealTimeEvent newEvent);
        
        /// <summary>
        /// Trigger a scan of the events.
        /// </summary>
        void ScanEvents();

        /// <summary>
        /// Reset all the event flags for a new day.
        /// </summary>
        /// <remarks>Realtime events are setup as a timespan hours since </remarks>
        void ResetEvents();

        /// <summary>
        /// Clear all the events in the list.
        /// </summary>
        void ClearEvents();

        /// <summary>
        /// Set the current time for the event scanner (so we can use same code for backtesting and live events)
        /// </summary>
        /// <param name="time">Current real or backtest time.</param>
        void SetTime(DateTime time);

        /// <summary>
        /// Trigger and exit signal to terminate real time event scanner.
        /// </summary>
        void Exit();
    }
}
