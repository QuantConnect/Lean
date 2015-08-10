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
using NodaTime;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Specifies that an event should fire at a specific local time each day
    /// </summary>
    public class SpecificTimeTimeRule : ITimeRule
    {
        private readonly TimeSpan _timeOfDay;
        private readonly DateTimeZone _timeZone;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecificTimeTimeRule"/> class
        /// </summary>
        /// <param name="timeZone">The time zone the event times reference</param>
        /// <param name="timeOfDay">The time of day in the time zone the event should fire</param>
        public SpecificTimeTimeRule(DateTimeZone timeZone, TimeSpan timeOfDay)
        {
            _timeZone = timeZone;
            _timeOfDay = timeOfDay;
        }

        /// <summary>
        /// Gets a name for this rule
        /// </summary>
        public string Name
        {
            get { return _timeOfDay.TotalHours.ToString("0.##"); }
        }

        /// <summary>
        /// Creates the event times for the specified dates in UTC
        /// </summary>
        /// <param name="dates">The dates to apply times to</param>
        /// <returns>An enumerable of date times that is the result
        /// of applying this rule to the specified dates</returns>
        public IEnumerable<DateTime> CreateUtcEventTimes(IEnumerable<DateTime> dates)
        {
            return from date in dates
                   let localEventTime = date + _timeOfDay
                   let utcEventTime = localEventTime.ConvertToUtc(_timeZone)
                   select utcEventTime;

        }
    }
}