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
using System.IO;
using Moq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.AlgorithmFactory.Python.Wrappers;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using LeanEngine = QuantConnect.Lean.Engine.Engine;

namespace QuantConnect.Tests.Python
{
    [TestFixture, NonParallelizable]
    public class FinancialAdvisorPythonInteropTests
    {
        private string _baseCode;

        [SetUp]
        public void Setup()
        {
            _baseCode = File.ReadAllText(
                Path.Combine(
                    "./RegressionAlgorithms",
                    "Test_AlgorithmPythonWrapper.py"));
        }

        [Test]
        public void BrokerageAccountServicesAreAvailableDuringInitialize()
        {
            using (Py.GIL())
            using (var algorithm = CreateAlgorithm(
                "def initialize(self): self.name = " +
                "str(self.brokerage_account_snapshot.generation)"))
            {
                var now = DateTime.UtcNow;
                var snapshot = CreateReadySnapshot(7, now);
                var assignment = new BrokerageAccountGroupAssignment(
                    BrokerageAccountGroupAssignmentStatus.Pending,
                    8,
                    now,
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
                    now,
                    "Group",
                    "Ratio",
                    new Dictionary<string, decimal>(),
                    null,
                    "membership",
                    string.Empty,
                    "configuration",
                    string.Empty,
                    string.Empty);
                var provider = new Mock<IBrokerageAccountStateProvider>();
                provider.Setup(instance => instance.GetAccountSnapshot())
                    .Returns(snapshot);
                var groupManager = new Mock<IBrokerageAccountGroupManager>();
                groupManager.Setup(instance =>
                        instance.GetAccountGroupAssignment())
                    .Returns(assignment);
                var allocationManager =
                    new Mock<IBrokerageAccountGroupAllocationManager>();
                allocationManager.Setup(instance =>
                        instance.GetAccountGroupAllocationUpdate())
                    .Returns(allocation);
                var consumer = (IBrokerageAccountServiceConsumer)algorithm;

                consumer.SetBrokerageAccountStateProvider(provider.Object);
                consumer.SetBrokerageAccountGroupManager(groupManager.Object);
                consumer.SetBrokerageAccountGroupAllocationManager(
                    allocationManager.Object);
                algorithm.Initialize();

                Assert.Multiple(() =>
                {
                    Assert.AreSame(
                        snapshot,
                        algorithm.BaseAlgorithm.BrokerageAccountSnapshot);
                    Assert.AreSame(
                        assignment,
                        algorithm.BaseAlgorithm.BrokerageAccountGroupAssignment);
                    Assert.AreSame(
                        allocation,
                        algorithm.BaseAlgorithm
                            .BrokerageAccountGroupAllocationUpdate);
                    Assert.AreEqual("7", algorithm.Name);
                });
            }
        }

        [Test]
        public void RealPythonWrapperHonorsMutationServiceBoundary()
        {
            var snapshot = CreateReadySnapshot(7, DateTime.UtcNow);
            var provider = new Mock<IBrokerageAccountStateProvider>();
            provider.Setup(instance => instance.GetAccountSnapshot())
                .Returns(snapshot);
            var manager = new Mock<IBrokerageAccountGroupManager>();
            manager.Setup(instance => instance.RequestAccountGroupAssignment(
                    "Account",
                    "Group",
                    "membership",
                    "configuration",
                    null))
                .Returns(true);
            using var algorithm = CreateAlgorithm("def initialize(self): pass");
            var consumer = (IBrokerageAccountServiceConsumer)algorithm;
            consumer.SetBrokerageAccountStateProvider(provider.Object);
            consumer.SetBrokerageAccountGroupManager(manager.Object);
            algorithm.Initialize();
            algorithm.BaseAlgorithm.SetLocked();

            Assert.IsFalse(algorithm.BaseAlgorithm
                .RequestBrokerageAccountGroupAssignment(
                    "Account",
                    "Group",
                    null,
                    snapshot));

            var acceptedInsideBoundary = false;
            LeanEngine.RunWithBrokerageAccountMutationsEnabled(
                algorithm,
                () => acceptedInsideBoundary = algorithm.BaseAlgorithm
                    .RequestBrokerageAccountGroupAssignment(
                        "Account",
                        "Group",
                        null,
                        snapshot));

            Assert.Multiple(() =>
            {
                Assert.IsTrue(acceptedInsideBoundary);
                Assert.IsFalse(algorithm.BaseAlgorithm
                    .RequestBrokerageAccountGroupAssignment(
                        "Account",
                        "Group",
                        null,
                        snapshot));
            });
            manager.Verify(instance => instance.RequestAccountGroupAssignment(
                "Account",
                "Group",
                "membership",
                "configuration",
                null), Times.Once);
        }

