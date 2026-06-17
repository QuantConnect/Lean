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
using System.Collections.Generic;
using System.Threading;

namespace QuantConnect.Util
{
    /// <summary>
    /// A worker pool that routes items into queues by key and processes each queue with its own thread.
    /// It starts with a minimum number of workers and adds more on demand (up to a maximum) when a queue
    /// starts to pile up. Items sharing a key go to the same queue, so they keep their relative order.
    /// </summary>
    /// <typeparam name="T">The item type being processed</typeparam>
    public class DynamicWorkerPool<T> : IDisposable
    {
        private readonly Action<T> _handler;
        private readonly Action<Exception> _onError;
        private readonly string _threadName;
        private readonly int _minWorkers;
        private readonly int _maxWorkers;

        private readonly BusyBlockingCollection<T>[] _queues;
        private readonly List<Thread> _workers;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly object _lock = new object();
        private int _activeWorkers;
        private bool _disposed;

        /// <summary>
        /// The number of worker threads currently running
        /// </summary>
        public int WorkerCount => Volatile.Read(ref _activeWorkers);

        /// <summary>
        /// True while any queue still has items to process
        /// </summary>
        public bool IsBusy
        {
            get
            {
                if (Volatile.Read(ref _disposed))
                {
                    return false;
                }
                for (var i = 0; i < _queues.Length; i++)
                {
                    if (_queues[i].IsBusy)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicWorkerPool{T}"/> class
        /// </summary>
        /// <param name="handler">The action invoked to process each item</param>
        /// <param name="minWorkers">The number of worker threads to start with (at least 1)</param>
        /// <param name="maxWorkers">The maximum number of worker threads the pool can grow to</param>
        /// <param name="onError">Optional callback invoked when the handler throws an unexpected exception</param>
        /// <param name="threadName">Optional name prefix used for the worker threads</param>
        public DynamicWorkerPool(Action<T> handler, int minWorkers, int maxWorkers, Action<Exception> onError = null, string threadName = "DynamicWorkerPool")
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _maxWorkers = Math.Max(1, maxWorkers);
            _minWorkers = Math.Min(Math.Max(1, minWorkers), _maxWorkers);
            _onError = onError;
            _threadName = threadName;

            _queues = new BusyBlockingCollection<T>[_maxWorkers];
            for (var i = 0; i < _maxWorkers; i++)
            {
                _queues[i] = new BusyBlockingCollection<T>();
            }
            _workers = new List<Thread>(_maxWorkers);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the pool with the minimum number of worker threads. Idempotent.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_workers.Count > 0)
                {
                    return;
                }
                for (var i = 0; i < _minWorkers; i++)
                {
                    StartWorker(i);
                }
                _activeWorkers = _minWorkers;
            }
        }

        /// <summary>
        /// Routes an item to a queue by <paramref name="key"/> and adds a worker if that queue is piling up
        /// </summary>
        /// <param name="key">The routing key; the same key maps to the same queue while the pool size is stable</param>
        /// <param name="item">The item to process</param>
        public void Enqueue(long key, T item)
        {
            var active = Volatile.Read(ref _activeWorkers);
            var index = (int)(key % active);
            if (index < 0)
            {
                index += active;
            }

            var queue = _queues[index];
            queue.Add(item);

            // the queue is piling up faster than its worker can process it: grow the pool
            if (active < _maxWorkers && queue.Count > 1)
            {
                Grow();
            }
        }

        /// <summary>
        /// Waits until all queues are empty and idle, or the timeout elapses
        /// </summary>
        /// <returns>True if the pool became idle, false on timeout</returns>
        public bool WaitForIdle(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (IsBusy)
            {
                if (DateTime.UtcNow >= deadline)
                {
                    return false;
                }
                Thread.Sleep(1);
            }
            return true;
        }

        /// <summary>
        /// Stops the pool, signaling the workers to exit and waiting for them to finish
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }

            _cancellationTokenSource.Cancel();
            foreach (var queue in _queues)
            {
                queue.CompleteAdding();
            }

            lock (_lock)
            {
                foreach (var worker in _workers)
                {
                    worker?.StopSafely(TimeSpan.FromSeconds(5), _cancellationTokenSource);
                }
            }

            foreach (var queue in _queues)
            {
                queue.DisposeSafely();
            }
            _cancellationTokenSource.DisposeSafely();
        }

        private void Grow()
        {
            lock (_lock)
            {
                if (_activeWorkers >= _maxWorkers || _cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
                StartWorker(_activeWorkers);
                // publish the new worker only after it has started so routing never targets a missing queue
                _activeWorkers++;
            }
        }

        private void StartWorker(int index)
        {
            var worker = new Thread(() => WorkerLoop(index)) { IsBackground = true, Name = $"{_threadName} {index}" };
            _workers.Add(worker);
            worker.Start();
        }

        private void WorkerLoop(int index)
        {
            try
            {
                foreach (var item in _queues[index].GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                    _handler(item);
                }
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
            catch (Exception err)
            {
                _onError?.Invoke(err);
            }
        }
    }
}
