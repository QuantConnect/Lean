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
                    new[] { TargetGroupName, SourceGroupName },
                    includeCompanionMembers: true)
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
                SnapshotTime.AddSeconds(6),
                includeCompanionMembers: true);
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
                1,
                services.AssignmentRequestCount,
                "A synchronous rejection must not retry against the same snapshot.");

            services.Snapshot = CreateAssignmentSnapshot(
                4,
                new[] { TargetGroupName, SourceGroupName },
                new[] { TargetGroupName, SourceGroupName },
                SnapshotTime.AddSeconds(16),
                includeCompanionMembers: true);
            algorithm.SetDateTime(SnapshotTime.AddSeconds(20));
            InvokeScheduledCallback(algorithm);
            Assert.AreEqual(
                2,
                services.AssignmentRequestCount,
                "A strictly newer snapshot may authorize the retry.");

            services.CompleteAssignment(
                BrokerageAccountGroupAssignmentStatus.Succeeded,
                new[] { TargetGroupName });
            services.Snapshot = CreateAssignmentSnapshot(
                5,
                new[] { TargetGroupName, SourceGroupName },
                new[] { TargetGroupName },
                SnapshotTime.AddSeconds(21),
                includeCompanionMembers: true);
            algorithm.SetDateTime(SnapshotTime.AddSeconds(25));
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

        [Test]
        public void GroupAssignmentEmptyTopologyUsesThirdTickCompleteDiscoveryInPython()
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateEmptyAssignmentSnapshot(2)
            };
            using var algorithm = CreateAlgorithm(
                "FinancialAdvisorGroupAssignmentAlgorithm",
                services,
                new Dictionary<string, string>
                {
                    ["fa-target-group"] = string.Empty
                });

            for (var tick = 1; tick <= 3; ++tick)
            {
                algorithm.SetDateTime(
                    SnapshotTime.AddSeconds(90 * tick));
                InvokeScheduledCallback(algorithm);
            }

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    1,
                    services.RefreshRequestCount,
                    "The first two empty scoped ticks must not be mistaken " +
                    "for complete refreshes.");
                CollectionAssert.IsEmpty(
                    services.RequestedGroupHistory.Single(),
                    "The third topology tick must issue complete discovery.");
            });
        }

        [TestCase("fa-allocation-value", "NaN")]
        [TestCase("fa-allocation-value", "Infinity")]
        [TestCase("fa-allocation-value", "-Infinity")]
        [TestCase(
            "fa-allocation-value",
            "79228162514264337593543950336")]
        [TestCase("fa-allocation-value", "1e-29")]
        [TestCase("fa-cash-change-threshold", "NaN")]
        [TestCase("fa-cash-change-threshold", "Infinity")]
        [TestCase("fa-cash-change-threshold", "-Infinity")]
        [TestCase(
            "fa-cash-change-threshold",
            "79228162514264337593543950336")]
        [TestCase("fa-cash-change-threshold", "1e-29")]
        public void GroupAssignmentRejectsNonSystemDecimalParametersInPython(
            string parameterName,
            string parameterValue)
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateAssignmentSnapshot(
                    2,
                    new[] { TargetGroupName },
                    Array.Empty<string>())
            };

            var exception = Assert.Throws<PythonException>(() =>
                CreateAlgorithm(
                    "FinancialAdvisorGroupAssignmentAlgorithm",
                    services,
                    new Dictionary<string, string>
                    {
                        [parameterName] = parameterValue
                    }));

            Assert.Multiple(() =>
            {
                StringAssert.Contains(parameterName, exception.Message);
                StringAssert.Contains("System.Decimal", exception.Message);
                Assert.AreEqual(0, services.AssignmentRequestCount);
            });
        }

        [TestCase(
            "79228162514264337593543950335",
            "79228162514264337593543950335")]
        [TestCase("1e-28", "0.0000000000000000000000000001")]
        public void GroupAssignmentAcceptsRepresentableDecimalBoundaryInPython(
            string allocationValue,
            string expectedAllocationValue)
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateAssignmentSnapshot(
                    2,
                    new[] { TargetGroupName },
                    Array.Empty<string>(),
                    allocationMethod: "ContractsOrShares")
            };
            using var algorithm = CreateAlgorithm(
                "FinancialAdvisorGroupAssignmentAlgorithm",
                services,
                new Dictionary<string, string>
                {
                    ["fa-allocation-value"] = allocationValue
                });

            algorithm.OnData(CreateEmptySlice());

            Assert.AreEqual(1, services.AssignmentRequestCount);
            Assert.AreEqual(
                decimal.Parse(expectedAllocationValue),
                services.Assignment.TargetAllocationValue);
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
                    Array.Empty<string>(),
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

        [TestCase("Percent", 40, 60)]
        [TestCase("Ratio", 7.5, 2.5)]
        [TestCase("ContractsOrShares", 19.75, 10.25)]
        public void GroupAssignmentPreservesExistingTargetAllocationInPython(
            string allocationMethod,
            double accountAAllocation,
            double accountBAllocation)
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateAssignmentSnapshot(
                    2,
                    new[] { TargetGroupName, SourceGroupName },
                    new[] { TargetGroupName, SourceGroupName },
                    allocationMethod: allocationMethod,
                    targetAccountAllocationValues:
                        new Dictionary<string, decimal>
                        {
                            ["AccountA"] =
                                Convert.ToDecimal(accountAAllocation),
                            ["AccountB"] =
                                Convert.ToDecimal(accountBAllocation)
                        },
                    includeCompanionMembers: true)
            };
            using var algorithm = CreateAlgorithm(
                "FinancialAdvisorGroupAssignmentAlgorithm",
                services,
                new Dictionary<string, string>
                {
                    ["fa-allocation-value"] = "1"
                });

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequestCount);
                Assert.IsNull(services.Assignment.TargetAllocationValue);
            });
        }

        [Test]
        public void GroupAssignmentReturnedFalseRequiresNewerSnapshotInPython()
        {
            BrokerageAccountSnapshot Snapshot(long generation) =>
                CreateAssignmentSnapshot(
                    generation,
                    new[] { TargetGroupName },
                    Array.Empty<string>(),
                    SnapshotTime.AddTicks(generation));
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = Snapshot(2),
                RejectNextAssignmentRequest = true
            };
            using var algorithm = CreateAlgorithm(
                "FinancialAdvisorGroupAssignmentAlgorithm",
                services);

            algorithm.OnData(CreateEmptySlice());
            algorithm.SetDateTime(SnapshotTime.AddSeconds(5));
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequestCount);
                Assert.AreEqual(1, services.RefreshRequestCount);
            });

            services.Snapshot = Snapshot(3);
            algorithm.OnData(CreateEmptySlice());

            Assert.AreEqual(2, services.AssignmentRequestCount);
        }

        [Test]
        public void GroupAssignmentWaitsForDelayedPendingPublicationInPython()
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateAssignmentSnapshot(
                    2,
                    new[] { TargetGroupName },
                    Array.Empty<string>()),
                DelayNextAssignmentPublication = true
            };
            using var algorithm = CreateAlgorithm(
                "FinancialAdvisorGroupAssignmentAlgorithm",
                services);

            algorithm.OnData(CreateEmptySlice());
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequestCount);
                Assert.AreEqual(
                    BrokerageAccountGroupAssignmentStatus.Unavailable,
                    services.Assignment.Status);
            });

            services.PublishDelayedAssignment();
            algorithm.OnData(CreateEmptySlice());
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(
                1,
                services.AssignmentRequestCount,
                "Waiting for a delayed Pending result must not submit a " +
                "duplicate mutation.");

            services.CompleteAssignment(
                BrokerageAccountGroupAssignmentStatus.Succeeded,
                new[] { TargetGroupName });
            services.Snapshot = CreateAssignmentSnapshot(
                3,
                new[] { TargetGroupName },
                new[] { TargetGroupName },
                SnapshotTime.AddSeconds(6));
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequestCount);
                Assert.That(
                    algorithm.LogMessages,
                    Has.One.Contains("FA assignment succeeded"));
            });
        }

        [Test]
        public void GroupAssignmentAcceptsSynchronouslyPublishedTerminalResultInPython()
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateAssignmentSnapshot(
                    2,
                    new[] { TargetGroupName },
                    Array.Empty<string>()),
                CompleteNextAssignmentSynchronously = true
            };
            using var algorithm = CreateAlgorithm(
                "FinancialAdvisorGroupAssignmentAlgorithm",
                services);

            algorithm.OnData(CreateEmptySlice());
            services.Snapshot = CreateAssignmentSnapshot(
                3,
                new[] { TargetGroupName },
                new[] { TargetGroupName },
                SnapshotTime.AddSeconds(6));
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequestCount);
                Assert.That(
                    algorithm.LogMessages,
                    Has.One.Contains("FA assignment succeeded"));
            });
        }

        [Test]
        public void GroupAssignmentTerminalFailureRequiresNewerSnapshotInPython()
        {
            BrokerageAccountSnapshot Snapshot(long generation) =>
                CreateAssignmentSnapshot(
                    generation,
                    new[] { TargetGroupName },
                    Array.Empty<string>(),
                    SnapshotTime.AddTicks(generation));
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = Snapshot(2)
            };
            using var algorithm = CreateAlgorithm(
                "FinancialAdvisorGroupAssignmentAlgorithm",
                services);

            algorithm.OnData(CreateEmptySlice());
            services.CompleteAssignment(
                BrokerageAccountGroupAssignmentStatus.Failed,
                Array.Empty<string>(),
                "distinctive readback failure");
            algorithm.OnData(CreateEmptySlice());
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.AssignmentRequestCount);
                Assert.AreEqual(1, services.RefreshRequestCount);
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains("distinctive readback failure"));
            });

            services.Snapshot = Snapshot(3);
            algorithm.OnData(CreateEmptySlice());

            Assert.AreEqual(2, services.AssignmentRequestCount);
        }

        [Test]
        public void GroupAssignmentFinalSourceMemberWaitsForTopologyChangeInPython()
        {
            BrokerageAccountSnapshot Snapshot(
                long generation,
                bool includeCompanionMembers) =>
                CreateAssignmentSnapshot(
                    generation,
                    new[] { TargetGroupName, SourceGroupName },
                    new[] { SourceGroupName },
                    SnapshotTime.AddTicks(generation),
                    includeCompanionMembers: includeCompanionMembers);
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = Snapshot(2, false)
            };
            using var algorithm = CreateAlgorithm(
                "FinancialAdvisorGroupAssignmentAlgorithm",
                services);

            algorithm.OnData(CreateEmptySlice());
            algorithm.SetDateTime(SnapshotTime.AddSeconds(90));
            InvokeScheduledCallback(algorithm);
            services.Snapshot = Snapshot(3, false);
            algorithm.SetDateTime(SnapshotTime.AddSeconds(180));
            InvokeScheduledCallback(algorithm);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, services.AssignmentRequestCount);
                Assert.AreEqual(2, services.RefreshRequestCount);
                Assert.IsTrue(
                    algorithm.ErrorMessages.Any(message =>
                        message.Contains("final member of source group")));
            });

            services.Snapshot = Snapshot(4, true);
            algorithm.SetDateTime(SnapshotTime.AddSeconds(270));
            InvokeScheduledCallback(algorithm);

            Assert.AreEqual(1, services.AssignmentRequestCount);
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
                "FinancialAdvisorUnifiedGroupsDemoAlgorithm",
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
                CollectionAssert.AreEquivalent(
                    new[] { "AccountA", "AccountB" },
                    services.RequestedAdditionalAccountHistory.Single());
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
        public void DemoMissingMemberKeepsInvalidReconciliationPendingInPython()
        {
            var preOrderSnapshot = CreateDemoSnapshot(
                2,
                SnapshotTime);
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = preOrderSnapshot
            };
            using var algorithm = CreateAlgorithm(
                "FinancialAdvisorUnifiedGroupsDemoAlgorithm",
                services);
            algorithm.SetProperty(
                "_pre_order_snapshot",
                preOrderSnapshot);
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

            services.Snapshot = CreateDemoSnapshot(
                3,
                SnapshotTime.AddSeconds(5).AddTicks(1),
                includeAccountB: false);
            algorithm.SetDateTime(SnapshotTime.AddSeconds(10));
            var reconciliationMessageCount = algorithm.LogMessages.Count(
                message => message.Contains("FA reconciliation"));
            InvokeScheduledCallback(algorithm);

            using (var retainedPreOrderSnapshot =
                algorithm.GetProperty("_pre_order_snapshot"))
            {
                Assert.Multiple(() =>
                {
                    Assert.AreEqual(2, services.RefreshRequestCount);
                    CollectionAssert.AreEquivalent(
                        new[] { "AccountA", "AccountB" },
                        services.RequestedAdditionalAccountHistory[0]);
                    CollectionAssert.AreEquivalent(
                        new[] { "AccountA", "AccountB" },
                        services.RequestedAdditionalAccountHistory[1]);
                    Assert.AreEqual(
                        3,
                        algorithm.GetProperty<long>(
                            "_pending_reconcile_generation"),
                        "The incomplete generation must not be reconsidered.");
                    Assert.AreEqual(
                        0,
                        algorithm.GetProperty<int>(
                            "_invalid_order_attempt_count"),
                        "Missing account state must not authorize an Invalid retry.");
                    Assert.AreEqual(
                        reconciliationMessageCount,
                        algorithm.LogMessages.Count(
                            message => message.Contains("FA reconciliation")),
                        "An incomplete snapshot must not emit partial reconciliation.");
                    Assert.IsFalse(retainedPreOrderSnapshot.IsNone());
                    Assert.That(
                        algorithm.ErrorMessages,
                        Has.One.Contains(
                            "Account state for 'AccountB' is unavailable"));
                });
            }

            services.Snapshot = CreateDemoSnapshot(
                4,
                SnapshotTime.AddSeconds(5).AddTicks(2));
            algorithm.SetDateTime(SnapshotTime.AddSeconds(15));
            InvokeScheduledCallback(algorithm);

            using var clearedPreOrderSnapshot =
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
                Assert.IsTrue(clearedPreOrderSnapshot.IsNone());
                Assert.AreEqual(
                    reconciliationMessageCount + 2,
                    algorithm.LogMessages.Count(
                        message => message.Contains("FA reconciliation")));
            });
        }

        [Test]
        public void DemoIncompletePreOrderMemberVectorDoesNotSubmitInPython()
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateDemoSnapshot(
                    2,
                    SnapshotTime,
                    includeAccountB: false)
            };
            using var algorithm =
                CreateEmptyTicketDemoAlgorithm(services);

            algorithm.OnData(CreateEmptySlice());

            Assert.AreEqual(
                0,
                algorithm.GetProperty<int>("submission_count"));
        }

        [TestCase("Equal")]
        [TestCase("NetLiq")]
        [TestCase("AvailableEquity")]
        [TestCase("Ratio")]
        [TestCase("Percent")]
        public void DemoSupportedSavedGroupMethodsPermitSubmissionInPython(
            string allocationMethod)
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateDemoSnapshot(
                    2,
                    SnapshotTime,
                    allocationMethod: allocationMethod)
            };
            using var algorithm =
                CreateEmptyTicketDemoAlgorithm(services);

            algorithm.OnData(CreateEmptySlice());

            Assert.AreEqual(
                1,
                algorithm.GetProperty<int>("submission_count"));
        }

        [Test]
        public void DemoUnsupportedSavedGroupMethodDoesNotSubmitInPython()
        {
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateDemoSnapshot(
                    2,
                    SnapshotTime,
                    allocationMethod: "ContractsOrShares")
            };
            using var algorithm =
                CreateEmptyTicketDemoAlgorithm(services);

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    0,
                    algorithm.GetProperty<int>("submission_count"));
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains(
                        "unsupported saved allocation method 'ContractsOrShares'"));
            });
        }

        [Test]
        public void DemoReconciliationUsesLastSuccessfulUpdateWhenCollectionStartIsUnavailableInPython()
        {
            var preOrderSnapshot = CreateDemoSnapshot(
                2,
                SnapshotTime.AddMinutes(-2));
            var services = new TestFinancialAdvisorServices
            {
                Snapshot = CreateDemoSnapshot(
                    3,
                    SnapshotTime.AddSeconds(-1))
            };
            using var algorithm = CreateAlgorithm(
                "FinancialAdvisorUnifiedGroupsDemoAlgorithm",
                services);
            algorithm.SetProperty(
                "_pre_order_snapshot",
                preOrderSnapshot);
            algorithm.SetProperty("_group_order_submitted", true);
            algorithm.SetProperty("_group_order_id", 41);

            using (Py.GIL())
            {
                algorithm.OnOrderEvent(
                    CreateOrderEvent(
                        41,
                        OrderStatus.Filled,
                        null));
            }
            services.Snapshot = CreateDemoSnapshot(
                4,
                default,
                lastSuccessfulUpdateUtc:
                    SnapshotTime.AddSeconds(5).AddTicks(1));
            algorithm.SetDateTime(SnapshotTime.AddSeconds(10));
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, services.RefreshRequestCount);
                Assert.AreEqual(
                    -1,
                    algorithm.GetProperty<long>(
                        "_pending_reconcile_generation"));
                Assert.That(
                    algorithm.LogMessages,
                    Has.One.Contains(
                        "account=AccountA, symbol=SPY, before=10, after=10, change=0"));
                Assert.That(
                    algorithm.LogMessages,
                    Has.One.Contains(
                        "account=AccountB, symbol=SPY, before=20, after=20, change=0"));
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
                $"FinancialAdvisorUnifiedGroupsDemoAlgorithmTest_{Guid.NewGuid():N}";
            var source = @"
