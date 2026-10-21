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
using System.Reflection;
using System.Threading;
using Moq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Packets;
using LeanEngine = QuantConnect.Lean.Engine.Engine;

namespace QuantConnect.Tests.Engine
{
    [TestFixture, NonParallelizable]
    public class BrokerageAccountServiceWiringTests
    {
        [SetUp]
        public void SetUp()
        {
            BrokerageAccountServiceLifecycleAlgorithm.Reset();
            BrokerageAccountServiceBacktestingSetupHandler.Reset();
        }

        [Test]
        public void BrokerageAccountServicesAreInstalledTest()
        {
            AssertEngineInstallsEveryService();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MutationServiceLifecycleAlwaysRevokesAuthority(bool throws)
        {
            var baseAlgorithm = CreateMutationServiceAlgorithm(
                out var snapshot,
                out var groupManager);

            Assert.IsFalse(baseAlgorithm.RequestBrokerageAccountGroupAssignment(
                "Account",
                "Group",
                null,
                snapshot));

            var acceptedInsideBoundary = false;
            bool RunAlgorithm()
            {
                acceptedInsideBoundary = baseAlgorithm.RequestBrokerageAccountGroupAssignment(
                    "Account",
                    "Group",
                    null,
                    snapshot);
                if (throws)
                {
                    throw new InvalidOperationException("Expected test exception.");
                }
                return true;
            }

            if (throws)
            {
                Assert.Throws<InvalidOperationException>(() =>
                    LeanEngine.ExecuteWithBrokerageAccountMutationLifecycle(
                        baseAlgorithm,
                        () => LeanEngine.RunWithBrokerageAccountMutationsEnabled(
                            baseAlgorithm,
                            RunAlgorithm)));
            }
            else
            {
                Assert.IsTrue(LeanEngine.ExecuteWithBrokerageAccountMutationLifecycle(
                    baseAlgorithm,
                    () => LeanEngine.RunWithBrokerageAccountMutationsEnabled(
                        baseAlgorithm,
                        RunAlgorithm)));
            }

            Assert.Multiple(() =>
            {
                Assert.IsTrue(acceptedInsideBoundary);
                Assert.IsFalse(baseAlgorithm.RequestBrokerageAccountGroupAssignment(
                    "Account",
                    "Group",
                    null,
                    snapshot));
            });
            groupManager.Verify(instance => instance.RequestAccountGroupAssignment(
                "Account",
                "Group",
                "membership",
                "configuration",
                null), Times.Once);
        }

        [Test]
        public void ControllerRevokesMutationServicesWhileWorkerIsBlocked()
        {
            var baseAlgorithm = CreateMutationServiceAlgorithm(
                out var snapshot,
                out var groupManager);
            using var workerEntered = new ManualResetEventSlim();
            using var releaseWorker = new ManualResetEventSlim();
            Exception workerException = null;
            var acceptedBeforeRevocation = false;
            var acceptedAfterRevocation = false;
            var worker = new Thread(() =>
            {
                try
                {
                    LeanEngine.RunWithBrokerageAccountMutationsEnabled(baseAlgorithm, () =>
                    {
                        acceptedBeforeRevocation = baseAlgorithm.RequestBrokerageAccountGroupAssignment(
                            "Account",
                            "Group",
                            null,
                            snapshot);
                        workerEntered.Set();
                        if (!releaseWorker.Wait(TimeSpan.FromSeconds(10)))
                        {
                            throw new TimeoutException("The mutation lifecycle test worker was not released.");
                        }
                        acceptedAfterRevocation = baseAlgorithm.RequestBrokerageAccountGroupAssignment(
                            "Account",
                            "Group",
                            null,
                            snapshot);
                        return true;
                    });
                }
                catch (Exception exception)
                {
                    workerException = exception;
                }
            })
            {
                IsBackground = true
            };

            var workerStarted = false;
            bool completed;
            bool acceptedByControllerAfterRevocation;
            try
            {
                completed = LeanEngine.ExecuteWithBrokerageAccountMutationLifecycle(
                    baseAlgorithm,
                    () =>
                    {
                        worker.Start();
                        workerStarted = true;
                        if (!workerEntered.Wait(TimeSpan.FromSeconds(10)))
                        {
                            throw new TimeoutException("The mutation lifecycle test worker did not start.");
                        }
                        return false;
                    });
                acceptedByControllerAfterRevocation =
                    baseAlgorithm.RequestBrokerageAccountGroupAssignment(
                        "Account",
                        "Group",
                        null,
                        snapshot);
            }
            finally
            {
                releaseWorker.Set();
            }

            var workerExited = !workerStarted || worker.Join(TimeSpan.FromSeconds(10));
            var acceptedAfterWorkerExit = baseAlgorithm.RequestBrokerageAccountGroupAssignment(
                "Account",
                "Group",
                null,
                snapshot);
            Assert.Multiple(() =>
            {
                Assert.IsFalse(completed);
                Assert.IsTrue(workerExited);
                Assert.IsNull(workerException);
                Assert.IsTrue(acceptedBeforeRevocation);
                Assert.IsFalse(acceptedByControllerAfterRevocation);
                Assert.IsFalse(acceptedAfterRevocation);
                Assert.IsFalse(acceptedAfterWorkerExit);
            });
            groupManager.Verify(instance => instance.RequestAccountGroupAssignment(
                "Account",
                "Group",
                "membership",
                "configuration",
                null), Times.Once);
        }

        [Test]
        public void ControllerRevocationPreventsLateWorkerFromReenablingMutationServices()
        {
            var baseAlgorithm = CreateMutationServiceAlgorithm(
                out var snapshot,
                out var groupManager);

            Assert.IsFalse(LeanEngine.ExecuteWithBrokerageAccountMutationLifecycle(
                baseAlgorithm,
                () => false));

            var acceptedByLateWorker = false;
            Assert.IsTrue(LeanEngine.RunWithBrokerageAccountMutationsEnabled(
                baseAlgorithm,
                () =>
                {
                    acceptedByLateWorker = baseAlgorithm.RequestBrokerageAccountGroupAssignment(
                        "Account",
                        "Group",
                        null,
                        snapshot);
                    return true;
                }));

            Assert.Multiple(() =>
            {
                Assert.IsFalse(acceptedByLateWorker);
                Assert.IsFalse(baseAlgorithm.RequestBrokerageAccountGroupAssignment(
                    "Account",
                    "Group",
                    null,
                    snapshot));
            });
            groupManager.Verify(instance => instance.RequestAccountGroupAssignment(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal?>()), Times.Never);
        }

        [TestCase(false, AlgorithmStatus.Completed)]
        [TestCase(true, AlgorithmStatus.RuntimeError)]
        public void EngineRunControlsBrokerageAccountServiceLifecycle(
            bool throwDuringOnData,
            AlgorithmStatus expectedStatus)
        {
            BrokerageAccountServiceLifecycleAlgorithm.ThrowDuringOnData =
                throwDuringOnData;

            AlgorithmRunner.RunLocalBacktest(
                typeof(BrokerageAccountServiceLifecycleAlgorithm).FullName,
                new Dictionary<string, string>(),
                Language.CSharp,
                expectedStatus,
                setupHandler: nameof(BrokerageAccountServiceBacktestingSetupHandler),
                algorithmLocation: "QuantConnect.Tests.dll");

            var algorithm = BrokerageAccountServiceLifecycleAlgorithm.Instance;
            var brokerage =
                BrokerageAccountServiceBacktestingSetupHandler.Brokerage;
            Assert.IsNotNull(algorithm);
            Assert.IsNotNull(brokerage);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    brokerage.Snapshot.Generation,
                    algorithm.SnapshotGenerationDuringInitialize,
                    "The state provider must be installed before Initialize.");
                Assert.IsTrue(algorithm.RefreshAcceptedDuringInitialize);
                Assert.IsFalse(algorithm.AssignmentAcceptedDuringInitialize);
                Assert.IsFalse(algorithm.AllocationAcceptedDuringInitialize);
                Assert.IsTrue(algorithm.AssignmentAcceptedDuringOnData);
                Assert.AreEqual(!throwDuringOnData, algorithm.OnEndOfAlgorithmWasCalled);
                Assert.AreEqual(!throwDuringOnData, algorithm.AllocationAcceptedDuringOnEnd);
                Assert.AreEqual(1, brokerage.RefreshRequestCount);
                Assert.AreEqual(1, brokerage.AssignmentRequestCount);
                Assert.AreEqual(
                    throwDuringOnData ? 0 : 1,
                    brokerage.AllocationRequestCount);
            });

