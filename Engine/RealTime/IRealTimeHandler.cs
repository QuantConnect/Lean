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
using System.ComponentModel.Composition;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Scheduling;

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Real time event handler, trigger functions at regular or pretimed intervals
    /// </summary>
    [InheritedExport(typeof(IRealTimeHandler))]
    public interface IRealTimeHandler : IEventSchedule
    {
        /// <summary>
        /// Thread status flag.
        /// </summary>
        bool IsActive
        {
            get;
        }

        /// <summary>
        /// Intializes the real time handler for the specified algorithm and job
        /// </summary>
        void Setup(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IApi api);

        /// <summary>
        /// Main entry point to scan and trigger the realtime events.
        /// </summary>
        void Run();

        /// <summary>
        /// Set the current time for the event scanner (so we can use same code for backtesting and live events)
        /// </summary>
        /// <param name="time">Current real or backtest time.</param>
        void SetTime(DateTime time);

        /// <summary>
        /// Scan for past events that didn't fire because there was no data at the scheduled time.
        /// </summary>
        /// <param name="time">Current time.</param>
        void ScanPastEvents(DateTime time);

        /// <summary>
        /// Trigger and exit signal to terminate real time event scanner.
        /// </summary>
        void Exit();

        /// <summary>
        /// Event fired each time that we add/remove securities from the data feed
        /// </summary>
        void OnSecuritiesChanged(SecurityChanges changes);
    }
}
