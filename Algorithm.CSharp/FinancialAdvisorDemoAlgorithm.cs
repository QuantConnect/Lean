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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrates unified Financial Advisor group orders and terminal-order snapshot reconciliation.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="financial advisor" />
    public class FinancialAdvisorDemoAlgorithm : QCAlgorithm
    {
        private const string GroupName = "TestGroupEQ";
        private static readonly TimeSpan ReconcileRetryInterval =
            TimeSpan.FromSeconds(5);
        private static readonly TimeSpan InvalidOrderRetryInterval =
            TimeSpan.FromSeconds(5);
        private static readonly TimeSpan TopologyRefreshInterval =
            TimeSpan.FromMinutes(1);
        private const int CompleteRefreshTopologyTicks = 3;
        private const int MaximumInvalidOrderRetries = 3;

        private readonly object _orderStateLock = new();
        private readonly Dictionary<int, TerminalOrderEvent>
            _terminalEventsDuringSubmission = new();
        private Symbol _symbol;
        private Func<OrderTicket> _submitGroupOrder;
        private BrokerageAccountSnapshot _preOrderSnapshot;
        private bool _initialSnapshotRefreshAccepted;
        private long _initialSnapshotRequestGeneration = -1;
        private bool _groupOrderSubmitted;
        private bool _orderSubmissionInProgress;
        private int _groupOrderId;
        private int _invalidOrderRetryCount;
        private DateTime _nextGroupOrderRetryUtc;
        private long _pendingReconcileGeneration = -1;
        private DateTime _pendingReconcileTerminalUtc;
        private DateTime _nextReconcileRefreshUtc;
        private int _scheduledRefreshTopologyTicks;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            _symbol = AddEquity("SPY").Symbol;
            _submitGroupOrder = () => SetHoldings(
                _symbol,
                1,
                asynchronous: true).FirstOrDefault();

            // The default order properties can be set here to choose the FA settings
            // to be automatically used in any order submission method (such as SetHoldings, Buy, Sell and Order)

            // Use a unified FA Account Group. Leaving FaMethod blank uses the
            // allocation method saved for the group in TWS.
            DefaultOrderProperties = new InteractiveBrokersOrderProperties
            {
                // account group created manually in IB/TWS
                FaGroup = GroupName
            };

            if (LiveMode)
            {
                // Every third one-minute topology tick expands to complete account state.
                Schedule.On(
                    DateRules.EveryDay(),
                    TimeRules.Every(TopologyRefreshInterval),
                    RequestScheduledSnapshotRefresh);
                _nextReconcileRefreshUtc = UtcTime;
                _nextGroupOrderRetryUtc = UtcTime;
                TryRequestInitialSnapshotRefresh(BrokerageAccountSnapshot);
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!LiveMode)
            {
                if (!Portfolio.Invested)
                {
                    // when logged into IB as a Financial Advisor, this call will use order properties
                    // set in the DefaultOrderProperties property of QCAlgorithm
                    SetHoldings(_symbol, 1);
                }
                return;
            }

            var snapshot = BrokerageAccountSnapshot;
            BrokerageAccountSnapshot preOrderSnapshot = null;
            var reconcile = false;
            var requestReconcileRefresh = false;
            lock (_orderStateLock)
            {
                if (_pendingReconcileGeneration >= 0)
                {
                    if (snapshot.IsReady &&
                        snapshot.Generation >
                            _pendingReconcileGeneration &&
                        snapshot.CollectionStartedUtc >=
                            _pendingReconcileTerminalUtc)
                    {
                        preOrderSnapshot = _preOrderSnapshot;
                        _preOrderSnapshot = null;
                        _pendingReconcileGeneration = -1;
                        reconcile = true;
                    }
                    else
                    {
                        requestReconcileRefresh = true;
                    }
                }
            }
            if (reconcile)
            {
                ReconcileAccountPositions(
                    preOrderSnapshot,
                    snapshot);
                return;
            }
            if (requestReconcileRefresh)
            {
                TryRequestReconcileRefresh(snapshot);
                return;
            }

            bool requestInitialRefresh;
            lock (_orderStateLock)
            {
                requestInitialRefresh =
                    !_initialSnapshotRefreshAccepted ||
                    !snapshot.IsReady ||
                    snapshot.Generation <=
                        _initialSnapshotRequestGeneration;
            }
            if (requestInitialRefresh)
            {
                TryRequestInitialSnapshotRefresh(snapshot);
                return;
            }

            lock (_orderStateLock)
            {
                if (_groupOrderSubmitted ||
                    _orderSubmissionInProgress ||
                    _invalidOrderRetryCount >
                        MaximumInvalidOrderRetries ||
                    UtcTime < _nextGroupOrderRetryUtc ||
                    !snapshot.IsReady ||
                    !snapshot.Groups.ContainsKey(GroupName))
                {
                    return;
                }

                _preOrderSnapshot = snapshot;
                _orderSubmissionInProgress = true;
                _terminalEventsDuringSubmission.Clear();
            }

            var ticket = _submitGroupOrder();
            var postSubmissionSnapshot = BrokerageAccountSnapshot;
            var postSubmissionUtc = UtcTime;
            string invalidOrderMessage;
            lock (_orderStateLock)
            {
                requestReconcileRefresh =
                    CompleteGroupOrderSubmissionLocked(
                        ticket,
                        postSubmissionSnapshot.Generation,
                        postSubmissionUtc,
                        out invalidOrderMessage);
            }
            ReportTerminalOrderAction(
                invalidOrderMessage,
                requestReconcileRefresh,
                postSubmissionSnapshot);
        }

        /// <summary>
        /// Requests authoritative account reconciliation after the parent group order becomes terminal.
        /// </summary>
        /// <param name="orderEvent">The aggregate parent order event</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (!LiveMode ||
                !orderEvent.Status.IsClosed())
            {
                return;
            }

            var snapshot = BrokerageAccountSnapshot;
            var now = UtcTime;
            string invalidOrderMessage;
            bool requestReconcileRefresh;
            lock (_orderStateLock)
            {
                if (_orderSubmissionInProgress &&
                    _groupOrderId == 0)
                {
                    _terminalEventsDuringSubmission[orderEvent.OrderId] =
                        new TerminalOrderEvent(orderEvent);
                    return;
                }
                if (orderEvent.OrderId != _groupOrderId ||
                    _pendingReconcileGeneration >= 0)
                {
                    return;
                }

                requestReconcileRefresh = ApplyTerminalOrderLocked(
                    new TerminalOrderEvent(orderEvent),
                    snapshot.Generation,
                    now,
                    out invalidOrderMessage);
            }
            ReportTerminalOrderAction(
                invalidOrderMessage,
                requestReconcileRefresh,
                snapshot);
        }

        private void TryRequestReconcileRefresh(BrokerageAccountSnapshot snapshot)
        {
            var now = UtcTime;
            lock (_orderStateLock)
            {
                if (_pendingReconcileGeneration < 0 ||
                    snapshot.Status ==
                        BrokerageAccountSnapshotStatus.Refreshing ||
                    now < _nextReconcileRefreshUtc)
                {
                    return;
                }

                _nextReconcileRefreshUtc =
                    now + ReconcileRetryInterval;
            }

            if (!RequestBrokerageAccountSnapshotRefresh(new[] { GroupName }))
            {
                Error(
                    $"The post-order snapshot refresh for Financial Advisor group " +
                    $"'{GroupName}' was not accepted.");
            }
        }

        private void TryRequestInitialSnapshotRefresh(
            BrokerageAccountSnapshot snapshot)
        {
            var now = UtcTime;
            lock (_orderStateLock)
            {
                if (snapshot.Status ==
                        BrokerageAccountSnapshotStatus.Refreshing ||
                    now < _nextReconcileRefreshUtc)
                {
                    return;
                }

                _nextReconcileRefreshUtc =
                    now + ReconcileRetryInterval;
            }

            var requestGeneration = snapshot.Generation;
            var accepted =
                RequestBrokerageAccountSnapshotRefresh(new[] { GroupName });
            lock (_orderStateLock)
            {
                _initialSnapshotRefreshAccepted = accepted;
                if (accepted)
                {
                    _initialSnapshotRequestGeneration =
                        requestGeneration;
                }
            }
            if (!accepted)
            {
                Error(
                    $"The initial snapshot refresh for Financial Advisor group " +
                    $"'{GroupName}' was not accepted.");
            }
        }

        private void RequestScheduledSnapshotRefresh()
        {
            var snapshot = BrokerageAccountSnapshot;
            var requestInitialRefresh = false;
            var completeRefresh = false;
            lock (_orderStateLock)
            {
                if (!LiveMode ||
                    _groupOrderId != 0 ||
                    _orderSubmissionInProgress ||
                    _pendingReconcileGeneration >= 0 ||
                    snapshot.Status ==
                        BrokerageAccountSnapshotStatus.Refreshing)
                {
                    return;
                }
                if (!_initialSnapshotRefreshAccepted)
                {
                    requestInitialRefresh = true;
                }
                else
                {
                    ++_scheduledRefreshTopologyTicks;
                    completeRefresh =
                        _scheduledRefreshTopologyTicks ==
                        CompleteRefreshTopologyTicks;
                    if (completeRefresh)
                    {
                        _scheduledRefreshTopologyTicks = 0;
                    }
                }
            }
            if (requestInitialRefresh)
            {
                TryRequestInitialSnapshotRefresh(snapshot);
                return;
            }

            var accepted = completeRefresh
                ? RequestBrokerageAccountSnapshotRefresh()
                : RequestBrokerageAccountSnapshotRefresh(
                    new[] { GroupName });
            if (!accepted)
            {
                Error(
                    completeRefresh
                        ? "The scheduled complete Financial Advisor snapshot " +
                            "refresh was not accepted."
                        : $"The scheduled topology refresh for Financial Advisor " +
                            $"group '{GroupName}' was not accepted.");
            }
        }

        private void ReconcileAccountPositions(
            BrokerageAccountSnapshot preOrderSnapshot,
            BrokerageAccountSnapshot currentSnapshot)
        {
            if (preOrderSnapshot == null ||
                !preOrderSnapshot.Groups.TryGetValue(
                    GroupName,
                    out var group))
            {
                Error(
                    $"The pre-order snapshot for Financial Advisor group " +
                    $"'{GroupName}' is unavailable.");
                return;
            }

            foreach (var accountId in group.AccountIds)
            {
                if (!preOrderSnapshot.Accounts.TryGetValue(
                        accountId,
                        out var previousAccount) ||
                    !currentSnapshot.Accounts.TryGetValue(
                        accountId,
                        out var currentAccount))
                {
                    Error(
                        $"Account state for '{accountId}' is unavailable during " +
                        $"Financial Advisor group reconciliation.");
                    continue;
                }

                var previousQuantity = GetPositionQuantity(previousAccount);
                var currentQuantity = GetPositionQuantity(currentAccount);
                Log(
                    $"FA reconciliation: account={accountId}, symbol={_symbol.Value}, " +
                    $"before={previousQuantity}, after={currentQuantity}, " +
                    $"change={currentQuantity - previousQuantity}");
            }
        }

        private bool CompleteGroupOrderSubmissionLocked(
            OrderTicket ticket,
            long snapshotGeneration,
            DateTime now,
            out string invalidOrderMessage)
        {
            invalidOrderMessage = null;
            _orderSubmissionInProgress = false;
            if (ticket == null)
            {
                _preOrderSnapshot = null;
                _groupOrderSubmitted = false;
                _terminalEventsDuringSubmission.Clear();
                return false;
            }

            _groupOrderId = ticket.OrderId;
            _groupOrderSubmitted = true;
            var ticketStatus = ticket.Status;
            _terminalEventsDuringSubmission.TryGetValue(
                ticket.OrderId,
                out var terminalOrderEvent);
            _terminalEventsDuringSubmission.Clear();
            if (terminalOrderEvent == null &&
                ticketStatus.IsClosed())
            {
                var response = ticket.SubmitRequest.Response;
                terminalOrderEvent = new TerminalOrderEvent(
                    ticket.OrderId,
                    ticketStatus,
                    now,
                    response?.ErrorMessage);
            }
            if (terminalOrderEvent == null)
            {
                _invalidOrderRetryCount = 0;
                return false;
            }

            return ApplyTerminalOrderLocked(
                terminalOrderEvent,
                snapshotGeneration,
                now,
                out invalidOrderMessage);
        }

        private bool ApplyTerminalOrderLocked(
            TerminalOrderEvent terminalOrderEvent,
            long snapshotGeneration,
            DateTime now,
            out string invalidOrderMessage)
        {
            invalidOrderMessage = null;
            _groupOrderId = 0;
            if (terminalOrderEvent.Status == OrderStatus.Invalid)
            {
                ++_invalidOrderRetryCount;
                _groupOrderSubmitted = false;
                _preOrderSnapshot = null;
                _nextGroupOrderRetryUtc =
                    _invalidOrderRetryCount <=
                        MaximumInvalidOrderRetries
                    ? now + InvalidOrderRetryInterval
                    : DateTime.MaxValue;
                var reason = string.IsNullOrWhiteSpace(
                        terminalOrderEvent.Message)
                    ? "No rejection reason was supplied."
                    : terminalOrderEvent.Message;
                invalidOrderMessage =
                    $"Financial Advisor group order " +
                    $"{terminalOrderEvent.OrderId} was rejected: {reason} " +
                    (_invalidOrderRetryCount <=
                            MaximumInvalidOrderRetries
                        ? $"Retry {_invalidOrderRetryCount} of " +
                            $"{MaximumInvalidOrderRetries} is scheduled."
                        : "The bounded retry limit was reached.");
                return false;
            }

            _invalidOrderRetryCount = 0;
            // Publish the causal timestamps before making the generation pending.
            _pendingReconcileTerminalUtc =
                terminalOrderEvent.UtcTime;
            _nextReconcileRefreshUtc = now;
            _pendingReconcileGeneration = snapshotGeneration;
            return true;
        }

        private void ReportTerminalOrderAction(
            string invalidOrderMessage,
            bool requestReconcileRefresh,
            BrokerageAccountSnapshot snapshot)
        {
            if (invalidOrderMessage != null)
            {
                Error(invalidOrderMessage);
            }
            if (requestReconcileRefresh)
            {
                TryRequestReconcileRefresh(snapshot);
            }
        }

        private decimal GetPositionQuantity(BrokerageAccountState account)
        {
            return account.Positions
                .Where(position => position.Symbol == _symbol)
                .Sum(position => position.Quantity);
        }

        private sealed class TerminalOrderEvent
        {
            public int OrderId { get; }
            public OrderStatus Status { get; }
            public DateTime UtcTime { get; }
            public string Message { get; }

            public TerminalOrderEvent(OrderEvent orderEvent)
                : this(
                    orderEvent.OrderId,
                    orderEvent.Status,
                    orderEvent.UtcTime,
                    orderEvent.Message)
            {
            }

            public TerminalOrderEvent(
                int orderId,
                OrderStatus status,
                DateTime utcTime,
                string message)
            {
                OrderId = orderId;
                Status = status;
                UtcTime = utcTime;
                Message = message;
            }
        }
    }
}
