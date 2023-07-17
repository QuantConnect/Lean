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
using QuantConnect.Scheduling;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Provides methods for creating common scheduled events
    /// </summary>
    public static class ScheduledEventFactory
    {
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
        [Obsolete("This method is deprecated. It will generate ScheduledEvents for the deprecated IAlgorithm.OnEndOfDay()")]
        public static ScheduledEvent EveryAlgorithmEndOfDay(IAlgorithm algorithm, IResultHandler resultHandler, DateTime start, DateTime end, TimeSpan endOfDayDelta, DateTime? currentUtcTime = null)
        {
            if (endOfDayDelta >= Time.OneDay)
            {
                throw new ArgumentException("Delta must be less than a day", nameof(endOfDayDelta));
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

            // Log a message warning the user this EOD will be deprecated soon
            algorithm.Debug("Usage of QCAlgorithm.OnEndOfDay() without a symbol will be deprecated August 2021. Always use a symbol when overriding this method: OnEndOfDay(symbol)");

            return new ScheduledEvent(CreateEventName("Algorithm", "EndOfDay"), times, (name, triggerTime) =>
            {
                try
                {
                    algorithm.OnEndOfDay();
                }
                catch (Exception err)
                {
                    resultHandler.RuntimeError($"Runtime error in {name} event: {err.Message}", err.StackTrace);
                    Log.Error(err, $"ScheduledEvent.{name}:");
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
                throw new ArgumentException("Delta must be less than a day", nameof(endOfDayDelta));
            }

            var isMarketAlwaysOpen = security.Exchange.Hours.IsMarketAlwaysOpen;

            // define all the times we want this event to be fired, every tradeable day for the securtiy
            // at the delta time before market close expressed in UTC
            var times =
                // for every date the exchange is open for this security
                from date in Time.EachTradeableDay(security, start, end)
                // get the next market close for the specified date if the market closes at some point.
                // Otherwise, use the given date at midnight
                let marketClose = isMarketAlwaysOpen ?
                    date.Date.AddDays(1) : security.Exchange.Hours.GetNextMarketClose(date, security.IsExtendedMarketHours)
                // define the time of day we want the event to fire before marketclose
                let eventTime = isMarketAlwaysOpen ? marketClose : marketClose.Subtract(endOfDayDelta)
                // convert the event time into UTC
                let eventUtcTime = eventTime.ConvertToUtc(security.Exchange.TimeZone)
                // perform filter to verify it's not before the current time
                where !currentUtcTime.HasValue || eventUtcTime > currentUtcTime
                select eventUtcTime;

            return new ScheduledEvent(CreateEventName(security.Symbol.ToString(), "EndOfDay"), times, (name, triggerTime) =>
            {
                try
                {
                    algorithm.OnEndOfDay(security.Symbol);
                }
                catch (Exception err)
                {
                    resultHandler.RuntimeError($"Runtime error in {name} event: {err.Message}", err.StackTrace);
                    Log.Error(err, $"ScheduledEvent.{name}:");
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
            return $"{scope}.{name}";
        }
    }
}
