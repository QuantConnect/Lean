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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class BrokerageAccountMutationReadinessSynchronizerTests
    {
        [Test]
        public void DisablesMutationServicesAfterNaturalStreamExhaustion()
        {
            var disableCount = 0;
            var synchronizer = new BrokerageAccountMutationReadinessSynchronizer(
                new FiniteSynchronizer(),
                () => disableCount++);

            Assert.AreEqual(2, synchronizer.StreamData(CancellationToken.None).Count());
            Assert.AreEqual(1, disableCount);
        }

        [Test]
        public void DisablesMutationServicesAfterEarlyEnumeratorDisposal()
        {
            var disableCount = 0;
            var synchronizer = new BrokerageAccountMutationReadinessSynchronizer(
                new FiniteSynchronizer(),
                () => disableCount++);
            var enumerator = synchronizer.StreamData(CancellationToken.None).GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(0, disableCount);

            enumerator.Dispose();

            Assert.AreEqual(1, disableCount);
            enumerator.Dispose();
            Assert.AreEqual(1, disableCount);
        }

        [Test]
        public void StreamExhaustionClosesAlgorithmMutationGate()
        {
            var asOfUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var snapshot = new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                1,
                asOfUtc,
                asOfUtc,
                new Dictionary<string, BrokerageAccountGroup>(),
                new Dictionary<string, BrokerageAccountState>(),
                Array.Empty<string>(),
                "membership",
                "configuration",
                string.Empty);
            var provider = new Mock<IBrokerageAccountStateProvider>();
            provider.Setup(instance => instance.GetAccountSnapshot()).Returns(snapshot);
            var groupManager = new Mock<IBrokerageAccountGroupManager>();
            groupManager
                .Setup(instance => instance.RequestAccountGroupAssignment(
                    "Account",
                    "Group",
                    "membership",
                    "configuration",
                    null))
                .Returns(true);
            var algorithm = new QCAlgorithm();
            var consumer = (IBrokerageAccountServiceConsumer)algorithm;
            consumer.SetBrokerageAccountStateProvider(provider.Object);
            consumer.SetBrokerageAccountGroupManager(groupManager.Object);
            algorithm.SetBrokerageAccountMutationServicesReady();

            Assert.IsTrue(algorithm.RequestBrokerageAccountGroupAssignment(
                "Account",
                "Group",
                null,
                snapshot));

            var synchronizer = new BrokerageAccountMutationReadinessSynchronizer(
                new FiniteSynchronizer(),
                () => algorithm.SetBrokerageAccountMutationServicesReady(false));
            synchronizer.StreamData(CancellationToken.None).ToList();

            Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAssignment(
                "Account",
                "Group",
                null,
                snapshot));
            groupManager.Verify(instance => instance.RequestAccountGroupAssignment(
                "Account",
                "Group",
                "membership",
                "configuration",
                null), Times.Once);
        }

        private sealed class FiniteSynchronizer : ISynchronizer
        {
            public IEnumerable<TimeSlice> StreamData(CancellationToken cancellationToken)
            {
                yield return null;
                yield return null;
            }
        }
    }
}
