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
    /// an order has requests in flight, so nothing needs releasing once the order closes. When a single consumer
    /// drains the queue, a lone fixed worker or the caller itself in synchronous mode (through
    /// <see cref="ProcessPending"/>), arrival order is already preserved so the per-order bookkeeping is skipped.
    /// </remarks>
    public class OrderRequestProcessingPool : IDisposable
    {
        // maximum time to wait for each worker thread to stop when disposing the pool
        private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(60);
        // the shared queue of requests cleared to run. every worker pulls from here so the load stays balanced
        private readonly IBusyCollection<WorkItem> _readyQueue;
        private readonly List<Thread> _threads;
        // for each order (or combo group) being processed, the follow up requests waiting their turn in arrival order,
        // or null until a second request actually needs parking. while the key is here the order is already running
        private readonly Dictionary<(bool IsGroup, int Id), Queue<OrderRequest>> _inFlight = new();
        // guards the in flight map, the threads list and the growth/shutdown flags
        private readonly Lock _lock = new();
        // maximum number of worker threads the pool can grow to on demand
        private readonly int _maximumThreads;
        // true when there are no worker threads and the caller drains the single queue itself
        private readonly bool _synchronous;
        // true when a single consumer drains the queue (synchronous or a single fixed worker), which already
        // preserves arrival order across all orders so the per-order serialization is skipped entirely
        private readonly bool _singleConsumer;
        // set under the lock when shutting down so the pool stops growing while the queue drains, before the
        // cancellation token is cancelled as the final hard stop
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
            _singleConsumer = _maximumThreads == 1;
            var initialThreadsCount = concurrencyEnabled ? Math.Min(Math.Max(1, minimumThreads), _maximumThreads) : 1;

            _readyQueue = new BusyBlockingCollection<WorkItem>();
            _threads = new(_maximumThreads);
            IsActive = true;
            for (var i = 0; i < initialThreadsCount; i++)
            {
                AddThread().Start();
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
            _singleConsumer = true;

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
            // a single consumer drains in arrival order across all orders, no need to serialize per order
            if (_singleConsumer)
            {
                _readyQueue.Add(new WorkItem(request, default));
                return;
            }

            var key = GetRoutingKey(order);
            WorkItem readyItem = default;
            Thread newThread = null;
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
                    newThread = TryExpand();
                    readyItem = new WorkItem(request, key);
                    run = true;
                }
            }

            // start the new worker and add outside the lock: starting an OS thread and a potentially blocking
            // add on a bounded queue shouldn't stall other dispatchers
            if (run)
            {
                newThread?.Start();
                _readyQueue.Add(readyItem);
            }
        }

        /// <summary>
        /// Drains the pending order requests on the calling thread. Only used in synchronous mode, where there
        /// are no worker threads and the caller pumps the single queue itself.
        /// </summary>
        public void ProcessPending()
        {
            Drain(item => _processRequest(item.Request));
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
        /// Stops every worker thread and waits for them to terminate, then releases the pool resources.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                // already disposed, nothing else to do
                if (_shuttingDown)
                {
                    return;
                }
                // stop growing so the threads list is frozen and safe to iterate without taking a snapshot
                _shuttingDown = true;
            }

            // let the workers drain whatever is queued and parked: once adding is complete their consuming
            // enumerables finish naturally when the queue empties, so join before cancelling anything. Only
            // escalate to StopSafely, which cancels the shared token and drops pending requests, on timeout
            _readyQueue.CompleteAdding();
            foreach (var thread in _threads)
            {
                try
                {
                    if (thread != null && !thread.Join(ShutdownTimeout))
                    {
                        Log.Error($"OrderRequestProcessingPool.Dispose(): Exceeded timeout: {(int)ShutdownTimeout.TotalSeconds} seconds waiting for '{thread.Name}' to finish processing");
                        thread.StopSafely(ShutdownTimeout, _cancellationTokenSource);
                    }
                }
                catch (ThreadStateException)
                {
                    // registered by a concurrent Dispatch but not started yet, nothing to drain on it
                }
            }

            IsActive = false;
            _readyQueue.DisposeSafely();
            _cancellationTokenSource.DisposeSafely();
        }

        /// <summary>
        /// Creates and registers a worker thread without starting it, so callers can start it outside the lock.
        /// Callers growing the pool on demand must hold <see cref="_lock"/>.
        /// </summary>
        /// <returns>The new worker thread, for the caller to start</returns>
        private Thread AddThread()
        {
            var thread = new Thread(Run) { IsBackground = true, Name = $"Transaction Thread {_threads.Count}" };
            _threads.Add(thread);
            return thread;
        }

        /// <summary>
        /// Grows the pool by one worker when every existing worker is already busy, up to the maximum.
        /// Caller must hold <see cref="_lock"/> and start the returned thread, if any, outside of it.
        /// </summary>
        /// <returns>The new worker thread to start, null when the pool doesn't need to grow</returns>
        private Thread TryExpand()
        {
            if (_shuttingDown || _threads.Count >= _maximumThreads)
            {
                return null;
            }

            // only grow when every worker is already busy, so the request being enqueued would have to wait
            if (Volatile.Read(ref _busyWorkers) >= _threads.Count)
            {
                Log.Trace($"OrderRequestProcessingPool.TryExpand(): adding new thread, current count {_threads.Count}");
                return AddThread();
            }
            return null;
        }

        /// <summary>
        /// Worker thread loop that consumes ready requests until the pool is shut down. A single fixed worker
        /// already consumes in arrival order so it skips the per-order bookkeeping.
        /// </summary>
        private void Run()
        {
            if (_singleConsumer)
            {
                Drain(item => _processRequest(item.Request));
            }
            else
            {
                Drain(ProcessInOrder);
            }
        }

        /// <summary>
        /// Consumes ready requests on the calling thread until the queue completes adding or the pool is shut down.
        /// </summary>
        private void Drain(Action<WorkItem> process)
        {
            try
            {
                foreach (var item in _readyQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                    process(item);
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
