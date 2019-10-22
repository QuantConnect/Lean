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

namespace QuantConnect.Util.RateLimit
{
    /// <summary>
    /// Provides a refill strategy that has a constant, quantized refill rate.
    /// For example, after 1 minute passes add 5 units. If 59 seconds has passed, it will add zero unit,
    /// but if 2 minutes have passed, then 10 units would be added.
    /// </summary>
    public class FixedIntervalRefillStrategy : IRefillStrategy
    {
        private readonly object _sync = new object();

        private long _nextRefillTimeTicks;

        private readonly long _refillAmount;
        private readonly long _refillIntervalTicks;
        private readonly ITimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedIntervalRefillStrategy"/> class.
        /// </summary>
        /// <param name="timeProvider">Provides the current time used for determining how much time has elapsed
        /// between invocations of the refill method</param>
        /// <param name="refillAmount">Defines the constant number of tokens to be made available for consumption
        /// each time the provided <paramref name="refillInterval"/> has passed</param>
        /// <param name="refillInterval">The amount of time that must pass before adding the specified <paramref name="refillAmount"/>
        /// back to the bucket</param>
        public FixedIntervalRefillStrategy(ITimeProvider timeProvider, long refillAmount, TimeSpan refillInterval)
        {
            _timeProvider = timeProvider;
            _refillAmount = refillAmount;
            _refillIntervalTicks = refillInterval.Ticks;
            _nextRefillTimeTicks = _timeProvider.GetUtcNow().Ticks + _refillIntervalTicks;
        }

        /// <summary>
        /// Computes the number of new tokens made available to the bucket for consumption by determining the
        /// number of time intervals that have passed and multiplying by the number of tokens to refill for
        /// each time interval.
        /// </summary>
        public long Refill()
        {
            lock (_sync)
            {
                var currentTimeTicks = _timeProvider.GetUtcNow().Ticks;
                if (currentTimeTicks < _nextRefillTimeTicks)
                {
                    return 0L;
                }

                // determine number of time increments that have passed
                var deltaTimeTicks = currentTimeTicks - _nextRefillTimeTicks;
                var intervalsElapsed = 1 + Math.Max(deltaTimeTicks / _refillIntervalTicks, 0);

                // update next refill time as quantized via the number of passed intervals
                _nextRefillTimeTicks += _refillIntervalTicks * intervalsElapsed;

                // refill by the tokens per interval times the number of intervals elapsed
                return _refillAmount * intervalsElapsed;
            }
        }
    }
}