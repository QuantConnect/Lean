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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using QuantConnect.Interfaces;

namespace QuantConnect.Util
{
    /// <summary>
    /// A non blocking <see cref="IBusyCollection{T}"/> implementation
    /// </summary>
    /// <typeparam name="T">The item type being processed</typeparam>
    public class BusyCollection<T> : IBusyCollection<T>
    {
        private readonly ConcurrentQueue<T> _collection = new ConcurrentQueue<T>();
        private readonly ManualResetEventSlim _processingCompletedEvent = new ManualResetEventSlim(true);
        private bool _completedAdding;

        /// <summary>
        /// Gets a wait handle that can be used to wait until this instance is done
        /// processing all of it's item
        /// </summary>
        public WaitHandle WaitHandle => _processingCompletedEvent.WaitHandle;

        /// <summary>
        /// Gets the number of items held within this collection
        /// </summary>
        public int Count => _collection.Count;

        /// <summary>
        /// Returns true if processing, false otherwise
        /// </summary>
        public bool IsBusy => _collection.Count > 0 || !_processingCompletedEvent.IsSet;

        /// <summary>
        /// Adds the items to this collection
        /// </summary>
        /// <param name="item">The item to be added</param>
        public void Add(T item)
        {
            Add(item, CancellationToken.None);
        }

        /// <summary>
        /// Adds the items to this collection
        /// </summary>
        /// <param name="item">The item to be added</param>
        /// <param name="cancellationToken">A cancellation token to observer</param>
        public void Add(T item, CancellationToken cancellationToken)
        {
            if (_completedAdding)
            {
                throw new InvalidOperationException("Collection has already been marked as not " +
                    $"accepting more additions, see {nameof(CompleteAdding)}");
            }

            // locking to avoid race condition with GetConsumingEnumerable()
            lock (_processingCompletedEvent)
            {
                // we're adding work to be done, mark us as busy
                _processingCompletedEvent.Reset();
                _collection.Enqueue(item);
            }
        }

        /// <summary>
        /// Provides a consuming enumerable for items in this collection.
        /// </summary>
        /// <returns>An enumerable that removes and returns items from the collection</returns>
        public IEnumerable<T> GetConsumingEnumerable()
        {
            return GetConsumingEnumerable(CancellationToken.None);
        }

        /// <summary>
        /// Provides a consuming enumerable for items in this collection.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observer</param>
        /// <returns>An enumerable that removes and returns items from the collection</returns>
        public IEnumerable<T> GetConsumingEnumerable(CancellationToken cancellationToken)
        {
            T item;
            while (_collection.TryDequeue(out item))
            {
                yield return item;
            }

            // locking to avoid race condition with Add()
            lock (_processingCompletedEvent)
            {
                if (!_collection.TryPeek(out item))
                {
                    _processingCompletedEvent.Set();
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _collection.Clear();
            _processingCompletedEvent.Dispose();
        }

        /// <summary>
        /// Marks the collection as not accepting any more additions
        /// </summary>
        public void CompleteAdding()
        {
            _completedAdding = true;
        }
    }
}