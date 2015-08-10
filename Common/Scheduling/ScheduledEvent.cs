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
using QuantConnect.Logging;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Real time self scheduling event
    /// </summary>
    public class ScheduledEvent : IDisposable
    {
        /// <summary>
        /// Gets the default time before market close end of trading day events will fire
        /// </summary>
        public static readonly TimeSpan SecurityEndOfDayDelta = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets the default time before midnight end of day events will fire
        /// </summary>
        public static readonly TimeSpan AlgorithmEndOfDayDelta = TimeSpan.FromMinutes(2);

        private bool _needsMoveNext = true;
        private bool _endOfScheduledEvents;

        private readonly string _name;
        private readonly Action<string, DateTime> _callback;
        private readonly IEnumerator<DateTime> _eventUtcTimes;

        /// <summary>
        /// Gets or sets whether this event will log each time it fires
        /// </summary>
        public bool IsLoggingEnabled
        {
            get; set;
        }

        /// <summary>
        /// Gets the next time this scheduled event will fire in UTC
        /// </summary>
        public DateTime NextEventUtcTime
        {
            get { return _endOfScheduledEvents ? DateTime.MaxValue : _eventUtcTimes.Current; }
        }

        /// <summary>
        /// Gets an identifier for this event
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Initalizes a new instance of the <see cref="ScheduledEvent"/> class
        /// </summary>
        /// <param name="name">An identifier for this event</param>
        /// <param name="eventUtcTime">The date time the event should fire</param>
        /// <param name="callback">Delegate to be called when the event time passes</param>
        public ScheduledEvent(string name, DateTime eventUtcTime, Action<string, DateTime> callback)
            : this(name, new[] { eventUtcTime }.AsEnumerable().GetEnumerator(), callback)
        {
        }

        /// <summary>
        /// Initalizes a new instance of the <see cref="ScheduledEvent"/> class
        /// </summary>
        /// <param name="name">An identifier for this event</param>
        /// <param name="eventUtcTimes">An enumerable that emits event times</param>
        /// <param name="callback">Delegate to be called each time an event passes</param>
        public ScheduledEvent(string name, IEnumerable<DateTime> eventUtcTimes, Action<string, DateTime> callback)
            : this(name, eventUtcTimes.GetEnumerator(), callback)
        {
        }

        /// <summary>
        /// Initalizes a new instance of the <see cref="ScheduledEvent"/> class
        /// </summary>
        /// <param name="name">An identifier for this event</param>
        /// <param name="eventUtcTimes">An enumerator that emits event times</param>
        /// <param name="callback">Delegate to be called each time an event passes</param>
        public ScheduledEvent(string name, IEnumerator<DateTime> eventUtcTimes, Action<string, DateTime> callback)
        {
            _name = name;
            _callback = callback;
            _eventUtcTimes = eventUtcTimes;
        }

        /// <summary>
        /// Scans this event and fires the callback if an event happened
        /// </summary>
        /// <param name="utcTime">The current time in UTC</param>
        public void Scan(DateTime utcTime)
        {
            if (_endOfScheduledEvents)
            {
                return;
            }

            if (_needsMoveNext)
            {
                // if we've passed an event or are just priming the pump, we need to move next
                if (!_eventUtcTimes.MoveNext())
                {
                    if (IsLoggingEnabled)
                    {
                        Log.Trace(string.Format("ScheduledEvent.{0}: Completed scheduled events.", Name));
                    }
                    _endOfScheduledEvents = true;
                    return;
                }
                if (IsLoggingEnabled)
                {
                    Log.Trace(string.Format("ScheduledEvent.{0}: Next event: {1} UTC", Name, _eventUtcTimes.Current.ToString(DateFormat.UI)));
                }
            }

            // if time has passed our event
            if (utcTime >= _eventUtcTimes.Current)
            {
                if (IsLoggingEnabled)
                {
                    Log.Trace(string.Format("ScheduledEvent.{0}: Firing at {1} UTC Scheduled at {2} UTC", Name,
                        utcTime.ToString(DateFormat.UI),
                        _eventUtcTimes.Current.ToString(DateFormat.UI))
                        );
                }
                // fire the event
                _callback(_name, _eventUtcTimes.Current);
                _needsMoveNext = true;
            }
            else
            {
                // we haven't passed the event time yet, so keep waiting on this Current
                _needsMoveNext = false;
            }
        }

        /// <summary>
        /// Fast forwards this schedule to the specified time without invoking the events
        /// </summary>
        /// <param name="utcTime">Frontier time</param>
        public void SkipEventsUntil(DateTime utcTime)
        {
            while (_eventUtcTimes.MoveNext())
            {
                // zoom through the enumerator until we get to the desired time
                if (utcTime <= _eventUtcTimes.Current)
                {
                    // pump is primed and ready to go
                    _needsMoveNext = false;

                    if (IsLoggingEnabled)
                    {
                        Log.Trace(string.Format("ScheduledEvent.{0}: Skipped events before {1}. Next event: {2}", Name,
                            utcTime.ToString(DateFormat.UI),
                            _eventUtcTimes.Current.ToString(DateFormat.UI)
                            ));
                    }
                    return;
                }
            }
            if (IsLoggingEnabled)
            {
                Log.Trace(string.Format("ScheduledEvent.{0}: Exhausted event stream during skip until {1}", Name,
                    utcTime.ToString(DateFormat.UI)
                    ));
            }
            _endOfScheduledEvents = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _eventUtcTimes.Dispose();
        }
    }
}
