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
using System.Collections;
using System.Collections.Generic;
using NodaTime;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// This enumerator will filter out data of the underlying enumerator based on a provided schedule
    /// </summary>
    public class ScheduledEnumerator : IEnumerator<BaseData>
    {
        private readonly IEnumerator<BaseData> _underlyingEnumerator;
        private readonly IEnumerator<DateTime> _scheduledTimes;
        private readonly ITimeProvider _frontierTimeProvider;
        private readonly DateTimeZone _scheduleTimeZone;
        private bool _scheduledTimesEnded;
        private BaseData _underlyingCandidateDataPoint;

        /// <summary>
        /// The current data point
        /// </summary>
        public BaseData Current { get; private set; }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="underlyingEnumerator">The underlying enumerator to filter</param>
        /// <param name="scheduledTimes">The scheduled times to emit new data points</param>
        /// <param name="frontierTimeProvider"></param>
        /// <param name="scheduleTimeZone"></param>
        public ScheduledEnumerator(IEnumerator<BaseData> underlyingEnumerator,
            IEnumerable<DateTime> scheduledTimes, 
            ITimeProvider frontierTimeProvider,
            DateTimeZone scheduleTimeZone)
        {
            _scheduleTimeZone = scheduleTimeZone;
            _frontierTimeProvider = frontierTimeProvider;
            _underlyingEnumerator = underlyingEnumerator;
            _scheduledTimes = scheduledTimes.GetEnumerator();
            if (!_scheduledTimes.MoveNext())
            {
                throw new ArgumentException("ScheduledEnumerator(): There should be at least 1 provided schedule time");
            }
        }

        /// <summary>
        /// Disposes of the underlying enumerator
        /// </summary>
        public void Dispose()
        {
            _scheduledTimes.Dispose();
            _underlyingEnumerator.Dispose();
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns> True if the enumerator was successfully advanced to the next element;
        /// false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            if (_scheduledTimesEnded)
            {
                Current = null;
                return false;
            }

            // lets get our candidate data point to emit
            if (_underlyingCandidateDataPoint == null && _underlyingEnumerator.Current != null)
            {
                _underlyingCandidateDataPoint = _underlyingEnumerator.Current;
            }

            // lets try to get a better candidate
            if (_underlyingEnumerator.Current == null
                || _underlyingEnumerator.Current.EndTime < _scheduledTimes.Current)
            {
                bool pullAgain;
                do
                {
                    pullAgain = false;
                    if (!_underlyingEnumerator.MoveNext())
                    {
                        if (_underlyingCandidateDataPoint != null)
                        {
                            // if we still have a candidate wait till we emit him before stopping
                            break;
                        }
                        Current = null;
                        return false;
                    }

                    if (_underlyingEnumerator.Current != null
                        && _underlyingEnumerator.Current.EndTime <= _scheduledTimes.Current)
                    {
                        // we got another data point which is a newer candidate to emit so let use it instead
                        // and drop the previous
                        _underlyingCandidateDataPoint = _underlyingEnumerator.Current;

                        // lets try again
                        pullAgain = true;
                    }
                } while (pullAgain);
            }

            // if we are at or past the schedule time we try to emit
            if (_underlyingCandidateDataPoint != null
                && _scheduledTimes.Current.ConvertToUtc(_scheduleTimeZone) <= _frontierTimeProvider.GetUtcNow())
            {
                Current = _underlyingCandidateDataPoint;
                _scheduledTimesEnded = !_scheduledTimes.MoveNext();
                _underlyingCandidateDataPoint = null;
                return true;
            }

            Current = null;
            return true;
        }

        /// <summary>
        /// Resets the underlying enumerator
        /// </summary>
        public void Reset()
        {
            _underlyingEnumerator.Reset();
        }
    }
}
