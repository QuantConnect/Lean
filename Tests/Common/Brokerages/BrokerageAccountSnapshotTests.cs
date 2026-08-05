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
        public void CompleteSnapshotGraphIsImmutableAndRoundTripsWithoutDataLoss()
        {
            var collectionStartedUtc = AsOfUtc.AddMinutes(-1);
            var lastSuccessfulUpdateUtc = AsOfUtc.AddMinutes(-2);
            var allocationValues = new Dictionary<string, decimal> { ["AccountA"] = 1.5m };
            var group = new BrokerageAccountGroup(
                "Group",
                "Ratio",
                new[] { "AccountA" },
                allocationValues);
            var unmappedPositions = new List<BrokerageAccountUnmappedPosition>
            {
                new(
                    brokerageContractId: "123",
                    brokerageSymbol: "ES",
                    localSymbol: "ESZ6",
                    brokerageSecurityType: "FUT",
                    currency: "USD",
                    exchange: "GLOBEX",
                    primaryExchange: "CME",
                    tradingClass: "ES",
                    expiration: "20261218",
                    strike: 0m,
                    right: string.Empty,
                    multiplier: "50",
                    quantity: -2.5m,
                    averagePrice: 5000.25m,
                    modelCode: "UnmappedModel",
                    errorMessage: "mapping failed",
                    brokerageStrike: "raw-strike",
                    brokerageAverageCost: "raw-cost")
            };
            var account = new BrokerageAccountState(
                "AccountA",
                new[] { "Group" },
                "Margin",
                1000m,
                100m,
                800m,
                700m,
                2000m,
                "USD",
                new Dictionary<string, decimal> { ["USD"] = 100m },
                new[] { new BrokerageAccountPosition(Symbols.SPY, 1.25m, 500m, "Model") },
                unmappedPositions);
            var allGroups = new Dictionary<string, BrokerageAccountGroup> { ["Group"] = group };
            var accountDirectory = new Dictionary<string, BrokerageAccountDirectoryEntry>
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
            };
            var snapshot = new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Failed,
                7,
                AsOfUtc,
                lastSuccessfulUpdateUtc,
                new Dictionary<string, BrokerageAccountGroup> { ["Group"] = group },
                new Dictionary<string, BrokerageAccountState> { ["AccountA"] = account },
                Array.Empty<string>(),
                "membership",
                "configuration",
                "refresh failed",
                "Primary",
                new[] { "Primary", "AccountA" },
                allGroups,
                accountDirectory,
                true,
                collectionStartedUtc);

            allocationValues["AccountA"] = 99m;
            unmappedPositions.Clear();
            allGroups.Clear();
            accountDirectory.Clear();

            var roundTrip = AssertRoundTrips(snapshot);

            Assert.IsNotNull(roundTrip);
            Assert.AreEqual(BrokerageAccountSnapshotStatus.Failed, roundTrip.Status);
            Assert.AreEqual(7, roundTrip.Generation);
            Assert.AreEqual(AsOfUtc, roundTrip.AsOfUtc);
            Assert.AreEqual(lastSuccessfulUpdateUtc, roundTrip.LastSuccessfulUpdateUtc);
            Assert.IsTrue(roundTrip.IsComplete);
            Assert.IsTrue(roundTrip.HasUnmappedPositions);
            Assert.AreEqual(collectionStartedUtc, roundTrip.CollectionStartedUtc);
            Assert.AreEqual("Primary", roundTrip.PrimaryAccountId);
            Assert.AreEqual("membership", roundTrip.MembershipHash);
            Assert.AreEqual("configuration", roundTrip.GroupConfigurationVersion);
            Assert.AreEqual("refresh failed", roundTrip.ErrorMessage);
            CollectionAssert.AreEqual(new[] { "Primary", "AccountA" }, roundTrip.ManagedAccountIds);
            Assert.IsEmpty(roundTrip.UnassignedAccountIds);
            Assert.AreEqual("Ratio", roundTrip.Groups["Group"].AllocationMethod);
            CollectionAssert.AreEqual(new[] { "AccountA" }, roundTrip.Groups["Group"].AccountIds);
            Assert.AreEqual(1.5m, roundTrip.Groups["Group"].AccountAllocationValues["AccountA"]);
            Assert.AreEqual(1, roundTrip.AllGroups.Count);

            var directoryEntry = roundTrip.AccountDirectory["AccountA"];
            Assert.AreEqual(BrokerageAccountRelationship.Managed, directoryEntry.Relationship);
            CollectionAssert.AreEqual(new[] { "Group" }, directoryEntry.GroupNames);
            Assert.AreEqual("Margin", directoryEntry.AccountType);
            Assert.AreEqual("Family", directoryEntry.FamilyCode);
            Assert.AreEqual("Alias", directoryEntry.AccountAlias);

            var roundTripAccount = roundTrip.Accounts["AccountA"];
            Assert.AreEqual("AccountA", roundTripAccount.AccountId);
            CollectionAssert.AreEqual(new[] { "Group" }, roundTripAccount.GroupNames);
            Assert.AreEqual("Margin", roundTripAccount.AccountType);
            CollectionAssert.AreEqual(
                new decimal?[] { 1000m, 100m, 800m, 700m, 2000m },
                new[]
                {
                    roundTripAccount.NetLiquidation,
                    roundTripAccount.TotalCashValue,
                    roundTripAccount.AvailableFunds,
                    roundTripAccount.ExcessLiquidity,
                    roundTripAccount.BuyingPower
                });
            Assert.AreEqual("USD", roundTripAccount.ValuationCurrency);
            Assert.AreEqual(100m, roundTripAccount.CashBalances["USD"]);
            var position = roundTripAccount.Positions[0];
            Assert.AreEqual(Symbols.SPY, position.Symbol);
            Assert.AreEqual(1.25m, position.Quantity);
            Assert.AreEqual(500m, position.AveragePrice);
            Assert.AreEqual("Model", position.ModelCode);

            var unmapped = roundTripAccount.UnmappedPositions[0];
            Assert.AreEqual("123", unmapped.BrokerageContractId);
            Assert.AreEqual("ES", unmapped.BrokerageSymbol);
            Assert.AreEqual("ESZ6", unmapped.LocalSymbol);
            Assert.AreEqual("FUT", unmapped.BrokerageSecurityType);
            Assert.AreEqual("USD", unmapped.Currency);
            Assert.AreEqual("GLOBEX", unmapped.Exchange);
            Assert.AreEqual("CME", unmapped.PrimaryExchange);
            Assert.AreEqual("ES", unmapped.TradingClass);
            Assert.AreEqual("20261218", unmapped.Expiration);
            Assert.AreEqual(0m, unmapped.Strike);
            Assert.AreEqual("raw-strike", unmapped.BrokerageStrike);
            Assert.AreEqual(string.Empty, unmapped.Right);
            Assert.AreEqual("50", unmapped.Multiplier);
            Assert.AreEqual(-2.5m, unmapped.Quantity);
            Assert.AreEqual(5000.25m, unmapped.AveragePrice);
            Assert.AreEqual("raw-cost", unmapped.BrokerageAverageCost);
            Assert.AreEqual("UnmappedModel", unmapped.ModelCode);
            Assert.AreEqual("mapping failed", unmapped.ErrorMessage);

            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, BrokerageAccountGroup>)snapshot.AllGroups).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, BrokerageAccountDirectoryEntry>)snapshot.AccountDirectory).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, decimal>)snapshot.Groups["Group"].AccountAllocationValues).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<BrokerageAccountUnmappedPosition>)snapshot.Accounts["AccountA"].UnmappedPositions).Clear());
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
                    new Dictionary<string, BrokerageAccountGroup>(),
                    new Dictionary<string, BrokerageAccountState>(),
                    Array.Empty<string>(),
                    string.Empty,
                    string.Empty,
                    string.Empty));
            var missingPublicationTime = Assert.Throws<ArgumentException>(() =>
                new BrokerageAccountSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    1,
                    default,
                    AsOfUtc,
                    new Dictionary<string, BrokerageAccountGroup>(),
                    new Dictionary<string, BrokerageAccountState>(),
                    Array.Empty<string>(),
                    string.Empty,
                    string.Empty,
                    string.Empty));
            var missingSuccessfulUpdateTime = Assert.Throws<ArgumentException>(() =>
                new BrokerageAccountSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    1,
                    AsOfUtc,
                    default,
                    new Dictionary<string, BrokerageAccountGroup>(),
                    new Dictionary<string, BrokerageAccountState>(),
                    Array.Empty<string>(),
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
        public void MutationResultsAreImmutableAndRoundTripWithoutDataLoss()
        {
            var previousGroupNames = new List<string> { "Previous" };
            var resultingGroupNames = new List<string> { "Result" };
            var assignment = new BrokerageAccountGroupAssignment(
                BrokerageAccountGroupAssignmentStatus.Succeeded,
                11,
                AsOfUtc,
                "Account",
                "Result",
                previousGroupNames,
                resultingGroupNames,
                "expected-membership",
                "resulting-membership",
                "expected-configuration",
                "resulting-configuration",
                "assignment warning",
                2.5m);
            var requestedAllocations = new Dictionary<string, decimal> { ["Account"] = 1.5m };
            var resultingAllocations = new Dictionary<string, decimal> { ["Account"] = 2.5m };
            var allocation = new BrokerageAccountGroupAllocationUpdate(
                BrokerageAccountGroupAllocationUpdateStatus.Failed,
                12,
                AsOfUtc,
                "Result",
                "Ratio",
                requestedAllocations,
                resultingAllocations,
                "expected-membership",
                "resulting-membership",
                "expected-configuration",
                "resulting-configuration",
                "allocation failed");

            previousGroupNames.Clear();
            resultingGroupNames.Clear();
            requestedAllocations["Account"] = 99m;
            resultingAllocations.Clear();

            var assignmentRoundTrip = AssertRoundTrips(assignment);
            var allocationRoundTrip = AssertRoundTrips(allocation);

            Assert.IsNotNull(assignmentRoundTrip);
            Assert.AreEqual(BrokerageAccountGroupAssignmentStatus.Succeeded, assignmentRoundTrip.Status);
            Assert.AreEqual(11, assignmentRoundTrip.Generation);
            Assert.AreEqual(AsOfUtc, assignmentRoundTrip.AsOfUtc);
            Assert.AreEqual("Account", assignmentRoundTrip.AccountId);
            Assert.AreEqual("Result", assignmentRoundTrip.TargetGroupName);
            Assert.AreEqual(2.5m, assignmentRoundTrip.TargetAllocationValue);
            CollectionAssert.AreEqual(new[] { "Previous" }, assignmentRoundTrip.PreviousGroupNames);
            CollectionAssert.AreEqual(new[] { "Result" }, assignmentRoundTrip.ResultingGroupNames);
            Assert.AreEqual("expected-membership", assignmentRoundTrip.ExpectedMembershipHash);
            Assert.AreEqual("resulting-membership", assignmentRoundTrip.ResultingMembershipHash);
            Assert.AreEqual(
                "expected-configuration",
                assignmentRoundTrip.ExpectedGroupConfigurationVersion);
            Assert.AreEqual(
                "resulting-configuration",
                assignmentRoundTrip.ResultingGroupConfigurationVersion);
            Assert.AreEqual("assignment warning", assignmentRoundTrip.ErrorMessage);

            Assert.IsNotNull(allocationRoundTrip);
            Assert.AreEqual(BrokerageAccountGroupAllocationUpdateStatus.Failed, allocationRoundTrip.Status);
            Assert.AreEqual(12, allocationRoundTrip.Generation);
            Assert.AreEqual(AsOfUtc, allocationRoundTrip.AsOfUtc);
            Assert.AreEqual("Result", allocationRoundTrip.GroupName);
            Assert.AreEqual("Ratio", allocationRoundTrip.AllocationMethod);
            Assert.AreEqual(1.5m, allocationRoundTrip.RequestedAccountAllocationValues["Account"]);
            Assert.AreEqual(2.5m, allocationRoundTrip.ResultingAccountAllocationValues["Account"]);
            Assert.AreEqual("expected-membership", allocationRoundTrip.ExpectedMembershipHash);
            Assert.AreEqual("resulting-membership", allocationRoundTrip.ResultingMembershipHash);
            Assert.AreEqual(
                "expected-configuration",
                allocationRoundTrip.ExpectedGroupConfigurationVersion);
            Assert.AreEqual(
                "resulting-configuration",
                allocationRoundTrip.ResultingGroupConfigurationVersion);
            Assert.AreEqual("allocation failed", allocationRoundTrip.ErrorMessage);

            Assert.Throws<NotSupportedException>(() =>
                ((IList<string>)assignment.PreviousGroupNames).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, decimal>)allocation.ResultingAccountAllocationValues).Clear());
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

        private static T AssertRoundTrips<T>(T value)
        {
            var json = JsonConvert.SerializeObject(value);
            var roundTrip = JsonConvert.DeserializeObject<T>(json);

            Assert.AreEqual(json, JsonConvert.SerializeObject(roundTrip));
            return roundTrip;
        }

    }
}
