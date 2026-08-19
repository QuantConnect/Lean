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

using QuantConnect.Brokerages.Services.OrderPolling.Models;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Services.OrderPolling
{
    /// <summary>
    /// Reads orders from the brokerage on an interval and turns what it reads into order events.
    /// Use it when a brokerage has no order stream, when the stream goes down, or to check an order
    /// the broker never replied about. This base class holds what both modes share: the loop, the
    /// subscription registry, the compare and the events. The subclass decides what one poll reads:
    /// <see cref="SingleOrderPollingService"/> or <see cref="BulkOrdersPollingService"/>.
    /// </summary>
    public abstract class BaseBrokerageOrderPollingService : IDisposable
    {
        /// <summary>
        /// How many polls must fail one after another to raise the <see cref="Message"/> warning.
        /// </summary>
        private const int MaxFailedPollsBeforeWarning = 3;

        /// <summary>
        /// One lock for the registry and the polling task. Taken by the poll loop, the handler thread
        /// inside <see cref="ProcessOrderState"/>, and the order threads through the subscribe methods.
        /// </summary>
        private readonly Lock _lock = new();

        /// <summary>
        /// The registry: per brokerage order id, the last state seen and what was already reported for it.
        /// </summary>
        private readonly Dictionary<string, OrderTrackingEntry> _orderEntries = [];

        /// <summary>
        /// The brokerage's message handler, when it has one. Polled states enqueue here with
        /// <see cref="ProcessOrderState"/> as their listener, so they queue behind an order request
        /// that holds the stream lock.
        /// </summary>
        private readonly BrokerageConcurrentMessageHandler _messageHandler;

        /// <summary>
        /// Where each state a poll returns goes: the message handler, or without one straight into
        /// <see cref="ProcessOrderState"/>.
        /// </summary>
        private readonly Action<BrokerageOrderSnapshot> _processSnapshot;

        /// <summary>
        /// Resolves brokerage order ids to Lean orders on every compare.
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
        /// A subscribed order that nothing reported for <see cref="NotificationTimeout"/> of polling. Raised once;
        /// the id is unsubscribed with it. The brokerage decides what the silence means.
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
        /// How long the loop sleeps between polls.
        /// </summary>
        public TimeSpan PollInterval { get; }

        /// <summary>
        /// How long a subscribed order may stay completely unreported, in polling time, before
        /// <see cref="BrokerageOrderNeverNotified"/> is raised for it.
        /// </summary>
        public TimeSpan NotificationTimeout { get; }

        /// <summary>
        /// Initializes what both modes share: the message handler wiring, the order provider, and the two
        /// time settings with their defaults.
        /// </summary>
        /// <param name="messageHandler">The brokerage's message handler. The service registers
        /// <see cref="ProcessOrderState"/> and enqueues every polled state, so one handler serializes
        /// them with the brokerage's other messages. Null processes each state directly - only the
        /// poll loop calls then, so the calls are still one at a time.</param>
        /// <param name="orderProvider">Resolves brokerage order ids to Lean orders.</param>
        /// <param name="pollInterval">How long the loop sleeps between polls. Null falls back to the
        /// <c>brokerage-order-poll-interval-ms</c> configuration entry, default 3000 ms.</param>
        /// <param name="notificationTimeout">How long a subscribed order may stay unreported before
        /// <see cref="BrokerageOrderNeverNotified"/> is raised. Null takes 60000 ms.</param>
        protected BaseBrokerageOrderPollingService(BrokerageConcurrentMessageHandler messageHandler, IOrderProvider orderProvider,
            TimeSpan? pollInterval = null, TimeSpan? notificationTimeout = null)
        {
            if (messageHandler != null)
            {
                _messageHandler = messageHandler;
                _processSnapshot = messageHandler.HandleNewMessage;
                messageHandler.Register<BrokerageOrderSnapshot>(ProcessOrderState);
            }
            else
            {
                _processSnapshot = ProcessOrderState;
            }
            _orderProvider = orderProvider;
            PollInterval = pollInterval ?? TimeSpan.FromMilliseconds(Config.GetInt("brokerage-order-poll-interval-ms", 3 * 1000));
            NotificationTimeout = notificationTimeout ?? TimeSpan.FromMilliseconds(60 * 1000);
        }

        /// <summary>
        /// One read of the broker, giving the current order snapshots. The loop calls it every
        /// <see cref="PollInterval"/>, hands each snapshot on for processing, and counts a throw as one failed poll.
        /// A null snapshot is skipped: in per-id mode it means the broker does not know the id yet.
        /// </summary>
        protected abstract IEnumerable<BrokerageOrderSnapshot> GetOrderSnapshots();

        /// <summary>
        /// The brokerage order ids a poll still has to read: everything tracked whose end was not
        /// reported yet. The entries are copied under the registry lock and filtered outside it, so
        /// the order paths never wait on the filter.
        /// </summary>
        protected IEnumerable<string> GetOpenBrokerageIds()
        {
            var entries = default(KeyValuePair<string, OrderTrackingEntry>[]);
            lock (_lock)
            {
                entries = _orderEntries.ToArray();
            }

            foreach (var (brokerageId, entry) in entries)
            {
                if (!entry.TerminalReported)
                {
                    yield return brokerageId;
                }
            }
        }

        /// <summary>
        /// Subscribes to a brokerage order id. The first state to carry the id acknowledges the order;
        /// <see cref="NotificationTimeout"/> of silence raises <see cref="BrokerageOrderNeverNotified"/>.
        /// Idempotent: a repeat call never overwrites the recorded state.
        /// </summary>
        /// <param name="brokerageId">The brokerage order id to subscribe to.</param>
        public void Subscribe(string brokerageId)
        {
            Subscribe(brokerageId, lastSeen: null);
        }

        /// <summary>
        /// Subscribes to a brokerage order id, seeded with what another path already reported, so the
        /// next poll does not repeat it. Used for orders adopted at startup and for stream-to-polling
        /// handovers. Idempotent: a repeat call never overwrites the recorded state.
        /// </summary>
        /// <param name="brokerageId">The brokerage order id to subscribe to.</param>
        /// <param name="lastSeen">The state another path already reported for the order.</param>
        public void Subscribe(string brokerageId, BrokerageOrderSnapshot lastSeen)
        {
            lock (_lock)
            {
                if (!_orderEntries.TryGetValue(brokerageId, out var entry))
                {
                    entry = lastSeen == null ? new OrderTrackingEntry() : new OrderTrackingEntry(lastSeen);
                    _orderEntries[brokerageId] = entry;
                }
                entry.Subscribed = true;
            }
        }

        /// <summary>
        /// Subscribes to the new brokerage order id of a replace and drops the replaced id in one step.
        /// The first state to carry the new id reports the update submit. The new id starts with no fill
        /// state; a broker that carries fills across a replace seeds with
        /// <see cref="Subscribe(string, BrokerageOrderSnapshot)"/> instead.
        /// </summary>
        /// <param name="brokerageId">The brokerage order id the replacement runs under.</param>
        /// <param name="previousBrokerageId">The replaced brokerage order id, or null when it is unknown.</param>
        public void SubscribeReplacement(string brokerageId, string previousBrokerageId)
        {
            lock (_lock)
            {
                if (previousBrokerageId != null)
                {
                    _orderEntries.Remove(previousBrokerageId);
                }

                var entry = GetOrCreateEntry(brokerageId);
                entry.Subscribed = true;
                entry.IsReplacement = true;
            }
        }

        /// <summary>
        /// Unsubscribes an order and drops its state.
        /// </summary>
        /// <param name="brokerageId">The brokerage order id to unsubscribe.</param>
        public void Unsubscribe(string brokerageId)
        {
            lock (_lock)
            {
                _orderEntries.Remove(brokerageId);
            }
        }

        /// <summary>
        /// The entry for the id, created empty when the id is not tracked yet. The caller holds the
        /// registry lock.
        /// </summary>
        /// <param name="brokerageId">The brokerage order id to look up.</param>
        private OrderTrackingEntry GetOrCreateEntry(string brokerageId)
        {
            if (!_orderEntries.TryGetValue(brokerageId, out var entry))
            {
                entry = new OrderTrackingEntry();
                _orderEntries[brokerageId] = entry;
            }
            return entry;
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
                var entry = GetOrCreateEntry(brokerageId);

                entry.LastSnapshot = orderState;
                entry.Acknowledged = true;
                // the already-reported quantity never shrinks
                var filledQuantity = orderState.FilledQuantity ?? 0m;
                if (filledQuantity > entry.ReportedFilledQuantity)
                {
                    entry.ReportedFilledQuantity = filledQuantity;
                }
                // a state written by the other path means its submit is out, and a terminal state means
                // the end was already reported - a later poll must not repeat either
                if (orderState.Status != OrderStatus.New)
                {
                    entry.SubmittedOrderEventInvoked = true;
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
                if (_orderEntries.TryGetValue(brokerageId, out var entry))
                {
                    lastSeen = entry.LastSnapshot;
                }
                return lastSeen != null;
            }
        }

        /// <summary>
        /// Compares a state with the last one seen for the same order and raises <see cref="OrderEvents"/>
        /// with what is new: the submit first, then fills, then a close. Not safe to run twice at the
        /// same time - the message handler runs it one call at a time, and without a handler only the
        /// poll loop calls it.
        /// </summary>
        /// <param name="orderState">The state a poll read from the broker.</param>
        public void ProcessOrderState(BrokerageOrderSnapshot orderState)
        {
            if (orderState == null || string.IsNullOrEmpty(orderState.BrokerageOrderId))
            {
                return;
            }

            var brokerageId = orderState.BrokerageOrderId;

            // record the id as seen: the broker knows the order, so the notification timeout stops counting
            lock (_lock)
            {
                if (_orderEntries.TryGetValue(brokerageId, out var seenEntry))
                {
                    seenEntry.Acknowledged = true;
                }
            }

            // a list, because combo legs can share one brokerage id and each leg is its own Lean order
            var leanOrders = _orderProvider?.GetOrdersByBrokerageId(brokerageId);
            if (leanOrders == null || leanOrders.Count == 0)
            {
                // not ours, or ours with the id not on the Lean order yet - the next poll sees it again
                return;
            }

            if (leanOrders.TrueForAll(order => order.Status.IsClosed()))
            {
                // nothing left to report; dropping the state here is the only safe moment, because Lean
                // has already applied the end of the order
                Unsubscribe(brokerageId);
                return;
            }

            var timeUtc = orderState.TimeUtc == default ? DateTime.UtcNow : orderState.TimeUtc;
            var orderEvents = new List<OrderEvent>();

            // the whole compare runs under the registry lock, so the streaming path writing the same entry
            // through UpdateOrderState can never interleave with the diff's read-then-write bookkeeping
            lock (_lock)
            {
                var entry = GetOrCreateEntry(brokerageId);
                entry.Acknowledged = true;

                // the submit first, once: Lean requires it before any fill, and a market order can already
                // be Filled on its first read. The new id of a replace is past New already, so the update
                // submit goes out instead.
                if (!entry.SubmittedOrderEventInvoked
                    && (entry.LastSnapshot == null || entry.LastSnapshot.Status == OrderStatus.New)
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
                            entry.SubmittedOrderEventInvoked = true;
                        }
                        else if (entry.IsReplacement && !leanOrder.Status.IsClosed())
                        {
                            orderEvents.Add(new OrderEvent(leanOrder, timeUtc, OrderFee.Zero, "Update submitted by polling")
                            {
                                Status = OrderStatus.UpdateSubmitted
                            });
                            entry.SubmittedOrderEventInvoked = true;
                        }
                    }
                }

                // then the fills, so a close never comes before a fill of the same order. A fill needs
                // both numbers: the service never invents a price, a read without one reports less.
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

                // the end of the order last, once. The state stays until a compare sees the Lean order
                // closed - dropping it here would re-report every fill on the next poll.
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

                entry.LastSnapshot = orderState;
            }

            if (orderEvents.Count > 0)
            {
                OrderEvents?.Invoke(this, orderEvents);
            }
        }

        /// <summary>
        /// The handover from a stream to polling: process what the stream already delivered, pre-load one
        /// <see cref="Subscribe(string, BrokerageOrderSnapshot)"/> per open Lean order, then start the
        /// loop. Does nothing while polling already runs.
        /// </summary>
        /// <example>
        /// The stream reported 100 of 233 shares, the pre-load carries 100, so the first poll reports only the other 133.
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

            // an empty locked block waits for any order request in flight and processes its buffered
            // stream messages, so the pre-load below counts every fill the stream delivered
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
                        Subscribe(lastSeen.BrokerageOrderId, lastSeen);
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
        /// Stops the polling loop but keeps the service usable, so a later <see cref="Start()"/> resumes
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
            CancellationTokenSource cancellationTokenSource;
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                cancellationTokenSource = _cancellationTokenSource;
            }

            Stop();
            cancellationTokenSource?.Dispose();
        }

        /// <summary>
        /// Re-reads the broker on each poll and routes every state, until cancelled.
        /// </summary>
        /// <param name="cancellationToken">Cancelled to stop the loop.</param>
        private async Task PollLoop(CancellationToken cancellationToken)
        {
            Log.Trace($"{GetType().Name}.{nameof(PollLoop)}(): started, polling every {PollInterval.TotalMilliseconds}ms.");

            // per run, so a stopped loop still finishing a slow read never shares them with the next run
            var consecutiveFailureCount = 0;
            var isPollingFailureReported = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var orderState in GetOrderSnapshots())
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        // a per-id read returns null when the broker does not know the id yet
                        if (orderState != null)
                        {
                            _processSnapshot(orderState);
                        }
                    }

                    consecutiveFailureCount = 0;
                    isPollingFailureReported = false;

                    // silence only means something after a read that succeeded: a failed poll asked
                    // the broker nothing, so it must not count against a subscribed order
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        CheckNotificationTimeouts();
                    }
                }
                catch (Exception ex)
                {
                    // A transient read failure must not kill the loop: log and try again next poll.
                    Log.Error($"{GetType().Name}.{nameof(PollLoop)}(): failed to poll orders: {ex.Message}");

                    // A failure that keeps coming back is not transient any more, and the run may have no
                    // order updates at all while it lasts, so say so once instead of only a log line.
                    if (++consecutiveFailureCount >= MaxFailedPollsBeforeWarning && !isPollingFailureReported)
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
        /// Counts one interval of silence per subscribed order nobody reported, and raises
        /// <see cref="BrokerageOrderNeverNotified"/> once for each that reached the notification timeout.
        /// Called only after a successful poll: only a read that succeeded proves the silence.
        /// </summary>
        private void CheckNotificationTimeouts()
        {
            List<(string BrokerageId, TimeSpan SubscribedDuration)> expired = null;
            lock (_lock)
            {
                foreach (var (brokerageId, entry) in _orderEntries)
                {
                    if (!entry.Subscribed || entry.Acknowledged)
                    {
                        continue;
                    }

                    entry.UnacknowledgedDuration += PollInterval;
                    if (entry.UnacknowledgedDuration >= NotificationTimeout)
                    {
                        (expired ??= []).Add((brokerageId, entry.UnacknowledgedDuration));
                    }
                }

                if (expired != null)
                {
                    foreach (var (brokerageId, _) in expired)
                    {
                        _orderEntries.Remove(brokerageId);
                    }
                }
            }

            if (expired != null)
            {
                foreach (var (brokerageId, subscribedDuration) in expired)
                {
                    // resolved outside the registry lock; a placement whose id was never assigned
                    // resolves to null
                    var leanOrder = _orderProvider?.GetOrdersByBrokerageId(brokerageId)?.FirstOrDefault();
                    BrokerageOrderNeverNotified?.Invoke(this, new BrokerageOrderNeverNotifiedEventArgs(brokerageId, leanOrder, subscribedDuration));
                }
            }
        }
    }
}
