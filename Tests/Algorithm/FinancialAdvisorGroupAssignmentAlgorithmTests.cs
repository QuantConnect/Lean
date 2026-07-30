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
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class FinancialAdvisorGroupAssignmentAlgorithmTests
    {
        private static readonly DateTime SnapshotTime =
            new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        [Test]
        public void SelectsOnlyManagedAliasMatchAndWaitsForCompletion()
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateSnapshot(
                    1,
                    new BrokerageAccountGroup(
                        "TargetGroup",
                        "Equal",
                        new[] { "AccountC" }),
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "MOVE-East"),
                    Entry(
                        "AccountB",
                        BrokerageAccountRelationship.Managed,
                        "Hold-West"),
                    Entry(
                        "AccountC",
                        BrokerageAccountRelationship.Managed,
                        "move-South",
                        "targetgroup"),
                    Entry(
                        "Master",
                        BrokerageAccountRelationship.Primary,
                        "MOVE-Primary"))
            };
            var algorithm = CreateAlgorithm(
                services,
                new Dictionary<string, string>
                {
                    ["fa-target-group"] = "targetgroup"
                });

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequests.Count);
                Assert.AreEqual(
                    "AccountA",
                    services.AssignmentRequests[0].AccountId);
                Assert.AreEqual(
                    "TargetGroup",
                    services.AssignmentRequests[0].TargetGroupName);
                Assert.IsNull(
                    services.AssignmentRequests[0].TargetAllocationValue);
                Assert.AreEqual(
                    "membership-1",
                    services.AssignmentRequests[0].MembershipHash);
                Assert.AreEqual(
                    "configuration-1",
                    services.AssignmentRequests[0].ConfigurationVersion);
            });

            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(
                1,
                services.AssignmentRequests.Count,
                "A Pending assignment must serialize later mutations.");

            services.CompleteAssignment(
                BrokerageAccountGroupAssignmentStatus.Succeeded,
                new[] { "TargetGroup" });
            algorithm.OnData(CreateEmptySlice());

            services.Snapshot = CreateSnapshot(
                2,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    new[] { "AccountA", "AccountC" }),
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "MOVE-East",
                    "TargetGroup"),
                Entry(
                    "AccountB",
                    BrokerageAccountRelationship.Managed,
                    "Hold-West"),
                Entry(
                    "AccountC",
                    BrokerageAccountRelationship.Managed,
                    "move-South",
                    "TargetGroup"),
                Entry(
                    "Master",
                    BrokerageAccountRelationship.Primary,
                    "MOVE-Primary"));
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequests.Count);
                Assert.That(
                    algorithm.LogMessages,
                    Has.One.Contains(
                        "resulting groups=[TargetGroup]"));
            });
        }

        [Test]
        public void ScheduledRefreshPolicyUsesTwoScopedTicksThenOneCompleteTick()
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateSnapshot(
                    1,
                    new BrokerageAccountGroup(
                        "TargetGroup",
                        "Equal",
                        new[] { "AccountA" }),
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "Hold-East",
                        "TargetGroup"))
            };
            var algorithm = CreateAlgorithm(services);
            services.ResetRefreshRequests();

            algorithm.SetDateTime(SnapshotTime.AddMinutes(1));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");
            Assert.AreEqual(
                1,
                services.RefreshRequestCount,
                "The scheduled callback must request directly.");
            services.Snapshot = CreateSnapshot(
                2,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    new[] { "AccountA" }),
                false,
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "Hold-East",
                    "TargetGroup"));

            algorithm.SetDateTime(SnapshotTime.AddMinutes(2));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");
            services.Snapshot = CreateSnapshot(
                3,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    new[] { "AccountA" }),
                false,
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "Hold-East",
                    "TargetGroup"));

            algorithm.SetDateTime(SnapshotTime.AddMinutes(3));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(3, services.RefreshRequestCount);
                Assert.AreEqual(3, services.RequestedGroupHistory.Count);
                CollectionAssert.AreEqual(
                    new[] { "TargetGroup" },
                    services.RequestedGroupHistory[0]);
                CollectionAssert.AreEqual(
                    new[] { "TargetGroup" },
                    services.RequestedGroupHistory[1]);
                CollectionAssert.IsEmpty(
                    services.RequestedGroupHistory[2],
                    "The third topology tick must issue one complete request.");
            });
        }

        [Test]
        public void RefreshCadenceIsNotPhaseLockedToMinuteData()
        {
            var field =
                typeof(FinancialAdvisorGroupAssignmentAlgorithm).GetField(
                    "TopologyRefreshInterval",
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.NonPublic);

            Assert.IsNotNull(field);
            var interval = (TimeSpan)field.GetValue(null);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(TimeSpan.FromSeconds(90), interval);
                Assert.AreNotEqual(
                    0,
                    interval.Ticks %
                        TimeSpan.FromMinutes(1).Ticks);
            });
        }

        [Test]
        public void EmptyRemovalTopologyStillReachesThirdTickCompleteDiscovery()
        {
            var emptyGroups =
                new Dictionary<string, BrokerageAccountGroup>();
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateSnapshot(
                    1,
                    emptyGroups,
                    emptyGroups,
                    true,
                    SnapshotTime,
                    Array.Empty<AccountEntry>())
            };
            var algorithm = CreateAlgorithm(
                services,
                new Dictionary<string, string>
                {
                    ["fa-target-group"] = string.Empty
                });

            for (var tick = 1; tick <= 3; ++tick)
            {
                algorithm.SetDateTime(
                    SnapshotTime.AddSeconds(90 * tick));
                InvokePrivateMethod(
                    algorithm,
                    "RequestScheduledSnapshotRefresh");
            }

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    1,
                    services.RefreshRequestCount,
                    "Empty topology ticks must advance until the third tick " +
                    "issues complete discovery.");
                CollectionAssert.IsEmpty(
                    services.RequestedGroupHistory.Single());
                Assert.AreEqual(
                    0,
                    GetPrivateField<int>(
                        algorithm,
                        "_scheduledRefreshTopologyTicks"));
            });
        }

        [Test]
        public void ConcurrentScheduledCallbackPreservesOneLaterRefreshIntent()
        {
            var firstSnapshot = CreateSnapshot(
                1,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    new[] { "AccountA" }),
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "Hold-East",
                    "TargetGroup"));
            var secondSnapshot = CreateSnapshot(
                2,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    new[] { "AccountA" }),
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "Hold-East",
                    "TargetGroup"));
            var thirdSnapshot = CreateSnapshot(
                3,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    new[] { "AccountA" }),
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "Hold-East",
                    "TargetGroup"));
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = firstSnapshot
            };
            var algorithm = CreateAlgorithm(services);
            services.ResetRefreshRequests();
            using var requestEntered = new ManualResetEventSlim();
            using var releaseRequest = new ManualResetEventSlim();
            services.RefreshRequestHook = stateProvider =>
            {
                if (stateProvider.RefreshRequestCount == 1)
                {
                    requestEntered.Set();
                    Assert.IsTrue(
                        releaseRequest.Wait(TimeSpan.FromSeconds(10)));
                    stateProvider.Snapshot = secondSnapshot;
                }
                else
                {
                    stateProvider.Snapshot = thirdSnapshot;
                }
            };

            var firstCallback = Task.Run(
                () => InvokePrivateMethod(
                    algorithm,
                    "RequestScheduledSnapshotRefresh"));
            Assert.IsTrue(
                requestEntered.Wait(TimeSpan.FromSeconds(10)));
            var secondCallback = Task.Run(
                () => InvokePrivateMethod(
                    algorithm,
                    "RequestScheduledSnapshotRefresh"));
            releaseRequest.Set();
            Assert.IsTrue(
                firstCallback.Wait(TimeSpan.FromSeconds(10)));
            Assert.IsTrue(
                secondCallback.Wait(TimeSpan.FromSeconds(10)));

            algorithm.SetDateTime(
                SnapshotTime.AddSeconds(5));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");

            Assert.AreEqual(
                2,
                services.RefreshRequestCount,
                "The concurrent callback must survive the retry gate and be " +
                "consumed by a later callback without OnData.");
        }

        [Test]
        public void LiveCallbacksRefreshWithoutOnData()
        {
            BrokerageAccountSnapshot CreateReadySnapshot(long generation) =>
                CreateSnapshot(
                    generation,
                    new BrokerageAccountGroup(
                        "TargetGroup",
                        "Equal",
                        new[] { "AccountA" }),
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "Hold-East",
                        "TargetGroup"));

            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateReadySnapshot(1)
            };
            services.RefreshRequestHook = stateProvider =>
                stateProvider.Snapshot = CreateReadySnapshot(
                    stateProvider.Snapshot.Generation + 1);
            var algorithm =
                new FinancialAdvisorGroupAssignmentAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(
                new DataManagerStub(algorithm));
            algorithm.SetLiveMode(true);
            algorithm.SetDateTime(SnapshotTime);
            algorithm.SetParameters(
                new Dictionary<string, string>());
            var consumer =
                (IBrokerageAccountServiceConsumer)algorithm;
            consumer.SetBrokerageAccountStateProvider(services);
            consumer.SetBrokerageAccountGroupManager(services);

            algorithm.Initialize();
            algorithm.SetLocked();
            algorithm.SetBrokerageAccountMutationServicesReady();

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    1,
                    services.RefreshRequestCount,
                    "Initialize must issue complete discovery.");
                CollectionAssert.IsEmpty(
                    services.RequestedGroupHistory[0]);
            });

            for (var tick = 1; tick <= 3; ++tick)
            {
                algorithm.SetDateTime(
                    SnapshotTime.AddMinutes(tick));
                InvokePrivateMethod(
                    algorithm,
                    "RequestScheduledSnapshotRefresh");
            }

            Assert.Multiple(() =>
            {
                Assert.AreEqual(4, services.RefreshRequestCount);
                CollectionAssert.AreEqual(
                    new[] { "TargetGroup" },
                    services.RequestedGroupHistory[1]);
                CollectionAssert.AreEqual(
                    new[] { "TargetGroup" },
                    services.RequestedGroupHistory[2]);
                CollectionAssert.IsEmpty(
                    services.RequestedGroupHistory[3],
                    "The third callback must request complete state.");
                Assert.AreEqual(
                    0,
                    services.AssignmentRequests.Count,
                    "No OnData call was made to enter mutation processing.");
            });
        }

        [Test]
        public void ScheduledCallbacksCompleteAssignmentWithoutOnData()
        {
            var initialSnapshot = CreateSnapshot(
                1,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    Array.Empty<string>()),
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "MOVE-East"));
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = initialSnapshot
            };
            var algorithm = CreateAlgorithm(services);

            algorithm.SetDateTime(
                SnapshotTime.AddSeconds(90));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");
            services.CompleteAssignment(
                BrokerageAccountGroupAssignmentStatus.Succeeded,
                new[] { "TargetGroup" });

            algorithm.SetDateTime(
                SnapshotTime.AddSeconds(180));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");

            services.RefreshRequestHook = stateProvider =>
                stateProvider.Snapshot = CreateSnapshot(
                    2,
                    new BrokerageAccountGroup(
                        "TargetGroup",
                        "Equal",
                        new[] { "AccountA" }),
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "MOVE-East",
                        "TargetGroup"));
            algorithm.SetDateTime(
                SnapshotTime.AddSeconds(270));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");
            algorithm.SetDateTime(
                SnapshotTime.AddSeconds(360));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequests.Count);
                Assert.That(
                    algorithm.LogMessages,
                    Has.One.Contains("FA assignment succeeded"));
                Assert.AreEqual(
                    -1,
                    GetPrivateField<long>(
                        algorithm,
                        "_minimumReadyGeneration"));
                CollectionAssert.IsEmpty(
                    services.RequestedGroupHistory[0],
                    "Mutation completion must request a confirming snapshot.");
            });
        }

        [Test]
        public void ScheduledCallbacksProcessCashConfirmationWithoutOnData()
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateSnapshot(
                    10,
                    new BrokerageAccountGroup(
                        "TargetGroup",
                        "Equal",
                        new[] { "AccountA" }),
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "MOVE-East",
                        "TargetGroup",
                        totalCashValue: 100m,
                        netLiquidation: 1000m))
            };
            var algorithm = CreateAlgorithm(
                services,
                new Dictionary<string, string>
                {
                    ["fa-cash-change-threshold"] = "1000"
                });

            algorithm.SetDateTime(
                SnapshotTime.AddSeconds(90));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");
            services.ResetRefreshRequests();

            services.Snapshot = CreateSnapshot(
                11,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    Array.Empty<string>()),
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "MOVE-East",
                    totalCashValue: 1200m,
                    netLiquidation: 1000m));
            algorithm.SetDateTime(
                SnapshotTime.AddSeconds(180));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");

            services.Snapshot = CreateSnapshot(
                12,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    Array.Empty<string>()),
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "MOVE-East",
                    totalCashValue: 1200m,
                    netLiquidation: 1000m));
            algorithm.SetDateTime(
                SnapshotTime.AddSeconds(270));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.RefreshRequestCount);
                CollectionAssert.IsEmpty(
                    services.RequestedGroupHistory[0],
                    "Cash changes require complete confirmation.");
                Assert.AreEqual(1, services.AssignmentRequests.Count);
                Assert.That(
                    algorithm.LogMessages,
                    Has.One.Contains(
                        "cash-change confirmation is Ready at generation 12"));
            });
        }

        [Test]
        public void RejectedInitialReadySnapshotRetriesCompleteWithoutOnData()
        {
            BrokerageAccountSnapshot CreateReadySnapshot(long generation) =>
                CreateSnapshot(
                    generation,
                    new BrokerageAccountGroup(
                        "TargetGroup",
                        "Equal",
                        new[] { "AccountA" }),
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "Hold-East",
                        "TargetGroup"));

            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateReadySnapshot(1),
                AcceptRefreshRequests = false
            };
            var algorithm =
                new FinancialAdvisorGroupAssignmentAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(
                new DataManagerStub(algorithm));
            algorithm.SetLiveMode(true);
            algorithm.SetDateTime(SnapshotTime);
            algorithm.SetParameters(
                new Dictionary<string, string>());
            var consumer =
                (IBrokerageAccountServiceConsumer)algorithm;
            consumer.SetBrokerageAccountStateProvider(services);
            consumer.SetBrokerageAccountGroupManager(services);

            algorithm.Initialize();
            algorithm.SetLocked();
            algorithm.SetBrokerageAccountMutationServicesReady();

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.RefreshRequestCount);
                CollectionAssert.IsEmpty(
                    services.RequestedGroupHistory[0],
                    "Initialize must attempt complete discovery.");
            });

            services.AcceptRefreshRequests = true;
            services.RefreshRequestHook = stateProvider =>
                stateProvider.Snapshot = CreateReadySnapshot(
                    stateProvider.Snapshot.Generation + 1);
            algorithm.SetDateTime(
                SnapshotTime.AddSeconds(5));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");

            for (var tick = 1; tick <= 3; ++tick)
            {
                algorithm.SetDateTime(
                    SnapshotTime.AddMinutes(tick));
                InvokePrivateMethod(
                    algorithm,
                    "RequestScheduledSnapshotRefresh");
            }

            Assert.Multiple(() =>
            {
                Assert.AreEqual(5, services.RefreshRequestCount);
                CollectionAssert.IsEmpty(
                    services.RequestedGroupHistory[1],
                    "The rejected initial request must retry complete discovery.");
                CollectionAssert.AreEqual(
                    new[] { "TargetGroup" },
                    services.RequestedGroupHistory[2]);
                CollectionAssert.AreEqual(
                    new[] { "TargetGroup" },
                    services.RequestedGroupHistory[3]);
                CollectionAssert.IsEmpty(
                    services.RequestedGroupHistory[4],
                    "Initial retry must not advance the topology cadence.");
                Assert.AreEqual(
                    0,
                    services.AssignmentRequests.Count,
                    "No OnData call was made.");
            });
        }

        [Test]
        public void ScopedReadyTopologyCanDriveAssignment()
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateSnapshot(
                    2,
                    new BrokerageAccountGroup(
                        "TargetGroup",
                        "Equal",
                        Array.Empty<string>()),
                    false,
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "MOVE-East"))
            };
            var algorithm = CreateAlgorithm(services);

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequests.Count);
                Assert.AreEqual(
                    "AccountA",
                    services.AssignmentRequests[0].AccountId);
                Assert.AreEqual(
                    "membership-2",
                    services.AssignmentRequests[0].MembershipHash);
            });
        }

        [Test]
        public void CrossGroupCandidateRefreshesDestinationAndEverySource()
        {
            var targetGroup = new BrokerageAccountGroup(
                "TargetGroup",
                "Equal",
                Array.Empty<string>());
            var sourceGroup = new BrokerageAccountGroup(
                "SourceGroup",
                "Equal",
                new[] { "AccountA" });
            var selectedGroups =
                new Dictionary<string, BrokerageAccountGroup>
                {
                    [targetGroup.Name] = targetGroup
                };
            var allGroups =
                new Dictionary<string, BrokerageAccountGroup>
                {
                    [targetGroup.Name] = targetGroup,
                    [sourceGroup.Name] = sourceGroup
                };
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateSnapshot(
                    1,
                    selectedGroups,
                    allGroups,
                    false,
                    SnapshotTime,
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "MOVE-East",
                        new[] { "SourceGroup" }))
            };
            var algorithm = CreateAlgorithm(services);

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, services.AssignmentRequests.Count);
                Assert.AreEqual(1, services.RefreshRequestCount);
                CollectionAssert.AreEqual(
                    new[] { "TargetGroup", "SourceGroup" },
                    services.RequestedGroupHistory[0]);
            });

            services.Snapshot = CreateSnapshot(
                2,
                allGroups,
                allGroups,
                false,
                SnapshotTime.AddTicks(2),
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "MOVE-East",
                    new[] { "SourceGroup" }));
            algorithm.SetDateTime(
                SnapshotTime.AddSeconds(5));
            algorithm.OnData(CreateEmptySlice());

            Assert.AreEqual(1, services.AssignmentRequests.Count);
        }

        [Test]
        public void CandidateAlreadyInTargetAndAnotherGroupMovesToExactlyTarget()
        {
            var targetGroup = new BrokerageAccountGroup(
                "TargetGroup",
                "Equal",
                new[] { "AccountA" });
            var sourceGroup = new BrokerageAccountGroup(
                "SourceGroup",
                "Equal",
                new[] { "AccountA" });
            var groups =
                new Dictionary<string, BrokerageAccountGroup>
                {
                    [targetGroup.Name] = targetGroup,
                    [sourceGroup.Name] = sourceGroup
                };
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateSnapshot(
                    1,
                    groups,
                    groups,
                    true,
                    SnapshotTime,
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "MOVE-East",
                        new[] { "TargetGroup", "SourceGroup" }))
            };
            var algorithm = CreateAlgorithm(services);

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequests.Count);
                Assert.AreEqual(
                    "TargetGroup",
                    services.AssignmentRequests[0].TargetGroupName);
            });
        }

        [Test]
        public void SynchronousAssignmentRejectionIsLoggedAndRetried()
        {
            BrokerageAccountSnapshot Snapshot(long generation) =>
                CreateSnapshot(
                    generation,
                    new BrokerageAccountGroup(
                        "TargetGroup",
                        "Equal",
                        Array.Empty<string>()),
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "MOVE-East"));
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = Snapshot(1)
            };
            services.EnqueueAssignmentException(
                new InvalidOperationException(
                    "source group was outside the selected scope"));
            var algorithm = CreateAlgorithm(services);

            Assert.DoesNotThrow(
                () => algorithm.OnData(CreateEmptySlice()));
            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, services.AssignmentRequests.Count);
                Assert.AreEqual(1, services.RefreshRequestCount);
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains(
                        "source group was outside the selected scope"));
            });

            services.Snapshot = Snapshot(2);
            algorithm.SetDateTime(
                SnapshotTime.AddSeconds(5));
            algorithm.OnData(CreateEmptySlice());

            Assert.AreEqual(1, services.AssignmentRequests.Count);
        }

        [Test]
        public void OldReadySnapshotRefreshesWithoutRequestingAssignment()
        {
            var group = new BrokerageAccountGroup(
                "TargetGroup",
                "Equal",
                Array.Empty<string>());
            var groups =
                new Dictionary<string, BrokerageAccountGroup>
                {
                    [group.Name] = group
                };
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateSnapshot(
                    1,
                    groups,
                    groups,
                    true,
                    SnapshotTime.AddMinutes(-6),
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "MOVE-East"))
            };
            var algorithm = CreateAlgorithm(services);

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, services.AssignmentRequests.Count);
                Assert.AreEqual(1, services.RefreshRequestCount);
                CollectionAssert.IsEmpty(
                    services.RequestedGroupHistory[0]);
            });
        }

        [TestCase("Equal", null)]
        [TestCase("NetLiq", null)]
        [TestCase("AvailableEquity", null)]
        [TestCase("ContractsOrShares", 2.5)]
        [TestCase("Ratio", 2.5)]
        [TestCase("Percent", 2.5)]
        public void UsesAllocationValueRequiredBySavedMethod(
            string allocationMethod,
            double? expectedAllocationValue)
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateSnapshot(
                    1,
                    new BrokerageAccountGroup(
                        "TargetGroup",
                        allocationMethod,
                        Array.Empty<string>()),
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "MOVE-East"))
            };
            var algorithm = CreateAlgorithm(
                services,
                new Dictionary<string, string>
                {
                    ["fa-allocation-value"] = "2.5"
                });

            algorithm.OnData(CreateEmptySlice());

            Assert.AreEqual(1, services.AssignmentRequests.Count);
            Assert.AreEqual(
                expectedAllocationValue.HasValue
                    ? Convert.ToDecimal(expectedAllocationValue.Value)
                    : null,
                services.AssignmentRequests[0].TargetAllocationValue);
        }

        [TestCase("ContractsOrShares", "0", true, "0")]
        [TestCase("ContractsOrShares", "-1", false, null)]
        [TestCase("Ratio", "0", false, null)]
        [TestCase("Ratio", "-1", false, null)]
        [TestCase("Ratio", "100.01", true, "100.01")]
        [TestCase("Percent", "0", false, null)]
        [TestCase("Percent", "-1", false, null)]
        [TestCase("Percent", "100", true, "100")]
        [TestCase("Percent", "100.01", false, null)]
        [TestCase("Equal", "-1", true, null)]
        [TestCase("NetLiq", "0", true, null)]
        [TestCase("AvailableEquity", "-1", true, null)]
        public void ValidatesAllocationValueForDestinationSavedMethod(
            string allocationMethod,
            string allocationValue,
            bool expectedRequest,
            string expectedAllocationValue)
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateSnapshot(
                    1,
                    new BrokerageAccountGroup(
                        "TargetGroup",
                        allocationMethod,
                        Array.Empty<string>()),
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "MOVE-East"))
            };
            var algorithm = CreateAlgorithm(
                services,
                new Dictionary<string, string>
                {
                    ["fa-allocation-value"] = allocationValue
                });

            algorithm.OnData(CreateEmptySlice());

            Assert.AreEqual(
                expectedRequest ? 1 : 0,
                services.AssignmentRequests.Count);
            if (expectedRequest)
            {
                Assert.AreEqual(
                    expectedAllocationValue == null
                        ? null
                        : decimal.Parse(expectedAllocationValue),
                    services.AssignmentRequests[0].TargetAllocationValue);
            }
            else
            {
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains("fa-allocation-value"));
            }
        }

        [Test]
        public void EmptyTargetRemovesMatchingAccountFromEveryGroup()
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateSnapshot(
                    4,
                    new BrokerageAccountGroup(
                        "ExistingGroup",
                        "Equal",
                        new[] { "AccountA", "AccountB" }),
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "MOVE-East",
                        "ExistingGroup"),
                    Entry(
                        "AccountB",
                        BrokerageAccountRelationship.Managed,
                        "Hold-West",
                        "ExistingGroup"))
            };
            var algorithm = CreateAlgorithm(
                services,
                new Dictionary<string, string>
                {
                    ["fa-target-group"] = string.Empty
                });

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequests.Count);
                Assert.AreEqual(
                    string.Empty,
                    services.AssignmentRequests[0].TargetGroupName);
                Assert.IsNull(
                    services.AssignmentRequests[0].TargetAllocationValue);
            });

            services.CompleteAssignment(
                BrokerageAccountGroupAssignmentStatus.Succeeded,
                Array.Empty<string>());
            algorithm.OnData(CreateEmptySlice());
            services.Snapshot = CreateSnapshot(
                5,
                new BrokerageAccountGroup(
                    "ExistingGroup",
                    "Equal",
                    new[] { "AccountB" }),
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "MOVE-East"),
                Entry(
                    "AccountB",
                    BrokerageAccountRelationship.Managed,
                    "Hold-West",
                    "ExistingGroup"));
            algorithm.OnData(CreateEmptySlice());

            Assert.AreEqual(1, services.AssignmentRequests.Count);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FinancialChangeRequiresConfirmingReadyGeneration(
            bool changeTotalCashValue)
        {
            var initialTotalCash = 100m;
            var initialNetLiquidation = 1000m;
            var changedTotalCash =
                changeTotalCashValue ? 1200m : initialTotalCash;
            var changedNetLiquidation =
                changeTotalCashValue ? initialNetLiquidation : 2200m;
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateSnapshot(
                    10,
                    new BrokerageAccountGroup(
                        "TargetGroup",
                        "Equal",
                        new[] { "AccountA" }),
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "MOVE-East",
                        "TargetGroup",
                        initialTotalCash,
                        initialNetLiquidation))
            };
            var algorithm = CreateAlgorithm(
                services,
                new Dictionary<string, string>
                {
                    ["fa-cash-change-threshold"] = "1000"
                });
            services.ResetRefreshRequests();

            algorithm.OnData(CreateEmptySlice());

            services.Snapshot = CreateSnapshot(
                10,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    Array.Empty<string>()),
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "MOVE-East",
                    totalCashValue: changedTotalCash,
                    netLiquidation: changedNetLiquidation));
            algorithm.OnData(CreateEmptySlice());
            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, services.RefreshRequestCount);
                Assert.AreEqual(0, services.AssignmentRequests.Count);
            });

            algorithm.SetDateTime(SnapshotTime.AddMinutes(1));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");
            algorithm.OnData(CreateEmptySlice());
            services.Snapshot = CreateSnapshot(
                11,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    new[] { "AccountA" }),
                false,
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "MOVE-East",
                    "TargetGroup",
                    totalCashValue: changedTotalCash,
                    netLiquidation: changedNetLiquidation));
            algorithm.OnData(CreateEmptySlice());

            algorithm.SetDateTime(SnapshotTime.AddMinutes(2));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");
            algorithm.OnData(CreateEmptySlice());
            services.Snapshot = CreateSnapshot(
                12,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    new[] { "AccountA" }),
                false,
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "MOVE-East",
                    "TargetGroup",
                    totalCashValue: changedTotalCash,
                    netLiquidation: changedNetLiquidation));
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, services.RefreshRequestCount);
                Assert.AreEqual(0, services.AssignmentRequests.Count);
            });

            algorithm.SetDateTime(SnapshotTime.AddMinutes(3));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");
            algorithm.OnData(CreateEmptySlice());
            services.Snapshot = CreateSnapshot(
                13,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    Array.Empty<string>()),
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "MOVE-East",
                    totalCashValue: changedTotalCash,
                    netLiquidation: changedNetLiquidation));
            algorithm.SetDateTime(
                SnapshotTime.AddMinutes(3).AddSeconds(5));
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    4,
                    services.RefreshRequestCount,
                    "The third scheduled tick must request complete state, " +
                    "then the observed cash change must request confirmation.");
                CollectionAssert.IsEmpty(
                    services.RequestedGroupHistory[2]);
                CollectionAssert.IsEmpty(
                    services.RequestedGroupHistory[3]);
                Assert.AreEqual(0, services.AssignmentRequests.Count);
            });

            services.Snapshot = CreateSnapshot(
                14,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    Array.Empty<string>()),
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "MOVE-East",
                    totalCashValue: changedTotalCash,
                    netLiquidation: changedNetLiquidation));
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequests.Count);
                Assert.AreEqual(
                    "AccountA",
                    services.AssignmentRequests[0].AccountId);
                Assert.AreEqual(
                    "membership-14",
                    services.AssignmentRequests[0].MembershipHash);
            });
        }

        [Test]
        public void FailedAssignmentIsReportedAndNotRetriedOnSameSnapshot()
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateSnapshot(
                    3,
                    new BrokerageAccountGroup(
                        "TargetGroup",
                        "Equal",
                        Array.Empty<string>()),
                    Entry(
                        "AccountA",
                        BrokerageAccountRelationship.Managed,
                        "MOVE-East"))
            };
            var algorithm = CreateAlgorithm(services);

            algorithm.OnData(CreateEmptySlice());
            services.CompleteAssignment(
                BrokerageAccountGroupAssignmentStatus.Failed,
                Array.Empty<string>(),
                "distinctive readback failure");
            algorithm.OnData(CreateEmptySlice());
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequests.Count);
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains("distinctive readback failure"));
            });

            services.Snapshot = CreateSnapshot(
                4,
                new BrokerageAccountGroup(
                    "TargetGroup",
                    "Equal",
                    Array.Empty<string>()),
                Entry(
                    "AccountA",
                    BrokerageAccountRelationship.Managed,
                    "MOVE-East"));
            algorithm.OnData(CreateEmptySlice());

            Assert.AreEqual(2, services.AssignmentRequests.Count);
        }

        private static FinancialAdvisorGroupAssignmentAlgorithm
            CreateAlgorithm(
                TestFinancialAdvisorServices services,
                Dictionary<string, string> parameters = null)
        {
            var algorithm =
                new FinancialAdvisorGroupAssignmentAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(
                new DataManagerStub(algorithm));
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
            services.AcceptRefreshRequests = true;
            services.ResetRefreshRequests();
            SetPrivateField(
                algorithm,
                "_nextRefreshRetryUtc",
                SnapshotTime);
            SetPrivateField(
                algorithm,
                "_initialSnapshotRefreshAccepted",
                true);
            SetPrivateField(
                algorithm,
                "_initialSnapshotRequestGeneration",
                services.Snapshot.Generation - 1);
            algorithm.SetLocked();
            algorithm.SetBrokerageAccountMutationServicesReady();
            return algorithm;
        }

        private static Slice CreateEmptySlice()
        {
            return new Slice(
                SnapshotTime,
                Array.Empty<BaseData>(),
                SnapshotTime);
        }

        private static AccountEntry Entry(
            string accountId,
            BrokerageAccountRelationship relationship,
            string alias,
            string groupName = "",
            decimal totalCashValue = 100m,
            decimal netLiquidation = 1000m)
        {
            return new AccountEntry(
                accountId,
                relationship,
                alias,
                string.IsNullOrEmpty(groupName)
                    ? Array.Empty<string>()
                    : new[] { groupName },
                totalCashValue,
                netLiquidation);
        }

        private static AccountEntry Entry(
            string accountId,
            BrokerageAccountRelationship relationship,
            string alias,
            IReadOnlyList<string> groupNames,
            decimal totalCashValue = 100m,
            decimal netLiquidation = 1000m)
        {
            return new AccountEntry(
                accountId,
                relationship,
                alias,
                groupNames,
                totalCashValue,
                netLiquidation);
        }

        private static BrokerageAccountSnapshot CreateSnapshot(
            long generation,
            BrokerageAccountGroup group,
            params AccountEntry[] entries)
        {
            return CreateSnapshot(
                generation,
                group,
                true,
                entries);
        }

        private static BrokerageAccountSnapshot CreateSnapshot(
            long generation,
            BrokerageAccountGroup group,
            bool isComplete,
            params AccountEntry[] entries)
        {
            var groups =
                new Dictionary<string, BrokerageAccountGroup>
                {
                    [group.Name] = group
                };
            return CreateSnapshot(
                generation,
                groups,
                groups,
                isComplete,
                SnapshotTime.AddTicks(generation),
                entries);
        }

        private static BrokerageAccountSnapshot CreateSnapshot(
            long generation,
            IReadOnlyDictionary<string, BrokerageAccountGroup> groups,
            IReadOnlyDictionary<string, BrokerageAccountGroup> allGroups,
            bool isComplete,
            DateTime collectionStartedUtc,
            params AccountEntry[] entries)
        {
            var directory =
                entries.ToDictionary(
                    entry => entry.AccountId,
                    entry => new BrokerageAccountDirectoryEntry(
                        entry.AccountId,
                        entry.Relationship,
                        entry.GroupNames,
                        "INDIVIDUAL",
                        string.Empty,
                        entry.Alias),
                    StringComparer.OrdinalIgnoreCase);
            var managedEntries = entries
                .Where(entry => entry.Relationship ==
                    BrokerageAccountRelationship.Managed)
                .ToArray();
            var accounts =
                managedEntries.ToDictionary(
                    entry => entry.AccountId,
                    entry => new BrokerageAccountState(
                        entry.AccountId,
                        entry.GroupNames,
                        "INDIVIDUAL",
                        entry.NetLiquidation,
                        entry.TotalCashValue,
                        null,
                        null,
                        null,
                        "USD",
                        new Dictionary<string, decimal>(),
                        Array.Empty<BrokerageAccountPosition>()),
                    StringComparer.OrdinalIgnoreCase);
            var unassigned = managedEntries
                .Where(entry => entry.GroupNames.Count == 0)
                .Select(entry => entry.AccountId)
                .ToArray();
            var primary = entries.FirstOrDefault(
                entry => entry.Relationship ==
                    BrokerageAccountRelationship.Primary);

            return new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                generation,
                SnapshotTime.AddTicks(generation),
                SnapshotTime.AddTicks(generation),
                groups,
                accounts,
                unassigned,
                $"membership-{generation}",
                $"configuration-{generation}",
                string.Empty,
                primary?.AccountId ?? string.Empty,
                managedEntries.Select(entry => entry.AccountId).ToArray(),
                allGroups,
                directory,
                isComplete,
                collectionStartedUtc);
        }

        private static void InvokePrivateMethod(
            object instance,
            string name)
        {
            var method = instance.GetType().GetMethod(
                name,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Private method '{name}' was not found.");
            method.Invoke(instance, null);
        }

        private static void SetPrivateField<T>(
            object instance,
            string name,
            T value)
        {
            var field = instance.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Private field '{name}' was not found.");
            field.SetValue(instance, value);
        }

        private static T GetPrivateField<T>(
            object instance,
            string name)
        {
            var field = instance.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Private field '{name}' was not found.");
            return (T)field.GetValue(instance);
        }

        private sealed class AccountEntry
        {
            public string AccountId { get; }
            public BrokerageAccountRelationship Relationship { get; }
            public string Alias { get; }
            public IReadOnlyList<string> GroupNames { get; }
            public decimal TotalCashValue { get; }
            public decimal NetLiquidation { get; }

            public AccountEntry(
                string accountId,
                BrokerageAccountRelationship relationship,
                string alias,
                IReadOnlyList<string> groupNames,
                decimal totalCashValue,
                decimal netLiquidation)
            {
                AccountId = accountId;
                Relationship = relationship;
                Alias = alias;
                GroupNames = groupNames;
                TotalCashValue = totalCashValue;
                NetLiquidation = netLiquidation;
            }
        }

        private sealed class AssignmentRequest
        {
            public string AccountId { get; }
            public string TargetGroupName { get; }
            public string MembershipHash { get; }
            public string ConfigurationVersion { get; }
            public decimal? TargetAllocationValue { get; }

            public AssignmentRequest(
                string accountId,
                string targetGroupName,
                string membershipHash,
                string configurationVersion,
                decimal? targetAllocationValue)
            {
                AccountId = accountId;
                TargetGroupName = targetGroupName;
                MembershipHash = membershipHash;
                ConfigurationVersion = configurationVersion;
                TargetAllocationValue = targetAllocationValue;
            }
        }

        private sealed class TestFinancialAdvisorServices :
            IBrokerageAccountStateProvider,
            IBrokerageAccountGroupManager
        {
            private long _assignmentGeneration;
            private readonly Queue<Exception> _assignmentExceptions = new();

            public BrokerageAccountSnapshot Snapshot { get; set; }
            public BrokerageAccountGroupAssignment Assignment { get; private set; } =
                BrokerageAccountGroupAssignment.Unavailable;
            public List<AssignmentRequest> AssignmentRequests { get; } = new();
            public bool AcceptRefreshRequests { get; set; } = true;
            public int RefreshRequestCount { get; private set; }
            public long GenerationAtLastRefreshRequest { get; private set; }
            public Action<TestFinancialAdvisorServices>
                RefreshRequestHook
            { get; set; }
            public List<IReadOnlyCollection<string>> RequestedGroupHistory { get; } =
                new();

            public void EnqueueAssignmentException(Exception exception)
            {
                _assignmentExceptions.Enqueue(exception);
            }

            public BrokerageAccountSnapshot GetAccountSnapshot()
            {
                return Snapshot;
            }

            public bool RequestAccountSnapshotRefresh(
                IReadOnlyCollection<string> groupNames,
                IReadOnlyCollection<string> additionalAccountIds)
            {
                ++RefreshRequestCount;
                GenerationAtLastRefreshRequest = Snapshot.Generation;
                RequestedGroupHistory.Add(groupNames.ToArray());
                RefreshRequestHook?.Invoke(this);
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
                if (_assignmentExceptions.Count != 0)
                {
                    throw _assignmentExceptions.Dequeue();
                }
                AssignmentRequests.Add(
                    new AssignmentRequest(
                        accountId,
                        targetGroupName,
                        expectedMembershipHash,
                        expectedGroupConfigurationVersion,
                        targetAllocationValue));
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
                IReadOnlyList<string> resultingGroupNames,
                string errorMessage = "")
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
                    errorMessage,
                    Assignment.TargetAllocationValue);
            }

            public void ResetRefreshRequests()
            {
                RefreshRequestCount = 0;
                GenerationAtLastRefreshRequest = -1;
                RequestedGroupHistory.Clear();
            }
        }
    }
}
