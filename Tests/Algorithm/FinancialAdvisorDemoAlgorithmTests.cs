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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class FinancialAdvisorDemoAlgorithmTests
    {
        private const string GroupName = "TestGroupEQ";
        private static readonly DateTime SnapshotTime =
            new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        [TestCase(BrokerageAccountSnapshotStatus.Failed)]
        [TestCase(BrokerageAccountSnapshotStatus.Stale)]
        public void TerminalGroupOrderUsesSnapshotReconciliationTest(
            BrokerageAccountSnapshotStatus failureStatus)
        {
            var preOrderSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                5,
                10m,
                20m,
                SnapshotTime.AddMinutes(-2));
            var terminalTimeSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                11m,
                19m,
                SnapshotTime.AddSeconds(-10));
            var failedRefreshSnapshot = CreateSnapshot(
                failureStatus,
                8,
                13m,
                18m,
                SnapshotTime.AddSeconds(-5));
            var causallyOldSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                8,
                13m,
                18m,
                SnapshotTime.AddTicks(-1));
            var causallyValidSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                9,
                13m,
                18m,
                SnapshotTime);
            var provider = new TestAccountStateProvider
            {
                Snapshot = terminalTimeSnapshot
            };
            provider.RefreshRequestHook = stateProvider =>
                stateProvider.Snapshot = stateProvider.RefreshRequestCount switch
                {
                    1 => failedRefreshSnapshot,
                    2 => causallyOldSnapshot,
                    _ => causallyValidSnapshot
                };
            var algorithm = CreateAlgorithm(provider, preOrderSnapshot);

            algorithm.OnOrderEvent(CreateOrderEvent(41, OrderStatus.Submitted));
            algorithm.OnOrderEvent(CreateOrderEvent(42, OrderStatus.Filled));

            Assert.AreEqual(0, provider.RefreshRequestCount);

            algorithm.OnOrderEvent(CreateOrderEvent(41, OrderStatus.Filled));
            Assert.AreEqual(
                1,
                provider.RefreshRequestCount,
                "The installed terminal order must request reconciliation directly.");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, provider.RefreshRequestCount);
                Assert.AreEqual(
                    7,
                    provider.GenerationObservedAtRefreshRequest);
                CollectionAssert.AreEqual(
                    new[] { GroupName },
                    provider.RequestedGroups);
                CollectionAssert.IsEmpty(provider.RequestedAdditionalAccounts);
                Assert.AreSame(failedRefreshSnapshot, provider.Snapshot);
            });

            var messageCount = algorithm.LogMessages.Count;
            algorithm.OnData(CreateEmptySlice());
            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, provider.RefreshRequestCount);
                Assert.AreEqual(messageCount, algorithm.LogMessages.Count);
            });

            algorithm.SetDateTime(SnapshotTime.AddSeconds(5));
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(2, provider.RefreshRequestCount);
            Assert.AreSame(causallyOldSnapshot, provider.Snapshot);

            algorithm.OnData(CreateEmptySlice());
            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    messageCount,
                    algorithm.LogMessages.Count,
                    "A newer snapshot collected before the terminal event must not reconcile.");
                Assert.AreEqual(2, provider.RefreshRequestCount);
            });

            algorithm.SetDateTime(SnapshotTime.AddSeconds(10));
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(3, provider.RefreshRequestCount);
            Assert.AreSame(causallyValidSnapshot, provider.Snapshot);

            algorithm.OnData(CreateEmptySlice());

            var reconciliationMessages = algorithm.LogMessages
                .Skip(messageCount)
                .ToArray();
            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, reconciliationMessages.Length);
                Assert.That(
                    reconciliationMessages,
                    Has.One.Contains(
                        "account=AccountA, symbol=SPY, before=10, after=13, change=3"));
                Assert.That(
                    reconciliationMessages,
                    Has.One.Contains(
                        "account=AccountB, symbol=SPY, before=20, after=18, change=-2"));
            });

            algorithm.OnData(CreateEmptySlice());
            algorithm.OnOrderEvent(CreateOrderEvent(41, OrderStatus.Filled));

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    messageCount + 2,
                    algorithm.LogMessages.Count,
                    "A completed reconciliation must not run a second time.");
                Assert.AreEqual(
                    3,
                    provider.RefreshRequestCount,
                    "A duplicate terminal event must not request another refresh.");
            });
        }

        [Test]
        public void RejectedReconciliationRefreshRetriesAfterCadenceAndSkipsRefreshingTest()
        {
            var preOrderSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                5,
                10m,
                20m,
                SnapshotTime.AddMinutes(-2));
            var terminalTimeSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                11m,
                19m,
                SnapshotTime.AddSeconds(-10));
            var refreshingSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Refreshing,
                7,
                11m,
                19m,
                SnapshotTime);
            var failedSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Failed,
                7,
                11m,
                19m,
                SnapshotTime);
            var reconciledSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                8,
                13m,
                18m,
                SnapshotTime);
            var provider = new TestAccountStateProvider
            {
                Snapshot = terminalTimeSnapshot
            };
            provider.EnqueueRefreshResult(false);
            provider.EnqueueRefreshResult(true);
            provider.RefreshRequestHook = stateProvider =>
                stateProvider.Snapshot = reconciledSnapshot;
            var algorithm = CreateAlgorithm(provider, preOrderSnapshot);

            algorithm.OnOrderEvent(CreateOrderEvent(41, OrderStatus.Filled));
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(
                1,
                provider.RefreshRequestCount,
                "A rejected refresh must not retry before the bounded cadence.");

            algorithm.SetDateTime(SnapshotTime.AddSeconds(5));
            provider.Snapshot = refreshingSnapshot;
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(
                1,
                provider.RefreshRequestCount,
                "A reconciliation request must not overlap an active refresh.");

            provider.Snapshot = failedSnapshot;
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(2, provider.RefreshRequestCount);
            Assert.AreSame(reconciledSnapshot, provider.Snapshot);

            var messageCount = algorithm.LogMessages.Count;
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(messageCount + 2, algorithm.LogMessages.Count);
        }

        [Test]
        public void TerminalEventDuringSetHoldingsStartsCausalReconciliation()
        {
            var snapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                10m,
                20m,
                SnapshotTime.AddMinutes(-1));
            var provider = new TestAccountStateProvider
            {
                Snapshot = snapshot
            };
            var algorithm = CreateAlgorithm(provider, snapshot);
            SetPrivateField(algorithm, "_groupOrderSubmitted", false);
            SetPrivateField(algorithm, "_groupOrderId", 0);
            SetPrivateField(
                algorithm,
                "_initialSnapshotRequestGeneration",
                snapshot.Generation - 1);
            var ticket = CreateOrderTicket(
                algorithm,
                41,
                OrderStatus.New);
            SetPrivateField(
                algorithm,
                "_submitGroupOrder",
                new Func<OrderTicket>(() =>
                {
                    algorithm.OnOrderEvent(
                        CreateOrderEvent(
                            41,
                            OrderStatus.Filled));
                    return ticket;
                }));

            long generationObservedDuringRefresh = -1;
            DateTime terminalObservedDuringRefresh = default;
            provider.RefreshRequestHook = _ =>
            {
                generationObservedDuringRefresh =
                    GetPrivateField<long>(
                        algorithm,
                        "_pendingReconcileGeneration");
                terminalObservedDuringRefresh =
                    GetPrivateField<DateTime>(
                        algorithm,
                        "_pendingReconcileTerminalUtc");
            };

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, provider.RefreshRequestCount);
                Assert.AreEqual(
                    snapshot.Generation,
                    generationObservedDuringRefresh);
                Assert.AreEqual(
                    SnapshotTime,
                    terminalObservedDuringRefresh,
                    "The terminal timestamp must be visible before the " +
                    "pending generation triggers a refresh.");
                Assert.AreEqual(
                    0,
                    GetPrivateField<int>(
                        algorithm,
                        "_groupOrderId"));
            });
        }

        [Test]
        public void SynchronousInvalidOrderLogsReasonAndRetriesAfterBoundedDelay()
        {
            var snapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                10m,
                20m,
                SnapshotTime.AddMinutes(-1));
            var provider = new TestAccountStateProvider
            {
                Snapshot = snapshot
            };
            var algorithm = CreateAlgorithm(provider, snapshot);
            SetPrivateField(algorithm, "_groupOrderSubmitted", false);
            SetPrivateField(algorithm, "_groupOrderId", 0);
            SetPrivateField(
                algorithm,
                "_initialSnapshotRequestGeneration",
                snapshot.Generation - 1);
            var invalidTicket = CreateOrderTicket(
                algorithm,
                41,
                OrderStatus.Invalid,
                "saved allocation total is not lot-aligned");
            var retryTicket = CreateOrderTicket(
                algorithm,
                42,
                OrderStatus.New);
            var submissionCount = 0;
            SetPrivateField(
                algorithm,
                "_submitGroupOrder",
                new Func<OrderTicket>(() =>
                {
                    ++submissionCount;
                    if (submissionCount == 1)
                    {
                        algorithm.OnOrderEvent(
                            CreateOrderEvent(
                                41,
                                OrderStatus.Invalid,
                                "saved allocation total is not lot-aligned"));
                        return invalidTicket;
                    }
                    return retryTicket;
                }));

            algorithm.OnData(CreateEmptySlice());
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    1,
                    submissionCount,
                    "The retry must respect its bounded delay.");
                Assert.IsFalse(
                    GetPrivateField<bool>(
                        algorithm,
                        "_groupOrderSubmitted"));
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains(
                        "saved allocation total is not lot-aligned"));
            });

            algorithm.SetDateTime(SnapshotTime.AddSeconds(5));
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, submissionCount);
                Assert.IsTrue(
                    GetPrivateField<bool>(
                        algorithm,
                        "_groupOrderSubmitted"));
                Assert.AreEqual(
                    42,
                    GetPrivateField<int>(
                        algorithm,
                        "_groupOrderId"));
                Assert.AreEqual(0, provider.RefreshRequestCount);
            });
        }

        [Test]
        public void ScheduledRefreshPolicyUsesTwoScopedTicksThenOneCompleteTick()
        {
            var snapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                10m,
                20m,
                SnapshotTime);
            var provider = new TestAccountStateProvider
            {
                Snapshot = snapshot
            };
            var algorithm = CreateAlgorithm(provider, snapshot);
            SetPrivateField(algorithm, "_groupOrderId", 0);

            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");
            Assert.AreEqual(
                1,
                provider.RefreshRequestCount,
                "The scheduled callback must request directly.");
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(3, provider.RefreshRequestCount);
                Assert.AreEqual(3, provider.RequestedGroupHistory.Count);
                CollectionAssert.AreEqual(
                    new[] { GroupName },
                    provider.RequestedGroupHistory[0]);
                CollectionAssert.AreEqual(
                    new[] { GroupName },
                    provider.RequestedGroupHistory[1]);
                CollectionAssert.IsEmpty(
                    provider.RequestedGroupHistory[2],
                    "The third topology tick must issue one complete request.");
            });

            SetPrivateField(algorithm, "_groupOrderId", 41);
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");
            Assert.AreEqual(
                3,
                provider.RefreshRequestCount,
                "Scheduled policy must yield while the parent group order is active.");

            algorithm.OnOrderEvent(
                CreateOrderEvent(41, OrderStatus.Filled));
            Assert.AreEqual(
                4,
                provider.RefreshRequestCount,
                "The terminal event must request reconciliation directly.");

            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");

            Assert.AreEqual(
                4,
                provider.RefreshRequestCount,
                "Scheduled policy must yield to terminal-order reconciliation.");
        }

        [Test]
        public void ConcurrentScheduledCallbackPreservesOneLaterRefreshIntent()
        {
            var firstSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                10m,
                20m,
                SnapshotTime);
            var secondSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                8,
                10m,
                20m,
                SnapshotTime.AddTicks(1));
            var thirdSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                9,
                10m,
                20m,
                SnapshotTime.AddTicks(2));
            var provider = new TestAccountStateProvider
            {
                Snapshot = firstSnapshot
            };
            var algorithm = CreateAlgorithm(provider, firstSnapshot);
            SetPrivateField(algorithm, "_groupOrderId", 0);
            using var requestEntered = new ManualResetEventSlim();
            using var releaseRequest = new ManualResetEventSlim();
            provider.RefreshRequestHook = stateProvider =>
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

            Assert.AreEqual(
                2,
                provider.RefreshRequestCount,
                "Each concurrent callback must directly consume one coalesced intent.");
        }

        [Test]
        public void ScheduledCallbackDuringOnDataIsRetainedForOnDataOwner()
        {
            var snapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                10m,
                20m,
                SnapshotTime);
            var provider = new TestAccountStateProvider
            {
                Snapshot = snapshot
            };
            var algorithm = CreateAlgorithm(provider, snapshot);
            SetPrivateField(algorithm, "_groupOrderId", 0);
            using var snapshotRead = new ManualResetEventSlim();
            using var releaseSnapshotRead = new ManualResetEventSlim();
            provider.SnapshotReadHook = () =>
            {
                snapshotRead.Set();
                Assert.IsTrue(
                    releaseSnapshotRead.Wait(TimeSpan.FromSeconds(10)));
            };

            var onData = Task.Run(
                () => algorithm.OnData(CreateEmptySlice()));
            Assert.IsTrue(
                snapshotRead.Wait(TimeSpan.FromSeconds(10)));

            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");
            Assert.AreEqual(
                0,
                provider.RefreshRequestCount,
                "The callback must defer while OnData owns the state machine.");

            releaseSnapshotRead.Set();
            Assert.IsTrue(
                onData.Wait(TimeSpan.FromSeconds(10)));

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    1,
                    provider.RefreshRequestCount,
                    "OnData must consume the callback's retained intent.");
                CollectionAssert.AreEqual(
                    new[] { GroupName },
                    provider.RequestedGroupHistory[0]);
                Assert.AreEqual(
                    0,
                    GetPrivateField<int>(
                        algorithm,
                        "_scheduledRefreshIntent"));
            });
        }

        [Test]
        public void LiveCallbacksRefreshWithoutOnData()
        {
            var provider = new TestAccountStateProvider
            {
                Snapshot = CreateSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    7,
                    10m,
                    20m,
                    SnapshotTime)
            };
            var algorithm = CreateInitializedAlgorithm(provider);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    1,
                    provider.RefreshRequestCount,
                    "Initialize must issue the first scoped request.");
                CollectionAssert.AreEqual(
                    new[] { GroupName },
                    provider.RequestedGroupHistory[0]);
            });

            for (var tick = 1; tick <= 3; ++tick)
            {
                algorithm.SetDateTime(
                    SnapshotTime.AddMinutes(tick));
                provider.Snapshot = CreateSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    7 + tick,
                    10m,
                    20m,
                    SnapshotTime.AddMinutes(tick));
                InvokePrivateMethod(
                    algorithm,
                    "RequestScheduledSnapshotRefresh");
            }

            Assert.Multiple(() =>
            {
                Assert.AreEqual(4, provider.RefreshRequestCount);
                CollectionAssert.AreEqual(
                    new[] { GroupName },
                    provider.RequestedGroupHistory[1]);
                CollectionAssert.AreEqual(
                    new[] { GroupName },
                    provider.RequestedGroupHistory[2]);
                CollectionAssert.IsEmpty(
                    provider.RequestedGroupHistory[3],
                    "The third callback must request complete state.");
            });

            var terminalSnapshot = provider.Snapshot;
            SetPrivateField(
                algorithm,
                "_preOrderSnapshot",
                terminalSnapshot);
            SetPrivateField(
                algorithm,
                "_groupOrderSubmitted",
                true);
            SetPrivateField(
                algorithm,
                "_groupOrderId",
                41);
            long pendingGenerationAtRequest = -1;
            DateTime terminalUtcAtRequest = default;
            provider.RefreshRequestHook = _ =>
            {
                pendingGenerationAtRequest =
                    GetPrivateField<long>(
                        algorithm,
                        "_pendingReconcileGeneration");
                terminalUtcAtRequest =
                    GetPrivateField<DateTime>(
                        algorithm,
                        "_pendingReconcileTerminalUtc");
            };

            algorithm.OnOrderEvent(
                CreateOrderEvent(42, OrderStatus.Filled));
            algorithm.OnOrderEvent(
                CreateOrderEvent(41, OrderStatus.Filled));
            algorithm.OnOrderEvent(
                CreateOrderEvent(41, OrderStatus.Filled));

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    5,
                    provider.RefreshRequestCount,
                    "Only the installed terminal order may request reconciliation.");
                CollectionAssert.AreEqual(
                    new[] { GroupName },
                    provider.RequestedGroupHistory[4]);
                Assert.AreEqual(
                    terminalSnapshot.Generation,
                    pendingGenerationAtRequest);
                Assert.AreEqual(
                    SnapshotTime,
                    terminalUtcAtRequest);
            });

            var messageCount = algorithm.LogMessages.Count;
            provider.Snapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                terminalSnapshot.Generation + 1,
                13m,
                18m,
                SnapshotTime.AddMinutes(3).AddTicks(1));
            algorithm.SetDateTime(
                SnapshotTime.AddMinutes(4));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    5,
                    provider.RefreshRequestCount,
                    "The callback must consume the completed reconciliation " +
                    "before issuing another cadence request.");
                Assert.AreEqual(
                    -1,
                    GetPrivateField<long>(
                        algorithm,
                        "_pendingReconcileGeneration"));
                Assert.IsNull(
                    GetPrivateField<BrokerageAccountSnapshot>(
                        algorithm,
                        "_preOrderSnapshot"));
                Assert.That(
                    algorithm.LogMessages.Skip(messageCount),
                    Has.One.Contains(
                        "account=AccountA, symbol=SPY, before=10, after=13, change=3"));
                Assert.That(
                    algorithm.LogMessages.Skip(messageCount),
                    Has.One.Contains(
                        "account=AccountB, symbol=SPY, before=20, after=18, change=-2"));
            });
        }

        [Test]
        public void ScheduledCallbackRetriesRejectedInitialRequestWithoutOnData()
        {
            var provider = new TestAccountStateProvider
            {
                Snapshot = CreateSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    7,
                    10m,
                    20m,
                    SnapshotTime)
            };
            provider.EnqueueRefreshResult(false);
            var algorithm = CreateInitializedAlgorithm(provider);

            algorithm.SetDateTime(
                SnapshotTime.AddMinutes(1));
            InvokePrivateMethod(
                algorithm,
                "RequestScheduledSnapshotRefresh");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    2,
                    provider.RefreshRequestCount,
                    "The next scheduled callback must retry rejected discovery.");
                CollectionAssert.AreEqual(
                    new[] { GroupName },
                    provider.RequestedGroupHistory[0]);
                CollectionAssert.AreEqual(
                    new[] { GroupName },
                    provider.RequestedGroupHistory[1]);
            });
        }

        [TestCase(BrokerageAccountSnapshotStatus.Failed)]
        [TestCase(BrokerageAccountSnapshotStatus.Stale)]
        public void InitialSnapshotRefreshRetriesRejectedAndFailedResults(
            BrokerageAccountSnapshotStatus failureStatus)
        {
            var provider = new TestAccountStateProvider
            {
                Snapshot = CreateSnapshot(
                    failureStatus,
                    3,
                    10m,
                    20m,
                    SnapshotTime)
            };
            provider.EnqueueRefreshResult(false);
            provider.EnqueueRefreshResult(true);
            var algorithm = new FinancialAdvisorDemoAlgorithm();
            SetPrivateField(
                algorithm,
                typeof(QCAlgorithm),
                "_liveMode",
                true);
            ((IBrokerageAccountServiceConsumer)algorithm)
                .SetBrokerageAccountStateProvider(provider);
            algorithm.SetDateTime(SnapshotTime);
            SetPrivateField(
                algorithm,
                "_initialSnapshotRefreshAccepted",
                true);
            SetPrivateField(
                algorithm,
                "_nextReconcileRefreshUtc",
                SnapshotTime);

            algorithm.OnData(CreateEmptySlice());
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(
                1,
                provider.RefreshRequestCount,
                "A rejected initial refresh must retain cadence-limited retry intent.");

            algorithm.SetDateTime(SnapshotTime.AddSeconds(5));
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(
                2,
                provider.RefreshRequestCount,
                "The rejected initial refresh must be retried.");

            algorithm.SetDateTime(SnapshotTime.AddSeconds(10));
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(
                3,
                provider.RefreshRequestCount,
                "An accepted refresh that remains Failed or Stale must be retried.");
        }

        private static FinancialAdvisorDemoAlgorithm CreateAlgorithm(
            TestAccountStateProvider provider,
            BrokerageAccountSnapshot preOrderSnapshot)
        {
            var algorithm = new FinancialAdvisorDemoAlgorithm();
            SetPrivateField(
                algorithm,
                typeof(QCAlgorithm),
                "_liveMode",
                true);
            ((IBrokerageAccountServiceConsumer)algorithm)
                .SetBrokerageAccountStateProvider(provider);
            algorithm.SetDateTime(SnapshotTime);
            SetPrivateField(algorithm, "_symbol", Symbols.SPY);
            SetPrivateField(algorithm, "_preOrderSnapshot", preOrderSnapshot);
            SetPrivateField(algorithm, "_initialSnapshotRefreshAccepted", true);
            SetPrivateField(algorithm, "_groupOrderSubmitted", true);
            SetPrivateField(algorithm, "_groupOrderId", 41);
            return algorithm;
        }

        private static FinancialAdvisorDemoAlgorithm
            CreateInitializedAlgorithm(
                TestAccountStateProvider provider)
        {
            var algorithm = new FinancialAdvisorDemoAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(
                new DataManagerStub(algorithm));
            algorithm.SetLiveMode(true);
            algorithm.SetDateTime(SnapshotTime);
            ((IBrokerageAccountServiceConsumer)algorithm)
                .SetBrokerageAccountStateProvider(provider);
            algorithm.Initialize();
            algorithm.SetLocked();
            return algorithm;
        }

        private static OrderEvent CreateOrderEvent(
            int orderId,
            OrderStatus status,
            string message = null)
        {
            return new OrderEvent(
                orderId,
                Symbols.SPY,
                SnapshotTime,
                status,
                OrderDirection.Buy,
                100m,
                status == OrderStatus.Filled ? 5m : 0m,
                OrderFee.Zero)
            {
                Message = message
            };
        }

        private static OrderTicket CreateOrderTicket(
            FinancialAdvisorDemoAlgorithm algorithm,
            int orderId,
            OrderStatus status,
            string errorMessage = null)
        {
            var request = new SubmitOrderRequest(
                OrderType.Market,
                SecurityType.Equity,
                Symbols.SPY,
                1m,
                0m,
                0m,
                SnapshotTime,
                string.Empty,
                asynchronous: true);
            request.SetOrderId(orderId);
            if (status == OrderStatus.Invalid)
            {
                return OrderTicket.InvalidSubmitRequest(
                    algorithm.Transactions,
                    request,
                    OrderResponse.Error(
                        request,
                        OrderResponseErrorCode
                            .BrokerageFailedToSubmitOrder,
                        errorMessage));
            }
            return new OrderTicket(
                algorithm.Transactions,
                request);
        }

        private static Slice CreateEmptySlice()
        {
            return new Slice(
                SnapshotTime,
                Array.Empty<BaseData>(),
                SnapshotTime);
        }

        private static BrokerageAccountSnapshot CreateSnapshot(
            BrokerageAccountSnapshotStatus status,
            long generation,
            decimal accountAQuantity,
            decimal accountBQuantity,
            DateTime collectionStartedUtc)
        {
            var group = new BrokerageAccountGroup(
                GroupName,
                "Equal",
                new[] { "AccountA", "AccountB" });
            var groups = new Dictionary<string, BrokerageAccountGroup>
            {
                [GroupName] = group
            };
            var accounts = new Dictionary<string, BrokerageAccountState>
            {
                ["AccountA"] = CreateAccount("AccountA", accountAQuantity),
                ["AccountB"] = CreateAccount("AccountB", accountBQuantity)
            };

            return new BrokerageAccountSnapshot(
                status,
                generation,
                SnapshotTime.AddTicks(generation),
                SnapshotTime,
                groups,
                accounts,
                Array.Empty<string>(),
                "membership",
                "configuration",
                string.Empty,
                managedAccountIds: new[] { "AccountA", "AccountB" },
                collectionStartedUtc: collectionStartedUtc);
        }

        private static BrokerageAccountState CreateAccount(
            string accountId,
            decimal quantity)
        {
            return new BrokerageAccountState(
                accountId,
                new[] { GroupName },
                "INDIVIDUAL",
                null,
                null,
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

        private static void SetPrivateField<T>(
            FinancialAdvisorDemoAlgorithm algorithm,
            string name,
            T value)
        {
            SetPrivateField(
                algorithm,
                typeof(FinancialAdvisorDemoAlgorithm),
                name,
                value);
        }

        private static void SetPrivateField<T>(
            object instance,
            Type declaringType,
            string name,
            T value)
        {
            var field = declaringType.GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Private field '{name}' was not found.");
            field.SetValue(instance, value);
        }

        private static T GetPrivateField<T>(
            object instance,
            string name)
        {
            var field = instance.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Private field '{name}' was not found.");
            return (T)field.GetValue(instance);
        }

        private static void InvokePrivateMethod(
            object instance,
            string name)
        {
            var method = instance.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Private method '{name}' was not found.");
            method.Invoke(instance, null);
        }

        private sealed class TestAccountStateProvider :
            IBrokerageAccountStateProvider
        {
            public BrokerageAccountSnapshot Snapshot { get; set; }
            public int RefreshRequestCount { get; private set; }
            public long GenerationObservedAtRefreshRequest { get; private set; }
            public Action<TestAccountStateProvider> RefreshRequestHook { get; set; }
            public Action SnapshotReadHook { get; set; }
            public IReadOnlyCollection<string> RequestedGroups { get; private set; }
            public IReadOnlyCollection<string> RequestedAdditionalAccounts { get; private set; }
            public List<IReadOnlyCollection<string>> RequestedGroupHistory { get; } =
                new();
            private readonly Queue<bool> _refreshResults = new();

            public void EnqueueRefreshResult(bool accepted) =>
                _refreshResults.Enqueue(accepted);

            public BrokerageAccountSnapshot GetAccountSnapshot()
            {
                var hook = SnapshotReadHook;
                SnapshotReadHook = null;
                hook?.Invoke();
                return Snapshot;
            }

            public bool RequestAccountSnapshotRefresh(
                IReadOnlyCollection<string> groupNames,
                IReadOnlyCollection<string> additionalAccountIds)
            {
                ++RefreshRequestCount;
                GenerationObservedAtRefreshRequest = Snapshot.Generation;
                RequestedGroups = groupNames.ToArray();
                RequestedAdditionalAccounts = additionalAccountIds.ToArray();
                RequestedGroupHistory.Add(RequestedGroups);
                var accepted =
                    _refreshResults.Count == 0 || _refreshResults.Dequeue();
                if (accepted)
                {
                    RefreshRequestHook?.Invoke(this);
                }
                return accepted;
            }
        }
    }
}
