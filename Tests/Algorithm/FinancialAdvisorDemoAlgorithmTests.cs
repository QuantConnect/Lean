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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;

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

        private static OrderEvent CreateOrderEvent(
            int orderId,
            OrderStatus status)
        {
            return new OrderEvent(
                orderId,
                Symbols.SPY,
                SnapshotTime,
                status,
                OrderDirection.Buy,
                100m,
                status == OrderStatus.Filled ? 5m : 0m,
                OrderFee.Zero);
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

        private sealed class TestAccountStateProvider :
            IBrokerageAccountStateProvider
        {
            public BrokerageAccountSnapshot Snapshot { get; set; }
            public int RefreshRequestCount { get; private set; }
            public long GenerationObservedAtRefreshRequest { get; private set; }
            public Action<TestAccountStateProvider> RefreshRequestHook { get; set; }
            public IReadOnlyCollection<string> RequestedGroups { get; private set; }
            public IReadOnlyCollection<string> RequestedAdditionalAccounts { get; private set; }
            private readonly Queue<bool> _refreshResults = new();

            public void EnqueueRefreshResult(bool accepted) =>
                _refreshResults.Enqueue(accepted);

            public BrokerageAccountSnapshot GetAccountSnapshot()
            {
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
