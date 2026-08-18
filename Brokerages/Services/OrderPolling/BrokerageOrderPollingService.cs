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
using System.Linq;
using System.Threading;
using QuantConnect.Util;
using QuantConnect.Orders;
using QuantConnect.Brokerages.Services.OrderPolling.Models;
using QuantConnect.Logging;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Orders.Fees;
using System.Collections.Generic;
using QuantConnect.Configuration;

namespace QuantConnect.Brokerages.Services.OrderPolling
{
    /// <summary>
    /// Reads orders from the brokerage on an interval and turns the returned states into order events.
    /// Used when a brokerage has no order stream, when the stream is unavailable, or to resolve an order
    /// the broker never replied about. This base class owns everything both modes share - the loop, the
    /// watch registry, the compare and the events. What one sweep reads is the subclass:
    /// <see cref="PerOrderIdPollingService"/> or <see cref="AllOrdersPollingService"/>.
    /// </summary>
    public abstract class BrokerageOrderPollingService : IDisposable
    {
        /// <summary>
        /// How many sweeps in a row have to fail before the failure is reported through <see cref="Message"/>.
        /// </summary>
        private const int ConsecutiveFailuresBeforeReport = 3;

        /// <summary>
        /// Guards the registry and the polling task against the poll loop, the handler thread inside
        /// <see cref="ProcessOrderState"/>, the order threads through <see cref="Watch(string)"/> /
        /// <see cref="Unwatch"/> / <see cref="UpdateOrderState"/>, and the watch-timeout check.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The registry: per brokerage order id, the last state seen and what was already reported for it.
        /// </summary>
        private readonly Dictionary<string, OrderStateEntry> _orderStates = new();

        /// <summary>
        /// The brokerage's message handler, when it has one. The constructor wires it both ways: polled
        /// states enqueue here, and <see cref="ProcessOrderState"/> is registered as their listener, so
        /// polled states queue behind an order request that holds the stream lock.
        /// </summary>
        private readonly BrokerageConcurrentMessageHandler _messageHandler;

        /// <summary>
        /// Where each state a sweep returns goes: the message handler, or without one straight into
        /// <see cref="ProcessOrderState"/>.
        /// </summary>
        private readonly Action<BrokerageOrderSnapshot> _route;

        /// <summary>
        /// Resolves brokerage order ids to Lean orders on every compare, so the service never drifts from
        /// what Lean actually knows.
        /// </summary>
        private readonly IOrderProvider _orderProvider;

        /// <summary>
        /// Cancels the current polling task. Recreated by <see cref="Start"/> and cleared by <see cref="Stop"/>.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// The background polling task for the current run. <see cref="Stop"/> waits on it briefly, so the
        /// cancellation source is only disposed once the loop no longer uses it.
        /// </summary>
        private Task _pollingTask;

        /// <summary>
        /// Set by <see cref="Dispose"/> so a disposed service refuses to start again.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Backing field of <see cref="IsPolling"/>, read from the order placement and connection paths
        /// while the polling task is being started or stopped.
        /// </summary>
        private volatile bool _isPolling;

        /// <summary>
        /// The order events one state produced, in order: the submit first, then fills, then a close.
        /// Raised inside <see cref="ProcessOrderState"/>, never empty. The brokerage forwards them to
        /// its own <c>OnOrderEvents</c>.
        /// </summary>
        public event EventHandler<List<OrderEvent>> OrderEvents;

        /// <summary>
        /// A watched order that nothing reported for <see cref="WatchTimeout"/> of polling. Raised once;
        /// the id is unwatched with it. The brokerage decides what the silence means.
        /// </summary>
        public event EventHandler<BrokerageOrderNeverNotifiedEventArgs> BrokerageOrderNeverNotified;

        /// <summary>
        /// Several reads in a row failed, so the run currently has no order updates. Raised once per
        /// outage, as a <see cref="BrokerageMessageType.Warning"/>, never an error.
        /// </summary>
        public event EventHandler<BrokerageMessageEvent> Message;

