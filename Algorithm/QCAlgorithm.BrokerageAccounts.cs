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
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm : IBrokerageAccountServiceConsumer
    {
        private volatile IBrokerageAccountStateProvider _brokerageAccountStateProvider;
        private volatile IBrokerageAccountGroupManager _brokerageAccountGroupManager;
        private volatile IBrokerageAccountGroupAllocationManager _brokerageAccountGroupAllocationManager;

        /// <summary>
        /// Gets the latest immutable brokerage account snapshot. Reading this property does not perform an
        /// external request.
        /// </summary>
        [DocumentationAttribute(LiveTrading)]
        public BrokerageAccountSnapshot BrokerageAccountSnapshot =>
            _brokerageAccountStateProvider?.GetAccountSnapshot() ?? BrokerageAccountSnapshot.Unavailable;

        /// <summary>
        /// Gets the latest immutable brokerage account-group membership update result.
        /// Reading this property does not perform an external request.
        /// </summary>
        [DocumentationAttribute(LiveTrading)]
        public BrokerageAccountGroupAssignment BrokerageAccountGroupAssignment =>
            _brokerageAccountGroupManager?.GetAccountGroupAssignment() ?? BrokerageAccountGroupAssignment.Unavailable;

        /// <summary>
        /// Gets the latest immutable brokerage account-group allocation update result.
        /// Reading this property does not perform an external request.
        /// </summary>
        [DocumentationAttribute(LiveTrading)]
        public BrokerageAccountGroupAllocationUpdate BrokerageAccountGroupAllocationUpdate =>
            _brokerageAccountGroupAllocationManager?.GetAccountGroupAllocationUpdate() ??
                BrokerageAccountGroupAllocationUpdate.Unavailable;

        /// <summary>
        /// Requests asynchronous complete discovery of every brokerage account group and managed account, including
        /// account state for every managed account. An accepted request immediately changes
        /// <see cref="BrokerageAccountSnapshot"/> to refreshing without advancing its generation. Poll for a ready
        /// snapshot with a generation greater than the value observed before this request.
        /// </summary>
        /// <returns>True when the request was accepted or coalesced; otherwise, false.</returns>
        [DocumentationAttribute(LiveTrading)]
        public bool RequestBrokerageAccountSnapshotRefresh()
        {
            return _brokerageAccountStateProvider?.RequestAccountSnapshotRefresh(
                Array.Empty<string>(), Array.Empty<string>()) ?? false;
        }

        /// <summary>
        /// Requests an asynchronous brokerage account snapshot refresh for the specified account groups and
        /// additional managed accounts. An accepted request immediately changes
        /// <see cref="BrokerageAccountSnapshot"/> to refreshing without advancing its generation. Poll for a ready
        /// snapshot with a generation greater than the value observed before this request.
        /// </summary>
        /// <param name="groupNames">Brokerage account groups to include.</param>
        /// <param name="additionalAccountIds">Additional managed accounts to include outside the selected groups.</param>
        /// <returns>True when the request was accepted or coalesced; otherwise, false.</returns>
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

            if (_brokerageAccountStateProvider == null)
            {
                return false;
            }
            return _brokerageAccountStateProvider.RequestAccountSnapshotRefresh(groups, accounts);
        }

        /// <summary>
        /// Requests an account-group assignment using the version tokens from the snapshot on which the
        /// algorithm based the requested change.
        /// </summary>
        /// <remarks>
        /// Account-group mutations are available only after algorithm initialization completes.
        /// </remarks>
        /// <param name="accountId">Managed subaccount to assign.</param>
        /// <param name="targetGroupName">Destination group, or an empty string to remove every assignment.</param>
        /// <param name="targetAllocationValue">Optional brokerage-defined allocation value.</param>
        /// <param name="observedSnapshot">Ready snapshot observed while calculating the requested change.</param>
        /// <returns>True when the request was accepted; otherwise, false.</returns>
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

            if (!GetLocked())
            {
                return false;
            }

            if (_brokerageAccountGroupManager == null || _brokerageAccountStateProvider == null)
            {
                return false;
            }

            if (!observedSnapshot.IsReady)
            {
                return false;
            }

            return _brokerageAccountGroupManager.RequestAccountGroupAssignment(
                accountId,
                targetGroupName,
                observedSnapshot.MembershipHash,
                observedSnapshot.GroupConfigurationVersion,
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
        /// </remarks>
        /// <param name="groupName">Existing managed account group to update.</param>
        /// <param name="accountAllocationValues">Complete per-account allocation vector.</param>
        /// <param name="observedSnapshot">Ready snapshot observed while calculating the requested values.</param>
        /// <returns>True when the request was accepted; otherwise, false.</returns>
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

            if (!GetLocked())
            {
                return false;
            }

            if (_brokerageAccountGroupAllocationManager == null || _brokerageAccountStateProvider == null)
            {
                return false;
            }
            if (!observedSnapshot.IsReady)
            {
                return false;
            }

            return _brokerageAccountGroupAllocationManager.RequestAccountGroupAllocationUpdate(
                groupName,
                allocations,
                observedSnapshot.MembershipHash,
                observedSnapshot.GroupConfigurationVersion);
        }

        /// <summary>
        /// Requests a complete allocation-vector replacement using the version tokens from the snapshot on which
        /// the algorithm based the requested values.
        /// </summary>
        /// <remarks>
        /// Account-group mutations are available only after algorithm initialization completes.
        /// Account-identifier case handling is brokerage-defined. A provider may reject identifiers that differ only
        /// by case as duplicates.
        /// </remarks>
        /// <param name="groupName">Existing managed account group to update.</param>
        /// <param name="accountAllocationValues">Complete per-account allocation vector.</param>
        /// <param name="observedSnapshot">Ready snapshot observed while calculating the requested values.</param>
        /// <returns>True when the request was accepted; otherwise, false.</returns>
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