from FinancialAdvisorUnifiedGroupsDemoAlgorithm import FinancialAdvisorUnifiedGroupsDemoAlgorithm

class FinancialAdvisorUnifiedGroupsDemoAlgorithmUnderTest(FinancialAdvisorUnifiedGroupsDemoAlgorithm):
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
            try
            {
                algorithm.Initialize();
            }
            catch
            {
                algorithm.Dispose();
                throw;
            }
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
            DateTime collectionStartedUtc,
            bool includeAccountB = true,
            string allocationMethod = "Equal",
            DateTime? lastSuccessfulUpdateUtc = null)
        {
            var group = new BrokerageAccountGroup(
                DemoGroupName,
                allocationMethod,
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
                        10m)
                };
            if (includeAccountB)
            {
                accounts["AccountB"] = CreateAccount(
                    "AccountB",
                    new[] { group.Name },
                    20m);
            }

            return new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                generation,
                SnapshotTime.AddTicks(generation),
                lastSuccessfulUpdateUtc ??
                    SnapshotTime.AddTicks(generation),
                groups,
                accounts,
                Array.Empty<string>(),
                $"membership-{generation}",
                $"configuration-{generation}",
                string.Empty,
                managedAccountIds: new[] { "AccountA", "AccountB" },
                collectionStartedUtc: collectionStartedUtc);
        }

        private static BrokerageAccountSnapshot CreateAssignmentSnapshot(
            long generation,
            IReadOnlyCollection<string> selectedGroupNames,
            IReadOnlyCollection<string> accountGroupNames,
            DateTime? collectionStartedUtc = null,
            string allocationMethod = "Equal",
            IReadOnlyDictionary<string, decimal>
                targetAccountAllocationValues = null,
            bool includeCompanionMembers = false)
        {
            var targetAccountIds = accountGroupNames.Contains(
                    TargetGroupName,
                    StringComparer.OrdinalIgnoreCase)
                ? new List<string> { "AccountA" }
                : new List<string>();
            var sourceAccountIds = accountGroupNames.Contains(
                    SourceGroupName,
                    StringComparer.OrdinalIgnoreCase)
                ? new List<string> { "AccountA" }
                : new List<string>();
            if (includeCompanionMembers)
            {
                targetAccountIds.Add("AccountB");
                sourceAccountIds.Add("AccountC");
            }
            var targetGroup = new BrokerageAccountGroup(
                TargetGroupName,
                allocationMethod,
                targetAccountIds,
                targetAccountAllocationValues);
            var sourceGroup = new BrokerageAccountGroup(
                SourceGroupName,
                "Equal",
                sourceAccountIds);
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
            if (includeCompanionMembers)
            {
                directory["AccountB"] =
                    new BrokerageAccountDirectoryEntry(
                        "AccountB",
                        BrokerageAccountRelationship.Managed,
                        new[] { TargetGroupName },
                        "INDIVIDUAL",
                        string.Empty,
                        string.Empty);
                directory["AccountC"] =
                    new BrokerageAccountDirectoryEntry(
                        "AccountC",
                        BrokerageAccountRelationship.Managed,
                        new[] { SourceGroupName },
                        "INDIVIDUAL",
                        string.Empty,
                        string.Empty);
            }
            var accounts =
                new Dictionary<string, BrokerageAccountState>
                {
                    ["AccountA"] = CreateAccount(
                        "AccountA",
                        accountGroupNames,
                        0m)
                };
            if (includeCompanionMembers)
            {
                accounts["AccountB"] = CreateAccount(
                    "AccountB",
                    new[] { TargetGroupName },
                    0m);
                accounts["AccountC"] = CreateAccount(
                    "AccountC",
                    new[] { SourceGroupName },
                    0m);
            }
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
                managedAccountIds: accounts.Keys.ToArray(),
                allGroups: allGroups,
                accountDirectory: directory,
                isComplete: true,
                collectionStartedUtc: timestamp);
        }

        private static BrokerageAccountSnapshot CreateEmptyAssignmentSnapshot(
            long generation)
        {
            var timestamp = SnapshotTime.AddTicks(generation);
            return new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                generation,
                timestamp,
                timestamp,
                new Dictionary<string, BrokerageAccountGroup>(),
                new Dictionary<string, BrokerageAccountState>(),
                Array.Empty<string>(),
                $"membership-{generation}",
                $"configuration-{generation}",
                string.Empty,
                managedAccountIds: Array.Empty<string>(),
                allGroups:
                    new Dictionary<string, BrokerageAccountGroup>(),
                accountDirectory:
                    new Dictionary<
                        string,
                        BrokerageAccountDirectoryEntry>(),
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
            private BrokerageAccountGroupAssignment _delayedAssignment;

            public BrokerageAccountSnapshot Snapshot { get; set; }
            public BrokerageAccountGroupAssignment Assignment { get; private set; } =
                BrokerageAccountGroupAssignment.Unavailable;
            public bool AcceptRefreshRequests { get; set; } = true;
            public bool ThrowNextAssignmentRequest { get; set; }
            public bool RejectNextAssignmentRequest { get; set; }
            public bool DelayNextAssignmentPublication { get; set; }
            public bool CompleteNextAssignmentSynchronously { get; set; }
            public int RefreshRequestCount { get; private set; }
            public int AssignmentRequestCount { get; private set; }
            public List<IReadOnlyCollection<string>> RequestedGroupHistory { get; } =
                new();
            public List<IReadOnlyCollection<string>>
                RequestedAdditionalAccountHistory
            { get; } = new();

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
                RequestedAdditionalAccountHistory.Add(
                    additionalAccountIds.ToArray());
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
                if (RejectNextAssignmentRequest)
                {
                    RejectNextAssignmentRequest = false;
                    return false;
                }

                var assignment = new BrokerageAccountGroupAssignment(
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
                if (DelayNextAssignmentPublication)
                {
                    DelayNextAssignmentPublication = false;
                    _delayedAssignment = assignment;
                }
                else if (CompleteNextAssignmentSynchronously)
                {
                    CompleteNextAssignmentSynchronously = false;
                    Assignment = CreateCompletedAssignment(
                        assignment,
                        BrokerageAccountGroupAssignmentStatus.Succeeded,
                        new[] { targetGroupName },
                        string.Empty);
                }
                else
                {
                    Assignment = assignment;
                }
                return true;
            }

            public void PublishDelayedAssignment()
            {
                if (_delayedAssignment == null)
                {
                    throw new InvalidOperationException(
                        "No delayed assignment is available.");
                }

                Assignment = _delayedAssignment;
                _delayedAssignment = null;
            }

            public void CompleteAssignment(
                BrokerageAccountGroupAssignmentStatus status,
                IReadOnlyCollection<string> resultingGroupNames,
                string errorMessage = "")
            {
                Assignment = CreateCompletedAssignment(
                    Assignment,
                    status,
                    resultingGroupNames,
                    errorMessage);
            }

            private static BrokerageAccountGroupAssignment
                CreateCompletedAssignment(
                    BrokerageAccountGroupAssignment assignment,
                    BrokerageAccountGroupAssignmentStatus status,
                    IReadOnlyCollection<string> resultingGroupNames,
                    string errorMessage)
            {
                return new BrokerageAccountGroupAssignment(
                    status,
                    assignment.Generation,
                    SnapshotTime,
                    assignment.AccountId,
                    assignment.TargetGroupName,
                    assignment.PreviousGroupNames,
                    resultingGroupNames,
                    assignment.ExpectedMembershipHash,
                    $"resulting-membership-{assignment.Generation}",
                    assignment.ExpectedGroupConfigurationVersion,
                    $"resulting-configuration-{assignment.Generation}",
                    errorMessage,
                    assignment.TargetAllocationValue);
            }

            public void ResetRefreshRequests()
            {
                RefreshRequestCount = 0;
                RequestedGroupHistory.Clear();
            }
        }
    }
}
