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
using System.Linq;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Immutable account-level brokerage state for multi-account structures.
    /// </summary>
    public class BrokerageAccountSnapshot
    {
        private static readonly IReadOnlyDictionary<string, BrokerageAccountGroup> EmptyGroups =
            new ReadOnlyDictionary<string, BrokerageAccountGroup>(new Dictionary<string, BrokerageAccountGroup>());
        private static readonly IReadOnlyDictionary<string, BrokerageAccountState> EmptyAccounts =
            new ReadOnlyDictionary<string, BrokerageAccountState>(new Dictionary<string, BrokerageAccountState>());
        private static readonly IReadOnlyDictionary<string, BrokerageAccountDirectoryEntry> EmptyAccountDirectory =
            new ReadOnlyDictionary<string, BrokerageAccountDirectoryEntry>(new Dictionary<string, BrokerageAccountDirectoryEntry>());
        private static readonly IReadOnlyList<string> EmptyAccountIds = Array.Empty<string>();

        /// <summary>
        /// Snapshot returned when the brokerage does not expose account-level state.
        /// </summary>
        public static BrokerageAccountSnapshot Unavailable { get; } = new(
            BrokerageAccountSnapshotStatus.Unavailable,
            0,
            default,
            default,
            EmptyGroups,
            EmptyAccounts,
            EmptyAccountIds,
            string.Empty,
            string.Empty,
            string.Empty);

        /// <summary>
        /// Gets the current snapshot status.
        /// </summary>
        public BrokerageAccountSnapshotStatus Status { get; }

        /// <summary>
        /// Gets the monotonically increasing snapshot generation.
        /// </summary>
        public long Generation { get; }

        /// <summary>
        /// Gets the UTC time at which this snapshot was published.
        /// </summary>
        public DateTime AsOfUtc { get; }

        /// <summary>
        /// Gets the UTC time of the last successful snapshot refresh.
        /// </summary>
        public DateTime LastSuccessfulUpdateUtc { get; }

        /// <summary>
        /// Gets the UTC time at which collection of the account data in this snapshot started, when available.
        /// </summary>
        public DateTime CollectionStartedUtc { get; }

        /// <summary>
        /// Gets the brokerage groups selected for this snapshot.
        /// </summary>
        public IReadOnlyDictionary<string, BrokerageAccountGroup> Groups { get; }

        /// <summary>
        /// Gets every account group discovered from the brokerage, including groups outside a scoped refresh.
        /// </summary>
        public IReadOnlyDictionary<string, BrokerageAccountGroup> AllGroups { get; }

        /// <summary>
        /// Gets the primary account configured for the brokerage connection.
        /// </summary>
        public string PrimaryAccountId { get; }

        /// <summary>
        /// Gets every account identifier returned by the brokerage managed-account discovery API.
        /// </summary>
        public IReadOnlyList<string> ManagedAccountIds { get; }

        /// <summary>
        /// Gets the complete discovered account directory. Directory entries can exist without corresponding
        /// account state in <see cref="Accounts"/> when the snapshot was scoped.
        /// </summary>
        public IReadOnlyDictionary<string, BrokerageAccountDirectoryEntry> AccountDirectory { get; }

        /// <summary>
        /// Gets the account states collected for selected group members and any explicitly requested additional accounts.
        /// This collection and <see cref="UnassignedAccountIds"/> do not necessarily partition every managed account when
        /// the brokerage connection is scoped to a subset of groups.
        /// </summary>
        public IReadOnlyDictionary<string, BrokerageAccountState> Accounts { get; }

        /// <summary>
        /// Gets managed subaccounts that are not members of any account group visible to the brokerage session.
        /// Account state is included in <see cref="Accounts"/> only when it was explicitly requested.
        /// </summary>
        public IReadOnlyList<string> UnassignedAccountIds { get; }

        /// <summary>
        /// Gets the version of the selected group membership, managed-account universe, account aliases, and family codes.
        /// </summary>
        public string MembershipHash { get; }

        /// <summary>
        /// Gets the opaque brokerage-wide account-group configuration version.
        /// </summary>
        public string GroupConfigurationVersion { get; }

        /// <summary>
        /// Gets the latest refresh failure reason, or an empty string when no failure occurred.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets whether the latest snapshot refresh completed successfully.
        /// </summary>
        public bool IsReady => Status == BrokerageAccountSnapshotStatus.Ready;

        /// <summary>
        /// Gets whether <see cref="AllGroups"/> is complete and <see cref="Accounts"/> contains state for every
        /// managed account other than the primary and aggregate reporting identifiers.
        /// </summary>
        public bool IsComplete { get; }

        /// <summary>
        /// Gets whether any collected account contains a brokerage position that could not be mapped to a LEAN symbol.
        /// </summary>
        public bool HasUnmappedPositions { get; }

        /// <summary>
        /// Initializes an immutable brokerage account snapshot.
        /// </summary>
        /// <param name="status">Current snapshot status.</param>
        /// <param name="generation">Monotonically increasing snapshot generation.</param>
        /// <param name="asOfUtc">UTC time at which this snapshot was published.</param>
        /// <param name="lastSuccessfulUpdateUtc">UTC time of the last successful snapshot refresh.</param>
        /// <param name="groups">Brokerage groups selected for this snapshot.</param>
        /// <param name="accounts">Account states collected by this snapshot.</param>
        /// <param name="unassignedAccountIds">Managed accounts not assigned to any discovered group.</param>
        /// <param name="membershipHash">Version of membership, managed accounts, aliases, and family codes.</param>
        /// <param name="groupConfigurationVersion">Opaque brokerage-wide group configuration version.</param>
        /// <param name="errorMessage">Latest refresh failure reason.</param>
        /// <param name="primaryAccountId">Primary account configured for the brokerage connection.</param>
        /// <param name="managedAccountIds">Account identifiers returned by managed-account discovery.</param>
        /// <param name="allGroups">Every account group discovered from the brokerage.</param>
        /// <param name="accountDirectory">Complete discovered account directory.</param>
        /// <param name="isComplete">
        /// True when all groups and managed account states are represented; otherwise, false.
        /// </param>
        /// <param name="collectionStartedUtc">
        /// UTC time at which collection of the account data in this snapshot started, when available.
        /// </param>
        public BrokerageAccountSnapshot(
            BrokerageAccountSnapshotStatus status,
            long generation,
            DateTime asOfUtc,
            DateTime lastSuccessfulUpdateUtc,
            IReadOnlyDictionary<string, BrokerageAccountGroup> groups,
            IReadOnlyDictionary<string, BrokerageAccountState> accounts,
            IReadOnlyList<string> unassignedAccountIds,
            string membershipHash,
            string groupConfigurationVersion,
            string errorMessage,
            string primaryAccountId = "",
            IReadOnlyList<string> managedAccountIds = null,
            IReadOnlyDictionary<string, BrokerageAccountGroup> allGroups = null,
            IReadOnlyDictionary<string, BrokerageAccountDirectoryEntry> accountDirectory = null,
            bool isComplete = false,
            DateTime collectionStartedUtc = default)
        {
            Status = status;
            Generation = generation;
            AsOfUtc = asOfUtc;
            LastSuccessfulUpdateUtc = lastSuccessfulUpdateUtc;
            CollectionStartedUtc = collectionStartedUtc;
            ValidateStructure();
            ArgumentNullException.ThrowIfNull(groups);
            ArgumentNullException.ThrowIfNull(accounts);
            ArgumentNullException.ThrowIfNull(unassignedAccountIds);
            Groups = BrokerageAccountCollection.CopyDictionary(groups, nameof(groups), group => group?.Name);
            AllGroups = allGroups == null
                ? Groups
                : BrokerageAccountCollection.CopyDictionary(allGroups, nameof(allGroups), group => group?.Name);
            PrimaryAccountId = primaryAccountId ?? string.Empty;
            if (PrimaryAccountId.Length != 0)
            {
                BrokerageAccountCollection.ValidateIdentifier(
                    PrimaryAccountId,
                    nameof(primaryAccountId));
            }
            ManagedAccountIds = BrokerageAccountCollection.CopyIdentifiers(
                managedAccountIds ?? EmptyAccountIds,
                nameof(managedAccountIds));
            AccountDirectory = BrokerageAccountCollection.CopyDictionary(
                accountDirectory ?? EmptyAccountDirectory,
                nameof(accountDirectory),
                entry => entry?.AccountId);
            Accounts = BrokerageAccountCollection.CopyDictionary(
                accounts,
                nameof(accounts),
                account => account?.AccountId);
            UnassignedAccountIds = BrokerageAccountCollection.CopyIdentifiers(
                unassignedAccountIds ?? EmptyAccountIds,
                nameof(unassignedAccountIds));
            MembershipHash = membershipHash ?? string.Empty;
            GroupConfigurationVersion = groupConfigurationVersion ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
            IsComplete = isComplete;
            HasUnmappedPositions = Accounts.Values.Any(account => account.HasUnmappedPositions);
        }

        private void ValidateStructure()
        {
            if (Generation < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "generation",
                    "The snapshot generation cannot be negative.");
            }
            if (Status == BrokerageAccountSnapshotStatus.Ready &&
                (AsOfUtc == default || LastSuccessfulUpdateUtc == default))
            {
                throw new ArgumentException(
                    "A ready snapshot requires publication and successful update times.",
                    "asOfUtc");
            }
        }
    }
}
