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

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Provides augmentation of how often an enumerator can be called. Time is measured using
    /// an <see cref="ITimeProvider"/> instance and calls to the underlying enumerator are limited
    /// to a minimum time between each call.
    /// </summary>
    public class RateLimitEnumerator<T> : IEnumerator<T>
    {
        private T _current;
        private DateTime _lastCallTime;

        private readonly ITimeProvider _timeProvider;
        private readonly IEnumerator<T> _enumerator;
        private readonly TimeSpan _minimumTimeBetweenCalls;

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitEnumerator{T}"/> class
        /// </summary>
        /// <param name="enumerator">The underlying enumerator to place rate limits on</param>
        /// <param name="timeProvider">Time provider used for determing the time between calls</param>
        /// <param name="minimumTimeBetweenCalls">The minimum time allowed between calls to the underlying enumerator</param>
        public RateLimitEnumerator(IEnumerator<T> enumerator, ITimeProvider timeProvider, TimeSpan minimumTimeBetweenCalls)
        {
            _enumerator = enumerator;
            _timeProvider = timeProvider;
            _minimumTimeBetweenCalls = minimumTimeBetweenCalls;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public bool MoveNext()
        {
            // determine time since last successful call, do this on units of the minimum time
            // this will give us nice round emit times
            var currentTime = _timeProvider.GetUtcNow().RoundDown(_minimumTimeBetweenCalls);
            var timeBetweenCalls = currentTime - _lastCallTime;

            // if within limits, patch it through to move next
            if (timeBetweenCalls >= _minimumTimeBetweenCalls)
            {
                if (!_enumerator.MoveNext())
                {
                    // our underlying is finished
                    _current = default(T);
                    return false;
                }

                // only update last call time on non rate limited requests
                _lastCallTime = currentTime;
                _current = _enumerator.Current;
            }
            else
            {
                // we've been rate limitted
                _current = default(T);
            }

            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public void Reset()
        {
            _enumerator.Reset();
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public T Current
        {
            get { return _current; }
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        object IEnumerator.Current
        {
            get { return _current; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}