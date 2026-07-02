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
using System.Threading;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /// <summary>
    /// Runs order requests on background worker threads that pull from a single shared queue. The pool grows on
    /// demand when the workers get saturated and keeps every request of an order processed in order.
    /// </summary>
    /// <remarks>
    /// Workers pull from one shared queue, so the load spreads across them instead of pinning each order to a thread
    /// up front. To keep a single order (or combo group) in order, only one of its requests runs at a time. While one
    /// runs the rest wait parked, and the same worker takes them next in arrival order. This state only exists while
    /// an order has requests in flight, so nothing needs releasing once the order closes. In synchronous mode there
    /// are no workers and the caller drains the queue itself through <see cref="ProcessPending"/>.
    /// </remarks>
    public class OrderRequestProcessingPool
    {
        // the shared queue of requests cleared to run. every worker pulls from here so the load stays balanced
        private readonly IBusyCollection<WorkItem> _readyQueue;
        private readonly List<Thread> _threads;
        // for each order (or combo group) being processed, the follow up requests waiting their turn in arrival order,
        // or null until a second request actually needs parking. while the key is here the order is already running
        private readonly Dictionary<(bool IsGroup, int Id), Queue<OrderRequest>> _inFlight = new();
        // guards the in flight map, the threads list and the growth/shutdown flags
        private readonly object _lock = new object();
        // maximum number of worker threads the pool can grow to on demand
        private readonly int _maximumThreads;
        // true when there are no worker threads and the caller drains the single queue itself
        private readonly bool _synchronous;
        // set under the lock while shutting down so the pool stops growing
        private bool _shuttingDown;
        // number of workers currently processing a request, used to decide when the pool is saturated
        private int _busyWorkers;
        private readonly Action<OrderRequest> _processRequest;
        private readonly Action<Exception> _onError;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// True while the pool is processing order requests, false once it has been shut down.
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

            _readyQueue = new BusyBlockingCollection<WorkItem>();
            _threads = new(_maximumThreads);
            IsActive = true;
            for (var i = 0; i < initialThreadsCount; i++)
            {
                AddThread();
            }
        }

        /// <summary>
        /// Private constructor for the synchronous pool, a single non blocking queue and no worker threads.
        /// </summary>
        private OrderRequestProcessingPool(Action<OrderRequest> processRequest, Action<Exception> onError)
        {
            _synchronous = true;
            _processRequest = processRequest;
            _onError = onError;
            _maximumThreads = 1;

            _readyQueue = new BusyCollection<WorkItem>();
            _threads = new(0);
            IsActive = true;
        }

        /// <summary>
        /// Creates a synchronous pool with no worker threads. Its single queue is drained on the caller thread
        /// via <see cref="ProcessPending"/>.
        /// </summary>
        /// <param name="processRequest">Handles a single order request</param>
        /// <param name="onError">Invoked when processing fails unexpectedly</param>
        public static OrderRequestProcessingPool Synchronous(Action<OrderRequest> processRequest, Action<Exception> onError)
        {
            return new OrderRequestProcessingPool(processRequest, onError);
        }

        /// <summary>
        /// Dispatches an order request to be processed. If the order already has a request in flight, the new one
        /// waits parked so its worker runs it next and the order stays in arrival order. Otherwise it is queued for
        /// any worker to pick up, growing the pool first when every worker is already busy.
        /// </summary>
        /// <param name="request">The order request to process</param>
        /// <param name="order">The order the request belongs to, used to keep its requests ordered</param>
        public void Dispatch(OrderRequest request, Order order)
        {
            // synchronous mode has a single consumer draining in arrival order, no need to serialize per order
            if (_synchronous)
            {
                _readyQueue.Add(new WorkItem(request, default));
                return;
            }

            var key = GetRoutingKey(order);
            WorkItem readyItem = default;
            var run = false;
            lock (_lock)
            {
                if (_inFlight.TryGetValue(key, out var parked))
                {
                    // the order is already being processed, park this request so its worker runs it next in order,
                    // allocating the queue only now that a second request has actually arrived
                    if (parked == null)
                    {
                        _inFlight[key] = parked = new Queue<OrderRequest>();
                    }
                    parked.Enqueue(request);
                }
                else
                {
                    // claim the order without a queue, most orders never get a second request. grow the pool if
                    // every worker is already busy so this request would wait
                    _inFlight[key] = null;
                    TryExpand();
                    readyItem = new WorkItem(request, key);
                    run = true;
                }
            }

            // add outside the lock, it can block when the queue is at its bounded capacity
            if (run)
            {
                _readyQueue.Add(readyItem);
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
                foreach (var item in _readyQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                    _processRequest(item.Request);
                }
            }
            catch (Exception err)
            {
                // unexpected error, we need to close down shop
                _onError(err);
            }
        }

        /// <summary>
        /// Waits until no order has requests in flight, up to the given timeout. In practice only the synchronous
        /// early return runs. The threaded branch below is defensive, since its callers only reach it in backtesting
        /// where the pool is synchronous, so it never runs in a live deployment.
        /// </summary>
        /// <param name="timeout">The maximum time to wait</param>
        /// <returns>True if the pool was still processing when the timeout elapsed</returns>
        public bool WaitForProcessing(TimeSpan timeout)
        {
            // synchronous mode has no worker thread to drain the queue, the caller pumps it via ProcessPending
            if (_synchronous)
            {
                return false;
            }

            // re-check each pass since the shared queue signals idle as soon as a worker finds it empty, even if
            // another worker is still processing or a request is parked
            while (IsProcessing())
            {
                if (!_readyQueue.WaitHandle.WaitOne(timeout, _cancellationTokenSource.Token))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Whether any order still has a request in flight, either queued, being processed or parked.
        /// </summary>
        private bool IsProcessing()
        {
            lock (_lock)
            {
                return _inFlight.Count > 0 || _readyQueue.IsBusy;
            }
        }

        /// <summary>
        /// Stops every worker thread and waits for them to terminate, up to the given timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to wait for each thread to stop</param>
        public void Shutdown(TimeSpan timeout)
        {
            lock (_lock)
            {
                // stop growing so the threads list is frozen and safe to iterate without taking a snapshot
                _shuttingDown = true;
            }

            // let the workers drain whatever is queued, then stop them
            _readyQueue.CompleteAdding();
            foreach (var thread in _threads)
            {
                thread?.StopSafely(timeout, _cancellationTokenSource);
            }

            IsActive = false;
            _cancellationTokenSource.DisposeSafely();
        }

        /// <summary>
        /// Creates a worker thread and starts it.
        /// Callers growing the pool on demand must hold <see cref="_lock"/>.
        /// </summary>
        private void AddThread()
        {
            var threadId = _threads.Count;
            var thread = new Thread(Run) { IsBackground = true, Name = $"Transaction Thread {threadId}" };
            _threads.Add(thread);
            thread.Start();
        }

        /// <summary>
        /// Grows the pool by one worker when every existing worker is already busy, up to the maximum.
        /// Caller must hold <see cref="_lock"/>.
        /// </summary>
        private void TryExpand()
        {
            if (_synchronous || _shuttingDown || _threads.Count >= _maximumThreads || _cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            // only grow when every worker is already busy, so the request being enqueued would have to wait
            if (Volatile.Read(ref _busyWorkers) >= _threads.Count)
            {
                Log.Trace($"OrderRequestProcessingPool.TryExpand(): adding new thread, current count {_threads.Count}");
                AddThread();
            }
        }

        /// <summary>
        /// Worker thread loop that consumes ready requests until the pool is shut down.
        /// </summary>
        private void Run()
        {
            try
            {
                foreach (var item in _readyQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                    ProcessInOrder(item);
                }
            }
            catch (Exception err)
            {
                // unexpected error, we need to close down shop
                _onError(err);
            }
        }

        /// <summary>
        /// Processes a request and then drains, in arrival order, every follow up request parked for the same order,
        /// so a single worker handles the whole order in sequence before moving on to other work.
        /// </summary>
        private void ProcessInOrder(WorkItem item)
        {
            var request = item.Request;
            Interlocked.Increment(ref _busyWorkers);
            try
            {
                while (request != null)
                {
                    _processRequest(request);

                    lock (_lock)
                    {
                        var parked = _inFlight[item.Key];
                        if (parked != null && parked.Count > 0)
                        {
                            request = parked.Dequeue();
                        }
                        else
                        {
                            // no more requests for this order in flight, drop its bookkeeping
                            _inFlight.Remove(item.Key);
                            request = null;
                        }
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref _busyWorkers);
            }
        }

        /// <summary>
        /// Builds the routing key that ties an order's requests together, the combo group when it has one, otherwise
        /// the order itself. Order ids and group ids are separate counters that can share a value, so the flag keeps
        /// a simple order and a combo group from colliding.
        /// </summary>
        private static (bool IsGroup, int Id) GetRoutingKey(Order order)
        {
            var group = order.GroupOrderManager;
            return group?.Id > 0 ? (true, group.Id) : (false, order.Id);
        }

        /// <summary>
        /// Pairs a request with its routing key so the worker can drain the rest of the order without re-deriving it.
        /// </summary>
        private readonly struct WorkItem
        {
            public OrderRequest Request { get; }
            public (bool IsGroup, int Id) Key { get; }

            public WorkItem(OrderRequest request, (bool IsGroup, int Id) key)
            {
                Request = request;
                Key = key;
            }
        }
    }
}
