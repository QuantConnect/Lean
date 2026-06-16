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

namespace QuantConnect.Util
{
    /// <summary>
    /// A worker pool that routes items into a fixed number of partitions by key, keeping the routing
    /// stable while the number of workers grows on demand from a minimum up to a maximum when busy.
    /// Each partition is processed by a single worker at a time, so items sharing a key keep their order.
    /// </summary>
    /// <typeparam name="T">The item type being processed</typeparam>
    public class DynamicWorkerPool<T> : IDisposable
    {
        private readonly Action<T> _handler;
        private readonly Action<Exception> _onError;
        private readonly string _threadName;
        private readonly int _minWorkers;
        private readonly int _maxWorkers;

        private readonly ConcurrentQueue<T>[] _partitions;
        // 0 = free, 1 = claimed; ensures at most one worker processes a partition at a time
        private readonly int[] _claims;
        private readonly ManualResetEventSlim _workAvailable;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly List<Thread> _workers;
        private readonly object _workersLock = new object();

        private int _activeWorkerCount;
        private int _busyWorkers;
        private bool _started;

        /// <summary>
        /// The number of worker threads currently running
        /// </summary>
        public int WorkerCount => Volatile.Read(ref _activeWorkerCount);

        /// <summary>
        /// The fixed number of partitions used to route items (equal to the maximum worker count)
        /// </summary>
        public int PartitionCount => _partitions.Length;

        /// <summary>
        /// True while any partition has pending work or any worker is still processing an item
        /// </summary>
        public bool IsBusy => IsPoolBusy();

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

            _partitions = new ConcurrentQueue<T>[_maxWorkers];
            for (var i = 0; i < _maxWorkers; i++)
            {
                _partitions[i] = new ConcurrentQueue<T>();
            }
            _claims = new int[_maxWorkers];
            _workAvailable = new ManualResetEventSlim(false);
            _cancellationTokenSource = new CancellationTokenSource();
            _workers = new List<Thread>(_maxWorkers);
        }

        /// <summary>
        /// Starts the pool with the minimum number of worker threads. Idempotent.
        /// </summary>
        public void Start()
        {
            lock (_workersLock)
            {
                if (_started)
                {
                    return;
                }
                _started = true;

                for (var i = 0; i < _minWorkers; i++)
                {
                    _workers.Add(NewWorker(i));
                }
                _activeWorkerCount = _minWorkers;
                foreach (var worker in _workers)
                {
                    worker.Start();
                }
            }
        }

        /// <summary>
        /// Enqueues an item to be processed. Items are routed to a partition by <paramref name="key"/>,
        /// so all items sharing the same key land on the same partition and keep their relative order.
        /// </summary>
        /// <param name="key">The routing key (e.g. an order id); the same key always maps to the same partition</param>
        /// <param name="item">The item to process</param>
        public void Enqueue(long key, T item)
        {
            var partition = (int)(key % _partitions.Length);
            if (partition < 0)
            {
                partition += _partitions.Length;
            }
            _partitions[partition].Enqueue(item);

            // signal the workers and grow the pool if the partitions are starving
            _workAvailable.Set();
            MaybeScaleUp();
        }

        /// <summary>
        /// Waits until all partitions are empty and no worker is processing, or the timeout elapses
        /// </summary>
        /// <returns>True if the pool became idle, false on timeout</returns>
        public bool WaitForIdle(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (IsPoolBusy())
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
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
            _workAvailable.Set();

            lock (_workersLock)
            {
                foreach (var worker in _workers)
                {
                    worker?.StopSafely(TimeSpan.FromSeconds(5), _cancellationTokenSource);
                }
            }

            _workAvailable.DisposeSafely();
            _cancellationTokenSource.DisposeSafely();
        }

        private Thread NewWorker(int id)
        {
            return new Thread(WorkerLoop) { IsBackground = true, Name = $"{_threadName} {id}" };
        }

        /// <summary>
        /// Worker entry point. Scans the partitions, claiming and processing any that have pending work,
        /// and blocks when there is none.
        /// </summary>
        private void WorkerLoop()
        {
            var token = _cancellationTokenSource.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!ProcessAvailable())
                    {
                        // no work found: reset and re-scan before blocking to avoid lost wake-ups
                        _workAvailable.Reset();
                        if (!ProcessAvailable())
                        {
                            _workAvailable.Wait(token);
                        }
                    }
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
            finally
            {
                Interlocked.Decrement(ref _activeWorkerCount);
            }
        }

        /// <summary>
        /// Scans all partitions and processes the ones with pending work. A partition is claimed before
        /// processing so at most one worker handles it at a time, preserving per-key ordering.
        /// </summary>
        /// <returns>True if any work was processed</returns>
        private bool ProcessAvailable()
        {
            var worked = false;
            for (var i = 0; i < _partitions.Length; i++)
            {
                var partition = _partitions[i];
                if (partition.IsEmpty)
                {
                    continue;
                }

                // claim the partition; if another worker owns it, skip and let that worker process it
                if (Interlocked.CompareExchange(ref _claims[i], 1, 0) != 0)
                {
                    continue;
                }

                Interlocked.Increment(ref _busyWorkers);
                try
                {
                    while (partition.TryDequeue(out var item))
                    {
                        _handler(item);
                        worked = true;
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _busyWorkers);
                    Volatile.Write(ref _claims[i], 0);
                }

                // items may have been added between our last dequeue and releasing the claim;
                // make sure a worker wakes up to handle them
                if (!partition.IsEmpty)
                {
                    _workAvailable.Set();
                }
            }
            return worked;
        }

        /// <summary>
        /// Grows the pool by one worker (up to the maximum) when the partitions are starving, i.e. every
        /// running worker is already busy at the moment new work is enqueued.
        /// </summary>
        private void MaybeScaleUp()
        {
            var active = Volatile.Read(ref _activeWorkerCount);
            if (active >= _maxWorkers)
            {
                return;
            }

            if (Volatile.Read(ref _busyWorkers) >= active)
            {
                TrySpawnWorker();
            }
        }

        private void TrySpawnWorker()
        {
            lock (_workersLock)
            {
                if (!_started || _activeWorkerCount >= _maxWorkers || _cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                var worker = NewWorker(_workers.Count);
                _workers.Add(worker);
                _activeWorkerCount++;
                worker.Start();
            }

            // wake the new worker (and any idle ones) to pick up the backlog
            _workAvailable.Set();
        }

        private bool IsPoolBusy()
        {
            if (Volatile.Read(ref _busyWorkers) > 0)
            {
                return true;
            }
            for (var i = 0; i < _partitions.Length; i++)
            {
                if (!_partitions[i].IsEmpty)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
