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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Represents an enumerator capable of synchronizing live base data enumerators in time.
    /// This assumes that all enumerators have data time stamped in the same time zone.
    /// </summary>
    public class LiveBaseDataSynchronizingEnumerator : IEnumerator<BaseData>
    {
        private readonly ITimeProvider _timeProvider;
        private readonly DateTimeZone _exchangeTimeZone;
        private readonly List<IEnumerator<BaseData>> _enumerators;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiveBaseDataSynchronizingEnumerator"/> class
        /// </summary>
        /// <param name="timeProvider">The source of time used to gauge when this enumerator should emit extra bars when null data is returned from the source enumerator</param>
        /// <param name="exchangeTimeZone">The time zone the raw data is time stamped in</param>
        /// <param name="enumerators">The enumerators to be synchronized. NOTE: Assumes the same time zone for all data</param>
        public LiveBaseDataSynchronizingEnumerator(ITimeProvider timeProvider, DateTimeZone exchangeTimeZone, params IEnumerator<BaseData>[] enumerators)
        {
            _timeProvider = timeProvider;
            _exchangeTimeZone = exchangeTimeZone;
            _enumerators = enumerators.ToList();

            // prime enumerators
            _enumerators.ForEach(x => x.MoveNext());
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns> true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
        public bool MoveNext()
        {
            // use manual time provider from LiveTradingDataFeed
            var frontier = _timeProvider.GetUtcNow().ConvertFromUtc(_exchangeTimeZone);

            // check if any enumerator is ready to emit
            if (DataPointEmitted(_enumerators, frontier))
                return true;

            // advance enumerators with no current data
            var enumeratorsAdvanced = new List<IEnumerator<BaseData>>();
            _enumerators.ForEach(x =>
            {
                if (x.Current == null && x.MoveNext())
                {
                    enumeratorsAdvanced.Add(x);
                }
            });

            // check if any enumerator is ready to emit
            if (DataPointEmitted(enumeratorsAdvanced, frontier))
                return true;

            Current = null;

            // IEnumerator contract dictates that we return true unless we're actually
            // finished with the 'collection' and since this is live, we're never finished
            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
        public void Reset()
        {
            _enumerators.ForEach(x => Reset());
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        public BaseData Current { get; private set; }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>The current element in the collection.</returns>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _enumerators.ForEach(x => x.DisposeSafely());
        }

        private bool DataPointEmitted(IEnumerable<IEnumerator<BaseData>> enumerators, DateTime frontier)
        {
            // check if any enumerator is ready to emit
            var enumerator = enumerators
                .Where(x => x.Current != null && x.Current.EndTime <= frontier)
                .OrderBy(x => x.Current?.EndTime)
                .FirstOrDefault();

            if (enumerator != null)
            {
                // emit new data point
                Current = enumerator.Current;

                // advance enumerator
                enumerator.MoveNext();

                return true;
            }

            return false;
        }
    }
}
