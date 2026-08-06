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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class QCAlgorithmBrokerageAccountTests
    {
        private static readonly DateTime AsOfUtc = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        [Test]
        public void UnsupportedBrokerageReturnsUnavailableAndFalse()
        {
            var algorithm = new QCAlgorithm();

            Assert.AreSame(BrokerageAccountSnapshot.Unavailable, algorithm.BrokerageAccountSnapshot);
            Assert.AreSame(BrokerageAccountGroupAssignment.Unavailable, algorithm.BrokerageAccountGroupAssignment);
            Assert.AreSame(
                BrokerageAccountGroupAllocationUpdate.Unavailable,
                algorithm.BrokerageAccountGroupAllocationUpdate);
            Assert.IsFalse(algorithm.RequestBrokerageAccountSnapshotRefresh());
            Assert.IsFalse(algorithm.RequestBrokerageAccountSnapshotRefresh(Array.Empty<string>()));
            algorithm.SetLocked();
            Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAssignment(
                "Account",
                string.Empty,
                null,
                BrokerageAccountSnapshot.Unavailable));
            Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                "Group",
                new Dictionary<string, decimal>(),
                BrokerageAccountSnapshot.Unavailable));
        }

        [Test]
        public void PublicRequestMethodsValidateArgumentsWithoutAProvider()
        {
            var algorithm = new QCAlgorithm();

            Assert.Throws<ArgumentNullException>(() => algorithm.RequestBrokerageAccountSnapshotRefresh(null));
            Assert.Throws<ArgumentException>(() => algorithm.RequestBrokerageAccountSnapshotRefresh(
                new[] { "Group", "group" }));
            Assert.Throws<ArgumentException>(() => algorithm.RequestBrokerageAccountGroupAssignment(
                " ",
                string.Empty,
                null,
                BrokerageAccountSnapshot.Unavailable));
            Assert.Throws<ArgumentNullException>(() => algorithm.RequestBrokerageAccountGroupAssignment(
                "Account",
                null,
                null,
                BrokerageAccountSnapshot.Unavailable));
            Assert.Throws<ArgumentException>(() => algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                " ",
                new Dictionary<string, decimal>(),
                BrokerageAccountSnapshot.Unavailable));
            Assert.Throws<ArgumentNullException>(() => algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                "Group",
                (IReadOnlyDictionary<string, decimal>)null,
                BrokerageAccountSnapshot.Unavailable));
        }

        [Test]
        public void RefreshScopePreservesCallerOrderAndDefensivelyCopies()
        {
            var provider = new TestProvider(BrokerageAccountSnapshotStatus.Ready);
            var algorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(algorithm, provider, provider, provider);
            var requestedGroups = new List<string> { "GroupB", "GroupA" };
            var additionalAccounts = new List<string> { "AccountB", "AccountA" };

            Assert.IsTrue(algorithm.RequestBrokerageAccountSnapshotRefresh(
                requestedGroups,
                additionalAccounts));
            requestedGroups[0] = "Changed";
            additionalAccounts[0] = "Changed";

            CollectionAssert.AreEqual(new[] { "GroupB", "GroupA" }, provider.RequestedGroups);
            CollectionAssert.AreEqual(
                new[] { "AccountB", "AccountA" },
                provider.RequestedAdditionalAccountIds);
        }

        [Test]
        public void AdditionalAccountsRequireAnExplicitGroupScope()
        {
            var provider = new TestProvider(BrokerageAccountSnapshotStatus.Ready);
            var algorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(algorithm, provider, provider, provider);

            var exception = Assert.Throws<ArgumentException>(() =>
                algorithm.RequestBrokerageAccountSnapshotRefresh(
                    Array.Empty<string>(),
                    new[] { "AccountA" }));

            Assert.AreEqual("additionalAccountIds", exception.ParamName);
            Assert.IsNull(provider.RequestedGroups);
            Assert.IsNull(provider.RequestedAdditionalAccountIds);
        }

        [Test]
        public void PublicSetLockedDuringInitializeDoesNotEnableMutations()
        {
            var provider = new TestProvider(BrokerageAccountSnapshotStatus.Ready);
            var algorithm = new PrematurelyLockedAlgorithm();
            InstallBrokerageAccountServices(
                algorithm,
                provider,
                provider,
                provider,
                mutationsReady: false);

            algorithm.Initialize();
            Assert.AreSame(provider.GetAccountSnapshot(), algorithm.BrokerageAccountSnapshot);
            Assert.IsTrue(algorithm.RequestBrokerageAccountSnapshotRefresh());
            Assert.IsFalse(algorithm.GroupAssignmentAccepted);
            Assert.IsFalse(algorithm.GroupAllocationUpdateAccepted);
            Assert.IsFalse(provider.GroupAssignmentRequested);
            Assert.IsFalse(provider.GroupAllocationUpdateRequested);
        }

        [Test]
        public void AllocationIdentifiersAreForwardedVerbatim()
        {
            var provider = new TestProvider(BrokerageAccountSnapshotStatus.Ready);
            var algorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(algorithm, provider, null, provider);
            algorithm.SetLocked();
            const string accountId = "aCcOuNtA";
            var allocations = new Dictionary<string, decimal>
            {
                [accountId] = 1m
            };

            Assert.IsTrue(algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                "Group",
                allocations,
                provider.GetAccountSnapshot()));

            Assert.AreEqual(1, provider.RequestedAllocations.Count);
            CollectionAssert.AreEqual(new[] { accountId }, provider.RequestedAllocations.Keys);
            Assert.AreEqual(1m, provider.RequestedAllocations[accountId]);
        }

        [Test]
        public void AllocationEnumerableRejectsExactDuplicateIdentifiers()
        {
            var provider = new TestProvider(BrokerageAccountSnapshotStatus.Ready);
            var algorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(algorithm, provider, null, provider);
            var allocations = new[]
            {
                new KeyValuePair<string, decimal>("AccountA", 1m),
                new KeyValuePair<string, decimal>("AccountA", 2m)
            };

            var exception = Assert.Throws<ArgumentException>(() =>
                algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                    "Group",
                    allocations,
                    provider.GetAccountSnapshot()));

            Assert.AreEqual("accountAllocationValues", exception.ParamName);
            Assert.IsFalse(provider.GroupAllocationUpdateRequested);
        }

        [Test]
        public void AllocationRequestAcceptsStaticallyTypedMutableDictionary()
        {
            var provider = new TestProvider(BrokerageAccountSnapshotStatus.Ready);
            var algorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(algorithm, provider, null, provider);
            algorithm.SetLocked();
            IDictionary<string, decimal> allocations =
                new Dictionary<string, decimal> { ["AccountA"] = 1m };

            Assert.IsTrue(algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                "Group",
                allocations,
                provider.GetAccountSnapshot()));
            Assert.AreEqual(1m, provider.RequestedAllocations["AccountA"]);
        }

        [Test]
        public void AllocationDictionaryIsCopiedBeforeAsynchronousHandoff()
        {
            var provider = new TestProvider(BrokerageAccountSnapshotStatus.Ready);
            var algorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(algorithm, provider, null, provider);
            algorithm.SetLocked();
            var allocations = new Dictionary<string, decimal> { ["AccountA"] = 1m };

            Assert.IsTrue(algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                "Group",
                (IReadOnlyDictionary<string, decimal>)allocations,
                provider.GetAccountSnapshot()));
            allocations["AccountA"] = 2m;
            allocations["AccountB"] = 3m;

            Assert.AreEqual(1, provider.RequestedAllocations.Count);
            Assert.AreEqual(1m, provider.RequestedAllocations["AccountA"]);
            Assert.IsFalse(provider.RequestedAllocations.ContainsKey("AccountB"));
        }

        [Test]
        public void AllocationEnumerableIsCopiedBeforeAsynchronousHandoff()
        {
            var provider = new TestProvider(BrokerageAccountSnapshotStatus.Ready);
            var algorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(algorithm, provider, null, provider);
            algorithm.SetLocked();
            var allocations = new List<KeyValuePair<string, decimal>>
            {
                new("AccountA", 1m)
            };

            Assert.IsTrue(algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                "Group",
                (IEnumerable<KeyValuePair<string, decimal>>)allocations,
                provider.GetAccountSnapshot()));
            allocations[0] = new KeyValuePair<string, decimal>("AccountA", 2m);
            allocations.Add(new KeyValuePair<string, decimal>("AccountB", 3m));

            Assert.AreEqual(1, provider.RequestedAllocations.Count);
            Assert.AreEqual(1m, provider.RequestedAllocations["AccountA"]);
            Assert.IsFalse(provider.RequestedAllocations.ContainsKey("AccountB"));
        }

        [Test]
        public void MutationOverloadsUseTheCallerObservedSnapshotVersions()
        {
            var provider = new TestProvider(BrokerageAccountSnapshotStatus.Ready);
            var algorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(algorithm, provider, provider, provider);
            algorithm.SetLocked();
            var observedSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                "observed-membership",
                "observed-configuration");

            Assert.IsTrue(algorithm.RequestBrokerageAccountGroupAssignment(
                "AccountA",
                "GroupA",
                2.5m,
                observedSnapshot));
            Assert.AreEqual("AccountA", provider.AssignedAccountId);
            Assert.AreEqual("GroupA", provider.TargetGroupName);
            Assert.AreEqual(2.5m, provider.TargetAllocationValue);
            Assert.AreEqual("observed-membership", provider.ExpectedMembershipHash);
            Assert.AreEqual(
                "observed-configuration",
                provider.ExpectedGroupConfigurationVersion);

            Assert.IsTrue(algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                "GroupA",
                new Dictionary<string, decimal> { ["AccountA"] = 1m },
                observedSnapshot));
            Assert.AreEqual("observed-membership", provider.AllocationExpectedMembershipHash);
            Assert.AreEqual(
                "observed-configuration",
                provider.AllocationExpectedGroupConfigurationVersion);
        }

        [TestCase("", "configuration")]
        [TestCase(" ", "configuration")]
        [TestCase("membership", "")]
        [TestCase("membership", " ")]
        public void MutationsRejectReadySnapshotsWithBlankVersionTokens(
            string membershipHash,
            string groupConfigurationVersion)
        {
            var provider = new TestProvider(BrokerageAccountSnapshotStatus.Ready);
            var algorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(algorithm, provider, provider, provider);
            var observedSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                membershipHash,
                groupConfigurationVersion);

            Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAssignment(
                "Account",
                "Group",
                null,
                observedSnapshot));
            Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                "Group",
                new Dictionary<string, decimal> { ["Account"] = 1m },
                observedSnapshot));
            Assert.IsFalse(provider.GroupAssignmentRequested);
            Assert.IsFalse(provider.GroupAllocationUpdateRequested);
        }

        [TestCase(BrokerageAccountSnapshotStatus.Unavailable)]
        [TestCase(BrokerageAccountSnapshotStatus.Refreshing)]
        [TestCase(BrokerageAccountSnapshotStatus.Stale)]
        [TestCase(BrokerageAccountSnapshotStatus.Failed)]
        public void WriteRequiresReadySnapshotTest(BrokerageAccountSnapshotStatus status)
        {
            var provider = new TestProvider(status);
            var algorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(algorithm, provider, provider, provider);
            algorithm.SetLocked();

            Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAssignment(
                "Account",
                "Group",
                null,
                provider.GetAccountSnapshot()));
            Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                "Group",
                new Dictionary<string, decimal> { ["Account"] = 1m },
                provider.GetAccountSnapshot()));
            Assert.IsFalse(provider.GroupAssignmentRequested);
            Assert.IsFalse(provider.GroupAllocationUpdateRequested);
        }

        [Test]
        public void MutationsRequireTheirProviderAndTheStateProvider()
        {
            var provider = new TestProvider(BrokerageAccountSnapshotStatus.Ready);
            var algorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(algorithm, provider, null, null);
            algorithm.SetLocked();

            Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAssignment(
                "Account",
                "Group",
                null,
                provider.GetAccountSnapshot()));
            Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                "Group",
                new Dictionary<string, decimal> { ["Account"] = 1m },
                provider.GetAccountSnapshot()));

            algorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(algorithm, null, provider, provider);
            algorithm.SetLocked();

            Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAssignment(
                "Account",
                "Group",
                null,
                provider.GetAccountSnapshot()));
            Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                "Group",
                new Dictionary<string, decimal> { ["Account"] = 1m },
                provider.GetAccountSnapshot()));
        }

        [Test]
        public void PartialMutationProvidersOnlyEnableTheirOwnOperation()
        {
            var groupProvider = new TestProvider(BrokerageAccountSnapshotStatus.Ready);
            var groupAlgorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(groupAlgorithm, groupProvider, groupProvider, null);
            groupAlgorithm.SetLocked();

            Assert.IsTrue(groupAlgorithm.RequestBrokerageAccountGroupAssignment(
                "Account",
                "Group",
                null,
                groupProvider.GetAccountSnapshot()));
            Assert.IsFalse(groupAlgorithm.RequestBrokerageAccountGroupAllocationUpdate(
                "Group",
                new Dictionary<string, decimal> { ["Account"] = 1m },
                groupProvider.GetAccountSnapshot()));
            Assert.AreSame(
                BrokerageAccountGroupAllocationUpdate.Unavailable,
                groupAlgorithm.BrokerageAccountGroupAllocationUpdate);

            var allocationProvider = new TestProvider(BrokerageAccountSnapshotStatus.Ready);
            var allocationAlgorithm = new QCAlgorithm();
            InstallBrokerageAccountServices(
                allocationAlgorithm,
                allocationProvider,
                null,
                allocationProvider);
            allocationAlgorithm.SetLocked();

            Assert.IsFalse(allocationAlgorithm.RequestBrokerageAccountGroupAssignment(
                "Account",
                "Group",
                null,
                allocationProvider.GetAccountSnapshot()));
            Assert.IsTrue(allocationAlgorithm.RequestBrokerageAccountGroupAllocationUpdate(
                "Group",
                new Dictionary<string, decimal> { ["Account"] = 1m },
                allocationProvider.GetAccountSnapshot()));
            Assert.AreSame(
                BrokerageAccountGroupAssignment.Unavailable,
                allocationAlgorithm.BrokerageAccountGroupAssignment);
        }

        private static BrokerageAccountSnapshot CreateSnapshot(
            BrokerageAccountSnapshotStatus status,
            string membershipHash = "membership",
            string groupConfigurationVersion = "configuration")
        {
            return new BrokerageAccountSnapshot(
                status,
                1,
                AsOfUtc,
                status == BrokerageAccountSnapshotStatus.Ready ? AsOfUtc : default,
                new Dictionary<string, BrokerageAccountGroup>(),
                new Dictionary<string, BrokerageAccountState>(),
                Array.Empty<string>(),
                membershipHash,
                groupConfigurationVersion,
                string.Empty);
        }

        private static void InstallBrokerageAccountServices(
            QCAlgorithm algorithm,
            IBrokerageAccountStateProvider stateProvider,
            IBrokerageAccountGroupManager groupManager,
            IBrokerageAccountGroupAllocationManager allocationManager,
            bool mutationsReady = true)
        {
            var consumer = (IBrokerageAccountServiceConsumer)algorithm;
            consumer.SetBrokerageAccountStateProvider(stateProvider);
            consumer.SetBrokerageAccountGroupManager(groupManager);
            consumer.SetBrokerageAccountGroupAllocationManager(allocationManager);
            if (mutationsReady)
            {
                algorithm.SetBrokerageAccountMutationServicesReady(true);
            }
        }

        private sealed class PrematurelyLockedAlgorithm : QCAlgorithm
        {
            public bool GroupAssignmentAccepted { get; private set; }
            public bool GroupAllocationUpdateAccepted { get; private set; }

            public override void Initialize()
            {
                SetLocked();
                var observedSnapshot = BrokerageAccountSnapshot;
                GroupAssignmentAccepted = RequestBrokerageAccountGroupAssignment(
                    "Account",
                    "Group",
                    null,
                    observedSnapshot);
                GroupAllocationUpdateAccepted = RequestBrokerageAccountGroupAllocationUpdate(
                    "Group",
                    new Dictionary<string, decimal> { ["Account"] = 1m },
                    observedSnapshot);
            }
        }

        private sealed class TestProvider : IBrokerageAccountStateProvider,
            IBrokerageAccountGroupManager,
            IBrokerageAccountGroupAllocationManager
        {
            private readonly BrokerageAccountSnapshot _snapshot;

            public IReadOnlyCollection<string> RequestedGroups { get; private set; }
            public IReadOnlyCollection<string> RequestedAdditionalAccountIds { get; private set; }
            public string AssignedAccountId { get; private set; }
            public string TargetGroupName { get; private set; }
            public decimal? TargetAllocationValue { get; private set; }
            public string ExpectedMembershipHash { get; private set; }
            public string ExpectedGroupConfigurationVersion { get; private set; }
            public bool GroupAssignmentRequested { get; private set; }
            public IReadOnlyDictionary<string, decimal> RequestedAllocations { get; private set; }
            public string AllocationExpectedMembershipHash { get; private set; }
            public string AllocationExpectedGroupConfigurationVersion { get; private set; }
            public bool GroupAllocationUpdateRequested { get; private set; }

            public TestProvider(BrokerageAccountSnapshotStatus status)
            {
                _snapshot = CreateSnapshot(status);
            }

            public BrokerageAccountSnapshot GetAccountSnapshot()
            {
                return _snapshot;
            }

            public bool RequestAccountSnapshotRefresh(
                IReadOnlyCollection<string> groupNames,
                IReadOnlyCollection<string> additionalAccountIds)
            {
                RequestedGroups = groupNames;
                RequestedAdditionalAccountIds = additionalAccountIds;
                return true;
            }

            public BrokerageAccountGroupAssignment GetAccountGroupAssignment()
            {
                return BrokerageAccountGroupAssignment.Unavailable;
            }

            public bool RequestAccountGroupAssignment(
                string accountId,
                string targetGroupName,
                string expectedMembershipHash,
                string expectedGroupConfigurationVersion,
                decimal? targetAllocationValue = null)
            {
                GroupAssignmentRequested = true;
                AssignedAccountId = accountId;
                TargetGroupName = targetGroupName;
                TargetAllocationValue = targetAllocationValue;
                ExpectedMembershipHash = expectedMembershipHash;
                ExpectedGroupConfigurationVersion = expectedGroupConfigurationVersion;
                return true;
            }

            public BrokerageAccountGroupAllocationUpdate GetAccountGroupAllocationUpdate()
            {
                return BrokerageAccountGroupAllocationUpdate.Unavailable;
            }

            public bool RequestAccountGroupAllocationUpdate(
                string groupName,
                IReadOnlyDictionary<string, decimal> accountAllocationValues,
                string expectedMembershipHash,
                string expectedGroupConfigurationVersion)
            {
                GroupAllocationUpdateRequested = true;
                RequestedAllocations = accountAllocationValues;
                AllocationExpectedMembershipHash = expectedMembershipHash;
                AllocationExpectedGroupConfigurationVersion = expectedGroupConfigurationVersion;
                return true;
            }
        }
    }
}
