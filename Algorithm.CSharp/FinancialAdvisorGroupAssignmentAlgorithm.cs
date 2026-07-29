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
using System.Text.RegularExpressions;
using QuantConnect.Brokerages;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstrates alias-driven movement of managed accounts between existing Financial Advisor groups.
    ///
    /// PREREQUISITES: ib-financial-advisors-group-filter must be empty and
    /// ib-financial-advisors-group-management-enabled=true, which implies unified groups.
    /// The fa-alias-pattern parameter is a case-insensitive regular expression. An exact empty
    /// fa-target-group removes matching accounts from every group.
    /// </summary>
    /// <meta name="tag" content="financial advisor" />
    /// <meta name="tag" content="live trading" />
    public class FinancialAdvisorGroupAssignmentAlgorithm : QCAlgorithm
    {
        private static readonly TimeSpan RefreshRetryInterval =
            TimeSpan.FromSeconds(5);
        private static readonly TimeSpan TopologyRefreshInterval =
            TimeSpan.FromMinutes(1);
        private const int CompleteRefreshTopologyTicks = 3;

        private Regex _aliasPattern;
        private string _targetGroupName;
        private decimal _targetAllocationValue;
        private decimal _cashChangeThreshold;
        private DateTime _nextRefreshRetryUtc;
        private bool _refreshRequestOutstanding;
        private long _refreshRequestedAfterGeneration = -1;
        private BrokerageAccountSnapshot _cashBaselineSnapshot;
        private bool _cashConfirmationRequired;
        private long _cashChangeGeneration = -1;
        private long _lastEvaluatedGeneration = -1;
        private bool _assignmentRequestAccepted;
        private long _assignmentGenerationBeforeRequest = -1;
        private long _pendingAssignmentGeneration = -1;
        private long _assignmentSnapshotGeneration = -1;
        private long _minimumReadyGeneration = -1;
        private string _pendingAccountId = string.Empty;
        private int _scheduledRefreshTopologyTicks;

        /// <summary>
        /// Configures the alias rule, destination, allocation value, cash-change threshold,
        /// and the algorithm-owned snapshot refresh cadence.
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
                GetParameter("fa-allocation-value", 1m);
            _cashChangeThreshold =
                GetParameter("fa-cash-change-threshold", 1000m);

            if (_targetAllocationValue <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "fa-allocation-value",
                    "The destination allocation value must be positive.");
            }
            if (_cashChangeThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "fa-cash-change-threshold",
                    "The cash-change threshold cannot be negative.");
            }

            _nextRefreshRetryUtc = UtcTime;
            if (LiveMode)
            {
                // Every third one-minute topology tick expands to complete account state.
                Schedule.On(
                    DateRules.EveryDay(),
                    TimeRules.Every(TopologyRefreshInterval),
                    RequestScheduledSnapshotRefresh);
                TryRequestSnapshotRefresh(BrokerageAccountSnapshot);
            }
        }

        /// <summary>
        /// Polls asynchronous mutations, evaluates each authoritative snapshot generation once,
        /// and owns the cadence used to discover external account changes.
        /// </summary>
        /// <param name="slice">The current data slice.</param>
        public override void OnData(Slice slice)
        {
            if (!LiveMode)
            {
                return;
            }

            var snapshot = BrokerageAccountSnapshot;
            UpdateRefreshRequestState(snapshot);

            if (PollAssignment())
            {
                return;
            }

            if (_minimumReadyGeneration >= 0)
            {
                if (!snapshot.IsReady ||
                    snapshot.Generation <= _minimumReadyGeneration)
                {
                    TryRequestSnapshotRefresh(snapshot);
                    return;
                }
                _minimumReadyGeneration = -1;
            }

            if (_cashConfirmationRequired)
            {
                if (!snapshot.IsReady ||
                    !snapshot.IsComplete ||
                    snapshot.Generation <= _cashChangeGeneration)
                {
                    TryRequestSnapshotRefresh(snapshot);
                    return;
                }

                _cashConfirmationRequired = false;
                _cashBaselineSnapshot = snapshot;
                Log(
                    $"FA cash-change confirmation is Ready at generation " +
                    $"{snapshot.Generation}; reevaluating group membership.");
            }

            if (_refreshRequestOutstanding ||
                !snapshot.IsReady)
            {
                TryRequestSnapshotRefresh(snapshot);
                return;
            }

            if (snapshot.Generation == _lastEvaluatedGeneration)
            {
                return;
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
                return;
            }

            if (snapshot.IsComplete &&
                (_cashBaselineSnapshot == null ||
                    snapshot.Generation > _cashBaselineSnapshot.Generation))
            {
                _cashBaselineSnapshot = snapshot;
            }

            EvaluateGroupAssignment(snapshot);
        }

        private bool PollAssignment()
        {
            if (!_assignmentRequestAccepted)
            {
                return false;
            }

            var assignment = BrokerageAccountGroupAssignment;
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
            }

            _lastEvaluatedGeneration = Math.Max(
                _lastEvaluatedGeneration,
                _assignmentSnapshotGeneration);
            _assignmentRequestAccepted = false;
            _pendingAssignmentGeneration = -1;
            _pendingAccountId = string.Empty;
            return true;
        }

        private void EvaluateGroupAssignment(
            BrokerageAccountSnapshot snapshot)
        {
            BrokerageAccountGroup destinationGroup = null;
            decimal? allocationValue = null;
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
                    return;
                }

                if (!TryGetAllocationValue(
                        destinationGroup,
                        out allocationValue))
                {
                    _lastEvaluatedGeneration = snapshot.Generation;
                    return;
                }
            }

            var candidate = snapshot.AccountDirectory.Values
                .Where(IsAliasMatch)
                .Where(entry => destinationGroup == null
                    ? entry.GroupNames.Count != 0
                    : !entry.GroupNames.Contains(
                        destinationGroup.Name,
                        StringComparer.OrdinalIgnoreCase))
                .OrderBy(
                    entry => entry.AccountId,
                    StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            _lastEvaluatedGeneration = snapshot.Generation;
            if (candidate == null)
            {
                return;
            }

            var assignmentBeforeRequest =
                BrokerageAccountGroupAssignment;
            var canonicalTarget = destinationGroup?.Name ?? string.Empty;
            if (!RequestBrokerageAccountGroupAssignment(
                    candidate.AccountId,
                    canonicalTarget,
                    allocationValue,
                    snapshot))
            {
                Error(
                    $"FA assignment was not accepted: account={candidate.AccountId}, " +
                    $"target='{canonicalTarget}', snapshot generation=" +
                    $"{snapshot.Generation}.");
                return;
            }

            _assignmentRequestAccepted = true;
            _assignmentGenerationBeforeRequest =
                assignmentBeforeRequest.Generation;
            _assignmentSnapshotGeneration = snapshot.Generation;
            _pendingAssignmentGeneration = -1;
            _pendingAccountId = candidate.AccountId;
            PollAssignment();
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
            out decimal? allocationValue)
        {
            allocationValue = null;
            if (new[] { "Equal", "NetLiq", "AvailableEquity" }.Contains(
                    destinationGroup.AllocationMethod,
                    StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            if (new[] { "ContractsOrShares", "Ratio", "Percent" }.Contains(
                    destinationGroup.AllocationMethod,
                    StringComparer.OrdinalIgnoreCase))
            {
                allocationValue = _targetAllocationValue;
                return true;
            }

            Error(
                $"FA destination group '{destinationGroup.Name}' uses " +
                $"unsupported saved allocation method " +
                $"'{destinationGroup.AllocationMethod}'.");
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
                    BrokerageAccountSnapshotStatus.Stale)
            {
                _refreshRequestOutstanding = false;
            }
        }

        private void TryRequestSnapshotRefresh(
            BrokerageAccountSnapshot snapshot,
            IReadOnlyCollection<string> groupNames = null)
        {
            if (_refreshRequestOutstanding ||
                snapshot.Status ==
                    BrokerageAccountSnapshotStatus.Refreshing ||
                UtcTime < _nextRefreshRetryUtc)
            {
                return;
            }

            _nextRefreshRetryUtc =
                UtcTime + RefreshRetryInterval;
            var accepted = groupNames == null
                ? RequestBrokerageAccountSnapshotRefresh()
                : RequestBrokerageAccountSnapshotRefresh(groupNames);
            if (!accepted)
            {
                Error(
                    $"{(groupNames == null ? "The complete" : "The scoped")} " +
                    "Financial Advisor snapshot refresh was " +
                    "not accepted; the algorithm will retry.");
                return;
            }

            _refreshRequestOutstanding = true;
            _refreshRequestedAfterGeneration = snapshot.Generation;
        }

        private void RequestScheduledSnapshotRefresh()
        {
            var snapshot = BrokerageAccountSnapshot;
            UpdateRefreshRequestState(snapshot);
            if (!LiveMode ||
                _refreshRequestOutstanding ||
                _assignmentRequestAccepted ||
                _minimumReadyGeneration >= 0 ||
                _cashConfirmationRequired ||
                snapshot.Status ==
                    BrokerageAccountSnapshotStatus.Refreshing ||
                UtcTime < _nextRefreshRetryUtc)
            {
                return;
            }

            ++_scheduledRefreshTopologyTicks;
            if (_scheduledRefreshTopologyTicks ==
                CompleteRefreshTopologyTicks)
            {
                _scheduledRefreshTopologyTicks = 0;
                TryRequestSnapshotRefresh(snapshot);
                return;
            }

            IReadOnlyCollection<string> groupNames;
            if (_targetGroupName.Length == 0)
            {
                groupNames = snapshot.AllGroups.Keys.ToArray();
                if (groupNames.Count == 0)
                {
                    return;
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
            TryRequestSnapshotRefresh(snapshot, groupNames);
        }
    }
}
