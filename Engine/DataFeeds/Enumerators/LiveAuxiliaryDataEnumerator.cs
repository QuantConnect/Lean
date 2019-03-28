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

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NodaTime;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Emits auxiliary data points ready to be time synced
    /// </summary>
    public class LiveAuxiliaryDataEnumerator : IEnumerator<BaseData>
    {
        private readonly DateTimeZone _timeZone;
        private readonly ITimeProvider _timeProvider;
        private readonly ConcurrentQueue<BaseData> _queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiveAuxiliaryDataEnumerator"/> class
        /// </summary>
        /// <param name="timeZone">The time zone the raw data is time stamped in</param>
        /// <param name="timeProvider">The time provider instance used to determine when bars are completed and
        /// can be emitted</param>
        public LiveAuxiliaryDataEnumerator(DateTimeZone timeZone, ITimeProvider timeProvider)
        {
            _timeZone = timeZone;
            _timeProvider = timeProvider;
            _queue = new ConcurrentQueue<BaseData>();
        }
        /// <summary>
        /// Pushes the data point. This data point will be emitted after the allotted time has passed
        /// </summary>
        /// <param name="data">The new data point</param>
        public void Enqueue(BaseData data)
        {
            _queue.Enqueue(data);
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            BaseData dataPoint;

            // check if there's a data point there and if its time to pull it off
            if (_queue.TryPeek(out dataPoint) && dataPoint.EndTime.ConvertToUtc(_timeZone) <= _timeProvider.GetUtcNow())
            {
                // data point is good to go, set it to current and remove from the queue
                Current = dataPoint;
                _queue.TryDequeue(out dataPoint);
            }
            else
            {
                Current = null;
            }

            // IEnumerator contract dictates that we return true unless we're actually
            // finished with the 'collection' and since this is live, we're never finished
            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            _queue.Clear();
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public BaseData Current
        {
            get; private set;
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}