        [Test]
        public void BrokerageAccountSnapshotDictionariesAreAvailableFromPython()
        {
            using (Py.GIL())
            using (var algorithm = CreateAlgorithm(
                "def initialize(self): self.name = " +
                "f'{len(list(self.brokerage_account_snapshot.account_directory.values))}:' + " +
                "f'{len(list(self.brokerage_account_snapshot.groups.values))}'"))
            {
                var group = new BrokerageAccountGroup(
                    "GroupA",
                    "Equal",
                    new[] { "AccountA" });
                var groups = new Dictionary<string, BrokerageAccountGroup>
                {
                    [group.Name] = group
                };
                var accountDirectory =
                    new Dictionary<string, BrokerageAccountDirectoryEntry>
                    {
                        ["AccountA"] = new(
                            "AccountA",
                            BrokerageAccountRelationship.Managed,
                            new[] { group.Name })
                    };
                var accounts = new Dictionary<string, BrokerageAccountState>
                {
                    ["AccountA"] = new(
                        "AccountA",
                        new[] { group.Name },
                        string.Empty,
                        null,
                        null,
                        null,
                        null,
                        null,
                        string.Empty,
                        null,
                        null)
                };
                var now = DateTime.UtcNow;
                var snapshot = new BrokerageAccountSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    1,
                    now,
                    now,
                    groups,
                    accounts,
                    Array.Empty<string>(),
                    "membership",
                    "configuration",
                    string.Empty,
                    managedAccountIds: new[] { "AccountA" },
                    accountDirectory: accountDirectory);
                var provider = new Mock<IBrokerageAccountStateProvider>();
                provider.Setup(instance => instance.GetAccountSnapshot())
                    .Returns(snapshot);

                ((IBrokerageAccountServiceConsumer)algorithm.BaseAlgorithm)
                    .SetBrokerageAccountStateProvider(provider.Object);
                algorithm.Initialize();

                Assert.AreEqual("1:1", algorithm.Name);
            }
        }

        [Test]
        public void DecimalSnapshotValueProjectsToPythonFloat()
        {
            var group = new BrokerageAccountGroup(
                "GroupA",
                "Ratio",
                new[] { "AccountA" },
                new Dictionary<string, decimal>
                {
                    ["AccountA"] = 0.1m
                });
            var now = DateTime.UtcNow;
            var snapshot = new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                1,
                now,
                now,
                new Dictionary<string, BrokerageAccountGroup>
                {
                    [group.Name] = group
                },
                new Dictionary<string, BrokerageAccountState>(),
                Array.Empty<string>(),
                "membership",
                "configuration",
                string.Empty,
                managedAccountIds: new[] { "AccountA" });
            var provider = new Mock<IBrokerageAccountStateProvider>();
            provider.Setup(instance => instance.GetAccountSnapshot())
                .Returns(snapshot);

