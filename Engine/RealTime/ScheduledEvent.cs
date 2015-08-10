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
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.RealTime
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
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _eventUtcTimes.Dispose();
        }

        /// <summary>
        /// Creates a new <see cref="ScheduledEvent"/> that will fire at the specified <paramref name="timeOfDay"/> for every day in
        /// <paramref name="dates"/>
        /// </summary>
        /// <param name="name">An identifier for this event</param>
        /// <param name="dates">The dates to set events for at the specified time. These act as a base time to which
        /// the <paramref name="timeOfDay"/> is added to, that is, the implementation does not use .Date before
        /// the addition</param>
        /// <param name="timeOfDay">The time each tradeable date to fire the event</param>
        /// <param name="callback">The delegate to call when an event fires</param>
        /// <param name="currentUtcTime">Specfies the current time in UTC, before which, no events will be scheduled. Specify null to skip this filter.</param>
        /// <returns>A new <see cref="ScheduledEvent"/> instance that fires events each tradeable day from the start to the finish at the specified time</returns>
        public static ScheduledEvent EveryDayAt(string name, IEnumerable<DateTime> dates, TimeSpan timeOfDay, Action<string, DateTime> callback, DateTime? currentUtcTime = null)
        {
            var eventTimes = dates.Select(x => x.Date + timeOfDay);
            if (currentUtcTime.HasValue)
            {
                eventTimes = eventTimes.Where(x => x < currentUtcTime.Value);
            }
            return new ScheduledEvent(name, eventTimes, callback);
        }

        /// <summary>
        /// Creates a new <see cref="ScheduledEvent"/> that will fire before market close by the specified time 
        /// </summary>
        /// <param name="algorithm">The algorithm instance the event is fo</param>
        /// <param name="resultHandler">The result handler, used to communicate run time errors</param>
        /// <param name="start">The date to start the events</param>
        /// <param name="end">The date to end the events</param>
        /// <param name="endOfDayDelta">The time difference between the market close and the event, positive time will fire before market close</param>
        /// <param name="currentUtcTime">Specfies the current time in UTC, before which, no events will be scheduled. Specify null to skip this filter.</param>
        /// <returns>The new <see cref="ScheduledEvent"/> that will fire near market close each tradeable dat</returns>
        public static ScheduledEvent EveryAlgorithmEndOfDay(IAlgorithm algorithm, IResultHandler resultHandler, DateTime start, DateTime end, TimeSpan endOfDayDelta, DateTime? currentUtcTime = null)
        {
            if (endOfDayDelta >= Time.OneDay)
            {
                throw new ArgumentException("Delta must be less than a day", "endOfDayDelta");
            }

            // set up an event to fire every tradeable date for the algorithm as a whole
            var eodEventTime = Time.OneDay.Subtract(endOfDayDelta);

            // create enumerable of end of day in algorithm's time zone
            var times =
                // for every date any exchange is open in the algorithm
                from date in Time.EachTradeableDay(algorithm.Securities.Values, start, end)
                // define the time of day we want the event to fire, a little before midnight
                let eventTime = date + eodEventTime
                // convert the event time into UTC
                let eventUtcTime = eventTime.ConvertToUtc(algorithm.TimeZone)
                // perform filter to verify it's not before the current time
                where !currentUtcTime.HasValue || eventUtcTime > currentUtcTime.Value
                select eventUtcTime;

            return new ScheduledEvent(CreateEventName("Algorithm", "EndOfDay"), times, (name, triggerTime) =>
            {
                Log.Debug(string.Format("ScheduledEvent.{0}: Firing at {1}", name, triggerTime));
                try
                {
                    algorithm.OnEndOfDay();
                    Log.Debug(string.Format("ScheduledEvent.{0}: Fired On End of Day Event() for Day({1})", name, triggerTime.ToShortDateString()));
                }
                catch (Exception err)
                {
                    resultHandler.RuntimeError(string.Format("Runtime error in {0} event: {1}", name, err.Message), err.StackTrace);
                    Log.Error(string.Format("ScheduledEvent.{0}: {1}", name, err.Message));
                }
            });
        }

        /// <summary>
        /// Creates a new <see cref="ScheduledEvent"/> that will fire before market close by the specified time 
        /// </summary>
        /// <param name="algorithm">The algorithm instance the event is fo</param>
        /// <param name="resultHandler">The result handler, used to communicate run time errors</param>
        /// <param name="security">The security used for defining tradeable dates</param>
        /// <param name="start">The first date for the events</param>
        /// <param name="end">The date to end the events</param>
        /// <param name="endOfDayDelta">The time difference between the market close and the event, positive time will fire before market close</param>
        /// <param name="currentUtcTime">Specfies the current time in UTC, before which, no events will be scheduled. Specify null to skip this filter.</param>
        /// <returns>The new <see cref="ScheduledEvent"/> that will fire near market close each tradeable dat</returns>
        public static ScheduledEvent EverySecurityEndOfDay(IAlgorithm algorithm, IResultHandler resultHandler, Security security, DateTime start, DateTime end, TimeSpan endOfDayDelta, DateTime? currentUtcTime = null)
        {
            if (endOfDayDelta >= Time.OneDay)
            {
                throw new ArgumentException("Delta must be less than a day", "endOfDayDelta");
            }

            // define all the times we want this event to be fired, every tradeable day for the securtiy
            // at the delta time before market close expressed in UTC
            var times =
                // for every date the exchange is open for this security
                from date in Time.EachTradeableDay(security, start, end)
                // get the next market close for the specified date
                let marketClose = security.Exchange.Hours.GetNextMarketClose(date, security.IsExtendedMarketHours)
                // define the time of day we want the event to fire before marketclose
                let eventTime = marketClose.Subtract(endOfDayDelta)
                // convert the event time into UTC
                let eventUtcTime = eventTime.ConvertToUtc(security.Exchange.TimeZone)
                // perform filter to verify it's not before the current time
                where !currentUtcTime.HasValue || eventUtcTime > currentUtcTime
                select eventUtcTime;

            return new ScheduledEvent(CreateEventName(security.Symbol, "EndOfDay"), times, (name, triggerTime) =>
            {
                try
                {
                    algorithm.OnEndOfDay(security.Symbol);
                }
                catch (Exception err)
                {
                    resultHandler.RuntimeError(string.Format("Runtime error in {0} event: {1}", name, err.Message), err.StackTrace);
                    Log.Error(string.Format("ScheduledEvent.{0}: {1}", name, err.Message));
                }
            });
        }

        /// <summary>
        /// Defines the format of event names generated by this system.
        /// </summary>
        /// <param name="scope">The scope of the event, example, 'Algorithm' or 'Security'</param>
        /// <param name="name">A name for this specified event in this scope, example, 'EndOfDay'</param>
        /// <returns>A string representing a fully scoped event name</returns>
        public static string CreateEventName(string scope, string name)
        {
            return string.Format("{0}.{1}", scope, name);
        }
    }
}
