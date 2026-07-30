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
    /// It requires ib-financial-advisors-unified-groups-enabled=true,
    /// ib-financial-advisors-group-filter to be empty, and an existing group whose saved method is Equal,
    /// NetLiq, AvailableEquity, Ratio, or Percent. ContractsOrShares instead requires updating and
    /// confirming the saved vector before submitting a parent with the exact saved total; use
    /// FinancialAdvisorGroupAssignmentAlgorithm for that mutation and confirmed-readback pattern.
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
            TimeSpan.FromSeconds(90);
        private static readonly TimeSpan MaximumSnapshotAge =
            TimeSpan.FromMinutes(5);
        private const int CompleteRefreshTopologyTicks = 3;
        private const int MaximumInvalidOrderAttempts = 3;
        private const int MaximumEmptyOrderAttempts = 3;

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
        private bool _onDataStateActive;
        private int _groupOrderId;
        private int _invalidOrderAttemptCount;
        private int _emptyOrderAttemptCount;
        private DateTime _nextGroupOrderRetryUtc;
        private long _pendingReconcileGeneration = -1;
        private DateTime _pendingReconcileTerminalUtc;
        private TerminalOrderEvent _pendingTerminalOrderEvent;
        private DateTime _nextReconcileRefreshUtc;
        private int _scheduledRefreshTopologyTicks;
        private int _scheduledRefreshIntent;

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

            // Use a unified FA Account Group. Leaving FaMethod blank uses the saved
            // allocation method; this aggregate demo expects a computed, Ratio, or
            // Percent group rather than ContractsOrShares.
            DefaultOrderProperties = new InteractiveBrokersOrderProperties
            {
                // account group created manually in IB/TWS
                FaGroup = GroupName
            };

            if (LiveMode)
            {
                // The 90-second cadence cannot remain phase-locked to minute data;
                // every third topology tick expands to complete account state.
                Schedule.On(
                    DateRules.EveryDay(),
                    TimeRules.Every(TopologyRefreshInterval),
                    RequestScheduledSnapshotRefresh);
                _nextReconcileRefreshUtc = UtcTime;
                _nextGroupOrderRetryUtc = UtcTime;
                TryRequestInitialSnapshotRefresh(
                    BrokerageAccountSnapshot);
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

            lock (_orderStateLock)
            {
                _onDataStateActive = true;
            }
            try
            {
                var snapshot = BrokerageAccountSnapshot;
                var now = UtcTime;
                string invalidOrderMessage;
                bool requestReconcileRefresh;
                lock (_orderStateLock)
                {
                    requestReconcileRefresh =
                        TryApplyTerminalOrderIntentLocked(
                            snapshot.Generation,
                            now,
                            out invalidOrderMessage);
                }
                if (invalidOrderMessage != null ||
                    requestReconcileRefresh)
                {
                    ReportTerminalOrderAction(
                        invalidOrderMessage,
                        requestReconcileRefresh,
                        snapshot);
                    return;
                }

                BrokerageAccountSnapshot preOrderSnapshot = null;
                TerminalOrderEvent reconciledTerminalOrderEvent = null;
                var reconcile = false;
                requestReconcileRefresh = false;
                string reconciliationErrorMessage = null;
                lock (_orderStateLock)
                {
                    reconcile = TryTakeReconciliationLocked(
                        snapshot,
                        now,
                        out preOrderSnapshot,
                        out reconciledTerminalOrderEvent,
                        out requestReconcileRefresh,
                        out reconciliationErrorMessage);
                }
                if (reconciliationErrorMessage != null)
                {
                    Error(reconciliationErrorMessage);
                }
                if (reconcile)
                {
                    ReconcileAccountPositions(
                        preOrderSnapshot,
                        snapshot);
                    lock (_orderStateLock)
                    {
                        invalidOrderMessage =
                            CompleteTerminalOrderReconciliationLocked(
                                reconciledTerminalOrderEvent,
                                now);
                    }
                    if (invalidOrderMessage != null)
                    {
                        Error(invalidOrderMessage);
                    }
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
                        !IsSnapshotFresh(snapshot) ||
                        snapshot.Generation <=
                            _initialSnapshotRequestGeneration;
                }
                if (requestInitialRefresh)
                {
                    TryRequestInitialSnapshotRefresh(snapshot);
                    return;
                }
                if (TryProcessScheduledSnapshotRefresh(
                        allowOnDataOwner: true))
                {
                    return;
                }
                lock (_orderStateLock)
                {
                    var submissionSnapshot =
                        BrokerageAccountSnapshot;
                    if (_groupOrderSubmitted ||
                        _orderSubmissionInProgress ||
                        _invalidOrderAttemptCount >=
                            MaximumInvalidOrderAttempts ||
                        _emptyOrderAttemptCount >=
                            MaximumEmptyOrderAttempts ||
                        UtcTime < _nextGroupOrderRetryUtc ||
                        !IsSnapshotFresh(submissionSnapshot) ||
                        !submissionSnapshot.Groups.ContainsKey(
                            GroupName))
                    {
                        return;
                    }

                    _preOrderSnapshot = submissionSnapshot;
                    _orderSubmissionInProgress = true;
                    _terminalEventsDuringSubmission.Clear();
                }

                var ticket = _submitGroupOrder();
                var postSubmissionSnapshot = BrokerageAccountSnapshot;
                var postSubmissionUtc = UtcTime;
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
            finally
            {
                lock (_orderStateLock)
                {
                    _onDataStateActive = false;
                }
            }
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

            string invalidOrderMessage = null;
            var reconcileRefreshRejected = false;
            lock (_orderStateLock)
            {
                var terminalOrderEvent =
                    new TerminalOrderEvent(orderEvent);
                if (_orderSubmissionInProgress)
                {
                    _terminalEventsDuringSubmission[orderEvent.OrderId] =
                        terminalOrderEvent;
                }
                else if (orderEvent.OrderId == _groupOrderId &&
                    _pendingReconcileGeneration < 0)
                {
                    var snapshot =
                        BrokerageAccountSnapshot;
                    var requestReconcileRefresh =
                        ApplyTerminalOrderLocked(
                            terminalOrderEvent,
                            snapshot.Generation,
                            UtcTime,
                            out invalidOrderMessage);
                    if (requestReconcileRefresh &&
                        TryRequestReconcileRefreshLocked(
                            snapshot,
                            out var accepted))
                    {
                        reconcileRefreshRejected = !accepted;
                    }
                }
            }
            if (invalidOrderMessage != null)
            {
                Error(invalidOrderMessage);
            }
            if (reconcileRefreshRejected)
            {
                ReportReconcileRefreshRejection();
            }
        }

        private void TryRequestReconcileRefresh(BrokerageAccountSnapshot snapshot)
        {
            var attempted = false;
            var accepted = true;
            lock (_orderStateLock)
            {
                attempted = TryRequestReconcileRefreshLocked(
                    snapshot,
                    out accepted);
            }
            if (attempted && !accepted)
            {
                ReportReconcileRefreshRejection();
            }
        }

        private bool TryRequestReconcileRefreshLocked(
            BrokerageAccountSnapshot snapshot,
            out bool accepted)
        {
            accepted = true;
            var now = UtcTime;
            if (_pendingReconcileGeneration < 0 ||
                snapshot.Status ==
                    BrokerageAccountSnapshotStatus.Refreshing ||
                now < _nextReconcileRefreshUtc)
            {
                return false;
            }

            _nextReconcileRefreshUtc =
                now + ReconcileRetryInterval;
            // The IB implementation only coalesces into its in-memory
            // QueueRefresh while this sample state is protected.
            accepted = RequestBrokerageAccountSnapshotRefresh(
                new[] { GroupName },
                GetReconciliationAccountIdsLocked());
            return true;
        }

        private IReadOnlyCollection<string> GetReconciliationAccountIdsLocked()
        {
            if (_preOrderSnapshot != null &&
                _preOrderSnapshot.Groups.TryGetValue(
                    GroupName,
                    out var group))
            {
                return group.AccountIds;
            }

            return Array.Empty<string>();
        }

        private void TryRequestInitialSnapshotRefresh(
            BrokerageAccountSnapshot snapshot)
        {
            var attempted = false;
            var accepted = true;
            lock (_orderStateLock)
            {
                attempted = TryRequestInitialSnapshotRefreshLocked(
                    snapshot,
                    out accepted);
            }
            if (attempted && !accepted)
            {
                Error(
                    $"The initial snapshot refresh for Financial Advisor group " +
                    $"'{GroupName}' was not accepted.");
            }
        }

        private bool TryRequestInitialSnapshotRefreshLocked(
            BrokerageAccountSnapshot snapshot,
            out bool accepted)
        {
            accepted = true;
            var now = UtcTime;
            if (snapshot.Status ==
                    BrokerageAccountSnapshotStatus.Refreshing ||
                now < _nextReconcileRefreshUtc)
            {
                return false;
            }

            _nextReconcileRefreshUtc =
                now + ReconcileRetryInterval;
            // The IB implementation only coalesces into its in-memory
            // QueueRefresh while this sample state is protected.
            accepted = RequestBrokerageAccountSnapshotRefresh(
                new[] { GroupName });
            _initialSnapshotRefreshAccepted = accepted;
            if (accepted)
            {
                _initialSnapshotRequestGeneration =
                    snapshot.Generation;
            }
            return true;
        }

        private void RequestScheduledSnapshotRefresh()
        {
            Interlocked.Exchange(
                ref _scheduledRefreshIntent,
                1);
            if (!TryProcessSnapshotRequestPriority())
            {
                TryProcessScheduledSnapshotRefresh();
            }
        }

        private bool TryProcessScheduledSnapshotRefresh(
            bool allowOnDataOwner = false)
        {
            bool completeRefresh;
            bool accepted;
            lock (_orderStateLock)
            {
                if ((_onDataStateActive &&
                        !allowOnDataOwner) ||
                    _groupOrderId != 0 ||
                    _orderSubmissionInProgress ||
                    _pendingReconcileGeneration >= 0 ||
                    !_initialSnapshotRefreshAccepted)
                {
                    return false;
                }
                var snapshot =
                    BrokerageAccountSnapshot;
                if (snapshot.Status ==
                    BrokerageAccountSnapshotStatus.Refreshing)
                {
                    return false;
                }
                if (snapshot.Generation <=
                    _initialSnapshotRequestGeneration)
                {
                    return false;
                }
                if (Interlocked.Exchange(
                        ref _scheduledRefreshIntent,
                        0) == 0)
                {
                    return false;
                }

                ++_scheduledRefreshTopologyTicks;
                completeRefresh =
                    _scheduledRefreshTopologyTicks ==
                    CompleteRefreshTopologyTicks;
                if (completeRefresh)
                {
                    _scheduledRefreshTopologyTicks = 0;
                }
                // The IB implementation only coalesces into its in-memory
                // QueueRefresh while this sample state is protected.
                accepted = completeRefresh
                    ? RequestBrokerageAccountSnapshotRefresh()
                    : RequestBrokerageAccountSnapshotRefresh(
                        new[] { GroupName });
            }
            if (!accepted)
            {
                Error(
                    completeRefresh
                        ? "The scheduled complete Financial Advisor snapshot " +
                            "refresh was not accepted."
                        : $"The scheduled topology refresh for Financial Advisor " +
                            $"group '{GroupName}' was not accepted.");
            }
            return true;
        }

        private bool TryProcessSnapshotRequestPriority()
        {
            BrokerageAccountSnapshot snapshot;
            BrokerageAccountSnapshot preOrderSnapshot = null;
            TerminalOrderEvent reconciledTerminalOrderEvent = null;
            var reconcile = false;
            var initialRefreshRejected = false;
            var reconcileRefreshRejected = false;
            string terminalOrderMessage = null;
            string reconciliationErrorMessage = null;
            lock (_orderStateLock)
            {
                if (_onDataStateActive)
                {
                    return true;
                }

                snapshot = BrokerageAccountSnapshot;
                if (_pendingReconcileGeneration >= 0)
                {
                    reconcile = TryTakeReconciliationLocked(
                        snapshot,
                        UtcTime,
                        out preOrderSnapshot,
                        out reconciledTerminalOrderEvent,
                        out var requestReconcileRefresh,
                        out reconciliationErrorMessage);
                    if (requestReconcileRefresh &&
                        TryRequestReconcileRefreshLocked(
                            snapshot,
                            out var accepted))
                    {
                        reconcileRefreshRejected = !accepted;
                    }
                }
                else if (!_initialSnapshotRefreshAccepted ||
                    !IsSnapshotFresh(snapshot) ||
                    snapshot.Generation <=
                        _initialSnapshotRequestGeneration)
                {
                    if (TryRequestInitialSnapshotRefreshLocked(
                            snapshot,
                            out var accepted))
                    {
                        initialRefreshRejected = !accepted;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (reconcile)
            {
                ReconcileAccountPositions(
                    preOrderSnapshot,
                    snapshot);
                lock (_orderStateLock)
                {
                    terminalOrderMessage =
                        CompleteTerminalOrderReconciliationLocked(
                            reconciledTerminalOrderEvent,
                            UtcTime);
                }
            }
            if (reconciliationErrorMessage != null)
            {
                Error(reconciliationErrorMessage);
            }
            if (initialRefreshRejected)
            {
                Error(
                    $"The initial snapshot refresh for Financial Advisor group " +
                    $"'{GroupName}' was not accepted.");
            }
            if (reconcileRefreshRejected)
            {
                ReportReconcileRefreshRejection();
            }
            if (terminalOrderMessage != null)
            {
                Error(terminalOrderMessage);
            }
            return true;
        }

        private bool TryTakeReconciliationLocked(
            BrokerageAccountSnapshot snapshot,
            DateTime now,
            out BrokerageAccountSnapshot preOrderSnapshot,
            out TerminalOrderEvent terminalOrderEvent,
            out bool requestRefresh,
            out string errorMessage)
        {
            preOrderSnapshot = null;
            terminalOrderEvent = null;
            requestRefresh = false;
            errorMessage = null;
            if (_pendingReconcileGeneration < 0)
            {
                return false;
            }

            requestRefresh = true;
            if (!IsSnapshotFresh(snapshot) ||
                snapshot.Generation <= _pendingReconcileGeneration ||
                snapshot.CollectionStartedUtc < _pendingReconcileTerminalUtc)
            {
                return false;
            }

            if (!HasCompleteReconciliationAccountState(
                    _preOrderSnapshot,
                    snapshot,
                    out errorMessage))
            {
                // Require another publication rather than reconsidering this
                // incomplete generation on every state-machine callback.
                _pendingReconcileGeneration = snapshot.Generation;
                _nextReconcileRefreshUtc = now;
                return false;
            }

            preOrderSnapshot = _preOrderSnapshot;
            _preOrderSnapshot = null;
            terminalOrderEvent = _pendingTerminalOrderEvent;
            _pendingTerminalOrderEvent = null;
            _pendingReconcileGeneration = -1;
            requestRefresh = false;
            return true;
        }

        private static bool HasCompleteReconciliationAccountState(
            BrokerageAccountSnapshot preOrderSnapshot,
            BrokerageAccountSnapshot currentSnapshot,
            out string errorMessage)
        {
            if (preOrderSnapshot == null ||
                !preOrderSnapshot.Groups.TryGetValue(
                    GroupName,
                    out var group))
            {
                errorMessage =
                    $"The pre-order snapshot for Financial Advisor group " +
                    $"'{GroupName}' is unavailable; reconciliation remains pending.";
                return false;
            }

            foreach (var accountId in group.AccountIds)
            {
                if (!preOrderSnapshot.Accounts.ContainsKey(accountId) ||
                    !currentSnapshot.Accounts.ContainsKey(accountId))
                {
                    errorMessage =
                        $"Account state for '{accountId}' is unavailable during " +
                        "Financial Advisor group reconciliation; reconciliation " +
                        "remains pending until a strictly newer snapshot contains " +
                        "every original group member.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        private void ReportReconcileRefreshRejection()
        {
            Error(
                $"The post-order snapshot refresh for Financial Advisor group " +
                $"'{GroupName}' was not accepted.");
        }

        private void ReconcileAccountPositions(
            BrokerageAccountSnapshot preOrderSnapshot,
            BrokerageAccountSnapshot currentSnapshot)
        {
            var group = preOrderSnapshot.Groups[GroupName];

            foreach (var accountId in group.AccountIds)
            {
                var previousAccount = preOrderSnapshot.Accounts[accountId];
                var currentAccount = currentSnapshot.Accounts[accountId];
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
                ++_emptyOrderAttemptCount;
                _nextGroupOrderRetryUtc =
                    _emptyOrderAttemptCount <
                        MaximumEmptyOrderAttempts
                    ? now + InvalidOrderRetryInterval
                    : DateTime.MaxValue;
                if (_emptyOrderAttemptCount >=
                    MaximumEmptyOrderAttempts)
                {
                    invalidOrderMessage =
                        $"SetHoldings returned no Financial Advisor parent " +
                        $"order ticket on {MaximumEmptyOrderAttempts} attempts; " +
                        "the bounded retry limit was reached.";
                }
                return false;
            }

            _emptyOrderAttemptCount = 0;
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
                _invalidOrderAttemptCount = 0;
                return false;
            }

            return ApplyTerminalOrderLocked(
                terminalOrderEvent,
                snapshotGeneration,
                now,
                out invalidOrderMessage);
        }

        private bool TryApplyTerminalOrderIntentLocked(
            long snapshotGeneration,
            DateTime now,
            out string invalidOrderMessage)
        {
            invalidOrderMessage = null;
            if (_orderSubmissionInProgress ||
                _groupOrderId == 0 ||
                _pendingReconcileGeneration >= 0 ||
                !_terminalEventsDuringSubmission.TryGetValue(
                    _groupOrderId,
                    out var terminalOrderEvent))
            {
                return false;
            }

            _terminalEventsDuringSubmission.Remove(_groupOrderId);
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
                var reason = string.IsNullOrWhiteSpace(
                        terminalOrderEvent.Message)
                    ? "No rejection reason was supplied."
                    : terminalOrderEvent.Message;
                invalidOrderMessage =
                    $"Financial Advisor group order " +
                    $"{terminalOrderEvent.OrderId} was rejected: {reason} " +
                    "A newer account snapshot will be reconciled before " +
                    "retry eligibility is decided.";
            }

            // Publish the causal timestamps before making the generation pending.
            _pendingReconcileTerminalUtc =
                terminalOrderEvent.UtcTime;
            _pendingTerminalOrderEvent = terminalOrderEvent;
            _nextReconcileRefreshUtc = now;
            _pendingReconcileGeneration = snapshotGeneration;
            return true;
        }

        private string CompleteTerminalOrderReconciliationLocked(
            TerminalOrderEvent terminalOrderEvent,
            DateTime now)
        {
            if (terminalOrderEvent == null ||
                terminalOrderEvent.Status != OrderStatus.Invalid)
            {
                _invalidOrderAttemptCount = 0;
                return null;
            }

            ++_invalidOrderAttemptCount;
            _groupOrderSubmitted = false;
            _nextGroupOrderRetryUtc =
                _invalidOrderAttemptCount <
                    MaximumInvalidOrderAttempts
                ? now + InvalidOrderRetryInterval
                : DateTime.MaxValue;
            return _invalidOrderAttemptCount <
                    MaximumInvalidOrderAttempts
                ? $"Financial Advisor group order " +
                    $"{terminalOrderEvent.OrderId} was reconciled after " +
                    $"rejection. Attempt {_invalidOrderAttemptCount} of " +
                    $"{MaximumInvalidOrderAttempts} was rejected; the next " +
                    "attempt is scheduled."
                : $"Financial Advisor group order " +
                    $"{terminalOrderEvent.OrderId} was reconciled after " +
                    "rejection. The bounded retry limit was reached.";
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

        private bool IsSnapshotFresh(
            BrokerageAccountSnapshot snapshot)
        {
            if (!snapshot.IsReady)
            {
                return false;
            }

            var financialDataAsOfUtc =
                snapshot.CollectionStartedUtc == default
                    ? snapshot.LastSuccessfulUpdateUtc
                    : snapshot.CollectionStartedUtc;
            return financialDataAsOfUtc >=
                UtcTime - MaximumSnapshotAge;
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
