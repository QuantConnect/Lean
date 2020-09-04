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
using System.Runtime.CompilerServices;
using NodaTime;
using NodaTime.TimeZones;

namespace QuantConnect
{
    /// <summary>
    /// Represents the discontinuties in a single time zone and provides offsets to UTC.
    /// This type assumes that times will be asked in a forward marching manner.
    /// This type is not thread safe.
    /// </summary>
    public class TimeZoneOffsetProvider
    {
        private static readonly long DateTimeMaxValueTicks = DateTime.MaxValue.Ticks;

        private long _nextDiscontinuity;
        private long _currentOffsetTicks;
        private readonly DateTimeZone _timeZone;
        private readonly Queue<long> _discontinuities;

        /// <summary>
        /// Gets the time zone this instances provides offsets for
        /// </summary>
        public DateTimeZone TimeZone
        {
            get { return _timeZone; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeZoneOffsetProvider"/> class
        /// </summary>
        /// <param name="timeZone">The time zone to provide offsets for</param>
        /// <param name="utcStartTime">The start of the range of offsets</param>
        /// <param name="utcEndTime">The end of the range of offsets</param>
        public TimeZoneOffsetProvider(DateTimeZone timeZone, DateTime utcStartTime, DateTime utcEndTime)
        {
            _timeZone = timeZone;

            // pad the end so we get the correct zone interval
            utcEndTime += TimeSpan.FromDays(2*365);

            var start = DateTimeZone.Utc.AtLeniently(LocalDateTime.FromDateTime(utcStartTime));
            var end = DateTimeZone.Utc.AtLeniently(LocalDateTime.FromDateTime(utcEndTime));
            var zoneIntervals = _timeZone.GetZoneIntervals(start.ToInstant(), end.ToInstant()).ToList();

            // short circuit time zones with no discontinuities
            if (zoneIntervals.Count == 1 && zoneIntervals[0].Start == Instant.MinValue && zoneIntervals[0].End == Instant.MaxValue)
            {
                // end of discontinuities
                _discontinuities = new Queue<long>();
                _nextDiscontinuity = DateTime.MaxValue.Ticks;
                _currentOffsetTicks = _timeZone.GetUtcOffset(Instant.FromDateTimeUtc(DateTime.UtcNow)).Ticks;
            }
            else
            {
                // get the offset just before the next discontinuity to initialize
                _discontinuities = new Queue<long>(zoneIntervals.Select(GetDateTimeUtcTicks));
                _nextDiscontinuity = _discontinuities.Dequeue();
                _currentOffsetTicks = _timeZone.GetUtcOffset(Instant.FromDateTimeUtc(new DateTime(_nextDiscontinuity - 1, DateTimeKind.Utc))).Ticks;
            }
        }

        /// <summary>
        /// Gets the offset in ticks from this time zone to UTC, such that UTC time + offset = local time
        /// </summary>
        /// <param name="utcTime">The time in UTC to get an offset to local</param>
        /// <returns>The offset in ticks between UTC and the local time zone</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetOffsetTicks(DateTime utcTime)
        {
            // keep advancing our discontinuity until the requested time, don't recompute if already at max value
            while (utcTime.Ticks >= _nextDiscontinuity && _nextDiscontinuity != DateTimeMaxValueTicks)
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

        /// <summary>
        /// Converts the specified local time to UTC. This function will advance this offset provider
        /// </summary>
        /// <param name="localTime">The local time to be converted to UTC</param>
        /// <returns>The specified time in UTC</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime ConvertToUtc(DateTime localTime)
        {
            // it's important to walk forward to the next time zone discontinuity
            // to ensure a deterministic read. We continue reading with the current
            // offset until the converted value is beyond the next discontinuity, at
            // which time we advance the offset again.
            var currentEndTimeTicks = localTime.Ticks;
            var currentEndTimeUtc = new DateTime(currentEndTimeTicks - _currentOffsetTicks);
            var offsetTicks = GetOffsetTicks(currentEndTimeUtc);
            var emitTimeUtcTicks = currentEndTimeTicks - offsetTicks;
            while (emitTimeUtcTicks > _nextDiscontinuity)
            {
                // advance to the next discontinuity to get the new offset
                offsetTicks = GetOffsetTicks(new DateTime(_nextDiscontinuity));
                emitTimeUtcTicks = currentEndTimeTicks - offsetTicks;
            }

            return new DateTime(emitTimeUtcTicks);
        }

        /// <summary>
        /// Gets this offset provider's next discontinuity
        /// </summary>
        /// <returns>The next discontinuity in UTC ticks</returns>
        public long GetNextDiscontinuity()
        {
            return _nextDiscontinuity;
        }

        /// <summary>
        /// Converts the specified <paramref name="utcTime"/> using the offset resolved from
        /// a call to <see cref="GetOffsetTicks"/>
        /// </summary>
        /// <param name="utcTime">The time to convert from utc</param>
        /// <returns>The same instant in time represented in the <see cref="TimeZone"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual DateTime ConvertFromUtc(DateTime utcTime)
        {
            return new DateTime(utcTime.Ticks + GetOffsetTicks(utcTime));
        }

        /// <summary>
        /// Gets the zone interval's start time in DateTimeKind.Utc ticks
        /// </summary>
        private static long GetDateTimeUtcTicks(ZoneInterval zoneInterval)
        {
            // can't convert these values directly to date times, so just shortcut these here
            // we set the min value to one since the logic in the ctor will decrement this value to
            // determine the last instant BEFORE the discontinuity
            if (zoneInterval.Start == Instant.MinValue) return 1;
            if (zoneInterval.Start == Instant.MaxValue) return DateTime.MaxValue.Ticks;

            return zoneInterval.Start.ToDateTimeUtc().Ticks;
        }
    }
}
