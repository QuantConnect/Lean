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
using NodaTime;
using QuantConnect.Data;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// This enumerator will filter out data of the underlying enumerator based on a provided schedule.
    /// Will respect the schedule above the data, meaning will let older data through if the underlying provides none for the schedule date
    /// </summary>
    public class ScheduledEnumerator : IEnumerator<BaseData>
    {
        private readonly IEnumerator<BaseData> _underlyingEnumerator;
        private readonly IEnumerator<DateTime> _scheduledTimes;
        private readonly ITimeProvider _frontierTimeProvider;
        private readonly DateTimeZone _scheduleTimeZone;
        private BaseData _underlyingCandidateDataPoint;
        private bool _scheduledTimesEnded;

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
        /// <param name="startTime">the underlying request start time</param>
        public ScheduledEnumerator(IEnumerator<BaseData> underlyingEnumerator,
            IEnumerable<DateTime> scheduledTimes,
            ITimeProvider frontierTimeProvider,
            DateTimeZone scheduleTimeZone,
            DateTime startTime)
        {
            _scheduleTimeZone = scheduleTimeZone;
            _frontierTimeProvider = frontierTimeProvider;
            _underlyingEnumerator = underlyingEnumerator;
            _scheduledTimes = scheduledTimes.GetEnumerator();
            // move our schedule enumerator to current start time
            MoveScheduleForward(startTime);
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
            if (_underlyingCandidateDataPoint == null)
            {
                if (_underlyingEnumerator.Current != null && _underlyingEnumerator.Current.EndTime <= _scheduledTimes.Current)
                {
                    _underlyingCandidateDataPoint = _underlyingEnumerator.Current;
                }
                else if (Current != null)
                {
                    // we will keep the last data point, even if we already emitted it, there could be a case where the user has a schedule in a
                    // period where there's not new data (or it's far in the future) so let's just FF the previous point
                    _underlyingCandidateDataPoint = Current.Clone(fillForward: true);
                }
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

                    if (_underlyingEnumerator.Current != null)
                    {
                        if (_underlyingEnumerator.Current.EndTime <= _scheduledTimes.Current)
                        {
                            // lets try again
                            pullAgain = true;
                            // we got another data point which is a newer candidate to emit so let use it instead
                            // and drop the previous
                            _underlyingCandidateDataPoint = _underlyingEnumerator.Current;
                        }
                        else if (_underlyingCandidateDataPoint == null)
                        {
                            // this is the first data point we got and it's After our schedule, let's move our schedule forward
                            _underlyingCandidateDataPoint = _underlyingEnumerator.Current;
                            MoveScheduleForward();
                        }
                    }
                } while (pullAgain);
            }

            if (_underlyingCandidateDataPoint != null
            // if we are at or past the schedule time we try to emit, in backtest this emits right away, since time is data driven, in live though
            // we don't emit right away because the underlying might provide us with a newer data point
                && _scheduledTimes.Current.ConvertToUtc(_scheduleTimeZone) <= GetUtcNow())
            {
                Current = _underlyingCandidateDataPoint;
                // we align the data endtime with the schedule, we respect the schedule above the data time. In backtesting,
                // time is driven by the data, so let's make sure we emit at the scheduled time even if the data is older
                Current.EndTime = _scheduledTimes.Current;
                if (Current.Time > Current.EndTime)
                {
                    Current.Time = _scheduledTimes.Current;
                }

                MoveScheduleForward();
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

        /// <summary>
        /// Disposes of the underlying enumerator
        /// </summary>
        public void Dispose()
        {
            _scheduledTimes.Dispose();
            _underlyingEnumerator.Dispose();
        }

        /// <summary>
        /// Available in live trading only, in backtesting frontier is driven and sycned already by the data itself
        /// so we can't hold data here based on it
        /// </summary>
        private DateTime GetUtcNow()
        {
            if (_frontierTimeProvider != null)
            {
                return _frontierTimeProvider.GetUtcNow();
            }
            return DateTime.MaxValue;
        }

        private void MoveScheduleForward(DateTime? frontier = null)
        {
            do
            {
                _scheduledTimesEnded = !_scheduledTimes.MoveNext();
            }
            while (!_scheduledTimesEnded && frontier.HasValue && _scheduledTimes.Current < frontier.Value);
        }
    }
}
