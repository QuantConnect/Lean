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
*/

using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantConnect.Interfaces;

namespace QuantConnect
{
    /// <summary>
    /// Provides a means of centralizing time for various time zones.
    /// </summary>
    public class TimeKeeper : ITimeKeeper
    {
        private DateTime _utcDateTime;

        private readonly Dictionary<string, LocalTimeKeeper> _localTimeKeepers;

        /// <summary>
        /// Gets the current time in UTC
        /// </summary>
        public DateTime UtcTime
        {
            get { return _utcDateTime; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeKeeper"/> class at the specified
        /// UTC time and for the specified time zones. Each time zone specified will cause the
        /// creation of a <see cref="LocalTimeKeeper"/> to handle conversions for that time zone.
        /// </summary>
        /// <param name="utcDateTime">The initial time</param>
        /// <param name="timeZones">The time zones used to instantiate <see cref="LocalTimeKeeper"/> instances.</param>
        public TimeKeeper(DateTime utcDateTime, params DateTimeZone[] timeZones)
            : this(utcDateTime, timeZones ?? Enumerable.Empty<DateTimeZone>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeKeeper"/> class at the specified
        /// UTC time and for the specified time zones. Each time zone specified will cause the
        /// creation of a <see cref="LocalTimeKeeper"/> to handle conversions for that time zone.
        /// </summary>
        /// <param name="utcDateTime">The initial time</param>
        /// <param name="timeZones">The time zones used to instantiate <see cref="LocalTimeKeeper"/> instances.</param>
        public TimeKeeper(DateTime utcDateTime, IEnumerable<DateTimeZone> timeZones)
        {
            _utcDateTime = utcDateTime;
            _localTimeKeepers = timeZones.Distinct().Select(x => new LocalTimeKeeper(utcDateTime, x)).ToDictionary(x => x.TimeZone.Id);
        }

        /// <summary>
        /// Sets the current UTC time for this time keeper and the attached child <see cref="LocalTimeKeeper"/> instances.
        /// </summary>
        /// <param name="utcDateTime">The current time in UTC</param>
        public void SetUtcDateTime(DateTime utcDateTime)
        {
            _utcDateTime = utcDateTime;
            foreach (var timeZone in _localTimeKeepers)
            {
                timeZone.Value.UpdateTime(utcDateTime);
            }
        }

        /// <summary>
        /// Gets the local time in the specified time zone. If the specified <see cref="DateTimeZone"/>
        /// has not already been added, this will throw a <see cref="KeyNotFoundException"/>.
        /// </summary>
        /// <param name="timeZone">The time zone to get local time for</param>
        /// <returns>The local time in the specifed time zone</returns>
        public DateTime GetTimeIn(DateTimeZone timeZone)
        {
            return GetLocalTimeKeeper(timeZone).LocalTime;
        }

        /// <summary>
        /// Gets the <see cref="LocalTimeKeeper"/> instance for the specified time zone
        /// </summary>
        /// <param name="timeZone">The time zone whose <see cref="LocalTimeKeeper"/> we seek</param>
        /// <returns>The <see cref="LocalTimeKeeper"/> instance for the specified time zone</returns>
        public LocalTimeKeeper GetLocalTimeKeeper(DateTimeZone timeZone)
        {
            LocalTimeKeeper localTimeKeeper;
            if (!_localTimeKeepers.TryGetValue(timeZone.Id, out localTimeKeeper))
            {
                localTimeKeeper = new LocalTimeKeeper(UtcTime, timeZone);
                _localTimeKeepers[timeZone.Id] = localTimeKeeper;
            }
            return localTimeKeeper;
        }

        /// <summary>
        /// Adds the specified time zone to this time keeper
        /// </summary>
        /// <param name="timeZone"></param>
        public void AddTimeZone(DateTimeZone timeZone)
        {
            if (!_localTimeKeepers.ContainsKey(timeZone.Id))
            {
                _localTimeKeepers[timeZone.Id] = new LocalTimeKeeper(_utcDateTime, timeZone);
            }
        }
    }
}
