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
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.AlgorithmFactory.Python.Wrappers;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Python
{
    [TestFixture, NonParallelizable]
    public class FinancialAdvisorPythonSampleTests
    {
        private const string DemoGroupName = "TestGroupEQ";
        private const string TargetGroupName = "TargetGroup";
        private const string SourceGroupName = "SourceGroup";
        private static readonly DateTime SnapshotTime =
            new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        [Test]
        public void GroupAssignmentScheduledCallbacksAdvanceMutationWithoutOnData()
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateAssignmentSnapshot(
                    2,
                    new[] { TargetGroupName },
                    new[] { TargetGroupName, SourceGroupName })
            };
            using var algorithm = CreateAlgorithm(
                "FinancialAdvisorGroupAssignmentAlgorithm",
                services);

            InvokeScheduledCallback(algorithm);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.RefreshRequestCount);
                CollectionAssert.AreEquivalent(
                    new[] { TargetGroupName, SourceGroupName },
                    services.RequestedGroupHistory.Single());
                Assert.AreEqual(0, services.AssignmentRequestCount);
            });

            services.Snapshot = CreateAssignmentSnapshot(
                3,
                new[] { TargetGroupName, SourceGroupName },
                new[] { TargetGroupName, SourceGroupName },
                SnapshotTime.AddSeconds(6));
            services.ThrowNextAssignmentRequest = true;
            algorithm.SetDateTime(SnapshotTime.AddSeconds(10));

            Assert.DoesNotThrow(() => InvokeScheduledCallback(algorithm));
            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequestCount);
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains("rejected synchronously"));
            });

            algorithm.SetDateTime(SnapshotTime.AddSeconds(15));
            InvokeScheduledCallback(algorithm);
            Assert.AreEqual(
                2,
                services.AssignmentRequestCount,
                "The same snapshot must retry after a synchronous rejection.");

            services.CompleteAssignment(
                BrokerageAccountGroupAssignmentStatus.Succeeded,
                new[] { TargetGroupName });
            services.Snapshot = CreateAssignmentSnapshot(
                4,
                new[] { TargetGroupName, SourceGroupName },
                new[] { TargetGroupName },
                SnapshotTime.AddSeconds(16));
            algorithm.SetDateTime(SnapshotTime.AddSeconds(20));
            InvokeScheduledCallback(algorithm);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    2,
                    services.AssignmentRequestCount,
                    "Completion polling must run without an OnData callback.");
                Assert.That(
                    algorithm.LogMessages,
                    Has.One.Contains("FA assignment succeeded"));
                CollectionAssert.AreEqual(
                    new[] { TargetGroupName },
                    services.RequestedGroupHistory.Last(),
                    "The retained cadence intent should run after mutation confirmation.");
            });
        }

        [TestCase("100", 1)]
        [TestCase("100.01", 0)]
        public void GroupAssignmentValidatesPercentInPython(
            string allocationValue,
            int expectedRequests)
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateAssignmentSnapshot(
                    2,
                    new[] { TargetGroupName, SourceGroupName },
                    new[] { SourceGroupName },
                    allocationMethod: "Percent")
            };
            using var algorithm = CreateAlgorithm(
                "FinancialAdvisorGroupAssignmentAlgorithm",
                services,
                new Dictionary<string, string>
                {
                    ["fa-allocation-value"] = allocationValue
                });

            algorithm.OnData(CreateEmptySlice());

            Assert.AreEqual(
                expectedRequests,
                services.AssignmentRequestCount);
            if (expectedRequests == 0)
            {
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains("no greater than 100"));
            }
        }

        [Test]
        public void DemoInvalidOrderReconcilesBeforeRetryInPython()
        {
            var snapshot = CreateDemoSnapshot(
                2,
                SnapshotTime);
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = snapshot
            };
            using var algorithm = CreateAlgorithm(
                "FinancialAdvisorDemoAlgorithm",
                services);
            algorithm.SetProperty("_pre_order_snapshot", snapshot);
            algorithm.SetProperty("_group_order_submitted", true);
            algorithm.SetProperty("_group_order_id", 41);

            using (Py.GIL())
            {
                algorithm.OnOrderEvent(
                    CreateOrderEvent(
                        41,
                        OrderStatus.Invalid,
                        "distinctive rejection"));
            }

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.RefreshRequestCount);
                CollectionAssert.AreEqual(
                    new[] { DemoGroupName },
                    services.RequestedGroupHistory.Single());
                Assert.AreEqual(
                    snapshot.Generation,
                    algorithm.GetProperty<long>(
                        "_pending_reconcile_generation"));
                Assert.AreEqual(
                    0,
                    algorithm.GetProperty<int>(
                        "_invalid_order_attempt_count"),
                    "Retry eligibility must remain undecided until reconciliation.");
            });

            services.Snapshot = CreateDemoSnapshot(
                3,
                SnapshotTime.AddSeconds(5).AddTicks(1));
            algorithm.SetDateTime(SnapshotTime.AddSeconds(10));
            InvokeScheduledCallback(algorithm);

            using var preOrderSnapshot =
                algorithm.GetProperty("_pre_order_snapshot");
            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    -1,
                    algorithm.GetProperty<long>(
                        "_pending_reconcile_generation"));
                Assert.AreEqual(
                    1,
                    algorithm.GetProperty<int>(
                        "_invalid_order_attempt_count"));
                Assert.IsTrue(preOrderSnapshot.IsNone());
                Assert.AreEqual(
                    2,
                    algorithm.LogMessages.Count(
                        message => message.Contains("FA reconciliation")));
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains("distinctive rejection"));
            });
        }

        [Test]
        public void DemoRejectsOldSnapshotAndBoundsEmptyTicketRetriesInPython()
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateDemoSnapshot(
                    2,
                    SnapshotTime.AddMinutes(-6))
            };
            using var algorithm = CreateEmptyTicketDemoAlgorithm(services);

            algorithm.OnData(CreateEmptySlice());
            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    0,
                    algorithm.GetProperty<int>("submission_count"),
                    "A Ready but over-age snapshot must not authorize an order.");
                Assert.AreEqual(1, services.RefreshRequestCount);
            });

            services.Snapshot = CreateDemoSnapshot(
                3,
                SnapshotTime.AddSeconds(5).AddTicks(1));
            algorithm.SetDateTime(SnapshotTime.AddSeconds(10));
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(
                1,
                algorithm.GetProperty<int>("submission_count"));

            algorithm.SetDateTime(SnapshotTime.AddSeconds(14));
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(
                1,
                algorithm.GetProperty<int>("submission_count"),
                "An empty-ticket retry must respect the bounded delay.");

            algorithm.SetDateTime(SnapshotTime.AddSeconds(15));
            algorithm.OnData(CreateEmptySlice());
            algorithm.SetDateTime(SnapshotTime.AddSeconds(20));
            algorithm.OnData(CreateEmptySlice());
            algorithm.SetDateTime(SnapshotTime.AddSeconds(25));
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    3,
                    algorithm.GetProperty<int>("submission_count"),
                    "SetHoldings returning no tickets must stop at the retry limit.");
                Assert.AreEqual(
                    3,
                    algorithm.GetProperty<int>(
                        "_empty_order_attempt_count"));
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains(
                        "returned no Financial Advisor parent order ticket"));
            });
        }

        private static AlgorithmPythonWrapper CreateEmptyTicketDemoAlgorithm(
            TestFinancialAdvisorServices services)
        {
            var moduleName =
                $"FinancialAdvisorDemoAlgorithmTest_{Guid.NewGuid():N}";
            var source = @"
from FinancialAdvisorDemoAlgorithm import FinancialAdvisorDemoAlgorithm

class FinancialAdvisorDemoAlgorithmUnderTest(FinancialAdvisorDemoAlgorithm):
    def initialize(self):
        self.submission_count = 0
        super().initialize()

    def set_holdings(self, *args, **kwargs):
        self.submission_count += 1
        return []
";
            using (Py.GIL())
            {
                using var module = PyModule.FromString(
                    moduleName,
                    source);
            }
            return CreateAlgorithm(
                moduleName,
                services);
        }

        private static AlgorithmPythonWrapper CreateAlgorithm(
            string moduleName,
            TestFinancialAdvisorServices services,
            Dictionary<string, string> parameters = null)
        {
            var algorithm = new AlgorithmPythonWrapper(moduleName);
            algorithm.BaseAlgorithm.SubscriptionManager.SetDataManager(
                new DataManagerStub(algorithm.BaseAlgorithm));
            algorithm.SetLiveMode(true);
            algorithm.SetDateTime(SnapshotTime);
            algorithm.SetParameters(
                parameters ?? new Dictionary<string, string>());
            var consumer =
                (IBrokerageAccountServiceConsumer)algorithm;
            consumer.SetBrokerageAccountStateProvider(services);
            consumer.SetBrokerageAccountGroupManager(services);

            services.AcceptRefreshRequests = false;
            algorithm.Initialize();
            algorithm.SetLocked();
            algorithm.BaseAlgorithm
                .SetBrokerageAccountMutationServicesReady();
            services.AcceptRefreshRequests = true;
            services.ResetRefreshRequests();
            algorithm.SetDateTime(SnapshotTime.AddSeconds(5));
            algorithm.SetProperty(
                "_initial_snapshot_refresh_accepted",
                true);
            algorithm.SetProperty(
                "_initial_snapshot_request_generation",
                services.Snapshot.Generation - 1);
            return algorithm;
        }

        private static void InvokeScheduledCallback(
            AlgorithmPythonWrapper algorithm)
        {
            algorithm.InvokeVoidMethod(
                "_request_scheduled_snapshot_refresh");
        }

        private static Slice CreateEmptySlice()
        {
            return new Slice(
                SnapshotTime,
                Array.Empty<BaseData>(),
                SnapshotTime);
        }

        private static OrderEvent CreateOrderEvent(
            int orderId,
            OrderStatus status,
            string message)
        {
            return new OrderEvent(
                orderId,
                Symbols.SPY,
                SnapshotTime.AddSeconds(5),
                status,
                OrderDirection.Buy,
                0m,
                0m,
                OrderFee.Zero)
            {
                Message = message
            };
        }

        private static BrokerageAccountSnapshot CreateDemoSnapshot(
            long generation,
            DateTime collectionStartedUtc)
        {
            var group = new BrokerageAccountGroup(
                DemoGroupName,
                "Equal",
                new[] { "AccountA", "AccountB" });
            var groups =
                new Dictionary<string, BrokerageAccountGroup>
                {
                    [group.Name] = group
                };
            var accounts =
                new Dictionary<string, BrokerageAccountState>
                {
                    ["AccountA"] = CreateAccount(
                        "AccountA",
                        new[] { group.Name },
                        10m),
                    ["AccountB"] = CreateAccount(
                        "AccountB",
                        new[] { group.Name },
                        20m)
                };

            return new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                generation,
                SnapshotTime.AddTicks(generation),
                SnapshotTime.AddTicks(generation),
                groups,
                accounts,
                Array.Empty<string>(),
                $"membership-{generation}",
                $"configuration-{generation}",
                string.Empty,
                managedAccountIds: accounts.Keys.ToArray(),
                collectionStartedUtc: collectionStartedUtc);
        }

        private static BrokerageAccountSnapshot CreateAssignmentSnapshot(
            long generation,
            IReadOnlyCollection<string> selectedGroupNames,
            IReadOnlyCollection<string> accountGroupNames,
            DateTime? collectionStartedUtc = null,
            string allocationMethod = "Equal")
        {
            var targetGroup = new BrokerageAccountGroup(
                TargetGroupName,
                allocationMethod,
                accountGroupNames.Contains(
                    TargetGroupName,
                    StringComparer.OrdinalIgnoreCase)
                    ? new[] { "AccountA" }
                    : Array.Empty<string>());
            var sourceGroup = new BrokerageAccountGroup(
                SourceGroupName,
                "Equal",
                accountGroupNames.Contains(
                    SourceGroupName,
                    StringComparer.OrdinalIgnoreCase)
                    ? new[] { "AccountA" }
                    : Array.Empty<string>());
            var allGroups =
                new Dictionary<string, BrokerageAccountGroup>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    [targetGroup.Name] = targetGroup,
                    [sourceGroup.Name] = sourceGroup
                };
            var groups = allGroups
                .Where(pair => selectedGroupNames.Contains(
                    pair.Key,
                    StringComparer.OrdinalIgnoreCase))
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.OrdinalIgnoreCase);
            var directory =
                new Dictionary<string, BrokerageAccountDirectoryEntry>
                {
                    ["AccountA"] =
                        new BrokerageAccountDirectoryEntry(
                            "AccountA",
                            BrokerageAccountRelationship.Managed,
                            accountGroupNames,
                            "INDIVIDUAL",
                            string.Empty,
                            "MOVE-East")
                };
            var accounts =
                new Dictionary<string, BrokerageAccountState>
                {
                    ["AccountA"] = CreateAccount(
                        "AccountA",
                        accountGroupNames,
                        0m)
                };
            var timestamp =
                collectionStartedUtc ?? SnapshotTime;

            return new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                generation,
                timestamp,
                timestamp,
                groups,
                accounts,
                Array.Empty<string>(),
                $"membership-{generation}",
                $"configuration-{generation}",
                string.Empty,
                managedAccountIds: new[] { "AccountA" },
                allGroups: allGroups,
                accountDirectory: directory,
                isComplete: true,
                collectionStartedUtc: timestamp);
        }

        private static BrokerageAccountState CreateAccount(
            string accountId,
            IReadOnlyCollection<string> groupNames,
            decimal quantity)
        {
            return new BrokerageAccountState(
                accountId,
                groupNames,
                "INDIVIDUAL",
                1000m,
                100m,
                null,
                null,
                null,
                "USD",
                new Dictionary<string, decimal>(),
                new[]
                {
                    new BrokerageAccountPosition(
                        Symbols.SPY,
                        quantity,
                        100m)
                });
        }

        private sealed class TestFinancialAdvisorServices :
            IBrokerageAccountStateProvider,
            IBrokerageAccountGroupManager
        {
            private long _assignmentGeneration;

            public BrokerageAccountSnapshot Snapshot { get; set; }
            public BrokerageAccountGroupAssignment Assignment { get; private set; } =
                BrokerageAccountGroupAssignment.Unavailable;
            public bool AcceptRefreshRequests { get; set; } = true;
            public bool ThrowNextAssignmentRequest { get; set; }
            public int RefreshRequestCount { get; private set; }
            public int AssignmentRequestCount { get; private set; }
            public List<IReadOnlyCollection<string>> RequestedGroupHistory { get; } =
                new();

            public BrokerageAccountSnapshot GetAccountSnapshot()
            {
                return Snapshot;
            }

            public bool RequestAccountSnapshotRefresh(
                IReadOnlyCollection<string> groupNames,
                IReadOnlyCollection<string> additionalAccountIds)
            {
                ++RefreshRequestCount;
                RequestedGroupHistory.Add(groupNames.ToArray());
                return AcceptRefreshRequests;
            }

            public BrokerageAccountGroupAssignment GetAccountGroupAssignment()
            {
                return Assignment;
            }

            public bool RequestAccountGroupAssignment(
                string accountId,
                string targetGroupName,
                string expectedMembershipHash,
                string expectedGroupConfigurationVersion,
                decimal? targetAllocationValue = null)
            {
                ++AssignmentRequestCount;
                if (ThrowNextAssignmentRequest)
                {
                    ThrowNextAssignmentRequest = false;
                    throw new InvalidOperationException(
                        "distinctive synchronous rejection");
                }

                Assignment = new BrokerageAccountGroupAssignment(
                    BrokerageAccountGroupAssignmentStatus.Pending,
                    ++_assignmentGeneration,
                    SnapshotTime,
                    accountId,
                    targetGroupName,
                    Snapshot.AccountDirectory[accountId].GroupNames,
                    Array.Empty<string>(),
                    expectedMembershipHash,
                    string.Empty,
                    expectedGroupConfigurationVersion,
                    string.Empty,
                    string.Empty,
                    targetAllocationValue);
                return true;
            }

            public void CompleteAssignment(
                BrokerageAccountGroupAssignmentStatus status,
                IReadOnlyCollection<string> resultingGroupNames)
            {
                Assignment = new BrokerageAccountGroupAssignment(
                    status,
                    Assignment.Generation,
                    SnapshotTime,
                    Assignment.AccountId,
                    Assignment.TargetGroupName,
                    Assignment.PreviousGroupNames,
                    resultingGroupNames,
                    Assignment.ExpectedMembershipHash,
                    $"resulting-membership-{Assignment.Generation}",
                    Assignment.ExpectedGroupConfigurationVersion,
                    $"resulting-configuration-{Assignment.Generation}",
                    string.Empty,
                    Assignment.TargetAllocationValue);
            }

            public void ResetRefreshRequests()
            {
                RefreshRequestCount = 0;
                RequestedGroupHistory.Clear();
            }
        }
    }
}
