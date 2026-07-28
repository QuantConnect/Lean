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
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class BrokerageAccountSnapshotTests
    {
        private static readonly DateTime AsOfUtc = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        [Test]
        public void SnapshotDefensivelyCopiesCollectionsAndPreservesCanonicalIdentifiers()
        {
            var members = new List<string> { "AccountB", "accounta" };
            var positions = new List<BrokerageAccountPosition>
            {
                new(Symbols.SPY, 1.25m, 500m, " Model ")
            };
            var cash = new Dictionary<string, decimal> { ["USD"] = 100m };
            var group = new BrokerageAccountGroup("Group", "Ratio", members);
            var account = new BrokerageAccountState(
                "AccountA",
                new[] { "Group" },
                " Margin ",
                1000m,
                100m,
                900m,
                900m,
                2000m,
                " USD ",
                cash,
                positions);
            var groups = new Dictionary<string, BrokerageAccountGroup> { ["Group"] = group };
            var accounts = new Dictionary<string, BrokerageAccountState> { ["AccountA"] = account };

            var snapshot = new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Stale,
                1,
                AsOfUtc,
                AsOfUtc,
                groups,
                accounts,
                new[] { "AccountC" },
                "membership",
                "configuration",
                string.Empty,
                "Primary",
                new[] { "AccountC", "AccountA" });

            members.Add("AccountD");
            positions.Clear();
            cash["USD"] = 0m;
            groups.Clear();
            accounts.Clear();

            CollectionAssert.AreEqual(new[] { "accounta", "AccountB" }, group.AccountIds);
            CollectionAssert.AreEqual(new[] { "Group" }, account.GroupNames);
            Assert.AreEqual("AccountA", account.AccountId);
            Assert.AreEqual("Margin", account.AccountType);
            Assert.AreEqual("USD", account.ValuationCurrency);
            Assert.AreEqual(100m, account.CashBalances["usd"]);
            Assert.AreEqual(1, account.Positions.Count);
            Assert.AreEqual("Model", account.Positions[0].ModelCode);
            Assert.AreEqual(1, snapshot.Groups.Count);
            Assert.AreEqual(1, snapshot.Accounts.Count);
            Assert.AreEqual("Primary", snapshot.PrimaryAccountId);
            CollectionAssert.AreEqual(new[] { "AccountA", "AccountC" }, snapshot.ManagedAccountIds);
            CollectionAssert.AreEqual(new[] { "AccountC" }, snapshot.UnassignedAccountIds);
            Assert.AreSame(snapshot.Groups, snapshot.AllGroups);
            Assert.AreEqual(default(DateTime), snapshot.CollectionStartedUtc);
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, BrokerageAccountGroup>)snapshot.Groups).Add("Other", group));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<string>)snapshot.ManagedAccountIds).Add("Other"));

            Assert.Throws<ArgumentException>(() => new BrokerageAccountGroup(
                "Duplicate",
                "Ratio",
                new[] { "AccountA", "accounta" }));
        }

        [Test]
        public void SnapshotRoundTripsThroughJsonWithCompleteAccountDirectory()
        {
            var collectionStartedUtc = AsOfUtc.AddMinutes(-1);
            var group = new BrokerageAccountGroup(
                "Group",
                "Ratio",
                new[] { "AccountA" },
                new Dictionary<string, decimal> { ["AccountA"] = 1m });
            var account = new BrokerageAccountState(
                "AccountA",
                new[] { "Group" },
                "Margin",
                1000m,
                100m,
                900m,
                900m,
                2000m,
                "USD",
                new Dictionary<string, decimal> { ["USD"] = 100m },
                new[] { new BrokerageAccountPosition(Symbols.SPY, 1.25m, 500m) });
            var snapshot = new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                1,
                AsOfUtc,
                AsOfUtc,
                new Dictionary<string, BrokerageAccountGroup> { ["Group"] = group },
                new Dictionary<string, BrokerageAccountState> { ["AccountA"] = account },
                Array.Empty<string>(),
                "membership",
                "configuration",
                string.Empty,
                "Primary",
                new[] { "Primary", "AccountA" },
                null,
                new Dictionary<string, BrokerageAccountDirectoryEntry>
                {
                    ["Primary"] = new(
                        "Primary",
                        BrokerageAccountRelationship.Primary,
                        Array.Empty<string>()),
                    ["AccountA"] = new(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        new[] { "Group" },
                        "Margin",
                        "Family",
                        "Alias")
                },
                true,
                collectionStartedUtc);

            var roundTrip = JsonConvert.DeserializeObject<BrokerageAccountSnapshot>(
                JsonConvert.SerializeObject(snapshot));

            Assert.IsNotNull(roundTrip);
            Assert.IsTrue(roundTrip.IsComplete);
            Assert.AreEqual(collectionStartedUtc, roundTrip.CollectionStartedUtc);
            Assert.AreEqual("Alias", roundTrip.AccountDirectory["AccountA"].AccountAlias);
            Assert.AreEqual(1.25m, roundTrip.Accounts["AccountA"].Positions[0].Quantity);
            Assert.AreEqual(1m, roundTrip.Groups["Group"].AccountAllocationValues["AccountA"]);
        }

        [Test]
        public void DictionaryIdentifiersRejectWhitespaceOrMismatchedInputs()
        {
            var duplicateCash = new Dictionary<string, decimal>
            {
                ["USD"] = 1m,
                [" usd "] = 2m
            };
            var duplicate = Assert.Throws<ArgumentException>(() => new BrokerageAccountState(
                "AccountA",
                Array.Empty<string>(),
                string.Empty,
                null,
                null,
                null,
                null,
                null,
                "USD",
                duplicateCash,
                Array.Empty<BrokerageAccountPosition>()));
            Assert.AreEqual("cashBalances", duplicate.ParamName);
            StringAssert.Contains("leading or trailing whitespace", duplicate.Message);

            var group = new BrokerageAccountGroup("Group", "NetLiq", new[] { "AccountA" });
            var mismatch = Assert.Throws<ArgumentException>(() => new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                1,
                AsOfUtc,
                AsOfUtc,
                new Dictionary<string, BrokerageAccountGroup> { ["Other"] = group },
                new Dictionary<string, BrokerageAccountState>(),
                Array.Empty<string>(),
                string.Empty,
                string.Empty,
                string.Empty));
            Assert.AreEqual("groups", mismatch.ParamName);
            StringAssert.Contains("does not match", mismatch.Message);
        }

        [Test]
        public void CollectionValidationReportsPublicConstructorParameterNames()
        {
            var group = Assert.Throws<ArgumentException>(() =>
                new BrokerageAccountGroup(
                    "Group",
                    "NetLiq",
                    new[] { "Account", "account" }));
            var directory = Assert.Throws<ArgumentException>(() =>
                new BrokerageAccountDirectoryEntry(
                    "Account",
                    BrokerageAccountRelationship.Managed,
                    new[] { "Group", "group" }));
            var snapshot = Assert.Throws<ArgumentException>(() =>
                new BrokerageAccountSnapshot(
                    BrokerageAccountSnapshotStatus.Unavailable,
                    0,
                    default,
                    default,
                    new Dictionary<string, BrokerageAccountGroup>(),
                    new Dictionary<string, BrokerageAccountState>(),
                    Array.Empty<string>(),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    managedAccountIds: new[] { "Account", "account" }));
            var assignment = Assert.Throws<ArgumentException>(() =>
                new BrokerageAccountGroupAssignment(
                    BrokerageAccountGroupAssignmentStatus.Pending,
                    1,
                    AsOfUtc,
                    "Account",
                    "Group",
                    new[] { "Group", "group" },
                    Array.Empty<string>(),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty));
            var allocation = Assert.Throws<ArgumentException>(() =>
                new BrokerageAccountGroupAllocationUpdate(
                    BrokerageAccountGroupAllocationUpdateStatus.Pending,
                    1,
                    AsOfUtc,
                    "Group",
                    "Ratio",
                    new Dictionary<string, decimal> { [" Account"] = 1m },
                    null,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty));

            Assert.AreEqual("accountIds", group.ParamName);
            Assert.AreEqual("groupNames", directory.ParamName);
            Assert.AreEqual("managedAccountIds", snapshot.ParamName);
            Assert.AreEqual("previousGroupNames", assignment.ParamName);
            Assert.AreEqual("requestedAccountAllocationValues", allocation.ParamName);
        }

        [Test]
        public void GroupAllocationsRejectAccountsOutsideMembership()
        {
            var exception = Assert.Throws<ArgumentException>(() => new BrokerageAccountGroup(
                "Group",
                "Ratio",
                new[] { "AccountA" },
                new Dictionary<string, decimal> { ["AccountB"] = 1m }));

            Assert.AreEqual("accountAllocationValues", exception.ParamName);
            StringAssert.Contains("is not a member", exception.Message);
        }

        [Test]
        public void PositionRequiresSymbol()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new BrokerageAccountPosition(null, 1m, 1m));

            Assert.AreEqual("symbol", exception.ParamName);
        }

        [Test]
        public void AccountStateRejectsNullPositionEntries()
        {
            var mapped = Assert.Throws<ArgumentException>(() => new BrokerageAccountState(
                "AccountA",
                Array.Empty<string>(),
                string.Empty,
                null,
                null,
                null,
                null,
                null,
                string.Empty,
                null,
                new BrokerageAccountPosition[] { null }));
            var unmapped = Assert.Throws<ArgumentException>(() => new BrokerageAccountState(
                "AccountA",
                Array.Empty<string>(),
                string.Empty,
                null,
                null,
                null,
                null,
                null,
                string.Empty,
                null,
                Array.Empty<BrokerageAccountPosition>(),
                new BrokerageAccountUnmappedPosition[] { null }));

            Assert.AreEqual("positions", mapped.ParamName);
            Assert.AreEqual("unmappedPositions", unmapped.ParamName);
        }

        [Test]
        public void SnapshotRejectsInvalidGenerationAndMissingReadyTimes()
        {
            var negativeGeneration = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BrokerageAccountSnapshot(
                    BrokerageAccountSnapshotStatus.Unavailable,
                    -1,
                    default,
                    default,
                    null,
                    null,
                    null,
                    string.Empty,
                    string.Empty,
                    string.Empty));
            var missingPublicationTime = Assert.Throws<ArgumentException>(() =>
                new BrokerageAccountSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    1,
                    default,
                    AsOfUtc,
                    null,
                    null,
                    null,
                    string.Empty,
                    string.Empty,
                    string.Empty));
            var missingSuccessfulUpdateTime = Assert.Throws<ArgumentException>(() =>
                new BrokerageAccountSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    1,
                    AsOfUtc,
                    default,
                    null,
                    null,
                    null,
                    string.Empty,
                    string.Empty,
                    string.Empty));

            Assert.AreEqual("generation", negativeGeneration.ParamName);
            Assert.AreEqual("asOfUtc", missingPublicationTime.ParamName);
            Assert.AreEqual("asOfUtc", missingSuccessfulUpdateTime.ParamName);
        }

        [Test]
        public void SnapshotDoesNotEnforceTimestampOrdering()
        {
            Assert.DoesNotThrow(() => new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Stale,
                1,
                AsOfUtc,
                AsOfUtc.AddSeconds(1),
                new Dictionary<string, BrokerageAccountGroup>(),
                new Dictionary<string, BrokerageAccountState>(),
                Array.Empty<string>(),
                string.Empty,
                string.Empty,
                string.Empty,
                collectionStartedUtc: AsOfUtc.AddSeconds(2)));
        }

        [Test]
        public void SnapshotRejectsNullRequiredCollections()
        {
            var groups = Assert.Throws<ArgumentNullException>(() => new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Unavailable,
                0,
                default,
                default,
                null,
                new Dictionary<string, BrokerageAccountState>(),
                Array.Empty<string>(),
                string.Empty,
                string.Empty,
                string.Empty));
            var accounts = Assert.Throws<ArgumentNullException>(() => new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Unavailable,
                0,
                default,
                default,
                new Dictionary<string, BrokerageAccountGroup>(),
                null,
                Array.Empty<string>(),
                string.Empty,
                string.Empty,
                string.Empty));
            var unassigned = Assert.Throws<ArgumentNullException>(() => new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Unavailable,
                0,
                default,
                default,
                new Dictionary<string, BrokerageAccountGroup>(),
                new Dictionary<string, BrokerageAccountState>(),
                null,
                string.Empty,
                string.Empty,
                string.Empty));

            Assert.AreEqual("groups", groups.ParamName);
            Assert.AreEqual("accounts", accounts.ParamName);
            Assert.AreEqual("unassignedAccountIds", unassigned.ParamName);
        }

        [Test]
        public void SnapshotRejectsDuplicateOrNullDictionaryEntries()
        {
            var group = new BrokerageAccountGroup("Group", "Equal", Array.Empty<string>());
            var duplicate = Assert.Throws<ArgumentException>(() => new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Unavailable,
                0,
                default,
                default,
                new Dictionary<string, BrokerageAccountGroup>
                {
                    ["Group"] = group,
                    ["group"] = group
                },
                new Dictionary<string, BrokerageAccountState>(),
                Array.Empty<string>(),
                string.Empty,
                string.Empty,
                string.Empty));
            var nullValue = Assert.Throws<ArgumentException>(() => new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Unavailable,
                0,
                default,
                default,
                new Dictionary<string, BrokerageAccountGroup> { ["Group"] = null },
                new Dictionary<string, BrokerageAccountState>(),
                Array.Empty<string>(),
                string.Empty,
                string.Empty,
                string.Empty));

            Assert.AreEqual("groups", duplicate.ParamName);
            Assert.AreEqual("groups", nullValue.ParamName);
        }

        [Test]
        public void SnapshotLeavesBrokerageTopologyValidationToProvider()
        {
            var selected = new BrokerageAccountGroup(
                "Group",
                "Equal",
                new[] { "Primary" });
            var discovered = new BrokerageAccountGroup(
                "Group",
                "Ratio",
                new[] { "Aggregate" });

            var snapshot = new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                1,
                AsOfUtc,
                AsOfUtc,
                new Dictionary<string, BrokerageAccountGroup> { [selected.Name] = selected },
                new Dictionary<string, BrokerageAccountState>(),
                new[] { "Primary" },
                "membership",
                "configuration",
                string.Empty,
                primaryAccountId: "Primary",
                allGroups: new Dictionary<string, BrokerageAccountGroup>
                {
                    [discovered.Name] = discovered
                },
                accountDirectory: new Dictionary<string, BrokerageAccountDirectoryEntry>
                {
                    ["Primary"] = new(
                        "Primary",
                        BrokerageAccountRelationship.Primary,
                        new[] { "OtherGroup" })
                },
                isComplete: true);

            Assert.IsTrue(snapshot.IsReady);
            Assert.IsTrue(snapshot.IsComplete);
            Assert.AreSame(selected, snapshot.Groups["Group"]);
            Assert.AreSame(discovered, snapshot.AllGroups["Group"]);
        }

        [Test]
        public void SnapshotExposesUnmappedPositionsWithoutLosingAccountState()
        {
            var unmapped = new BrokerageAccountUnmappedPosition(
                "123",
                "UNKNOWN",
                "UNKNOWN",
                "FUT",
                "USD",
                "EXCHANGE",
                "PRIMARY",
                "CLASS",
                "202612",
                0m,
                string.Empty,
                "50",
                2m,
                100m,
                "Model",
                "mapping failed");
            var account = new BrokerageAccountState(
                "AccountA",
                Array.Empty<string>(),
                "Margin",
                1000m,
                100m,
                900m,
                900m,
                2000m,
                "USD",
                new Dictionary<string, decimal> { ["USD"] = 100m },
                Array.Empty<BrokerageAccountPosition>(),
                new[] { unmapped });
            var snapshot = new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                1,
                AsOfUtc,
                AsOfUtc,
                new Dictionary<string, BrokerageAccountGroup>(),
                new Dictionary<string, BrokerageAccountState> { ["AccountA"] = account },
                new[] { "AccountA" },
                "membership",
                "configuration",
                string.Empty,
                managedAccountIds: new[] { "AccountA" },
                accountDirectory: new Dictionary<string, BrokerageAccountDirectoryEntry>
                {
                    ["AccountA"] = new(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        Array.Empty<string>(),
                        "Margin")
                });

            Assert.IsTrue(snapshot.IsReady);
            Assert.IsTrue(snapshot.HasUnmappedPositions);
            Assert.IsTrue(account.HasUnmappedPositions);
            Assert.IsEmpty(account.Positions);
            Assert.AreEqual("123", account.UnmappedPositions[0].BrokerageContractId);
            Assert.AreEqual(2m, account.UnmappedPositions[0].Quantity);
            Assert.AreEqual(1000m, account.NetLiquidation);
        }

        [TestCase(BrokerageAccountSnapshotStatus.Unavailable, false)]
        [TestCase(BrokerageAccountSnapshotStatus.Refreshing, false)]
        [TestCase(BrokerageAccountSnapshotStatus.Ready, true)]
        [TestCase(BrokerageAccountSnapshotStatus.Stale, false)]
        [TestCase(BrokerageAccountSnapshotStatus.Failed, false)]
        public void IsReadyMatchesStatus(BrokerageAccountSnapshotStatus status, bool expected)
        {
            Assert.AreEqual(expected, CreateSnapshot(status).IsReady);
        }

        [TestCase(BrokerageAccountGroupAssignmentStatus.Unavailable, false, true)]
        [TestCase(BrokerageAccountGroupAssignmentStatus.Pending, true, false)]
        [TestCase(BrokerageAccountGroupAssignmentStatus.Succeeded, false, true)]
        [TestCase(BrokerageAccountGroupAssignmentStatus.Failed, false, true)]
        public void AssignmentStatusHelpersAreConsistent(
            BrokerageAccountGroupAssignmentStatus status,
            bool isPending,
            bool isCompleted)
        {
            var result = new BrokerageAccountGroupAssignment(
                status,
                1,
                AsOfUtc,
                "Account",
                "Group",
                new[] { "Previous" },
                new[] { "Result" },
                "expected-membership",
                "resulting-membership",
                "expected-configuration",
                "resulting-configuration",
                string.Empty);

            Assert.AreEqual(isPending, result.IsPending);
            Assert.AreEqual(isCompleted, result.IsCompleted);
            Assert.AreEqual("Account", result.AccountId);
            Assert.AreEqual("Group", result.TargetGroupName);
            CollectionAssert.AreEqual(new[] { "Previous" }, result.PreviousGroupNames);
            CollectionAssert.AreEqual(new[] { "Result" }, result.ResultingGroupNames);
        }

        [TestCase(BrokerageAccountGroupAllocationUpdateStatus.Unavailable, false, true)]
        [TestCase(BrokerageAccountGroupAllocationUpdateStatus.Pending, true, false)]
        [TestCase(BrokerageAccountGroupAllocationUpdateStatus.Succeeded, false, true)]
        [TestCase(BrokerageAccountGroupAllocationUpdateStatus.Failed, false, true)]
        public void AllocationUpdateStatusHelpersAreConsistent(
            BrokerageAccountGroupAllocationUpdateStatus status,
            bool isPending,
            bool isCompleted)
        {
            var requested = new Dictionary<string, decimal> { ["Account"] = 1m };
            var result = new BrokerageAccountGroupAllocationUpdate(
                status,
                1,
                AsOfUtc,
                "Group",
                " Ratio ",
                requested,
                requested,
                "expected-membership",
                "resulting-membership",
                "expected-configuration",
                "resulting-configuration",
                string.Empty);
            requested["Account"] = 2m;

            Assert.AreEqual(isPending, result.IsPending);
            Assert.AreEqual(isCompleted, result.IsCompleted);
            Assert.AreEqual("Group", result.GroupName);
            Assert.AreEqual("Ratio", result.AllocationMethod);
            Assert.AreEqual(1m, result.RequestedAccountAllocationValues["Account"]);
        }

        [Test]
        public void MutationResultsRejectNegativeGeneration()
        {
            var assignment = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BrokerageAccountGroupAssignment(
                    BrokerageAccountGroupAssignmentStatus.Pending,
                    -1,
                    AsOfUtc,
                    "Account",
                    "Group",
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty));
            var allocation = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BrokerageAccountGroupAllocationUpdate(
                    BrokerageAccountGroupAllocationUpdateStatus.Pending,
                    -1,
                    AsOfUtc,
                    "Group",
                    "Ratio",
                    null,
                    null,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty));

            Assert.AreEqual("generation", assignment.ParamName);
            Assert.AreEqual("generation", allocation.ParamName);
        }

        private static BrokerageAccountSnapshot CreateSnapshot(BrokerageAccountSnapshotStatus status)
        {
            return new BrokerageAccountSnapshot(
                status,
                1,
                AsOfUtc,
                status == BrokerageAccountSnapshotStatus.Ready ? AsOfUtc : default,
                new Dictionary<string, BrokerageAccountGroup>(),
                new Dictionary<string, BrokerageAccountState>(),
                Array.Empty<string>(),
                string.Empty,
                string.Empty,
                string.Empty);
        }

    }
}