        /// <summary>
        /// True while the polling task is running.
        /// </summary>
        public bool IsPolling => _isPolling;

        /// <summary>
        /// How long the loop sleeps between sweeps.
        /// </summary>
        public TimeSpan PollInterval { get; }

        /// <summary>
        /// How long a watched order may stay completely unreported, in polling time, before
        /// <see cref="BrokerageOrderNeverNotified"/> is raised for it.
        /// </summary>
        public TimeSpan WatchTimeout { get; }

        /// <summary>
        /// Initializes what both modes share: the message handler wiring, the order provider, and the two
        /// time settings with their defaults.
        /// </summary>
        /// <param name="messageHandler">The brokerage's message handler. The service wires it both ways
        /// itself: it registers <see cref="ProcessOrderState"/> and enqueues every polled state, so one
        /// handler serializes polled states with everything else the brokerage processes. Null routes each
        /// state straight into <see cref="ProcessOrderState"/> - only the poll loop calls it then, so the
        /// calls are still one at a time.</param>
        /// <param name="orderProvider">Resolves brokerage order ids to Lean orders.</param>
        /// <param name="pollInterval">How long the loop sleeps between sweeps. Null falls back to the
        /// <c>brokerage-order-poll-interval-ms</c> configuration entry, default 3000 ms.</param>
        /// <param name="watchTimeout">How long a watched order may stay unreported before
        /// <see cref="BrokerageOrderNeverNotified"/> is raised. Null falls back to one minute.</param>
        protected BrokerageOrderPollingService(BrokerageConcurrentMessageHandler messageHandler, IOrderProvider orderProvider,
            TimeSpan? pollInterval = null, TimeSpan? watchTimeout = null)
        {
            if (messageHandler != null)
            {
                _messageHandler = messageHandler;
                _route = messageHandler.HandleNewMessage;
                messageHandler.Register<BrokerageOrderSnapshot>(ProcessOrderState);
            }
            else
            {
                _route = ProcessOrderState;
            }
            _orderProvider = orderProvider;
            PollInterval = pollInterval ?? TimeSpan.FromMilliseconds(Config.GetInt("brokerage-order-poll-interval-ms", 3000));
            WatchTimeout = watchTimeout ?? TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// One read of the broker, giving the states the sweep saw. The loop calls it every
        /// <see cref="PollInterval"/>, hands each state to the route, and counts a throw as one failed sweep.
        /// A null state is skipped: in per-id mode it means the broker does not know the id yet.
        /// </summary>
        protected abstract IEnumerable<BrokerageOrderSnapshot> Sweep();

        /// <summary>
        /// A copy of the brokerage order ids a sweep still has to read: everything tracked whose end was
        /// not reported yet. Taken under the registry lock, so an order placed mid-sweep is picked up by
        /// the next one.
        /// </summary>
        protected List<string> GetWatchedBrokerageIds()
        {
            lock (_lock)
            {
                var brokerageIds = new List<string>(_orderStates.Count);
                foreach (var (brokerageId, entry) in _orderStates)
                {
                    if (!entry.TerminalReported)
                    {
                        brokerageIds.Add(brokerageId);
                    }
                }
                return brokerageIds;
            }
        }

        /// <summary>
        /// Watches a brokerage order id, with nothing seen for it yet, so the first state to carry the id
        /// acknowledges the order and <see cref="WatchTimeout"/> of silence raises
        /// <see cref="BrokerageOrderNeverNotified"/>. Idempotent: watching an already-watched id never overwrites
        /// its state.
        /// </summary>
        /// <param name="brokerageId">The brokerage order id to watch.</param>
        public void Watch(string brokerageId)
        {
            Watch(brokerageId, lastSeen: null);
        }

        /// <summary>
        /// Watches a brokerage order id, seeded with what another path already reported, so the next poll
        /// does not repeat it. Used for orders adopted at startup, for a submit reported from the request
        /// path, and to move state onto the new id of a replace. Idempotent: watching an already-watched
        /// id never overwrites its state.
        /// </summary>
        /// <param name="brokerageId">The brokerage order id to watch.</param>
        /// <param name="lastSeen">The state another path already reported for the order.</param>
        public void Watch(string brokerageId, BrokerageOrderSnapshot lastSeen)
        {
            lock (_lock)
            {
                if (!_orderStates.TryGetValue(brokerageId, out var entry))
                {
                    entry = new OrderStateEntry();
                    if (lastSeen != null)
                    {
                        entry.LastSeen = lastSeen;
                        entry.ReportedFilledQuantity = lastSeen.FilledQuantity ?? 0m;
                        // seeded means another path already heard from the broker about this order,
                        // and a seed carrying the order's end means the end was already reported
                        entry.Acknowledged = true;
                        entry.SubmitReported = lastSeen.Status != OrderStatus.New;
                        entry.TerminalReported = lastSeen.Status == OrderStatus.Canceled || lastSeen.Status == OrderStatus.Invalid;
                    }
                    _orderStates[brokerageId] = entry;
                }
                entry.Watched = true;
            }
        }

        /// <summary>
        /// Watches the new brokerage order id of a replace and drops the replaced id in the same step.
        /// The first state to carry the new id reports the order as update submitted, which a stream
        /// would otherwise do. The new id starts with no fill state, because a replacement that counts
        /// its executions from zero must not inherit the old order's numbers; a broker that carries the
        /// fills across a replace seeds with <see cref="Watch(string, BrokerageOrderSnapshot)"/> instead.
        /// </summary>
        /// <param name="brokerageId">The brokerage order id the replacement runs under.</param>
        /// <param name="previousBrokerageId">The replaced brokerage order id, or null when it is unknown.</param>
        public void WatchReplacement(string brokerageId, string previousBrokerageId)
        {
            lock (_lock)
            {
                if (previousBrokerageId != null)
                {
                    _orderStates.Remove(previousBrokerageId);
                }

                if (!_orderStates.TryGetValue(brokerageId, out var entry))
                {
                    entry = new OrderStateEntry();
                    _orderStates[brokerageId] = entry;
                }
                entry.Watched = true;
                entry.IsReplacement = true;
            }
        }

        /// <summary>
        /// Stops watching an order and drops its state.
        /// </summary>
        /// <param name="brokerageId">The brokerage order id to stop watching.</param>
        public void Unwatch(string brokerageId)
        {
            lock (_lock)
            {
                _orderStates.Remove(brokerageId);
            }
        }

        /// <summary>
        /// Records what another path already reported for an order, so the next poll does not repeat it.
        /// Called by the streaming path while the stream lives, after it reports its own event.
        /// </summary>
        /// <param name="brokerageId">The brokerage order id the state belongs to.</param>
        /// <param name="orderState">The cumulative state the other path reported.</param>
        public void UpdateOrderState(string brokerageId, BrokerageOrderSnapshot orderState)
        {
            lock (_lock)
            {
                if (!_orderStates.TryGetValue(brokerageId, out var entry))
                {
                    entry = new OrderStateEntry();
                    _orderStates[brokerageId] = entry;
                }

                entry.LastSeen = orderState;
                entry.Acknowledged = true;
                // the already-reported quantity never shrinks
                var filledQuantity = orderState.FilledQuantity ?? 0m;
                if (filledQuantity > entry.ReportedFilledQuantity)
                {
                    entry.ReportedFilledQuantity = filledQuantity;
                }
                // a state written by the other path means its submit is out, and a terminal state means
                // the end was already reported - a later sweep must not repeat either
                if (orderState.Status != OrderStatus.New)
                {
                    entry.SubmitReported = true;
                }
                if (orderState.Status == OrderStatus.Canceled || orderState.Status == OrderStatus.Invalid)
                {
                    entry.TerminalReported = true;
                }
            }
        }

        /// <summary>
        /// The last state seen for an order, from any path. The streaming path reads it for its own
        /// duplicate check, and a replace reads it to move the state to the new id.
        /// </summary>
        /// <param name="brokerageId">The brokerage order id to look up.</param>
        /// <param name="lastSeen">When this method returns <c>true</c>, the last state seen; otherwise null.</param>
        /// <returns><c>true</c> when a state was ever recorded for the id; otherwise <c>false</c>.</returns>
        public bool TryGetLastOrderState(string brokerageId, out BrokerageOrderSnapshot lastSeen)
        {
            lock (_lock)
            {
                lastSeen = null;
                if (_orderStates.TryGetValue(brokerageId, out var entry))
                {
                    lastSeen = entry.LastSeen;
                }
                return lastSeen != null;
            }
        }

        /// <summary>
        /// Compares a state with the last one seen for the same order and raises <see cref="OrderEvents"/>
        /// with what is new: the submit first, then fills, then a close. The constructor registers it on the
        /// message handler, so polled orders queue behind an order request that holds the stream lock. Not
        /// safe to run twice at the same time - the handler runs it one call at a time, and without a
        /// handler only the poll loop calls it.
        /// </summary>
        /// <param name="orderState">The state a sweep read from the broker.</param>
        public void ProcessOrderState(BrokerageOrderSnapshot orderState)
        {
            if (orderState == null || string.IsNullOrEmpty(orderState.BrokerageOrderId))
            {
                return;
            }

            var brokerageId = orderState.BrokerageOrderId;

            // record the id as seen: the broker knows the order, so the watch timeout stops counting
            lock (_lock)
            {
                if (_orderStates.TryGetValue(brokerageId, out var seenEntry))
                {
                    seenEntry.Acknowledged = true;
                }
            }

            // a list, because combo legs can share one brokerage id and each leg is its own Lean order
            var leanOrders = _orderProvider?.GetOrdersByBrokerageId(brokerageId);
            if (leanOrders == null || leanOrders.Count == 0)
            {
                // not ours, or ours with the id not on the Lean order yet - the next sweep sees it again
                return;
            }

            if (leanOrders.TrueForAll(order => order.Status.IsClosed()))
            {
                // nothing left to report; dropping the state here is the only safe moment, because Lean
                // has already applied the end of the order
                Unwatch(brokerageId);
                return;
            }

            var timeUtc = orderState.TimeUtc == default ? DateTime.UtcNow : orderState.TimeUtc;
            var orderEvents = new List<OrderEvent>();

            // the whole compare runs under the registry lock, so the streaming path writing the same entry
            // through UpdateOrderState can never interleave with the diff's read-then-write bookkeeping
            lock (_lock)
            {
                if (!_orderStates.TryGetValue(brokerageId, out var entry))
                {
                    entry = new OrderStateEntry();
                    _orderStates[brokerageId] = entry;
                }
                entry.Acknowledged = true;

                // the submit first, once: when nothing was emitted for the id yet, the Lean order is still New,
                // and the state is not a reject. Lean requires it before any fill, and a market order can
                // already be Filled the first time a poll sees it. The new id of a replace is the one case
                // where the Lean order is already past New: there the state proves the replacement is live,
                // so the update submit goes out instead.
                if (!entry.SubmitReported
                    && (entry.LastSeen == null || entry.LastSeen.Status == OrderStatus.New)
                    && orderState.Status != OrderStatus.Invalid)
                {
                    foreach (var leanOrder in leanOrders)
                    {
                        if (leanOrder.Status == OrderStatus.New)
                        {
                            orderEvents.Add(new OrderEvent(leanOrder, timeUtc, OrderFee.Zero, "Submitted by polling")
                            {
                                Status = OrderStatus.Submitted
                            });
                            entry.SubmitReported = true;
                        }
                        else if (entry.IsReplacement && !leanOrder.Status.IsClosed())
                        {
                            orderEvents.Add(new OrderEvent(leanOrder, timeUtc, OrderFee.Zero, "Update submitted by polling")
                            {
                                Status = OrderStatus.UpdateSubmitted
                            });
                            entry.SubmitReported = true;
                        }
                    }
                }

                // then the fills, so a close can never outrun a fill of the same order. A fill needs both
                // numbers: without a price the service would have to invent one, and it never invents a
                // number - a read without prices simply reports less.
                if (orderState.FilledQuantity.HasValue && orderState.FillPrice.HasValue)
                {
                    var cumulativeFilled = orderState.FilledQuantity.Value;
                    var newPart = cumulativeFilled - entry.ReportedFilledQuantity;
                    if (newPart > 0m)
                    {
                        var fillPrice = orderState.FillPrice.Value;
                        if (leanOrders.Count == 1)
                        {
                            var leanOrder = leanOrders[0];
                            if (!leanOrder.Status.IsClosed())
                            {
                                orderEvents.Add(new OrderEvent(leanOrder, timeUtc, OrderFee.Zero)
                                {
                                    Status = cumulativeFilled >= leanOrder.AbsoluteQuantity ? OrderStatus.Filled : OrderStatus.PartiallyFilled,
                                    FillQuantity = leanOrder.Direction == OrderDirection.Sell ? -newPart : newPart,
                                    FillPrice = fillPrice
                                });
                            }
                            entry.ReportedFilledQuantity = cumulativeFilled;
                        }
                        else if (leanOrders[0].GroupOrderManager == null || leanOrders[0].GroupOrderManager.Quantity == 0m)
                        {
                            Log.Error($"{GetType().Name}.{nameof(ProcessOrderState)}(): cannot split the fill of brokerage order '{brokerageId}' " +
                                $"across {leanOrders.Count} Lean orders without a group quantity, skipping the fill.");
                        }
                        else
                        {
                            // one brokerage id, many Lean leg orders: the state counts in strategy units, and
                            // each leg gets its share of the new part, sized by its own quantity
                            var groupQuantity = Math.Abs(leanOrders[0].GroupOrderManager.Quantity);
                            foreach (var leanOrder in leanOrders)
                            {
                                if (leanOrder.Status.IsClosed())
                                {
                                    continue;
                                }
                                orderEvents.Add(new OrderEvent(leanOrder, timeUtc, OrderFee.Zero)
                                {
                                    Status = cumulativeFilled >= groupQuantity ? OrderStatus.Filled : OrderStatus.PartiallyFilled,
                                    FillQuantity = leanOrder.Quantity * newPart / groupQuantity,
                                    FillPrice = fillPrice
                                });
                            }
                            entry.ReportedFilledQuantity = cumulativeFilled;
                        }
                    }
                }

                // the end of the order last, once. The id leaves the read list, but its state stays until a
                // compare sees the Lean order closed - forgetting it here would re-report every fill if the
                // next sweep lands before Lean applies this event.
                if ((orderState.Status == OrderStatus.Canceled || orderState.Status == OrderStatus.Invalid) && !entry.TerminalReported)
                {
                    foreach (var leanOrder in leanOrders)
                    {
                        if (!leanOrder.Status.IsClosed())
                        {
                            orderEvents.Add(new OrderEvent(leanOrder, timeUtc, OrderFee.Zero, orderState.Message)
                            {
                                Status = orderState.Status
                            });
                        }
                    }
                    entry.TerminalReported = true;
                }

                entry.LastSeen = orderState;
            }

            if (orderEvents.Count > 0)
            {
                OrderEvents?.Invoke(this, orderEvents);
            }
        }

        /// <summary>
        /// The whole handover from a stream to polling, in the only safe order: process what the stream
        /// already delivered, pre-load the registry with one <see cref="Watch(string, BrokerageOrderSnapshot)"/>
        /// per open Lean order, then start the loop. Does nothing while polling already runs.
        /// </summary>
        /// <example>
        /// The stream reported 100 of 233 shares, the pre-load carries 100, so the first sweep reports only the other 133.
        /// </example>
        /// <param name="preLoadOpenOrders">Builds the state another path already reported for one open Lean
        /// order: the brokerage id, the order's status and the cumulative filled quantity. A null return
        /// skips the order, and a null callback pre-loads nothing.</param>
        public void Start(Func<Order, BrokerageOrderSnapshot> preLoadOpenOrders)
        {
            if (IsPolling)
            {
                return;
            }

            // an empty locked block waits for any order request in flight and processes the stream messages
            // it buffered, so the pre-load below counts every fill the stream delivered
            _messageHandler?.WithLockedStream(() => { });

            if (preLoadOpenOrders != null)
            {
                Log.Trace($"{GetType().Name}.{nameof(Start)}(): pre-loading the open orders.");

                var openOrderCount = 0;
                var preLoadedCount = 0;
                foreach (var openLeanOrder in _orderProvider?.GetOpenOrders() ?? [])
                {
                    openOrderCount++;
                    var lastSeen = preLoadOpenOrders(openLeanOrder);
                    if (lastSeen != null && !string.IsNullOrEmpty(lastSeen.BrokerageOrderId))
                    {
                        Watch(lastSeen.BrokerageOrderId, lastSeen);
                        preLoadedCount++;
                    }
                }

                Log.Trace($"{GetType().Name}.{nameof(Start)}(): pre-loaded {preLoadedCount} of {openOrderCount} open order(s).");
            }

            Start();
        }

        /// <summary>
        /// Starts the background polling task. Idempotent while running; after <see cref="Stop"/> a later
        /// call resumes polling. Does nothing once the service has been disposed.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                // A run is active while the source exists: Start creates it, Stop clears it. A stopped
                // run's task exits on its own without handling more orders, so a new run does not wait for it.
                if (_disposed || _cancellationTokenSource != null)
                {
                    return;
                }

                _isPolling = true;
                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;
                // Task.Run so the first read starts on a pool thread instead of blocking the caller.
                _pollingTask = Task.Run(() => PollLoop(cancellationToken));
            }
        }

