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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /// <summary>
    /// Holds the worker threads and their queues used to process order requests, dispatching each
    /// request to the queue pinned to its order and growing the pool on demand when it gets saturated.
    /// </summary>
    /// <remarks>
    /// In concurrent mode each thread owns a single <see cref="BusyBlockingCollection{T}"/> it consumes,
    /// the pool starts at the minimum number of threads and grows up to the maximum when every thread is
    /// busy with pending work. In synchronous mode there are no worker threads: a single non blocking queue
    /// is drained on the caller thread via <see cref="ProcessPending"/>.
    /// </remarks>
    public class OrderRequestProcessingPool
    {
        // one queue per worker thread; the newly updated order requests wait here to be processed
        private readonly List<IBusyCollection<OrderRequest>> _queues;
        private readonly List<Thread> _threads;
        // pins each order (or combo group) to one queue for its whole life, so all its requests are handled
        // in order by the same thread even after the pool grows and re-routes new orders to other queues
        private readonly Dictionary<int, int> _queueIndexByKey = new();
        // guards on demand growth of the queues/threads against concurrent reads in Run/Dispatch/Shutdown
        private readonly object _lock = new object();
        // maximum number of threads (and queues) the pool can grow to on demand
        private readonly int _maximumThreads;
        // true when there are no worker threads and the caller drains the single queue itself
        private readonly bool _synchronous;
        private readonly Action<OrderRequest> _processRequest;
        private readonly Action<Exception> _onError;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// True while the pool is processing order requests, false once its worker threads have finished.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// The number of worker threads currently running.
        /// </summary>
        public int ThreadCount
        {
            get
            {
                lock (_lock)
                {
                    return _threads.Count;
                }
            }
        }

        /// <summary>
        /// Creates a threaded pool and starts its initial worker threads. When concurrency is enabled the pool
        /// starts at <paramref name="minimumThreads"/> and grows on demand up to <paramref name="maximumThreads"/>,
        /// otherwise it runs a single fixed worker thread.
        /// </summary>
        /// <param name="concurrencyEnabled">True to grow the pool on demand, false to run a single worker thread</param>
        /// <param name="minimumThreads">The number of worker threads the pool starts with when growing</param>
        /// <param name="maximumThreads">The maximum number of worker threads the pool can grow to on demand</param>
        /// <param name="processRequest">Handles a single order request</param>
        /// <param name="onError">Invoked when processing fails unexpectedly</param>
        public OrderRequestProcessingPool(bool concurrencyEnabled, int minimumThreads, int maximumThreads,
            Action<OrderRequest> processRequest, Action<Exception> onError)
        {
            _synchronous = false;
            _processRequest = processRequest;
            _onError = onError;
            // concurrency grows the pool minimum..maximum on demand, otherwise a single fixed thread is used
            _maximumThreads = concurrencyEnabled ? Math.Max(1, maximumThreads) : 1;
            var initialThreadsCount = concurrencyEnabled ? Math.Min(Math.Max(1, minimumThreads), _maximumThreads) : 1;

            _queues = new(_maximumThreads);
            _threads = new(_maximumThreads);
            IsActive = true;
            for (var i = 0; i < initialThreadsCount; i++)
            {
                AddThread();
            }
        }

        /// <summary>
        /// Private constructor for the synchronous pool: a single non blocking queue and no worker threads.
        /// </summary>
        private OrderRequestProcessingPool(Action<OrderRequest> processRequest, Action<Exception> onError)
        {
            _synchronous = true;
            _processRequest = processRequest;
            _onError = onError;
            _maximumThreads = 1;

            _queues = new(1) { new BusyCollection<OrderRequest>() };
            _threads = new(0);
            IsActive = true;
        }

        /// <summary>
        /// Creates a synchronous pool with no worker threads: its single queue is drained on the caller thread
        /// via <see cref="ProcessPending"/>.
        /// </summary>
        /// <param name="processRequest">Handles a single order request</param>
        /// <param name="onError">Invoked when processing fails unexpectedly</param>
        public static OrderRequestProcessingPool Synchronous(Action<OrderRequest> processRequest, Action<Exception> onError)
        {
            return new OrderRequestProcessingPool(processRequest, onError);
        }

        /// <summary>
        /// Dispatches an order request to the queue pinned to its routing key, growing the pool first if
        /// every existing thread is already saturated.
        /// </summary>
        /// <param name="request">The order request to process</param>
        /// <param name="routingKey">Identifies the order (or combo group) the request belongs to</param>
        public void Dispatch(OrderRequest request, int routingKey)
        {
            IBusyCollection<OrderRequest> queue;
            lock (_lock)
            {
                // grow the pool first if every existing thread is already saturated
                TryExpand();

                // reuse the order's pinned queue if it has one, so it is never re-routed when the pool grows
                if (!_queueIndexByKey.TryGetValue(routingKey, out var queueIndex))
                {
                    queueIndex = routingKey % _queues.Count;
                    _queueIndexByKey[routingKey] = queueIndex;
                }
                queue = _queues[queueIndex];
            }

            // add outside the lock, since it can block when the queue is at its bounded capacity
            queue.Add(request);
        }

        /// <summary>
        /// Releases the queue pinned to the given routing key once its order reaches a final state, keeping the
        /// pin map bounded to the orders still in flight.
        /// </summary>
        /// <param name="routingKey">The routing key previously used in <see cref="Dispatch"/></param>
        public void Release(int routingKey)
        {
            lock (_lock)
            {
                _queueIndexByKey.Remove(routingKey);
            }
        }

        /// <summary>
        /// Drains the pending order requests on the calling thread. Only used in synchronous mode, where there
        /// are no worker threads and the caller pumps the single queue itself.
        /// </summary>
        public void ProcessPending()
        {
            try
            {
                Consume(_queues[0]);
            }
            catch (Exception err)
            {
                // unexpected error, we need to close down shop
                _onError(err);
            }
        }

        /// <summary>
        /// Waits for every queue to finish processing its pending requests, up to the given timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to wait</param>
        /// <returns>True if any queue was still busy when the timeout elapsed</returns>
        public bool WaitForProcessing(TimeSpan timeout)
        {
            // synchronous mode has no worker thread to drain the queue, the caller pumps it via ProcessPending
            if (_synchronous)
            {
                return false;
            }

            List<IBusyCollection<OrderRequest>> queues;
            lock (_lock)
            {
                // snapshot under the lock since the queues list may be growing on demand concurrently
                queues = _queues.ToList();
            }

            return queues.Any(queue => queue.IsBusy && !queue.WaitHandle.WaitOne(timeout, _cancellationTokenSource.Token));
        }

        /// <summary>
        /// Stops every worker thread and waits for them to terminate, up to the given timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to wait for each thread to stop</param>
        public void Shutdown(TimeSpan timeout)
        {
            List<IBusyCollection<OrderRequest>> queues;
            List<Thread> threads;
            lock (_lock)
            {
                // snapshot under the lock since the pool might still be growing on demand concurrently
                queues = _queues.ToList();
                threads = _threads.ToList();
            }

            foreach (var queue in queues)
            {
                queue.CompleteAdding();
            }

            foreach (var thread in threads)
            {
                thread?.StopSafely(timeout, _cancellationTokenSource);
            }

            IsActive = false;
            _cancellationTokenSource.DisposeSafely();
        }

        /// <summary>
        /// Creates a queue and its dedicated worker thread and starts it.
        /// Callers growing the pool on demand must hold <see cref="_lock"/>.
        /// </summary>
        private void AddThread()
        {
            var threadId = _queues.Count; // matches the queue index this thread will consume
            _queues.Add(new BusyBlockingCollection<OrderRequest>());
            var thread = new Thread(() => Run(threadId)) { IsBackground = true, Name = $"Transaction Thread {threadId}" };
            _threads.Add(thread);
            thread.Start();
        }

        /// <summary>
        /// Grows the pool only when every thread is busy and still has pending requests, up to the maximum.
        /// Caller must hold <see cref="_lock"/>.
        /// </summary>
        private void TryExpand()
        {
            if (_synchronous || _queues.Count >= _maximumThreads || _cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            // only grow when the whole pool is saturated: every thread busy and with requests still waiting
            for (var i = 0; i < _queues.Count; i++)
            {
                var queue = _queues[i];
                if (!queue.IsBusy || queue.Count == 0)
                {
                    return;
                }
            }

            AddThread();
        }

        /// <summary>
        /// Worker thread entry point: consumes its queue until the pool is shut down.
        /// </summary>
        private void Run(int threadId)
        {
            IBusyCollection<OrderRequest> queue;
            lock (_lock)
            {
                // capture our queue safely, the queues list may be growing on demand concurrently
                queue = _queues[threadId];
            }

            try
            {
                Consume(queue);
            }
            catch (Exception err)
            {
                // unexpected error, we need to close down shop
                _onError(err);
            }

            Log.Trace($"OrderRequestProcessingPool.Run(): Ending Thread {threadId}...");
            IsActive = false;
        }

        /// <summary>
        /// Processes every request the queue yields, handing each one to the configured processor.
        /// </summary>
        private void Consume(IBusyCollection<OrderRequest> queue)
        {
            foreach (var request in queue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                _processRequest(request);
            }
        }
    }
}
