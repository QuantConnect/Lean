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
using System.Runtime.CompilerServices;
using Moq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.AlgorithmFactory.Python.Wrappers;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using LeanEngine = QuantConnect.Lean.Engine.Engine;

namespace QuantConnect.Tests.Engine
{
    [TestFixture]
    public class BrokerageAccountServiceWiringTests
    {
        [Test]
        public void BrokerageAccountServicesAreInstalledAndForwardedTest()
        {
            AssertEngineInstallsEveryService();
            AssertPythonWrapperForwardsEveryService();
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

        private static void AssertPythonWrapperForwardsEveryService()
        {
            var asOfUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var snapshot = new BrokerageAccountSnapshot(
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
            var assignment = new BrokerageAccountGroupAssignment(
                BrokerageAccountGroupAssignmentStatus.Pending,
                8,
                asOfUtc,
                "Account",
                "Group",
                Array.Empty<string>(),
                Array.Empty<string>(),
                "membership",
                string.Empty,
                "configuration",
                string.Empty,
                string.Empty);
            var allocation = new BrokerageAccountGroupAllocationUpdate(
                BrokerageAccountGroupAllocationUpdateStatus.Pending,
                9,
                asOfUtc,
                "Group",
                "Ratio",
                new Dictionary<string, decimal>(),
                null,
                "membership",
                string.Empty,
                "configuration",
                string.Empty,
                string.Empty);
            var stateProvider = new Mock<IBrokerageAccountStateProvider>();
            var groupManager = new Mock<IBrokerageAccountGroupManager>();
            var allocationManager = new Mock<IBrokerageAccountGroupAllocationManager>();
            stateProvider.Setup(instance => instance.GetAccountSnapshot()).Returns(snapshot);
            groupManager.Setup(instance => instance.GetAccountGroupAssignment()).Returns(assignment);
            allocationManager
                .Setup(instance => instance.GetAccountGroupAllocationUpdate())
                .Returns(allocation);

            var baseAlgorithm = new QCAlgorithm();
            var wrapper = (AlgorithmPythonWrapper)RuntimeHelpers.GetUninitializedObject(
                typeof(AlgorithmPythonWrapper));
            var baseAlgorithmField = typeof(AlgorithmPythonWrapper)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(field => field.FieldType == typeof(QCAlgorithm));
            baseAlgorithmField.SetValue(wrapper, baseAlgorithm);
            var consumer = (IBrokerageAccountServiceConsumer)wrapper;

            consumer.SetBrokerageAccountStateProvider(stateProvider.Object);
            consumer.SetBrokerageAccountGroupManager(groupManager.Object);
            consumer.SetBrokerageAccountGroupAllocationManager(allocationManager.Object);

            Assert.AreSame(snapshot, baseAlgorithm.BrokerageAccountSnapshot);
            Assert.AreSame(assignment, baseAlgorithm.BrokerageAccountGroupAssignment);
            Assert.AreSame(allocation, baseAlgorithm.BrokerageAccountGroupAllocationUpdate);
        }

    }
}
