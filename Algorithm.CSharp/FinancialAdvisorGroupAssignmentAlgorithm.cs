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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using QuantConnect.Brokerages;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstrates alias-driven movement of managed accounts between existing Financial Advisor groups.
    ///
    /// Live prerequisites: ib-financial-advisors-group-filter must be empty, and both
    /// ib-financial-advisors-group-management-enabled=true and
    /// ib-financial-advisors-unified-groups-enabled=true are required.
    /// The fa-alias-pattern parameter is a case-insensitive regular expression. An empty
    /// fa-target-group value requests removal of matching accounts from every group, but this
    /// sample will not remove the final member from a source group.
    /// ContractsOrShares child values may be fractional, but their saved total must be lot-aligned;
    /// for a lot size of one, 12.5 + 7.5 = 20 is valid.
    /// Percent requires 100 for the first member of an empty group and a value strictly between
    /// zero and 100 when adding to a group that already has members.
    /// Do not request configuration mutations during OnEndOfAlgorithm or teardown: a request may be
    /// accepted but is not guaranteed to reach the broker or publish a result.
    /// </summary>
    /// <meta name="tag" content="financial advisor" />
    /// <meta name="tag" content="live trading" />
    public class FinancialAdvisorGroupAssignmentAlgorithm : QCAlgorithm
    {
        private static readonly TimeSpan RefreshRetryInterval =
            TimeSpan.FromSeconds(5);
        private static readonly TimeSpan TopologyRefreshInterval =
            TimeSpan.FromSeconds(90);
        private static readonly TimeSpan MaximumSnapshotAge =
            TimeSpan.FromMinutes(5);
        private const int CompleteRefreshTopologyTicks = 3;

        // The IB provider queues asynchronous refreshes without socket I/O, allowing request
        // acceptance and sample state to be updated under this lock.
        private readonly object _orderStateLock = new();
        private Regex _aliasPattern;
        private string _targetGroupName;
        private decimal _targetAllocationValue;
        private decimal _cashChangeThreshold;
        private DateTime _nextRefreshRetryUtc;
        private bool _initialSnapshotRefreshAccepted;
        private long _initialSnapshotRequestGeneration = -1;
        private bool _refreshRequestOutstanding;
        private long _refreshRequestedAfterGeneration = -1;
        private BrokerageAccountSnapshot _cashBaselineSnapshot;
        private bool _cashConfirmationRequired;
        private long _cashChangeGeneration = -1;
        private long _lastEvaluatedGeneration = -1;
        private bool _assignmentRequestAccepted;
        private bool _stateMachineActive;
        private long _assignmentGenerationBeforeRequest = -1;
        private long _pendingAssignmentGeneration = -1;
        private long _assignmentSnapshotGeneration = -1;
        private long _minimumReadyGeneration = -1;
        private string _pendingAccountId = string.Empty;
        private string _lastFinalSourceMemberBlockKey;
        private string _lastUnsupportedAllocationMethodKey = string.Empty;
        private int _scheduledRefreshTopologyTicks;
        private int _scheduledRefreshIntent;

        /// <summary>
        /// Configures the alias rule, destination, allocation value, cash/net-liquidation change
        /// threshold, and algorithm-owned snapshot refresh policy.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(100000);
            AddEquity("SPY", Resolution.Minute);

            _aliasPattern = new Regex(
                GetParameter("fa-alias-pattern", "^MOVE-"),
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _targetGroupName = GetParameter("fa-target-group", "TargetGroup");
            _targetAllocationValue =
                GetDecimalParameter("fa-allocation-value", "1");
            _cashChangeThreshold =
                GetDecimalParameter("fa-cash-change-threshold", "1000");

            if (_cashChangeThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "fa-cash-change-threshold",
                    "The cash-change threshold cannot be negative.");
            }

            _nextRefreshRetryUtc = UtcTime;
            if (LiveMode)
            {
                // Scoped group refreshes run every 90 seconds; every third tick expands to a
                // complete-discovery request.
                Schedule.On(
                    DateRules.EveryDay(),
                    TimeRules.Every(TopologyRefreshInterval),
                    RequestScheduledSnapshotRefresh);
                TryRequestSnapshotRefresh(
                    BrokerageAccountSnapshot,
                    isInitialRequest: true);
            }
        }

        private decimal GetDecimalParameter(
            string name,
            string defaultValue)
        {
            var value = GetParameter(name, defaultValue);
            if (!decimal.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var result))
            {
                throw new ArgumentException(
                    $"Algorithm parameter '{name}' must be a decimal number.",
                    name);
            }

            return result;
        }

        /// <summary>
        /// Polls asynchronous mutations, evaluates each authoritative snapshot generation once,
        /// and processes scheduled refreshes that discover external cash and net-liquidation changes.
        /// </summary>
        /// <param name="slice">The current data slice.</param>
        public override void OnData(Slice slice)
        {
            if (!LiveMode)
            {
                return;
            }

            if (!TryEnterStateMachine())
            {
                return;
            }
            try
            {
                if (!TryAdvanceStateMachine())
                {
                    TryProcessScheduledSnapshotRefresh();
                }
            }
            finally
            {
                ExitStateMachine();
            }
        }

        private bool TryAdvanceStateMachine()
        {
            var snapshot = BrokerageAccountSnapshot;
            UpdateRefreshRequestState(snapshot);
            if (PollAssignment())
            {
                return true;
            }

            if (!_initialSnapshotRefreshAccepted ||
                snapshot.Generation <= _initialSnapshotRequestGeneration)
            {
                TryRequestSnapshotRefresh(
                    snapshot,
                    isInitialRequest: true);
                return true;
            }

            if (_minimumReadyGeneration >= 0)
            {
                if (!IsSnapshotFresh(snapshot) ||
                    snapshot.Generation <= _minimumReadyGeneration)
                {
                    TryRequestSnapshotRefresh(snapshot);
                    return true;
                }
                _minimumReadyGeneration = -1;
            }

            if (_cashConfirmationRequired)
            {
                if (!IsSnapshotFresh(snapshot) ||
                    !snapshot.IsComplete ||
                    snapshot.Generation <= _cashChangeGeneration)
                {
                    TryRequestSnapshotRefresh(snapshot);
                    return true;
                }

                _cashConfirmationRequired = false;
                _cashBaselineSnapshot = snapshot;
                Log(
                    $"FA cash-change confirmation is Ready at generation " +
                    $"{snapshot.Generation}; reevaluating group membership.");
            }

            if (_refreshRequestOutstanding ||
                !IsSnapshotFresh(snapshot))
            {
                TryRequestSnapshotRefresh(snapshot);
                return true;
            }

            if (Interlocked.CompareExchange(
                    ref _scheduledRefreshIntent,
                    0,
                    0) != 0)
            {
                TryProcessScheduledSnapshotRefresh();
                return true;
            }

            if (snapshot.Generation == _lastEvaluatedGeneration)
            {
                return false;
            }

            if (snapshot.IsComplete &&
                _cashBaselineSnapshot != null &&
                snapshot.Generation > _cashBaselineSnapshot.Generation &&
                HasMaterialFinancialChange(
                    _cashBaselineSnapshot,
                    snapshot,
                    out var changeDescription))
            {
                _cashBaselineSnapshot = snapshot;
                _cashChangeGeneration = snapshot.Generation;
                _cashConfirmationRequired = true;
                _lastEvaluatedGeneration = snapshot.Generation;
                Log(
                    $"FA financial change detected at generation " +
                    $"{snapshot.Generation}: {changeDescription}. Requesting confirmation.");
                TryRequestSnapshotRefresh(snapshot);
                return true;
            }

            if (snapshot.IsComplete &&
                (_cashBaselineSnapshot == null ||
                    snapshot.Generation > _cashBaselineSnapshot.Generation))
            {
                _cashBaselineSnapshot = snapshot;
            }

            return EvaluateGroupAssignment(snapshot);
        }

        private bool TryEnterStateMachine()
        {
            lock (_orderStateLock)
            {
                if (_stateMachineActive)
                {
                    return false;
                }
                _stateMachineActive = true;
                return true;
            }
        }

        private void ExitStateMachine()
        {
            lock (_orderStateLock)
            {
                _stateMachineActive = false;
            }
        }

        private bool PollAssignment()
        {
            if (!_assignmentRequestAccepted)
            {
                return false;
            }

            var assignment = BrokerageAccountGroupAssignment;
            if (!string.Equals(
                    assignment.AccountId,
                    _pendingAccountId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (_pendingAssignmentGeneration < 0)
            {
                if (assignment.Status ==
                        BrokerageAccountGroupAssignmentStatus.Unavailable ||
                    assignment.Generation <=
                        _assignmentGenerationBeforeRequest)
                {
                    return true;
                }
                _pendingAssignmentGeneration = assignment.Generation;
            }

            if (assignment.Generation != _pendingAssignmentGeneration ||
                !assignment.IsCompleted)
            {
                return true;
            }

            if (assignment.Status ==
                BrokerageAccountGroupAssignmentStatus.Succeeded)
            {
                Log(
                    $"FA assignment succeeded: account={assignment.AccountId}, " +
                    $"target='{assignment.TargetGroupName}', resulting groups=[" +
                    $"{string.Join(", ", assignment.ResultingGroupNames)}], " +
                    $"assignment generation={assignment.Generation}.");
                _minimumReadyGeneration = _assignmentSnapshotGeneration;
            }
            else
            {
                Error(
                    $"FA assignment failed: account={_pendingAccountId}, " +
                    $"assignment generation={assignment.Generation}, " +
                    $"error={assignment.ErrorMessage}");
                _minimumReadyGeneration = Math.Max(
                    _minimumReadyGeneration,
                    _assignmentSnapshotGeneration);
            }

            _lastEvaluatedGeneration = Math.Max(
                _lastEvaluatedGeneration,
                _assignmentSnapshotGeneration);
            _assignmentRequestAccepted = false;
            _pendingAssignmentGeneration = -1;
            _pendingAccountId = string.Empty;
            return true;
        }

        private bool EvaluateGroupAssignment(
            BrokerageAccountSnapshot snapshot)
        {
            BrokerageAccountGroup destinationGroup = null;
            if (_targetGroupName.Length != 0)
            {
                destinationGroup = snapshot.AllGroups.Values.FirstOrDefault(
                    group => group.Name.Equals(
                        _targetGroupName,
                        StringComparison.OrdinalIgnoreCase));
                if (destinationGroup == null)
                {
                    Error(
                        $"FA destination group '{_targetGroupName}' is not " +
                        $"present in Ready snapshot generation {snapshot.Generation}.");
                    _lastEvaluatedGeneration = snapshot.Generation;
                    return true;
                }
            }

            var candidates = snapshot.AccountDirectory.Values
                .Where(IsAliasMatch)
                .Where(entry => destinationGroup == null
                    ? entry.GroupNames.Count != 0
                    : entry.GroupNames.Count != 1 ||
                        !entry.GroupNames[0].Equals(
                            destinationGroup.Name,
                            StringComparison.OrdinalIgnoreCase))
                .OrderBy(
                    entry => entry.AccountId,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();
            _lastEvaluatedGeneration = snapshot.Generation;
            if (candidates.Length == 0)
            {
                return false;
            }

            BrokerageAccountDirectoryEntry candidate = null;
            BrokerageAccountDirectoryEntry firstBlockedCandidate = null;
            BrokerageAccountGroup firstFinalSourceGroup = null;
            List<string> requiredGroupNames = null;
            foreach (var currentCandidate in candidates)
            {
                var currentRequiredGroupNames = new List<string>();
                if (destinationGroup != null)
                {
                    currentRequiredGroupNames.Add(destinationGroup.Name);
                }
                foreach (var sourceGroupName in currentCandidate.GroupNames)
                {
                    if (!currentRequiredGroupNames.Contains(
                            sourceGroupName,
                            StringComparer.OrdinalIgnoreCase))
                    {
                        currentRequiredGroupNames.Add(sourceGroupName);
                    }
                }
                var finalSourceGroup = currentCandidate.GroupNames
                    .Where(groupName => destinationGroup == null ||
                        !groupName.Equals(
                            destinationGroup.Name,
                            StringComparison.OrdinalIgnoreCase))
                    .Where(snapshot.AllGroups.ContainsKey)
                    .Select(groupName => snapshot.AllGroups[groupName])
                    .FirstOrDefault(group =>
                        group.AccountIds.Count == 1 &&
                        group.AccountIds[0].Equals(
                            currentCandidate.AccountId,
                            StringComparison.OrdinalIgnoreCase));
                if (finalSourceGroup != null)
                {
                    firstBlockedCandidate ??= currentCandidate;
                    firstFinalSourceGroup ??= finalSourceGroup;
                    continue;
                }
                if (currentRequiredGroupNames.Any(
                        groupName => !snapshot.Groups.ContainsKey(groupName)))
                {
                    _minimumReadyGeneration = snapshot.Generation;
                    TryRequestSnapshotRefresh(
                        snapshot,
                        currentRequiredGroupNames);
                    return true;
                }

                candidate = currentCandidate;
                requiredGroupNames = currentRequiredGroupNames;
                break;
            }
            if (candidate == null)
            {
                var blockKey = snapshot.GroupConfigurationVersion;
                if (!blockKey.Equals(
                        _lastFinalSourceMemberBlockKey,
                        StringComparison.Ordinal))
                {
                    _lastFinalSourceMemberBlockKey = blockKey;
                    Error(
                        $"FA assignment for account '{firstBlockedCandidate.AccountId}' cannot " +
                        $"remove the final member of source group " +
                        $"'{firstFinalSourceGroup.Name}'. Waiting for a scheduled topology " +
                        "refresh before reevaluating.");
                }
                return true;
            }
            _lastFinalSourceMemberBlockKey = null;

            var requiresAllocationValue = destinationGroup != null &&
                !destinationGroup.AccountIds.Contains(
                    candidate.AccountId,
                    StringComparer.OrdinalIgnoreCase);
            decimal? allocationValue = null;
            if (destinationGroup != null &&
                !TryGetAllocationValue(
                    destinationGroup,
                    snapshot.GroupConfigurationVersion,
                    requiresAllocationValue,
                    out allocationValue))
            {
                return true;
            }

            var assignmentBeforeRequest =
                BrokerageAccountGroupAssignment;
            var canonicalTarget = destinationGroup?.Name ?? string.Empty;
            var accepted = false;
            Exception synchronousRejection = null;
            try
            {
                accepted = RequestBrokerageAccountGroupAssignment(
                    candidate.AccountId,
                    canonicalTarget,
                    allocationValue,
                    snapshot);
            }
            catch (Exception exception)
            {
                synchronousRejection = exception;
            }
            finally
            {
                if (accepted)
                {
                    _assignmentRequestAccepted = true;
                    _assignmentGenerationBeforeRequest =
                        assignmentBeforeRequest.Generation;
                    _assignmentSnapshotGeneration =
                        snapshot.Generation;
                    _pendingAssignmentGeneration = -1;
                    _pendingAccountId = candidate.AccountId;
                }
            }
            if (synchronousRejection != null)
            {
                Error(
                    $"FA assignment was rejected synchronously: " +
                    $"account={candidate.AccountId}, target='{canonicalTarget}', " +
                    $"snapshot generation={snapshot.Generation}, " +
                    $"error={synchronousRejection.Message}");
                _minimumReadyGeneration = snapshot.Generation;
                TryRequestSnapshotRefresh(
                    snapshot,
                    requiredGroupNames);
                return true;
            }
            if (!accepted)
            {
                Error(
                    $"FA assignment was not accepted: account={candidate.AccountId}, " +
                    $"target='{canonicalTarget}', snapshot generation=" +
                    $"{snapshot.Generation}.");
                _minimumReadyGeneration = snapshot.Generation;
                TryRequestSnapshotRefresh(
                    snapshot,
                    requiredGroupNames);
                return true;
            }

            PollAssignment();
            return true;
        }

        private bool IsAliasMatch(
            BrokerageAccountDirectoryEntry entry)
        {
            return entry.Relationship ==
                    BrokerageAccountRelationship.Managed &&
                !string.IsNullOrWhiteSpace(entry.AccountAlias) &&
                _aliasPattern.IsMatch(entry.AccountAlias);
        }

        private bool TryGetAllocationValue(
            BrokerageAccountGroup destinationGroup,
            string groupConfigurationVersion,
            bool required,
            out decimal? allocationValue)
        {
            allocationValue = null;
            if (new[] { "Equal", "NetLiq", "AvailableEquity" }.Contains(
                    destinationGroup.AllocationMethod,
                    StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            if (destinationGroup.AllocationMethod.Equals(
                    "ContractsOrShares",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!required)
                {
                    return true;
                }
                if (_targetAllocationValue < 0)
                {
                    Error(
                        $"FA destination group '{destinationGroup.Name}' uses " +
                        "ContractsOrShares, so fa-allocation-value cannot be negative.");
                    return false;
                }

                allocationValue = _targetAllocationValue;
                return true;
            }
            if (destinationGroup.AllocationMethod.Equals(
                    "Ratio",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!required)
                {
                    return true;
                }
                if (_targetAllocationValue <= 0)
                {
                    Error(
                        $"FA destination group '{destinationGroup.Name}' uses " +
                        $"{destinationGroup.AllocationMethod}, so " +
                        "fa-allocation-value must be positive.");
                    return false;
                }

                allocationValue = _targetAllocationValue;
                return true;
            }
            if (destinationGroup.AllocationMethod.Equals(
                    "Percent",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!required)
                {
                    return true;
                }
                var targetGroupIsEmpty =
                    destinationGroup.AccountIds.Count == 0;
                if (targetGroupIsEmpty
                    ? _targetAllocationValue != 100
                    : _targetAllocationValue <= 0 ||
                        _targetAllocationValue >= 100)
                {
                    Error(
                        $"FA destination group '{destinationGroup.Name}' uses " +
                        (targetGroupIsEmpty
                            ? "Percent, so fa-allocation-value must be exactly 100 " +
                                "when the group has no members."
                            : "Percent, so fa-allocation-value must be greater than " +
                                "zero and less than 100 when the group already has members."));
                    return false;
                }

                allocationValue = _targetAllocationValue;
                return true;
            }

            var unsupportedMethodKey =
                $"{destinationGroup.Name}\0{destinationGroup.AllocationMethod}\0" +
                groupConfigurationVersion;
            if (!unsupportedMethodKey.Equals(
                    _lastUnsupportedAllocationMethodKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                _lastUnsupportedAllocationMethodKey = unsupportedMethodKey;
                Error(destinationGroup.AllocationMethod.Equals(
                        "PctChange",
                        StringComparison.OrdinalIgnoreCase)
                    ? $"FA destination group '{destinationGroup.Name}' uses saved " +
                        "PctChange. IB paper TWS accepts that configuration, but " +
                        "this LEAN sample does not mutate PctChange groups."
                    : $"FA destination group '{destinationGroup.Name}' uses " +
                        $"unsupported saved allocation method " +
                        $"'{destinationGroup.AllocationMethod}'.");
            }
            return false;
        }

        private bool HasMaterialFinancialChange(
            BrokerageAccountSnapshot previous,
            BrokerageAccountSnapshot current,
            out string description)
        {
            foreach (var directoryEntry in current.AccountDirectory.Values
                .Where(IsAliasMatch))
            {
                if (!previous.Accounts.TryGetValue(
                        directoryEntry.AccountId,
                        out var previousAccount) ||
                    !current.Accounts.TryGetValue(
                        directoryEntry.AccountId,
                        out var currentAccount))
                {
                    continue;
                }

                if (ExceedsThreshold(
                        previousAccount.TotalCashValue,
                        currentAccount.TotalCashValue,
                        out var cashChange))
                {
                    description =
                        $"account={directoryEntry.AccountId}, " +
                        $"TotalCashValue change={cashChange}";
                    return true;
                }
                if (ExceedsThreshold(
                        previousAccount.NetLiquidation,
                        currentAccount.NetLiquidation,
                        out var netLiquidationChange))
                {
                    description =
                        $"account={directoryEntry.AccountId}, " +
                        $"NetLiquidation change={netLiquidationChange}";
                    return true;
                }
            }

            description = string.Empty;
            return false;
        }

        private bool ExceedsThreshold(
            decimal? previous,
            decimal? current,
            out decimal change)
        {
            change = 0;
            if (!previous.HasValue || !current.HasValue)
            {
                return false;
            }

            change = current.Value - previous.Value;
            return Math.Abs(change) > _cashChangeThreshold;
        }

        private void UpdateRefreshRequestState(
            BrokerageAccountSnapshot snapshot)
        {
            lock (_orderStateLock)
            {
                UpdateRefreshRequestStateLocked(snapshot);
            }
        }

        private void UpdateRefreshRequestStateLocked(
            BrokerageAccountSnapshot snapshot)
        {
            if (!_refreshRequestOutstanding)
            {
                return;
            }

            if (snapshot.IsReady &&
                snapshot.Generation > _refreshRequestedAfterGeneration)
            {
                _refreshRequestOutstanding = false;
            }
            else if (snapshot.Status ==
                    BrokerageAccountSnapshotStatus.Failed ||
                snapshot.Status ==
                    BrokerageAccountSnapshotStatus.Stale ||
                snapshot.Status ==
                    BrokerageAccountSnapshotStatus.Unavailable)
            {
                _refreshRequestOutstanding = false;
            }
        }

        private void TryRequestSnapshotRefresh(
            BrokerageAccountSnapshot snapshot,
            IReadOnlyCollection<string> groupNames = null,
            bool isInitialRequest = false)
        {
            string errorMessage;
            lock (_orderStateLock)
            {
                errorMessage = TryRequestSnapshotRefreshLocked(
                    snapshot,
                    groupNames,
                    isInitialRequest);
            }
            if (errorMessage != null)
            {
                Error(errorMessage);
            }
        }

        private string TryRequestSnapshotRefreshLocked(
            BrokerageAccountSnapshot snapshot,
            IReadOnlyCollection<string> groupNames,
            bool isInitialRequest = false)
        {
            if (_refreshRequestOutstanding ||
                snapshot.Status ==
                    BrokerageAccountSnapshotStatus.Refreshing ||
                UtcTime < _nextRefreshRetryUtc)
            {
                return null;
            }

            _nextRefreshRetryUtc =
                UtcTime + RefreshRetryInterval;
            var scheduledRefreshIntent = Interlocked.Exchange(
                ref _scheduledRefreshIntent,
                0);
            var accepted = groupNames == null
                ? RequestBrokerageAccountSnapshotRefresh()
                : RequestBrokerageAccountSnapshotRefresh(groupNames);
            if (isInitialRequest)
            {
                _initialSnapshotRefreshAccepted = accepted;
                if (accepted)
                {
                    _initialSnapshotRequestGeneration =
                        snapshot.Generation;
                }
            }
            if (!accepted)
            {
                if (scheduledRefreshIntent != 0)
                {
                    Interlocked.Exchange(
                        ref _scheduledRefreshIntent,
                        1);
                }
                return
                    $"{(groupNames == null ? "The complete" : "The scoped")} " +
                    "Financial Advisor snapshot refresh was " +
                    "not accepted; the algorithm will retry.";
            }

            _refreshRequestOutstanding = true;
            _refreshRequestedAfterGeneration = snapshot.Generation;
            return null;
        }

        private void RequestScheduledSnapshotRefresh()
        {
            if (!TryEnterStateMachine())
            {
                Interlocked.Exchange(
                    ref _scheduledRefreshIntent,
                    1);
                return;
            }
            try
            {
                TryAdvanceStateMachine();

                bool refreshRequestOutstanding;
                lock (_orderStateLock)
                {
                    refreshRequestOutstanding =
                        _refreshRequestOutstanding;
                }
                if (!refreshRequestOutstanding)
                {
                    Interlocked.Exchange(
                        ref _scheduledRefreshIntent,
                        1);
                    TryAdvanceStateMachine();
                }
            }
            finally
            {
                ExitStateMachine();
            }
        }

        private bool TryProcessScheduledSnapshotRefresh()
        {
            string errorMessage = null;
            lock (_orderStateLock)
            {
                var snapshot =
                    BrokerageAccountSnapshot;
                UpdateRefreshRequestStateLocked(snapshot);
                if (!_initialSnapshotRefreshAccepted ||
                    snapshot.Generation <=
                        _initialSnapshotRequestGeneration)
                {
                    errorMessage =
                        TryRequestSnapshotRefreshLocked(
                            snapshot,
                            null,
                            isInitialRequest: true);
                }
                else if (!_refreshRequestOutstanding &&
                    !snapshot.IsReady)
                {
                    errorMessage =
                        TryRequestSnapshotRefreshLocked(
                            snapshot,
                            null);
                }
                else if (_refreshRequestOutstanding ||
                    snapshot.Status ==
                        BrokerageAccountSnapshotStatus.Refreshing ||
                    UtcTime < _nextRefreshRetryUtc)
                {
                    return false;
                }
                else
                {
                    if (Interlocked.Exchange(
                            ref _scheduledRefreshIntent,
                            0) == 0)
                    {
                        return false;
                    }

                    var completeRefresh =
                        _scheduledRefreshTopologyTicks + 1 ==
                            CompleteRefreshTopologyTicks;
                    ++_scheduledRefreshTopologyTicks;
                    if (completeRefresh)
                    {
                        _scheduledRefreshTopologyTicks = 0;
                    }
                    IReadOnlyCollection<string> groupNames = null;
                    if (!completeRefresh)
                    {
                        if (_targetGroupName.Length == 0)
                        {
                            groupNames = snapshot.AllGroups.Keys.ToArray();
                            if (groupNames.Count == 0)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            var destinationGroup =
                                snapshot.AllGroups.Values.FirstOrDefault(
                                    group => group.Name.Equals(
                                        _targetGroupName,
                                        StringComparison.OrdinalIgnoreCase));
                            groupNames = new[]
                            {
                                destinationGroup?.Name ?? _targetGroupName
                            };
                        }
                    }
                    errorMessage = TryRequestSnapshotRefreshLocked(
                        snapshot,
                        groupNames);
                    if (errorMessage != null)
                    {
                        Interlocked.Exchange(
                            ref _scheduledRefreshIntent,
                            1);
                    }
                }
            }
            if (errorMessage != null)
            {
                Error(errorMessage);
            }
            return true;
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
    }
}
