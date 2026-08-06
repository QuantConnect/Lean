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
using System.Collections.ObjectModel;
using System.Threading;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm : IBrokerageAccountServiceConsumer
    {
        private const int BrokerageAccountMutationServicesDisabled = 0;
        private const int BrokerageAccountMutationServicesEnabled = 1;
        private const int BrokerageAccountMutationServicesRevoked = 2;

        private volatile IBrokerageAccountStateProvider _brokerageAccountStateProvider;
        private volatile IBrokerageAccountGroupManager _brokerageAccountGroupManager;
        private volatile IBrokerageAccountGroupAllocationManager _brokerageAccountGroupAllocationManager;
        private int _brokerageAccountMutationServicesState;

        /// <summary>
        /// Gets the latest immutable brokerage account snapshot. Reading this property does not perform an
        /// external request.
        /// </summary>
        [DocumentationAttribute(LiveTrading)]
        public BrokerageAccountSnapshot BrokerageAccountSnapshot
        {
            get
            {
                var provider = _brokerageAccountStateProvider;
                return provider?.GetAccountSnapshot() ?? BrokerageAccountSnapshot.Unavailable;
            }
        }

        /// <summary>
        /// Gets the latest immutable brokerage account-group membership update result.
        /// Reading this property does not perform an external request.
        /// </summary>
        [DocumentationAttribute(LiveTrading)]
        public BrokerageAccountGroupAssignment BrokerageAccountGroupAssignment
        {
            get
            {
                var manager = _brokerageAccountGroupManager;
                return manager?.GetAccountGroupAssignment() ?? BrokerageAccountGroupAssignment.Unavailable;
            }
        }

        /// <summary>
        /// Gets the latest immutable brokerage account-group allocation update result.
        /// Reading this property does not perform an external request.
        /// </summary>
        [DocumentationAttribute(LiveTrading)]
        public BrokerageAccountGroupAllocationUpdate BrokerageAccountGroupAllocationUpdate
        {
            get
            {
                var manager = _brokerageAccountGroupAllocationManager;
                return manager?.GetAccountGroupAllocationUpdate() ??
                    BrokerageAccountGroupAllocationUpdate.Unavailable;
            }
        }

        /// <summary>
        /// Requests complete brokerage account discovery asynchronously within the provider's configured deployment
        /// scope. Inspect
        /// <see cref="QuantConnect.Brokerages.BrokerageAccountSnapshot.IsComplete"/> on the published snapshot.
        /// Before accepted work proceeds, the provider publishes <see cref="BrokerageAccountSnapshotStatus.Refreshing"/>
        /// without advancing its generation; a coalesced request joins previously accepted work. A fast refresh can
        /// publish a terminal snapshot before the caller's next read, so observing
        /// <see cref="BrokerageAccountSnapshotStatus.Refreshing"/> is not required. Enforce a caller-owned timeout,
        /// handle <see cref="BrokerageAccountSnapshotStatus.Failed"/> or
        /// <see cref="BrokerageAccountSnapshotStatus.Stale"/>, and require a later-generation
        /// <see cref="BrokerageAccountSnapshotStatus.Ready"/> snapshot before treating the request as successful.
        /// </summary>
        /// <returns>
        /// True when the request was accepted or coalesced; otherwise, false. Acceptance does not indicate completion.
        /// </returns>
        [DocumentationAttribute(LiveTrading)]
        public bool RequestBrokerageAccountSnapshotRefresh()
        {
            var provider = _brokerageAccountStateProvider;
            return provider?.RequestAccountSnapshotRefresh(
                Array.Empty<string>(), Array.Empty<string>()) ?? false;
        }

        /// <summary>
        /// Requests an asynchronous brokerage account snapshot refresh for the specified account groups and
        /// additional managed accounts. Before accepted work proceeds, the provider publishes
        /// <see cref="BrokerageAccountSnapshotStatus.Refreshing"/> without advancing its generation; a coalesced request
        /// joins previously accepted work. A fast refresh can publish a terminal snapshot before the caller's next
        /// read, so observing <see cref="BrokerageAccountSnapshotStatus.Refreshing"/> is not required. Enforce a
        /// caller-owned timeout, handle <see cref="BrokerageAccountSnapshotStatus.Failed"/> or
        /// <see cref="BrokerageAccountSnapshotStatus.Stale"/>, and require a later-generation
        /// <see cref="BrokerageAccountSnapshotStatus.Ready"/> snapshot before treating the request as successful.
        /// </summary>
        /// <param name="groupNames">Brokerage account groups to include.</param>
        /// <param name="additionalAccountIds">Additional managed accounts to include outside the selected groups.</param>
        /// <returns>
        /// True when the request was accepted or coalesced; otherwise, false. Acceptance does not indicate completion.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="groupNames"/> is null.</exception>
        [DocumentationAttribute(LiveTrading)]
        public bool RequestBrokerageAccountSnapshotRefresh(
            IEnumerable<string> groupNames,
            IEnumerable<string> additionalAccountIds = null)
        {
            ArgumentNullException.ThrowIfNull(groupNames);

            var groups = NormalizeIdentifiers(groupNames, nameof(groupNames));
            IReadOnlyCollection<string> accounts = additionalAccountIds == null
                ? Array.Empty<string>()
                : NormalizeIdentifiers(additionalAccountIds, nameof(additionalAccountIds));
            if (groups.Count == 0 && accounts.Count != 0)
            {
                throw new ArgumentException(
                    "Additional account identifiers are not valid for complete discovery.",
                    nameof(additionalAccountIds));
            }

            var provider = _brokerageAccountStateProvider;
            return provider?.RequestAccountSnapshotRefresh(groups, accounts) ?? false;
        }

        /// <summary>
        /// Requests an account-group assignment using the version tokens from the snapshot on which the
        /// algorithm based the requested change.
        /// </summary>
        /// <remarks>
        /// Account-group mutations are available only after algorithm initialization completes.
        /// A request issued during or after <see cref="OnEndOfAlgorithm"/> may be accepted but is not
        /// guaranteed to reach the broker or publish a result. Algorithms must not request configuration
        /// mutations during teardown.
        /// </remarks>
        /// <param name="accountId">Managed subaccount to assign.</param>
        /// <param name="targetGroupName">Destination group, or an empty string to remove every assignment.</param>
        /// <param name="targetAllocationValue">Optional brokerage-defined allocation value.</param>
        /// <param name="observedSnapshot">Ready snapshot observed while calculating the requested change.</param>
        /// <returns>
        /// True when the request was accepted for asynchronous processing while the algorithm is running;
        /// otherwise, false.
        /// </returns>
        [DocumentationAttribute(LiveTrading)]
        public bool RequestBrokerageAccountGroupAssignment(
            string accountId,
            string targetGroupName,
            decimal? targetAllocationValue,
            BrokerageAccountSnapshot observedSnapshot)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new ArgumentException("A managed account identifier is required.", nameof(accountId));
            }
            ArgumentNullException.ThrowIfNull(targetGroupName);
            if (targetGroupName.Length != 0 && string.IsNullOrWhiteSpace(targetGroupName))
            {
                throw new ArgumentException(
                    "The target group must be a non-empty name or the exact empty string.",
                    nameof(targetGroupName));
            }
            ArgumentNullException.ThrowIfNull(observedSnapshot);
            ValidateBrokerageIdentifier(accountId, nameof(accountId));
            if (targetGroupName.Length != 0)
            {
                ValidateBrokerageIdentifier(targetGroupName, nameof(targetGroupName));
            }

            if (Volatile.Read(ref _brokerageAccountMutationServicesState) !=
                BrokerageAccountMutationServicesEnabled)
            {
                return false;
            }

            var manager = _brokerageAccountGroupManager;
            var provider = _brokerageAccountStateProvider;
            if (manager == null || provider == null)
            {
                return false;
            }

            var membershipHash = observedSnapshot.MembershipHash;
            var groupConfigurationVersion = observedSnapshot.GroupConfigurationVersion;
            if (!observedSnapshot.IsReady ||
                string.IsNullOrWhiteSpace(membershipHash) ||
                string.IsNullOrWhiteSpace(groupConfigurationVersion))
            {
                return false;
            }

            return manager.RequestAccountGroupAssignment(
                accountId,
                targetGroupName,
                membershipHash,
                groupConfigurationVersion,
                targetAllocationValue);
        }

        /// <summary>
        /// Requests a complete allocation-vector replacement using the version tokens from the snapshot on which
        /// the algorithm based the requested values.
        /// </summary>
        /// <remarks>
        /// Account-group mutations are available only after algorithm initialization completes.
        /// Account-identifier case handling is brokerage-defined. A provider may reject identifiers that differ only
        /// by case as duplicates.
        /// A request issued during or after <see cref="OnEndOfAlgorithm"/> may be accepted but is not
        /// guaranteed to reach the broker or publish a result. Algorithms must not request configuration
        /// mutations during teardown.
        /// </remarks>
        /// <param name="groupName">Existing managed account group to update.</param>
        /// <param name="accountAllocationValues">Complete per-account allocation vector.</param>
        /// <param name="observedSnapshot">Ready snapshot observed while calculating the requested values.</param>
        /// <returns>
        /// True when the request was accepted for asynchronous processing while the algorithm is running;
        /// otherwise, false.
        /// </returns>
        [DocumentationAttribute(LiveTrading)]
        public bool RequestBrokerageAccountGroupAllocationUpdate(
            string groupName,
            IReadOnlyDictionary<string, decimal> accountAllocationValues,
            BrokerageAccountSnapshot observedSnapshot)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("An account group name is required.", nameof(groupName));
            }
            ArgumentNullException.ThrowIfNull(accountAllocationValues);
            ArgumentNullException.ThrowIfNull(observedSnapshot);

            ValidateBrokerageIdentifier(groupName, nameof(groupName));
            var allocations = new Dictionary<string, decimal>(StringComparer.Ordinal);
            foreach (var pair in accountAllocationValues)
            {
                ValidateBrokerageIdentifier(
                    pair.Key,
                    nameof(accountAllocationValues));
                if (!allocations.TryAdd(pair.Key, pair.Value))
                {
                    throw new ArgumentException(
                        $"Allocation account identifier '{pair.Key}' is duplicated.",
                        nameof(accountAllocationValues));
                }
            }

            if (Volatile.Read(ref _brokerageAccountMutationServicesState) !=
                BrokerageAccountMutationServicesEnabled)
            {
                return false;
            }

            var manager = _brokerageAccountGroupAllocationManager;
            var provider = _brokerageAccountStateProvider;
            if (manager == null || provider == null)
            {
                return false;
            }

            var membershipHash = observedSnapshot.MembershipHash;
            var groupConfigurationVersion = observedSnapshot.GroupConfigurationVersion;
            if (!observedSnapshot.IsReady ||
                string.IsNullOrWhiteSpace(membershipHash) ||
                string.IsNullOrWhiteSpace(groupConfigurationVersion))
            {
                return false;
            }

            return manager.RequestAccountGroupAllocationUpdate(
                groupName,
                allocations,
                membershipHash,
                groupConfigurationVersion);
        }

        /// <summary>
        /// Requests a complete allocation-vector replacement using the version tokens from the snapshot on which
        /// the algorithm based the requested values.
        /// </summary>
        /// <remarks>
        /// Account-group mutations are available only after algorithm initialization completes.
        /// Account-identifier case handling is brokerage-defined. A provider may reject identifiers that differ only
        /// by case as duplicates.
        /// A request issued during or after <see cref="OnEndOfAlgorithm"/> may be accepted but is not
        /// guaranteed to reach the broker or publish a result. Algorithms must not request configuration
        /// mutations during teardown.
        /// </remarks>
        /// <param name="groupName">Existing managed account group to update.</param>
        /// <param name="accountAllocationValues">Complete per-account allocation vector.</param>
        /// <param name="observedSnapshot">Ready snapshot observed while calculating the requested values.</param>
        /// <returns>
        /// True when the request was accepted for asynchronous processing while the algorithm is running;
        /// otherwise, false.
        /// </returns>
        [DocumentationAttribute(LiveTrading)]
        public bool RequestBrokerageAccountGroupAllocationUpdate(
            string groupName,
            IEnumerable<KeyValuePair<string, decimal>> accountAllocationValues,
            BrokerageAccountSnapshot observedSnapshot)
        {
            ArgumentNullException.ThrowIfNull(accountAllocationValues);

            var allocations = new Dictionary<string, decimal>(StringComparer.Ordinal);
            foreach (var pair in accountAllocationValues)
            {
                ValidateBrokerageIdentifier(pair.Key, nameof(accountAllocationValues));
                if (!allocations.TryAdd(pair.Key, pair.Value))
                {
                    throw new ArgumentException(
                        $"Allocation account identifier '{pair.Key}' is duplicated.",
                        nameof(accountAllocationValues));
                }
            }
            return RequestBrokerageAccountGroupAllocationUpdate(
                groupName,
                (IReadOnlyDictionary<string, decimal>)allocations,
                observedSnapshot);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1033:Interface methods should be callable by child types",
            Justification = "Engine-only service injection must not be exposed to algorithm subclasses.")]
        void IBrokerageAccountServiceConsumer.SetBrokerageAccountStateProvider(
            IBrokerageAccountStateProvider provider)
        {
            _brokerageAccountStateProvider = provider;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1033:Interface methods should be callable by child types",
            Justification = "Engine-only service injection must not be exposed to algorithm subclasses.")]
        void IBrokerageAccountServiceConsumer.SetBrokerageAccountGroupManager(
            IBrokerageAccountGroupManager manager)
        {
            _brokerageAccountGroupManager = manager;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1033:Interface methods should be callable by child types",
            Justification = "Engine-only service injection must not be exposed to algorithm subclasses.")]
        void IBrokerageAccountServiceConsumer.SetBrokerageAccountGroupAllocationManager(
            IBrokerageAccountGroupAllocationManager manager)
        {
            _brokerageAccountGroupAllocationManager = manager;
        }

        internal void SetBrokerageAccountMutationServicesReady(bool ready)
        {
            if (ready)
            {
                var provider = _brokerageAccountStateProvider;
                var groupManager = _brokerageAccountGroupManager;
                var allocationManager = _brokerageAccountGroupAllocationManager;
                if (provider != null && (groupManager != null || allocationManager != null))
                {
                    Interlocked.CompareExchange(
                        ref _brokerageAccountMutationServicesState,
                        BrokerageAccountMutationServicesEnabled,
                        BrokerageAccountMutationServicesDisabled);
                }
                return;
            }

            Interlocked.CompareExchange(
                ref _brokerageAccountMutationServicesState,
                BrokerageAccountMutationServicesDisabled,
                BrokerageAccountMutationServicesEnabled);
        }

        internal void RevokeBrokerageAccountMutationServices()
        {
            Interlocked.Exchange(
                ref _brokerageAccountMutationServicesState,
                BrokerageAccountMutationServicesRevoked);
        }

        private static ReadOnlyCollection<string> NormalizeIdentifiers(
            IEnumerable<string> identifiers,
            string parameterName)
        {
            var result = new List<string>();
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var identifier in identifiers)
            {
                ValidateBrokerageIdentifier(identifier, parameterName);
                if (!unique.Add(identifier))
                {
                    throw new ArgumentException(
                        $"Identifier '{identifier}' is duplicated.",
                        parameterName);
                }
                result.Add(identifier);
            }
            return result.AsReadOnly();
        }

        private static void ValidateBrokerageIdentifier(
            string identifier,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(identifier) ||
                !identifier.Equals(identifier.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Identifiers must be non-empty and contain no leading or trailing whitespace.",
                    parameterName);
            }
        }
    }
}
