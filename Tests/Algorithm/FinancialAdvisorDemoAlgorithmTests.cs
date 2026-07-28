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

        [Test]
        public void TerminalGroupOrderUsesSnapshotReconciliationTest()
        {
            var preOrderSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                5,
                10m,
                20m);
            var terminalTimeSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                7,
                11m,
                19m);
            var postRequestStaleSnapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Stale,
                8,
                13m,
                18m);
            var provider = new TestAccountStateProvider
            {
                Snapshot = terminalTimeSnapshot
            };
            provider.RefreshRequestHook = stateProvider =>
                stateProvider.Snapshot = postRequestStaleSnapshot;
            var algorithm = new FinancialAdvisorDemoAlgorithm();
            SetPrivateField(
                algorithm,
                typeof(QCAlgorithm),
                "_liveMode",
                true);
            ((IBrokerageAccountServiceConsumer)algorithm)
                .SetBrokerageAccountStateProvider(provider);
            SetPrivateField(algorithm, "_symbol", Symbols.SPY);
            SetPrivateField(algorithm, "_preOrderSnapshot", preOrderSnapshot);
            SetPrivateField(algorithm, "_initialSnapshotRefreshAccepted", true);
            SetPrivateField(algorithm, "_groupOrderSubmitted", true);
            SetPrivateField(algorithm, "_groupOrderId", 41);

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
                Assert.AreSame(postRequestStaleSnapshot, provider.Snapshot);
            });

            var messageCount = algorithm.LogMessages.Count;
            provider.Snapshot = terminalTimeSnapshot;
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(
                messageCount,
                algorithm.LogMessages.Count,
                "A Ready snapshot at the terminal-time generation must not reconcile.");

            provider.Snapshot = postRequestStaleSnapshot;
            algorithm.OnData(CreateEmptySlice());
            Assert.AreEqual(
                messageCount,
                algorithm.LogMessages.Count,
                "A newer snapshot that is not Ready must not reconcile.");

            provider.Snapshot = CreateSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                8,
                13m,
                18m);
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
                    1,
                    provider.RefreshRequestCount,
                    "A duplicate terminal event must not request another refresh.");
            });
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
            decimal accountBQuantity)
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
                managedAccountIds: new[] { "AccountA", "AccountB" });
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
                RefreshRequestHook?.Invoke(this);
                return true;
            }

            public bool RequestConfiguredAccountSnapshotRefresh()
            {
                return false;
            }
        }
    }
}
