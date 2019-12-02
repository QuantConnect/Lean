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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        private T _lastEnqueued;
        private volatile bool _end;
        private volatile bool _disposed;

        private readonly bool _isBlocking;
        private readonly int _timeout;
        private readonly object _lock = new object();
        private readonly BlockingCollection<T> _blockingCollection;

        private int _count;
        private int _lowerThreshold;
        private Action _produce;
        private Task _producer;

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
                    return _blockingCollection.Count;
                }
            }
        }

        /// <summary>
        /// Gets the last item that was enqueued
        /// </summary>
        public T LastEnqueued
        {
            get { return _lastEnqueued; }
        }

        /// <summary>
        /// Returns true if the enumerator has finished and will not accept any more data
        /// </summary>
        public bool HasFinished
        {
            get { return _end || _disposed; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnqueueableEnumerator{T}"/> class
        /// </summary>
        /// <param name="blocking">Specifies whether or not to use the blocking behavior</param>
        public EnqueueableEnumerator(bool blocking = false)
        {
            _blockingCollection = new BlockingCollection<T>();
            _isBlocking = blocking;
            _timeout = blocking ? Timeout.Infinite : 0;
            CancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Enqueues the new data into this enumerator
        /// </summary>
        /// <param name="data">The data to be enqueued</param>
        public void Enqueue(T data)
        {
            lock (_lock)
            {
                if (_end) return;
                _blockingCollection.Add(data);
                _lastEnqueued = data;
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
                // no more items can be added, so no need to wait anymore
                _blockingCollection.CompleteAdding();
                _end = true;
            }
        }

        /// <summary>
        /// Cancellation token source used by the producer to wake up waiting consumer
        /// and trigger a new worker if required.
        /// </summary>
        /// <remarks>This was added to fix a race condition where the worker could stop
        /// after the consumer had already checked on him <see cref="TriggerProducer"/>,
        /// leaving the consumer waiting for ever GH issue 3885. This cancellation token
        /// works as a flag for this particular case waking up the consumer.</remarks>
        public CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public bool MoveNext()
        {
            TriggerProducer();

            try
            {
                T current;
                if (!_blockingCollection.TryTake(out current, _timeout, CancellationTokenSource.Token))
                {
                    _current = default(T);

                    // if the enumerator has blocking behavior and there is no more data, it has ended
                    if (_isBlocking)
                    {
                        lock (_lock)
                        {
                            _end = true;
                        }
                    }

                    return !_end;
                }

                _current = current;
            }
            catch (OperationCanceledException)
            {
                _count = 0;

                // lets wait for the producer to end
                _producer.Wait();
                return MoveNext();
            }

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
            lock (_lock)
            {
                if (_disposed) return;
                Stop();
                if (_blockingCollection != null) _blockingCollection.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Sets the production <see cref="Action"/> and the lower threshold production trigger.
        /// Will also start the first producer <see cref="Task"/>
        /// </summary>
        /// <param name="produce">The <see cref="Action"/> used to produce more items</param>
        /// <param name="lowerThreshold">The threshold used to determine
        /// if a new producer <see cref="Task"/> has to start</param>
        public void SetProducer(Action produce, int lowerThreshold)
        {
            _producer = Task.Run(produce);
            _produce = produce;
            _lowerThreshold = lowerThreshold;
        }

        /// <summary>
        /// If the <see cref="_produce"/> action was set <see cref="SetProducer"/>,
        /// this method will generate a new task to execute the <see cref="_produce"/> action
        /// when items are less than <see cref="_lowerThreshold"/>, the enumerator is not finished
        /// <see cref="HasFinished"/> and previous <see cref="_producer"/> already finished running.
        /// </summary>
        private void TriggerProducer()
        {
            if (_produce != null
                && !HasFinished
                && _producer.IsCompleted
                && _lowerThreshold > _count--)
            {
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    // refresh the cancellation token source
                    CancellationTokenSource = new CancellationTokenSource();
                }

                // we use local count for the outside if, for performance, and adjust here
                _count = Count;
                if (_lowerThreshold > _count)
                {
                    _producer = Task.Run(_produce);
                }
            }
        }
    }
}