            var assignmentRequestCount = brokerage.AssignmentRequestCount;
            var allocationRequestCount = brokerage.AllocationRequestCount;
            Assert.Multiple(() =>
            {
                Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAssignment(
                    "Account",
                    "Group",
                    null,
                    brokerage.Snapshot));
                Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                    "Group",
                    new Dictionary<string, decimal> { ["Account"] = 1m },
                    brokerage.Snapshot));
                Assert.AreEqual(
                    assignmentRequestCount,
                    brokerage.AssignmentRequestCount);
                Assert.AreEqual(
                    allocationRequestCount,
                    brokerage.AllocationRequestCount);
            });
        }

        private static void AssertEngineInstallsEveryService()
        {
            var algorithm = new Mock<IAlgorithm>();
            var consumer = algorithm.As<IBrokerageAccountServiceConsumer>();
            var brokerage = new Mock<IBrokerage>();
            var stateProvider = brokerage.As<IBrokerageAccountStateProvider>();
            var groupManager = brokerage.As<IBrokerageAccountGroupManager>();
            var allocationManager = brokerage.As<IBrokerageAccountGroupAllocationManager>();
            var method = typeof(LeanEngine).GetMethod(
                "SetBrokerageAccountServices",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(method);
            method.Invoke(null, new object[] { algorithm.Object, brokerage.Object });

            consumer.Verify(
                instance => instance.SetBrokerageAccountStateProvider(stateProvider.Object),
                Times.Once);
            consumer.Verify(
                instance => instance.SetBrokerageAccountGroupManager(groupManager.Object),
                Times.Once);
            consumer.Verify(
                instance => instance.SetBrokerageAccountGroupAllocationManager(allocationManager.Object),
                Times.Once);
        }

        private static QCAlgorithm CreateMutationServiceAlgorithm(
            out BrokerageAccountSnapshot snapshot,
            out Mock<IBrokerageAccountGroupManager> groupManager)
        {
            var asOfUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            snapshot = new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                asOfUtc,
                asOfUtc,
                new Dictionary<string, BrokerageAccountGroup>(),
                new Dictionary<string, BrokerageAccountState>(),
                Array.Empty<string>(),
                "membership",
                "configuration",
                string.Empty);
            var brokerage = new Mock<IBrokerage>();
            var stateProvider = brokerage.As<IBrokerageAccountStateProvider>();
            groupManager = brokerage.As<IBrokerageAccountGroupManager>();
            stateProvider.Setup(instance => instance.GetAccountSnapshot()).Returns(snapshot);
            groupManager
                .Setup(instance => instance.RequestAccountGroupAssignment(
                    "Account",
                    "Group",
                    "membership",
                    "configuration",
                    null))
                .Returns(true);
            var algorithm = new QCAlgorithm();
            var serviceMethod = typeof(LeanEngine).GetMethod(
                "SetBrokerageAccountServices",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(serviceMethod);
            serviceMethod.Invoke(null, new object[] { algorithm, brokerage.Object });
            algorithm.SetLocked();
            algorithm.SetFinishedWarmingUp();
            return algorithm;
        }

        public class BrokerageAccountServiceLifecycleAlgorithm : BasicTemplateDailyAlgorithm
        {
            public static BrokerageAccountServiceLifecycleAlgorithm Instance { get; private set; }

            public static bool ThrowDuringOnData { get; set; }

            public long SnapshotGenerationDuringInitialize { get; private set; }

            public bool RefreshAcceptedDuringInitialize { get; private set; }

            public bool AssignmentAcceptedDuringInitialize { get; private set; }

            public bool AllocationAcceptedDuringInitialize { get; private set; }

            public bool AssignmentAcceptedDuringOnData { get; private set; }

            public bool OnEndOfAlgorithmWasCalled { get; private set; }

            public bool AllocationAcceptedDuringOnEnd { get; private set; }

            public override void Initialize()
            {
                Instance = this;
                base.Initialize();

                var snapshot = BrokerageAccountSnapshot;
                SnapshotGenerationDuringInitialize = snapshot.Generation;
                RefreshAcceptedDuringInitialize =
                    RequestBrokerageAccountSnapshotRefresh();
                AssignmentAcceptedDuringInitialize =
                    RequestBrokerageAccountGroupAssignment(
                        "Account",
                        "Group",
                        null,
                        snapshot);
                AllocationAcceptedDuringInitialize =
                    RequestBrokerageAccountGroupAllocationUpdate(
                        "Group",
                        new Dictionary<string, decimal> { ["Account"] = 1m },
                        snapshot);
            }

            public override void OnData(Slice slice)
            {
                if (AssignmentAcceptedDuringOnData)
                {
                    return;
                }

                AssignmentAcceptedDuringOnData =
                    RequestBrokerageAccountGroupAssignment(
                        "Account",
                        "Group",
                        null,
                        BrokerageAccountSnapshot);
                if (ThrowDuringOnData)
                {
                    throw new InvalidOperationException(
                        "Expected lifecycle test exception.");
                }
            }

            public override void OnEndOfAlgorithm()
            {
                OnEndOfAlgorithmWasCalled = true;
                AllocationAcceptedDuringOnEnd =
                    RequestBrokerageAccountGroupAllocationUpdate(
                        "Group",
                        new Dictionary<string, decimal> { ["Account"] = 1m },
                        BrokerageAccountSnapshot);
            }

            public static void Reset()
            {
                Instance = null;
                ThrowDuringOnData = false;
            }
        }

        public class BrokerageAccountServiceBacktestingSetupHandler : BacktestingSetupHandler
        {
            public static BrokerageAccountServiceBacktestingBrokerage Brokerage { get; private set; }

            public override IBrokerage CreateBrokerage(
                AlgorithmNodePacket algorithmNodePacket,
                IAlgorithm uninitializedAlgorithm,
                out IBrokerageFactory factory)
            {
                factory = new BacktestingBrokerageFactory();
                Brokerage = new BrokerageAccountServiceBacktestingBrokerage(
                    uninitializedAlgorithm);
                return Brokerage;
            }

            public static void Reset()
            {
                Brokerage = null;
            }
        }

        public class BrokerageAccountServiceBacktestingBrokerage :
            BacktestingBrokerage,
            IBrokerageAccountStateProvider,
            IBrokerageAccountGroupManager,
            IBrokerageAccountGroupAllocationManager
        {
            public BrokerageAccountSnapshot Snapshot { get; }

            public int RefreshRequestCount { get; private set; }

            public int AssignmentRequestCount { get; private set; }

            public int AllocationRequestCount { get; private set; }

            public BrokerageAccountServiceBacktestingBrokerage(IAlgorithm algorithm)
                : base(algorithm)
            {
                var now = new DateTime(
                    2026,
                    1,
                    2,
                    3,
                    4,
                    5,
                    DateTimeKind.Utc);
                Snapshot = new BrokerageAccountSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    1,
                    now,
                    now,
                    new Dictionary<string, BrokerageAccountGroup>(),
                    new Dictionary<string, BrokerageAccountState>(),
                    Array.Empty<string>(),
                    "membership",
                    "configuration",
                    string.Empty);
            }

            public BrokerageAccountSnapshot GetAccountSnapshot()
            {
                return Snapshot;
            }

            public bool RequestAccountSnapshotRefresh(
                IReadOnlyCollection<string> groupNames,
                IReadOnlyCollection<string> additionalAccountIds)
            {
                RefreshRequestCount++;
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
                AssignmentRequestCount++;
                return true;
            }

            public BrokerageAccountGroupAllocationUpdate
                GetAccountGroupAllocationUpdate()
            {
                return BrokerageAccountGroupAllocationUpdate.Unavailable;
            }

            public bool RequestAccountGroupAllocationUpdate(
                string groupName,
                IReadOnlyDictionary<string, decimal> accountAllocationValues,
                string expectedMembershipHash,
                string expectedGroupConfigurationVersion)
            {
                AllocationRequestCount++;
                return true;
            }
        }
    }
}
