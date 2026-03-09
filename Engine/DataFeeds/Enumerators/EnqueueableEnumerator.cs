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
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// An implementation of <see cref="IEnumerator{T}"/> that relies on the
    /// <see cref="Enqueue"/> method being called and only ends when <see cref="Stop"/>
    /// is called
    /// </summary>
    /// <typeparam name="T">The item type yielded by the enumerator</typeparam>
    public class EnqueueableEnumerator<T> : IEnumerator<T>
    {
        private T _current;
        private bool _end;

        private readonly bool _isBlocking;
        private long _consumerCount;
        private Queue<T> _consumer = new();
        private Queue<T> _producer = new();
        private readonly object _lock = new object();
        private readonly ManualResetEventSlim _resetEvent = new(false);

        /// <summary>
        /// Gets the current number of items held in the internal queue
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    if (_end) return 0;
                    return _producer.Count + (int)Interlocked.Read(ref _consumerCount);
                }
            }
        }

        /// <summary>
        /// Returns true if the enumerator has finished and will not accept any more data
        /// </summary>
        public bool HasFinished
        {
            get { return _end; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnqueueableEnumerator{T}"/> class
        /// </summary>
        /// <param name="blocking">Specifies whether or not to use the blocking behavior</param>
        public EnqueueableEnumerator(bool blocking = false)
        {
            _isBlocking = blocking;
        }

        /// <summary>
        /// Enqueues the new data into this enumerator
        /// </summary>
        /// <param name="data">The data to be enqueued</param>
        public void Enqueue(T data)
        {
            lock (_lock)
            {
                _producer.Enqueue(data);
                // most of the time this will be set
                if(!_resetEvent.IsSet)
                {
                    _resetEvent.Set();
                }
            }
        }

        /// <summary>
        /// Signals the enumerator to stop enumerating when the items currently
        /// held inside are gone. No more items will be added to this enumerator.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (_end) return;
                _end = true;

                // no more items can be added, so no need to wait anymore
                _resetEvent.Set();
                _resetEvent.Dispose();
            }
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
            // we read with no lock most of the time
            if (_consumer.TryDequeue(out _current))
            {
                Interlocked.Decrement(ref _consumerCount);
                return true;
            }

            bool ended;
            do
            {
                var producer = _producer;
                lock (_lock)
                {
                    // swap queues
                    ended = _end;
                    _producer = _consumer;
                }
                _consumer = producer;
                if(_consumer.Count > 0)
                {
                    _current = _consumer.Dequeue();
                    Interlocked.Exchange(ref _consumerCount, _consumer.Count);
                    break;
                }

                // if we are here no queue has data
                if (ended)
                {
                    return false;
                }

                if (_isBlocking)
                {
                    try
                    {
                        _resetEvent.Wait(Timeout.Infinite);
                        _resetEvent.Reset();
                    }
                    catch (ObjectDisposedException)
                    {
                        // can happen if disposed
                    }
                }
                else
                {
                    break;
                }
            }
            while (!ended);

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
            throw new NotImplementedException("EnqueableEnumerator.Reset() has not been implemented yet.");
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
            get { return Current; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Stop();
        }
    }
}
