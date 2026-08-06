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
using Moq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class FinancialAdvisorUnifiedGroupsDemoAlgorithmTests
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
                CollectionAssert.AreEquivalent(
                    new[] { "AccountA", "AccountB" },
                    provider.RequestedAdditionalAccounts);
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
        public void MissingMemberStateKeepsInvalidReconciliationPendingUntilNewerCompleteSnapshot()
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
            var missingMemberSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                8,
                13m,
                18m,
                SnapshotTime,
                includeAccountB: false);
            var completeSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                9,
                13m,
                18m,
                SnapshotTime.AddTicks(1));
            var provider = new TestAccountStateProvider
            {
                Snapshot = terminalTimeSnapshot
            };
            provider.RefreshRequestHook = stateProvider =>
                stateProvider.Snapshot = stateProvider.RefreshRequestCount == 1
                    ? missingMemberSnapshot
                    : completeSnapshot;
            var algorithm = CreateAlgorithm(provider, preOrderSnapshot);

            algorithm.OnOrderEvent(
                CreateOrderEvent(
                    41,
                    OrderStatus.Invalid,
                    "distinctive rejection"));

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, provider.RefreshRequestCount);
                CollectionAssert.AreEquivalent(
                    new[] { "AccountA", "AccountB" },
                    provider.RequestedAdditionalAccountHistory[0]);
                Assert.AreEqual(
                    0,
                    GetPrivateField<int>(
                        algorithm,
                        "_invalidOrderAttemptCount"));
            });

            algorithm.SetDateTime(SnapshotTime.AddSeconds(5));
            var reconciliationMessageCount = algorithm.LogMessages.Count(
                message => message.Contains("FA reconciliation"));
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, provider.RefreshRequestCount);
                CollectionAssert.AreEquivalent(
                    new[] { "AccountA", "AccountB" },
                    provider.RequestedAdditionalAccountHistory[1]);
                Assert.AreEqual(
                    missingMemberSnapshot.Generation,
                    GetPrivateField<long>(
                        algorithm,
                        "_pendingReconcileGeneration"),
                    "The incomplete generation must not be reconsidered.");
                Assert.AreSame(
                    preOrderSnapshot,
                    GetPrivateField<BrokerageAccountSnapshot>(
                        algorithm,
                        "_preOrderSnapshot"));
                Assert.IsTrue(
                    GetPrivateField<bool>(
                        algorithm,
                        "_groupOrderSubmitted"));
                Assert.AreEqual(
                    0,
                    GetPrivateField<int>(
                        algorithm,
                        "_invalidOrderAttemptCount"),
                    "Missing account state must not authorize an Invalid retry.");
                Assert.AreEqual(
                    reconciliationMessageCount,
                    algorithm.LogMessages.Count(
                        message => message.Contains("FA reconciliation")),
                    "An incomplete snapshot must not emit partial reconciliation.");
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains("Account state for 'AccountB' is unavailable"));
            });

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    -1,
                    GetPrivateField<long>(
                        algorithm,
                        "_pendingReconcileGeneration"));
                Assert.IsNull(
                    GetPrivateField<BrokerageAccountSnapshot>(
                        algorithm,
                        "_preOrderSnapshot"));
                Assert.IsFalse(
                    GetPrivateField<bool>(
                        algorithm,
                        "_groupOrderSubmitted"));
                Assert.AreEqual(
                    1,
                    GetPrivateField<int>(
                        algorithm,
                        "_invalidOrderAttemptCount"));
                Assert.AreEqual(
                    reconciliationMessageCount + 2,
                    algorithm.LogMessages.Count(
                        message => message.Contains("FA reconciliation")));
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
        public void TerminalEventDuringMarketOrderStartsCausalReconciliation()
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
            var submissionCount = 0;
            SetOrderProcessor(
                algorithm,
                request =>
                {
                    ++submissionCount;
                    algorithm.OnOrderEvent(
                        CreateOrderEvent(
                            request.OrderId,
                            OrderStatus.Filled));
                    return new OrderTicket(
                        algorithm.Transactions,
                        request);
                });

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
                Assert.AreEqual(1, submissionCount);
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
        public void InvalidAfterPartialFillReconcilesBeforeBoundedRetry()
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
            provider.RefreshRequestHook = stateProvider =>
                stateProvider.Snapshot = CreateSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    snapshot.Generation + 1,
                    12m,
                    20m,
                    SnapshotTime);
            var algorithm = CreateAlgorithm(provider, snapshot);
            SetPrivateField(algorithm, "_groupOrderSubmitted", false);
            SetPrivateField(algorithm, "_groupOrderId", 0);
            SetPrivateField(
                algorithm,
                "_initialSnapshotRequestGeneration",
                snapshot.Generation - 1);
            var submissionCount = 0;
            var retryOrderId = 0;
            SetOrderProcessor(
                algorithm,
                request =>
                {
                    ++submissionCount;
                    if (submissionCount == 1)
                    {
                        algorithm.OnOrderEvent(
                            CreateOrderEvent(
                                request.OrderId,
                                OrderStatus.PartiallyFilled));
                        algorithm.OnOrderEvent(
                            CreateOrderEvent(
                                request.OrderId,
                                OrderStatus.Invalid,
                                "saved allocation total is not lot-aligned"));
                        return OrderTicket.InvalidSubmitRequest(
                            algorithm.Transactions,
                            request,
                            OrderResponse.Error(
                                request,
                                OrderResponseErrorCode
                                    .BrokerageFailedToSubmitOrder,
                                "saved allocation total is not lot-aligned"));
                    }
                    retryOrderId = request.OrderId;
                    return new OrderTicket(
                        algorithm.Transactions,
                        request);
                });

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    1,
                    submissionCount,
                    "The Invalid parent must not retry before reconciliation.");
                Assert.IsTrue(
                    GetPrivateField<bool>(
                        algorithm,
                        "_groupOrderSubmitted"));
                Assert.AreEqual(1, provider.RefreshRequestCount);
                Assert.AreEqual(
                    snapshot.Generation,
                    GetPrivateField<long>(
                        algorithm,
                        "_pendingReconcileGeneration"));
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains(
                        "saved allocation total is not lot-aligned"));
            });

            var messageCount = algorithm.LogMessages.Count;
            algorithm.OnData(CreateEmptySlice());
            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, submissionCount);
                Assert.IsFalse(
                    GetPrivateField<bool>(
                        algorithm,
                        "_groupOrderSubmitted"));
                Assert.AreEqual(
                    -1,
                    GetPrivateField<long>(
                        algorithm,
                        "_pendingReconcileGeneration"));
                Assert.That(
                    algorithm.LogMessages.Skip(messageCount),
                    Has.One.Contains(
                        "account=AccountA, symbol=SPY, before=10, after=12, change=2"));
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
                    retryOrderId,
                    GetPrivateField<int>(
                        algorithm,
                        "_groupOrderId"));
                Assert.AreEqual(
                    1,
                    GetPrivateField<int>(
                        algorithm,
                        "_invalidOrderAttemptCount"),
                    "A nonterminal retry must not erase earlier invalid attempts.");
                Assert.AreEqual(1, provider.RefreshRequestCount);
            });
        }

        [Test]
        public void NonInvalidTerminalResetsRetryCountOnlyAfterReconciliation()
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
            provider.RefreshRequestHook = stateProvider =>
                stateProvider.Snapshot = CreateSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    snapshot.Generation + 1,
                    11m,
                    20m,
                    SnapshotTime);
            var algorithm = CreateAlgorithm(provider, snapshot);
            SetPrivateField(algorithm, "_preOrderSnapshot", snapshot);
            SetPrivateField(algorithm, "_groupOrderSubmitted", true);
            SetPrivateField(algorithm, "_groupOrderId", 41);
            SetPrivateField(algorithm, "_invalidOrderAttemptCount", 1);

            algorithm.OnOrderEvent(
                CreateOrderEvent(41, OrderStatus.Filled));

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, provider.RefreshRequestCount);
                Assert.AreEqual(
                    1,
                    GetPrivateField<int>(
                        algorithm,
                        "_invalidOrderAttemptCount"),
                    "A terminal callback alone is not authoritative reconciliation.");
            });

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    0,
                    GetPrivateField<int>(
                        algorithm,
                        "_invalidOrderAttemptCount"));
                Assert.AreEqual(
                    -1,
                    GetPrivateField<long>(
                        algorithm,
                        "_pendingReconcileGeneration"));
            });
        }

        [Test]
        public void SynchronousMarketOrderFailureFailsClosedAndRethrows()
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
            SetPrivateField(algorithm, "_groupOrderSubmitted", false);
            SetPrivateField(algorithm, "_groupOrderId", 0);
            SetPrivateField(
                algorithm,
                "_initialSnapshotRequestGeneration",
                snapshot.Generation - 1);
            var submissionCount = 0;
            SetOrderProcessor(
                algorithm,
                request =>
                {
                    ++submissionCount;
                    if (submissionCount == 1)
                    {
                        algorithm.OnOrderEvent(
                            CreateOrderEvent(
                                request.OrderId,
                                OrderStatus.Filled));
                        throw new InvalidOperationException(
                            "Synchronous order processor failure.");
                    }
                    return new OrderTicket(
                        algorithm.Transactions,
                        request);
                });

            var exception = Assert.Throws<InvalidOperationException>(
                () => algorithm.OnData(CreateEmptySlice()));

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    "Synchronous order processor failure.",
                    exception.Message);
                Assert.AreEqual(1, submissionCount);
                Assert.IsNull(
                    GetPrivateField<BrokerageAccountSnapshot>(
                        algorithm,
                        "_preOrderSnapshot"));
                Assert.IsFalse(
                    GetPrivateField<bool>(
                        algorithm,
                        "_orderSubmissionInProgress"));
                Assert.IsFalse(
                    GetPrivateField<bool>(
                        algorithm,
                        "_onDataStateActive"));
                Assert.IsTrue(
                    GetPrivateField<bool>(
                        algorithm,
                        "_groupOrderSubmitted"));
                Assert.AreEqual(
                    0,
                    ((System.Collections.IDictionary)
                        GetPrivateField<object>(
                            algorithm,
                            "_terminalEventsDuringSubmission")).Count);
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains(
                        "Automatic resubmission is disabled"));
            });

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    1,
                    submissionCount,
                    "An indeterminate submission outcome must never auto-retry.");
                Assert.IsTrue(
                    GetPrivateField<bool>(
                        algorithm,
                        "_groupOrderSubmitted"));
            });
        }

        [Test]
        public void IncompletePreOrderMemberVectorDoesNotSubmitGroupOrder()
        {
            var snapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                10m,
                20m,
                SnapshotTime,
                includeAccountB: false);
            var provider = new TestAccountStateProvider
            {
                Snapshot = snapshot
            };
            var algorithm = CreateAlgorithm(provider, snapshot);
            SetPrivateField(algorithm, "_groupOrderSubmitted", false);
            SetPrivateField(algorithm, "_groupOrderId", 0);
            SetPrivateField<BrokerageAccountSnapshot>(
                algorithm,
                "_preOrderSnapshot",
                null);
            SetPrivateField(
                algorithm,
                "_initialSnapshotRequestGeneration",
                snapshot.Generation - 1);
            var submissionCount = 0;
            SetOrderProcessor(
                algorithm,
                request =>
                {
                    ++submissionCount;
                    return new OrderTicket(
                        algorithm.Transactions,
                        request);
                });

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, submissionCount);
                Assert.AreEqual(0, provider.RefreshRequestCount);
                Assert.IsNull(
                    GetPrivateField<BrokerageAccountSnapshot>(
                        algorithm,
                        "_preOrderSnapshot"));
            });
        }

        [TestCase("Equal")]
        [TestCase("NetLiq")]
        [TestCase("AvailableEquity")]
        [TestCase("Ratio")]
        [TestCase("Percent")]
        public void SupportedSavedGroupMethodsPermitSubmission(
            string allocationMethod)
        {
            var snapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                10m,
                20m,
                SnapshotTime,
                allocationMethod: allocationMethod);
            var provider = new TestAccountStateProvider
            {
                Snapshot = snapshot
            };
            var algorithm = CreateInitializedAlgorithm(provider);
            SetPrivateField(algorithm, "_groupOrderSubmitted", false);
            SetPrivateField(algorithm, "_groupOrderId", 0);
            SetPrivateField<BrokerageAccountSnapshot>(
                algorithm,
                "_preOrderSnapshot",
                null);
            SetPrivateField(
                algorithm,
                "_initialSnapshotRequestGeneration",
                snapshot.Generation - 1);
            var submissionCount = 0;
            SubmitOrderRequest submittedRequest = null;
            SetOrderProcessor(
                algorithm,
                request =>
                {
                    ++submissionCount;
                    submittedRequest = request;
                    return new OrderTicket(
                        algorithm.Transactions,
                        request);
                });

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, submissionCount);
                Assert.AreEqual(1m, submittedRequest.Quantity);
                Assert.AreEqual(
                    GroupName,
                    ((InteractiveBrokersOrderProperties)
                        submittedRequest.OrderProperties).FaGroup);
            });
        }

        [TestCase("ContractsOrShares")]
        [TestCase("PctChange")]
        public void UnsupportedSavedGroupMethodDoesNotSubmit(
            string allocationMethod)
        {
            var snapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                10m,
                20m,
                SnapshotTime,
                allocationMethod: allocationMethod);
            var provider = new TestAccountStateProvider
            {
                Snapshot = snapshot
            };
            var algorithm = CreateAlgorithm(provider, snapshot);
            SetPrivateField(algorithm, "_groupOrderSubmitted", false);
            SetPrivateField(algorithm, "_groupOrderId", 0);
            SetPrivateField<BrokerageAccountSnapshot>(
                algorithm,
                "_preOrderSnapshot",
                null);
            SetPrivateField(
                algorithm,
                "_initialSnapshotRequestGeneration",
                snapshot.Generation - 1);
            var submissionCount = 0;
            SetOrderProcessor(
                algorithm,
                request =>
                {
                    ++submissionCount;
                    return new OrderTicket(
                        algorithm.Transactions,
                        request);
                });

            algorithm.OnData(CreateEmptySlice());
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, submissionCount);
                Assert.That(
                    algorithm.ErrorMessages,
                    Has.One.Contains(
                        $"unsupported saved allocation method '{allocationMethod}'"));
                Assert.AreEqual(
                    1,
                    algorithm.ErrorMessages.Count(message => message.Contains(
                        $"unsupported saved allocation method '{allocationMethod}'")),
                    "An unchanged group configuration must report once.");
            });
        }

        [Test]
        public void ReconciliationUsesLastSuccessfulUpdateWhenCollectionStartIsUnavailable()
        {
            var preOrderSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                5,
                10m,
                20m,
                SnapshotTime.AddMinutes(-2));
            var terminalSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                11m,
                19m,
                SnapshotTime.AddSeconds(-1));
            var reconciledSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                8,
                13m,
                18m,
                default);
            var provider = new TestAccountStateProvider
            {
                Snapshot = terminalSnapshot
            };
            provider.RefreshRequestHook = stateProvider =>
                stateProvider.Snapshot = reconciledSnapshot;
            var algorithm = CreateAlgorithm(
                provider,
                preOrderSnapshot);

            algorithm.OnOrderEvent(
                CreateOrderEvent(41, OrderStatus.Filled));
            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, provider.RefreshRequestCount);
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
                    algorithm.LogMessages,
                    Has.One.Contains(
                        "account=AccountA, symbol=SPY, before=10, after=13, change=3"));
                Assert.That(
                    algorithm.LogMessages,
                    Has.One.Contains(
                        "account=AccountB, symbol=SPY, before=20, after=18, change=-2"));
            });
        }

        [Test]
        public void OldReadySnapshotRefreshesWithoutSubmittingOrder()
        {
            var snapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                10m,
                20m,
                SnapshotTime.AddMinutes(-6));
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
            var submissionCount = 0;
            SetOrderProcessor(
                algorithm,
                request =>
                {
                    ++submissionCount;
                    return new OrderTicket(
                        algorithm.Transactions,
                        request);
                });

            algorithm.OnData(CreateEmptySlice());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, submissionCount);
                Assert.AreEqual(1, provider.RefreshRequestCount);
                CollectionAssert.AreEqual(
                    new[] { GroupName },
                    provider.RequestedGroups);
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
        public void LiveInitializeRegistersAndExecutesNinetySecondRefreshSchedule()
        {
            BrokerageAccountSnapshot CreateReadySnapshot(long generation) =>
                CreateSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    generation,
                    10m,
                    20m,
                    SnapshotTime);

            var provider = new TestAccountStateProvider
            {
                Snapshot = CreateReadySnapshot(1)
            };
            provider.RefreshRequestHook = stateProvider =>
                stateProvider.Snapshot = CreateReadySnapshot(
                    stateProvider.Snapshot.Generation + 1);
            var eventSchedule = new RecordingEventSchedule();
            var algorithm = CreateAlgorithmForScheduleRegistration(
                provider,
                eventSchedule,
                liveMode: true);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, eventSchedule.Events.Count);
                Assert.AreEqual(
                    1,
                    provider.RefreshRequestCount,
                    "Live initialization must issue the initial scoped refresh.");
            });

            using var scheduledEvent = eventSchedule.Events.Single();
            var firstEventTime = scheduledEvent.NextEventUtcTime;
            algorithm.SetDateTime(firstEventTime);
            scheduledEvent.Scan(firstEventTime);
            var secondEventTime = scheduledEvent.NextEventUtcTime;

            Assert.Multiple(() =>
            {
                Assert.AreEqual(
                    TimeSpan.FromSeconds(90),
                    secondEventTime - firstEventTime);
                Assert.AreEqual(2, provider.RefreshRequestCount);
                CollectionAssert.AreEqual(
                    new[] { GroupName },
                    provider.RequestedGroupHistory.Last());
            });
        }

        [Test]
        public void NonLiveInitializeDoesNotRegisterRefreshSchedule()
        {
            var provider = new TestAccountStateProvider
            {
                Snapshot = CreateSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    1,
                    10m,
                    20m,
                    SnapshotTime)
            };
            var eventSchedule = new RecordingEventSchedule();

            CreateAlgorithmForScheduleRegistration(
                provider,
                eventSchedule,
                liveMode: false);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, eventSchedule.Events.Count);
                Assert.AreEqual(0, provider.RefreshRequestCount);
            });
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
                    SnapshotTime.AddSeconds(90 * tick));
                provider.Snapshot = CreateSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    7 + tick,
                    10m,
                    20m,
                    SnapshotTime.AddSeconds(90 * tick));
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
                SnapshotTime.AddMinutes(6));
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
            var algorithm = new FinancialAdvisorUnifiedGroupsDemoAlgorithm();
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

        [Test]
        public void NonLiveModeDoesNotUseBrokerageAccountSnapshotService()
        {
            var snapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                1,
                10m,
                20m,
                SnapshotTime);
            var provider = new TestAccountStateProvider
            {
                Snapshot = snapshot
            };
            var algorithm =
                new FinancialAdvisorUnifiedGroupsDemoAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(
                new DataManagerStub(algorithm));
            algorithm.SetDateTime(SnapshotTime);
            ((IBrokerageAccountServiceConsumer)algorithm)
                .SetBrokerageAccountStateProvider(provider);

            algorithm.Initialize();
            algorithm.SetLocked();
            algorithm.Securities[Symbols.SPY].Holdings.SetHoldings(1m, 1m);
            algorithm.OnData(CreateEmptySlice());

            Assert.AreEqual(0, provider.RefreshRequestCount);
        }

        private static FinancialAdvisorUnifiedGroupsDemoAlgorithm CreateAlgorithm(
            TestAccountStateProvider provider,
            BrokerageAccountSnapshot preOrderSnapshot)
        {
            var algorithm = new FinancialAdvisorUnifiedGroupsDemoAlgorithm();
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

        private static void SetOrderProcessor(
            FinancialAdvisorUnifiedGroupsDemoAlgorithm algorithm,
            Func<SubmitOrderRequest, OrderTicket> process)
        {
            var security = algorithm.Securities.TryGetValue(
                    Symbols.SPY,
                    out var initializedSecurity)
                ? initializedSecurity
                : SecurityTests.GetSecurity();
            security.SetMarketPrice(new Tick
            {
                Symbol = Symbols.SPY,
                Time = SnapshotTime,
                Value = 100m
            });
            if (initializedSecurity == null)
            {
                algorithm.Securities.Add(Symbols.SPY, security);
            }
            algorithm.Settings.FreePortfolioValuePercentage = 0m;
            if (!algorithm.GetLocked())
            {
                algorithm.SetCash(100000m);
            }
            algorithm.SetFinishedWarmingUp();

            var processor = new Mock<IOrderProcessor>(MockBehavior.Strict);
            processor.SetupGet(candidate => candidate.OrdersCount)
                .Returns(0);
            processor
                .Setup(candidate => candidate.GetOpenOrderTickets(
                    It.IsAny<Func<OrderTicket, bool>>()))
                .Returns(Array.Empty<OrderTicket>());
            processor
                .Setup(candidate => candidate.Process(
                    It.IsAny<OrderRequest>()))
                .Returns<OrderRequest>(request =>
                    process((SubmitOrderRequest)request));
            algorithm.Transactions.SetOrderProcessor(processor.Object);
        }

        private static FinancialAdvisorUnifiedGroupsDemoAlgorithm
            CreateInitializedAlgorithm(
                TestAccountStateProvider provider)
        {
            var algorithm = new FinancialAdvisorUnifiedGroupsDemoAlgorithm();
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

        private static FinancialAdvisorUnifiedGroupsDemoAlgorithm
            CreateAlgorithmForScheduleRegistration(
                TestAccountStateProvider provider,
                IEventSchedule eventSchedule,
                bool liveMode)
        {
            var algorithm = new FinancialAdvisorUnifiedGroupsDemoAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(
                new DataManagerStub(algorithm));
            algorithm.SetLiveMode(liveMode);
            algorithm.SetDateTime(SnapshotTime);
            algorithm.Schedule.SetEventSchedule(eventSchedule);
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
            DateTime collectionStartedUtc,
            bool includeAccountB = true,
            string allocationMethod = "Equal")
        {
            var group = new BrokerageAccountGroup(
                GroupName,
                allocationMethod,
                new[] { "AccountA", "AccountB" });
            var groups = new Dictionary<string, BrokerageAccountGroup>
            {
                [GroupName] = group
            };
            var accounts = new Dictionary<string, BrokerageAccountState>
            {
                ["AccountA"] = CreateAccount("AccountA", accountAQuantity)
            };
            if (includeAccountB)
            {
                accounts["AccountB"] =
                    CreateAccount("AccountB", accountBQuantity);
            }

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
            FinancialAdvisorUnifiedGroupsDemoAlgorithm algorithm,
            string name,
            T value)
        {
            SetPrivateField(
                algorithm,
                typeof(FinancialAdvisorUnifiedGroupsDemoAlgorithm),
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

        private sealed class RecordingEventSchedule : IEventSchedule
        {
            public List<ScheduledEvent> Events { get; } = new();

            public void Add(ScheduledEvent scheduledEvent)
            {
                Events.Add(scheduledEvent);
            }

            public void Remove(ScheduledEvent scheduledEvent)
            {
                Events.Remove(scheduledEvent);
            }
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
            public List<IReadOnlyCollection<string>>
                RequestedAdditionalAccountHistory
            { get; } = new();
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
                RequestedAdditionalAccountHistory.Add(
                    RequestedAdditionalAccounts);
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
