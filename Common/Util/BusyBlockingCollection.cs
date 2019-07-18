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
    /// A small wrapper around <see cref="BlockingCollection{T}"/> used to communicate busy state of the items
    /// being processed
    /// </summary>
    /// <typeparam name="T">The item type being processed</typeparam>
    public class BusyBlockingCollection<T> : IBusyCollection<T>
    {
        private readonly BlockingCollection<T> _collection;
        private readonly ManualResetEventSlim _processingCompletedEvent;
        private readonly object _lock = new object();

        /// <summary>
        /// Gets a wait handle that can be used to wait until this instance is done
        /// processing all of it's item
        /// </summary>
        public WaitHandle WaitHandle
        {
            get { return _processingCompletedEvent.WaitHandle; }
        }

        /// <summary>
        /// Gets the number of items held within this collection
        /// </summary>
        public int Count
        {
            get { return _collection.Count; }
        }

        /// <summary>
        /// Returns true if processing, false otherwise
        /// </summary>
        public bool IsBusy
        {
            get
            {
                lock (_lock)
                {
                    return _collection.Count > 0 || !_processingCompletedEvent.IsSet;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusyBlockingCollection{T}"/> class
        /// with a bounded capacity of <see cref="int.MaxValue"/>
        /// </summary>
        public BusyBlockingCollection()
            : this(int.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusyBlockingCollection{T}"/> class
        /// with the specified <paramref name="boundedCapacity"/>
        /// </summary>
        /// <param name="boundedCapacity">The maximum number of items allowed in the collection</param>
        public BusyBlockingCollection(int boundedCapacity)
        {
            _collection = new BlockingCollection<T>(boundedCapacity);

            // initialize as not busy
            _processingCompletedEvent = new ManualResetEventSlim(true);
        }

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
            bool added;
            lock (_lock)
            {
                // we're adding work to be done, mark us as busy
                _processingCompletedEvent.Reset();
                added = _collection.TryAdd(item, 0, cancellationToken);
            }

            if (!added)
            {
                _collection.Add(item, cancellationToken);
            }
        }

        /// <summary>
        /// Marks the <see cref="BusyBlockingCollection{T}"/> as not accepting any more additions
        /// </summary>
        public void CompleteAdding()
        {
            _collection.CompleteAdding();
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
            while (!_collection.IsCompleted)
            {
                T item;

                // check to see if something is immediately available
                bool tookItem;

                try
                {
                    tookItem = _collection.TryTake(out item, 0, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // if the operation was canceled, just bail on the enumeration
                    yield break;
                }

                if (tookItem)
                {
                    // something was immediately available, emit it
                    yield return item;
                    continue;
                }


                // we need to lock this with the Add method since we need to model the act of
                // taking/flipping the switch and adding/flipping the switch as one operation
                lock (_lock)
                {
                    // double check that there's nothing in the collection within a lock, it's possible
                    // that between the TryTake above and this statement, the Add method was called, so we
                    // don't want to flip the switch if there's something in the collection
                    if (_collection.Count == 0)
                    {
                        // nothing was immediately available, mark us as idle
                        _processingCompletedEvent.Set();
                    }
                }

                try
                {
                    // now block until something is available
                    tookItem = _collection.TryTake(out item, Timeout.Infinite, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // if the operation was canceled, just bail on the enumeration
                    yield break;
                }

                if (tookItem)
                {
                    // emit the item we found
                    yield return item;
                }
            }

            // no more items to process
            _processingCompletedEvent.Set();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _collection.Dispose();
            _processingCompletedEvent.Dispose();
        }
    }
}