            using (Py.GIL())
            using (var algorithm = CreateAlgorithm(
                "def initialize(self): from decimal import Decimal; " +
                "value = self.brokerage_account_snapshot.groups['GroupA']." +
                "account_allocation_values['AccountA']; " +
                "self.name = f\"{type(value).__name__}:" +
                "{Decimal.from_float(value) != Decimal('0.1')}\""))
            {
                ((IBrokerageAccountServiceConsumer)algorithm.BaseAlgorithm)
                    .SetBrokerageAccountStateProvider(provider.Object);
                algorithm.Initialize();

                Assert.Multiple(() =>
                {
                    Assert.AreEqual(
                        0.1m,
                        group.AccountAllocationValues["AccountA"],
                        "The managed snapshot retains its exact decimal value.");
                    Assert.AreEqual(
                        "float:True",
                        algorithm.Name,
                        "Python.NET projects System.Decimal through a Python float, which cannot preserve every decimal exactly.");
                });
            }
        }

        [TestCase("request_brokerage_account_group_allocation_update")]
        [TestCase("RequestBrokerageAccountGroupAllocationUpdate")]
        public void NativePythonDictionaryCanRequestGroupAllocationUpdate(
            string methodName)
        {
            IReadOnlyDictionary<string, decimal> requestedAllocations = null;
            var now = DateTime.UtcNow;
            var snapshot = CreateReadySnapshot(1, now);
            var provider = new Mock<IBrokerageAccountStateProvider>();
            provider.Setup(instance => instance.GetAccountSnapshot())
                .Returns(snapshot);
            var manager = new Mock<IBrokerageAccountGroupAllocationManager>();
            manager.Setup(instance =>
                    instance.RequestAccountGroupAllocationUpdate(
                        "GroupA",
                        It.IsAny<IReadOnlyDictionary<string, decimal>>(),
                        "membership",
                        "configuration"))
                .Callback<
                    string,
                    IReadOnlyDictionary<string, decimal>,
                    string,
                    string>((_, allocations, _, _) =>
                        requestedAllocations = allocations)
                .Returns(true);

            using (Py.GIL())
            using (var algorithm = CreateAlgorithm(
                $"def initialize(self): self.name = str(self.{methodName}(" +
                "'GroupA', {'AccountA': 1.25, 'accounta': 2}, " +
                $"self.brokerage_account_snapshot)){Environment.NewLine}" +
                $"    def on_data(self, slice): self.{methodName}(" +
                "'GroupA', {'AccountA': 1.25, 'accounta': 2}, " +
                "self.brokerage_account_snapshot)"))
            {
                var consumer =
                    (IBrokerageAccountServiceConsumer)algorithm.BaseAlgorithm;
                consumer.SetBrokerageAccountStateProvider(provider.Object);
                consumer.SetBrokerageAccountGroupAllocationManager(
                    manager.Object);

                algorithm.Initialize();

                Assert.AreEqual("False", algorithm.Name);
                Assert.IsNull(requestedAllocations);

                algorithm.BaseAlgorithm.SetLocked();
                algorithm.BaseAlgorithm
                    .SetBrokerageAccountMutationServicesReady(true);
                algorithm.OnData(
                    new Slice(now, Array.Empty<BaseData>(), now));

                Assert.Multiple(() =>
                {
                    Assert.AreEqual(
                        1.25m,
                        requestedAllocations["AccountA"]);
                    Assert.AreEqual(
                        2m,
                        requestedAllocations["accounta"]);
                });
            }
        }

        [TestCase("{1: 2}", "keys must be strings")]
        [TestCase("{'AccountA': 'not-a-number'}", "must be numeric")]
        [TestCase("{'AccountA': 79228162514264337593543950336}", "must be numeric")]
        public void InvalidNativePythonAllocationDictionaryThrowsManagedException(
            string allocationExpression,
            string expectedDiagnostic)
        {
            var manager = new Mock<IBrokerageAccountGroupAllocationManager>();
            ArgumentException exception;
            using (Py.GIL())
            using (var algorithm = CreateReadyAllocationAlgorithm(
                manager,
                "def initialize(self): pass"))
            using (var allocations = PythonEngine.Eval(allocationExpression))
            {
                exception = Assert.Throws<ArgumentException>(() =>
                    algorithm.BaseAlgorithm
                        .RequestBrokerageAccountGroupAllocationUpdate(
                            "GroupA",
                            allocations,
                            algorithm.BaseAlgorithm.BrokerageAccountSnapshot));
            }

            StringAssert.Contains(expectedDiagnostic, exception.Message);
            Assert.AreEqual("accountAllocationValues", exception.ParamName);
            manager.Verify(instance =>
                    instance.RequestAccountGroupAllocationUpdate(
                        It.IsAny<string>(),
                        It.IsAny<IReadOnlyDictionary<string, decimal>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                Times.Never);
        }

        [Test]
        public void NativePythonAllocationValidationThrowsManagedException()
        {
            var manager = new Mock<IBrokerageAccountGroupAllocationManager>();
            ArgumentException exception;
            using (Py.GIL())
            using (var algorithm = CreateReadyAllocationAlgorithm(
                manager,
                "def initialize(self): pass"))
            using (var allocations = PythonEngine.Eval("{' AccountA': 1}"))
            {
                exception = Assert.Throws<ArgumentException>(() =>
                    algorithm.BaseAlgorithm
                        .RequestBrokerageAccountGroupAllocationUpdate(
                            "GroupA",
                            allocations,
                            algorithm.BaseAlgorithm.BrokerageAccountSnapshot));
            }

            Assert.AreEqual(
                "accountAllocationValues",
                exception.ParamName);
            manager.Verify(instance =>
                    instance.RequestAccountGroupAllocationUpdate(
                        It.IsAny<string>(),
                        It.IsAny<IReadOnlyDictionary<string, decimal>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                Times.Never);
        }

        [Test]
        public void NativePythonAllocationKeysDoNotLeakReferences()
        {
            using (Py.GIL())
            {
                var algorithm = new QuantConnect.Algorithm.QCAlgorithm();
                using var key = new PyString("AccountA");
                using var value = new PyFloat(1d);
                using var allocations = new PyDict();
                allocations.SetItem(key, value);
                using dynamic sys = Py.Import("sys");
                using var referencesBeforeResult =
                    (PyObject)sys.getrefcount(key);
                var referencesBefore = referencesBeforeResult.As<int>();

                for (var i = 0; i < 1000; i++)
                {
                    Assert.IsFalse(
                        algorithm
                            .RequestBrokerageAccountGroupAllocationUpdate(
                                "GroupA",
                                allocations,
                                BrokerageAccountSnapshot.Unavailable));
                }

                using var referencesAfterResult =
                    (PyObject)sys.getrefcount(key);
                Assert.AreEqual(
                    referencesBefore,
                    referencesAfterResult.As<int>());
            }
        }

        private AlgorithmPythonWrapper CreateReadyAllocationAlgorithm(
            Mock<IBrokerageAccountGroupAllocationManager> manager,
            string code)
        {
            var now = DateTime.UtcNow;
            var snapshot = CreateReadySnapshot(1, now);
            var provider = new Mock<IBrokerageAccountStateProvider>();
            provider.Setup(instance => instance.GetAccountSnapshot())
                .Returns(snapshot);
            var algorithm = CreateAlgorithm(code);
            var consumer =
                (IBrokerageAccountServiceConsumer)algorithm.BaseAlgorithm;
            consumer.SetBrokerageAccountStateProvider(provider.Object);
            consumer.SetBrokerageAccountGroupAllocationManager(
                manager.Object);
            try
            {
                algorithm.Initialize();
                algorithm.BaseAlgorithm.SetLocked();
                algorithm.BaseAlgorithm
                    .SetBrokerageAccountMutationServicesReady(true);
                return algorithm;
            }
            catch
            {
                algorithm.Dispose();
                throw;
            }
        }

        private AlgorithmPythonWrapper CreateAlgorithm(string code)
        {
            var moduleName =
                $"FinancialAdvisorPythonInterop_{Guid.NewGuid():N}";
            var source = _baseCode.Replace(
                "class Test_AlgorithmPythonWrapper",
                $"class {moduleName}");
            source = $"{source}{Environment.NewLine}    {code}";

            using (Py.GIL())
            using (var module = PyModule.FromString(moduleName, source))
            {
                return new AlgorithmPythonWrapper(moduleName);
            }
        }

        private static BrokerageAccountSnapshot CreateReadySnapshot(
            long generation,
            DateTime now)
        {
            return new BrokerageAccountSnapshot(
                BrokerageAccountSnapshotStatus.Ready,
                generation,
                now,
                now,
                new Dictionary<string, BrokerageAccountGroup>(),
                new Dictionary<string, BrokerageAccountState>(),
                Array.Empty<string>(),
                "membership",
                "configuration",
                string.Empty);
        }
    }
}