        /// <summary>
        /// Stops the polling loop but keeps the service usable, so a later <see cref="Start"/> resumes
        /// polling. The registry survives a stop, so nothing already reported repeats after a restart.
        /// </summary>
        public void Stop()
        {
            Task pollingTask;
            CancellationTokenSource cancellationTokenSource;
            lock (_lock)
            {
                pollingTask = _pollingTask;
                cancellationTokenSource = _cancellationTokenSource;
                _cancellationTokenSource = null;
                _pollingTask = null;
                _isPolling = false;
            }

            if (cancellationTokenSource == null)
            {
                return;
            }

            cancellationTokenSource.Cancel();

            // Dispose the source only once the loop has actually stopped, so the loop never waits on a
            // disposed handle. If the loop is still blocked on a slow read, leave the source for the GC.
            if (pollingTask == null || pollingTask.Wait(TimeSpan.FromSeconds(2)))
            {
                cancellationTokenSource.DisposeSafely();
            }
        }

        /// <summary>
        /// Stops the polling loop and marks the service as disposed, so it cannot be started again.
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

            Stop();
        }

        /// <summary>
        /// Re-reads the broker on each sweep and routes every state, until cancelled.
        /// </summary>
        /// <param name="cancellationToken">Cancelled to stop the loop.</param>
        private async Task PollLoop(CancellationToken cancellationToken)
        {
            Log.Trace($"{GetType().Name}.{nameof(PollLoop)}(): started, polling every {PollInterval.TotalMilliseconds}ms.");

            // per run, so a stopped loop still draining a slow read never shares them with the next run
            var consecutiveFailureCount = 0;
            var isPollingFailureReported = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var orderState in Sweep())
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        // a per-id read returns null when the broker does not know the id yet
                        if (orderState != null)
                        {
                            _route(orderState);
                        }
                    }

                    consecutiveFailureCount = 0;
                    isPollingFailureReported = false;

                    // silence only means something after a read that succeeded: a failed sweep asked
                    // the broker nothing, so it must not count against a watched order
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        CheckWatchTimeouts();
                    }
                }
                catch (Exception ex)
                {
                    // A transient read failure must not kill the loop: log and try again next sweep.
                    Log.Error($"{GetType().Name}.{nameof(PollLoop)}(): failed to poll orders: {ex.Message}");

                    // A failure that keeps coming back is not transient any more, and the run may have no
                    // order updates at all while it lasts, so say so once instead of only a log line.
                    if (++consecutiveFailureCount >= ConsecutiveFailuresBeforeReport && !isPollingFailureReported)
                    {
                        isPollingFailureReported = true;
                        Message?.Invoke(this, new BrokerageMessageEvent(BrokerageMessageType.Warning, "OrderPollingFailed",
                            $"Several order reads in a row failed, so no order update is reported until a read succeeds: {ex.Message}"));
                    }
                }

                try
                {
                    await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            Log.Trace($"{GetType().Name}.{nameof(PollLoop)}(): stopped.");
        }

        /// <summary>
        /// Counts one interval of silence for every watched order the broker never reported, and raises
        /// <see cref="BrokerageOrderNeverNotified"/> once for each one that reached the watch timeout. Called only
        /// after a successful sweep, because only a read that succeeded proves the silence is real.
        /// </summary>
        private void CheckWatchTimeouts()
        {
            List<(string BrokerageId, TimeSpan WatchDuration)> expired = null;
            lock (_lock)
            {
                foreach (var (brokerageId, entry) in _orderStates)
                {
                    if (!entry.Watched || entry.Acknowledged)
                    {
                        continue;
                    }

                    entry.UnacknowledgedDuration += PollInterval;
                    if (entry.UnacknowledgedDuration >= WatchTimeout)
                    {
                        (expired ??= []).Add((brokerageId, entry.UnacknowledgedDuration));
                    }
                }

                if (expired != null)
                {
                    foreach (var (brokerageId, _) in expired)
                    {
                        _orderStates.Remove(brokerageId);
                    }
                }
            }

            if (expired != null)
            {
                foreach (var (brokerageId, watchDuration) in expired)
                {
                    // Resolved outside the registry lock. A placement whose id assignment was itself the
                    // thing that never happened resolves to no Lean order, so the args carry null then.
                    var leanOrder = _orderProvider?.GetOrdersByBrokerageId(brokerageId)?.FirstOrDefault();
                    BrokerageOrderNeverNotified?.Invoke(this, new BrokerageOrderNeverNotifiedEventArgs(brokerageId, leanOrder, watchDuration));
                }
            }
        }

        /// <summary>
        /// What the registry keeps per brokerage order id.
        /// </summary>
        private class OrderStateEntry
        {
            /// <summary>
            /// The last state seen for the order, from any path. Null when nothing was seen yet, so the
            /// submit is still due.
            /// </summary>
            public BrokerageOrderSnapshot LastSeen;

            /// <summary>
            /// The cumulative filled quantity already reported to Lean, by any path. Never shrinks.
            /// </summary>
            public decimal ReportedFilledQuantity;

            /// <summary>
            /// Set once the submit was reported for the order, by any path, so it goes out exactly once.
            /// </summary>
            public bool SubmitReported;

            /// <summary>
            /// Set once the order's end was reported, so the id leaves the read list and a later state
            /// for it reports nothing new.
            /// </summary>
            public bool TerminalReported;

            /// <summary>
            /// Set by <see cref="Watch(string)"/>: the watch timeout only applies to explicitly watched orders.
            /// </summary>
            public bool Watched;

            /// <summary>
            /// Set by <see cref="WatchReplacement"/>: the id is the new id of a replace, so the first
            /// state to carry it reports the update submit instead of a plain submit.
            /// </summary>
            public bool IsReplacement;

            /// <summary>
            /// Set once anything carried the id: a polled state, a stream write, or a seed. Stops the
            /// watch timeout.
            /// </summary>
            public bool Acknowledged;

            /// <summary>
            /// How long the order has been watched with nothing reporting it, in polling time.
            /// </summary>
            public TimeSpan UnacknowledgedDuration;
        }
    }
}
