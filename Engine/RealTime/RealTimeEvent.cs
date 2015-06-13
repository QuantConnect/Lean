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
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Realtime event object for holding information on the event time and callback.
    /// </summary>
    public class RealTimeEvent
    {
        // Trigger Timing
        private readonly DateTime _triggerTime;
        private readonly Action _callback;
        private readonly bool _logging;

        // Trigger Action
        private bool _triggered;

        /// <summary>
        /// Flag indicating the event has been triggered
        /// </summary>
        public bool Triggered
        {
            get { return _triggered; }
        }

        /// <summary>
        /// Setup new event to fire at a specific time. Managed by a RealTimeHandler thread.
        /// </summary>
        /// <param name="triggerTime">Time of day to trigger this event</param>
        /// <param name="callback">Action to run when the time passes.</param>
        /// <param name="logging">Enable logging the realtime events</param>
        /// <seealso cref="IRealTimeHandler"/>
        public RealTimeEvent(DateTime triggerTime, Action callback, bool logging = false)
        {
            _triggered = false;
            _triggerTime = triggerTime;
            _callback = callback;
            _logging = logging;
        }

        /// <summary>
        /// Scan this event to see if this real time event has been triggered.
        /// </summary>
        /// <param name="time">Current real or simulation time</param>
        public void Scan(DateTime time)
        {
            if (_triggered)
            {
                return;
            }

            //When the time passes the trigger time, trigger the event.
            if (time > _triggerTime)
            {
                _triggered = true;

                try
                {
                    if (_logging)
                    {
                        Log.Trace("RealTimeEvent.Scan(): Eventhandler Called: " + time.ToString("u"));
                    }
                    _callback();
                }
                catch (Exception err)
                {
                    Log.Error("RealTimeEvent.Scan(): Error in callback: " + err.Message);
                }
            }
        }

        /// <summary>
        /// Reset the triggered flag.
        /// </summary>
        public void Reset()
        {
            _triggered = false;
        }
    }
}
