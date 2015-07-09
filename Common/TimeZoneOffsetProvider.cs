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

namespace QuantConnect
{
    /// <summary>
    /// Represents the discontinuties in a single time zone and provides offsets to UTC.
    /// This type assumes that times will be asked in a forward marching manner.
    /// This type is not thread safe.
    /// </summary>
    public class TimeZoneOffsetProvider
    {
        private long _nextDiscontinuity;
        private long _currentOffsetTicks;
        private readonly DateTimeZone _timeZone;
        private readonly Queue<long> _discontinuities;

        public DateTime UtcNextDiscontinuity
        {
            get { return new DateTime(_nextDiscontinuity); }
        }

        public TimeSpan CurrentUtcOffset
        {
            get { return TimeSpan.FromTicks(_currentOffsetTicks); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeZoneOffsetProvider"/> class
        /// </summary>
        /// <param name="timeZone">The time zone to provide offsets for</param>
        /// <param name="utcStartTime">The start of the range of offsets</param>
        /// <param name="utcEndTime">The en of the range of offsets</param>
        public TimeZoneOffsetProvider(DateTimeZone timeZone, DateTime utcStartTime, DateTime utcEndTime)
        {
            _timeZone = timeZone;

            // pad the end so we get the correct zone interval
            utcEndTime += TimeSpan.FromDays(2*365);

            var start = DateTimeZone.Utc.AtLeniently(LocalDateTime.FromDateTime(utcStartTime));
            var end = DateTimeZone.Utc.AtLeniently(LocalDateTime.FromDateTime(utcEndTime));
            var zoneIntervals = _timeZone.GetZoneIntervals(start.ToInstant(), end.ToInstant());
            _discontinuities = new Queue<long>(zoneIntervals.Select(x => x.Start.ToDateTimeUtc().Ticks));

            var disc = _discontinuities.ToList();
            var t = new DateTime(disc[0]);

            if (_discontinuities.Count == 0)
            {
                // end of discontinuities
                _nextDiscontinuity = DateTime.MaxValue.Ticks;
                _currentOffsetTicks = _timeZone.GetUtcOffset(Instant.FromDateTimeUtc(DateTime.UtcNow)).Ticks;
            }
            else
            {
                // get the offset just before the next discontinuity to initialize
                _nextDiscontinuity = _discontinuities.Dequeue();
                _currentOffsetTicks = _timeZone.GetUtcOffset(Instant.FromDateTimeUtc(new DateTime(_nextDiscontinuity - 1, DateTimeKind.Utc))).Ticks;
            }
        }

        /// <summary>
        /// Gets the offset in ticks from this time zone to UTC, such that UTC time + offset = local time
        /// </summary>
        /// <param name="utcTime">The time in UTC to get an offset to local</param>
        /// <returns>The offset in ticks between UTC and the local time zone</returns>
        public long GetOffsetTicks(DateTime utcTime)
        {
            while (utcTime.Ticks >= _nextDiscontinuity)
            {
                // grab the next discontinuity
                _nextDiscontinuity = _discontinuities.Count == 0 
                    ? DateTime.MaxValue.Ticks 
                    : _discontinuities.Dequeue();

                // get the offset just before the next discontinuity
                var offset = _timeZone.GetUtcOffset(Instant.FromDateTimeUtc(new DateTime(_nextDiscontinuity - 1, DateTimeKind.Utc)));
                _currentOffsetTicks = offset.Ticks;
            }

            return _currentOffsetTicks;
        }
    }
}
