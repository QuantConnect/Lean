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
using NodaTime;

namespace QuantConnect
{
    /// <summary>
    /// Represents the current local time. This object is created via the <see cref="TimeKeeper"/> to
    /// manage conversions to local time.
    /// </summary>
    public class LocalTimeKeeper
    {
        private Lazy<DateTime> _localTime;
        private readonly DateTimeZone _timeZone;

        /// <summary>
        /// Event fired each time <see cref="UpdateTime"/> is called
        /// </summary>
        public event EventHandler<TimeUpdatedEventArgs> TimeUpdated;

        /// <summary>
        /// Gets the time zone of this <see cref="LocalTimeKeeper"/>
        /// </summary>
        public DateTimeZone TimeZone
        {
            get { return _timeZone; }
        }

        /// <summary>
        /// Gets the current time in terms of the <see cref="TimeZone"/>
        /// </summary>
        public DateTime LocalTime
        {
            get { return _localTime.Value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalTimeKeeper"/> class
        /// </summary>
        /// <param name="utcDateTime">The current time in UTC</param>
        /// <param name="timeZone">The time zone</param>
        internal LocalTimeKeeper(DateTime utcDateTime, DateTimeZone timeZone)
        {
            _timeZone = timeZone;
            _localTime = new Lazy<DateTime>(() => utcDateTime.ConvertTo(DateTimeZone.Utc, _timeZone));
        }

        /// <summary>
        /// Updates the current time of this time keeper
        /// </summary>
        /// <param name="utcDateTime">The current time in UTC</param>
        internal void UpdateTime(DateTime utcDateTime)
        {
            // redefine the lazy conversion each time this is set
            _localTime = new Lazy<DateTime>(() => utcDateTime.ConvertTo(DateTimeZone.Utc, _timeZone));
            if (TimeUpdated != null)
            {
                TimeUpdated(this, new TimeUpdatedEventArgs(_localTime.Value, TimeZone));
            }
        }
    }
}