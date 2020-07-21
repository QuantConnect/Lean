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

using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// An implementation of <see cref="IEnumerator{T}"/> that relies on "consolidated" data
    /// </summary>
    /// <typeparam name="T">The item type yielded by the enumerator</typeparam>
    public class ScannableEnumerator<T> : IEnumerator<T> where T : class, IBaseData
    {
        private T _current;

        private readonly IDataConsolidator _consolidator;
        private readonly DateTimeZone _timeZone;
        private readonly ITimeProvider _timeProvider;
        private readonly EventHandler _newDataAvailableHandler;

        private readonly ConcurrentQueue<T> _queue;

        /// <summary>
        /// Gets the current number of items held in the internal queue
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScannableEnumerator{T}"/> class
        /// </summary>
        /// <param name="consolidator">Consolidator taking BaseData updates and firing events containing new 'consolidated' data</param>
        /// <param name="timeZone">The time zone the raw data is time stamped in</param>
        /// <param name="timeProvider">The time provider instance used to determine when bars are completed and can be emitted</param>
        /// <param name="newDataAvailableHandler">The event handler for a new available data point</param>
        public ScannableEnumerator(IDataConsolidator consolidator, DateTimeZone timeZone, ITimeProvider timeProvider, EventHandler newDataAvailableHandler)
        {
            _consolidator = consolidator;
            _timeZone = timeZone;
            _timeProvider = timeProvider;
            _queue = new ConcurrentQueue<T>();
            _newDataAvailableHandler = newDataAvailableHandler ?? ((s, e) => { });

            _consolidator.DataConsolidated += (sender, data) =>
            {
                Enqueue(data as T);
                _newDataAvailableHandler(sender, EventArgs.Empty);
            };
        }

        /// <summary>
        /// Enqueues the new data into this enumerator
        /// </summary>
        /// <param name="data">The data to be enqueued</param>
        public void Enqueue(T data)
        {
            _queue.Enqueue(data);
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
            T current;
            if (!_queue.TryDequeue(out current))
            {
                _current = default(T);

                if (_consolidator.WorkingData != null)
                {
                    lock (_consolidator)
                    {
                        var utcTime = _timeProvider.GetUtcNow();
                        var localTime = utcTime.ConvertFromUtc(_timeZone);
                        _consolidator.Scan(localTime);
                    }

                    _queue.TryDequeue(out current);
                }
            }

            _current = current;

            // even if we don't have data to return, we haven't technically
            // passed the end of the collection, so always return true until
            // the enumerator is explicitly disposed or ended
            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public void Reset()
        {
            throw new NotImplementedException("ScannableEnumerator.Reset() has not been implemented yet.");
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public T Current => _current;

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {

        }
    }
}