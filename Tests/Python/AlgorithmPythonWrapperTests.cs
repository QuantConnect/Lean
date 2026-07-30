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

using Moq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.AlgorithmFactory.Python.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class AlgorithmPythonWrapperTests
    {
        private string _baseCode;

        [SetUp]
        public void Setup()
        {
            _baseCode = File.ReadAllText(Path.Combine("./RegressionAlgorithms", "Test_AlgorithmPythonWrapper.py"));
        }

        [TestCase("")]
        [TestCase("def OnEndOfDay(self): pass")]
        [TestCase("def OnEndOfDay(self, symbol): pass")]
        public void CallOnEndOfDayDoesNotThrow(string code)
        {
            // If we define either one or the other overload of OnEndOfDay.
            // the algorithm will not throw or log the error
            using (Py.GIL())
            {
                var algorithm = GetAlgorithm(code);

                Assert.Null(algorithm.RunTimeError);
                Assert.DoesNotThrow(() => algorithm.OnEndOfDay());
                Assert.Null(algorithm.RunTimeError);
                Assert.DoesNotThrow(() => algorithm.OnEndOfDay(Symbols.SPY));
                Assert.Null(algorithm.RunTimeError);
            }
        }

        [Test]
        [TestCase("def OnEndOfDay(self): self.Name = 'EOD'\r\n    def OnEndOfDay(self, symbol): self.Name = 'EODSymbol'", "EODSymbol")]
        [TestCase("def OnEndOfDay(self, symbol): self.Name = 'EODSymbol'\r\n    def OnEndOfDay(self): self.Name = 'EOD'", "EOD")]
        public void OnEndOfDayBothImplemented(string code, string expectedImplementation)
        {
            // If we implement both OnEndOfDay functions we expect it to not throw,
            // but only the latest will be seen and used.
            // To test this we will have the functions set something we can verify such as Algo name
            using (Py.GIL())
            {
                var algorithm = GetAlgorithm(code);
                
                Assert.Null(algorithm.RunTimeError);
                Assert.DoesNotThrow(() => algorithm.OnEndOfDay());
                Assert.Null(algorithm.RunTimeError);
                Assert.DoesNotThrow(() => algorithm.OnEndOfDay(Symbols.SPY));
                Assert.Null(algorithm.RunTimeError);

                // Check the name
                Assert.AreEqual(expectedImplementation, algorithm.Name);

                // Check the wrapper EOD Implemented variables to confirm
                switch (expectedImplementation)
                {
                    case "EOD":
                        Assert.IsTrue(algorithm.IsOnEndOfDayImplemented);
                        Assert.IsFalse(algorithm.IsOnEndOfDaySymbolImplemented);
                        break;
                    case "EODSymbol":
                        Assert.IsTrue(algorithm.IsOnEndOfDaySymbolImplemented);
                        Assert.IsFalse(algorithm.IsOnEndOfDayImplemented);
                        break;
                }
            }
        }

        [Test]
        public void CallOnEndOfDayExceptionNoParameter()
        {
            // When we define OnEndOfDay without a parameter and it has an user error (divide by zero)
            // it doesn't throw and stop the algorithm, but set its RuntimeError
            using (Py.GIL())
            {
                var algorithm = GetAlgorithm("def OnEndOfDay(self): 1/0");

                Assert.Null(algorithm.RunTimeError);
                Assert.DoesNotThrow(() => algorithm.OnEndOfDay());
                Assert.NotNull(algorithm.RunTimeError);
            }
        }

        [TestCase("", false)]
        [TestCase("def OnMarginCall(self, orders): pass", true)]
        [TestCase("def OnMarginCall(self, orders): return orders", false)]
        public void OnMarginCall(string code, bool throws)
        {
            using (Py.GIL())
            {
                var algorithm = GetAlgorithm(code);
                Assert.Null(algorithm.RunTimeError);

                var order = new SubmitOrderRequest(OrderType.Limit,
                        SecurityType.Base,
                        Symbol.Empty,
                        1,
                        1,
                        1,
                        DateTime.UtcNow,
                        "");
                if (throws)
                {
                    Assert.Throws<Exception>(() => algorithm.OnMarginCall(new List<SubmitOrderRequest> { order }));
                }
                else
                {
                    Assert.DoesNotThrow(() => algorithm.OnMarginCall(new List<SubmitOrderRequest> { order }));
                }
            }
        }

        [Test]
        public void CallOnEndOfDayExceptionWithParameter()
        {
            // When we define OnEndOfDay with the Symbol parameter and it has an user error (divide by zero)
            // it doesn't throw and stop the algorithm, but set its RuntimeError
            using (Py.GIL())
            {
                var algorithm = GetAlgorithm("def OnEndOfDay(self, symbol): 1/0");

                Assert.Null(algorithm.RunTimeError);
                Assert.DoesNotThrow(() => algorithm.OnEndOfDay(Symbols.SPY));
                Assert.NotNull(algorithm.RunTimeError);
            }
        }

        [Test]
        public void BrokerageAccountServicesAvailableDuringInitializeTest()
        {
            using (Py.GIL())
            {
                var algorithm = GetAlgorithm(
                    "def initialize(self): self.name = str(self.brokerage_account_snapshot.generation)");
                var now = DateTime.UtcNow;
                var snapshot = new BrokerageAccountSnapshot(
                    BrokerageAccountSnapshotStatus.Ready,
                    7,
                    now,
                    now,
                    new Dictionary<string, BrokerageAccountGroup>(),
                    new Dictionary<string, BrokerageAccountState>(),
                    Array.Empty<string>(),
                    "membership",
                    "configuration",
                    string.Empty);
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
                provider.Setup(instance => instance.GetAccountSnapshot()).Returns(snapshot);
                var groupManager = new Mock<IBrokerageAccountGroupManager>();
                groupManager.Setup(instance => instance.GetAccountGroupAssignment()).Returns(assignment);
                var allocationManager = new Mock<IBrokerageAccountGroupAllocationManager>();
                allocationManager.Setup(instance => instance.GetAccountGroupAllocationUpdate()).Returns(allocation);
                var consumer = (IBrokerageAccountServiceConsumer)algorithm;

                consumer.SetBrokerageAccountStateProvider(provider.Object);
                consumer.SetBrokerageAccountGroupManager(groupManager.Object);
                consumer.SetBrokerageAccountGroupAllocationManager(allocationManager.Object);
                algorithm.Initialize();

                Assert.AreSame(snapshot, algorithm.BaseAlgorithm.BrokerageAccountSnapshot);
                Assert.AreSame(assignment, algorithm.BaseAlgorithm.BrokerageAccountGroupAssignment);
                Assert.AreSame(allocation, algorithm.BaseAlgorithm.BrokerageAccountGroupAllocationUpdate);
                Assert.AreEqual("7", algorithm.Name);
            }
        }

        [Test]
        public void BrokerageAccountSnapshotDictionaryValuesAreAvailableFromPython()
        {
            using (Py.GIL())
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
                provider.Setup(instance => instance.GetAccountSnapshot()).Returns(snapshot);
                var algorithm = GetAlgorithm(
                    "def initialize(self): self.name = " +
                    "f'{len(list(self.brokerage_account_snapshot.account_directory.values))}:' + " +
                    "f'{len(list(self.brokerage_account_snapshot.groups.values))}'");

                ((IBrokerageAccountServiceConsumer)algorithm.BaseAlgorithm)
                    .SetBrokerageAccountStateProvider(provider.Object);
                algorithm.Initialize();

                Assert.AreEqual("1:1", algorithm.Name);
            }
        }

        [TestCase("request_brokerage_account_group_allocation_update")]
        [TestCase("RequestBrokerageAccountGroupAllocationUpdate")]
        public void NativePythonDictionaryCanRequestGroupAllocationUpdate(string methodName)
        {
            IReadOnlyDictionary<string, decimal> requestedAllocations = null;
            var now = DateTime.UtcNow;
            var snapshot = new BrokerageAccountSnapshot(
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
            var provider = new Mock<IBrokerageAccountStateProvider>();
            provider.Setup(instance => instance.GetAccountSnapshot()).Returns(snapshot);
            var manager = new Mock<IBrokerageAccountGroupAllocationManager>();
            manager.Setup(instance => instance.RequestAccountGroupAllocationUpdate(
                    "GroupA",
                    It.IsAny<IReadOnlyDictionary<string, decimal>>(),
                    "membership",
                    "configuration"))
                .Callback<string, IReadOnlyDictionary<string, decimal>, string, string>(
                    (_, allocations, _, _) => requestedAllocations = allocations)
                .Returns(true);

            using (Py.GIL())
            {
                var algorithm = GetAlgorithm(
                    $"def initialize(self): self.name = str(self.{methodName}(" +
                    "'GroupA', {'AccountA': 1.25, 'accounta': 2}, " +
                    $"self.brokerage_account_snapshot)){Environment.NewLine}" +
                    $"    def on_data(self, slice): self.{methodName}(" +
                    "'GroupA', {'AccountA': 1.25, 'accounta': 2}, " +
                    "self.brokerage_account_snapshot)");
                var consumer = (IBrokerageAccountServiceConsumer)algorithm.BaseAlgorithm;
                consumer.SetBrokerageAccountStateProvider(provider.Object);
                consumer.SetBrokerageAccountGroupAllocationManager(manager.Object);

                algorithm.Initialize();

                Assert.AreEqual("False", algorithm.Name);
                Assert.IsNull(requestedAllocations);

                algorithm.BaseAlgorithm.SetLocked();
                algorithm.BaseAlgorithm.SetBrokerageAccountMutationServicesReady();
                var nowUtc = DateTime.UtcNow;
                algorithm.OnData(new Slice(nowUtc, Array.Empty<BaseData>(), nowUtc));

                Assert.AreEqual(1.25m, requestedAllocations["AccountA"]);
                Assert.AreEqual(2m, requestedAllocations["accounta"]);
            }
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
                using var referencesBeforeResult = (PyObject)sys.getrefcount(key);
                var referencesBefore = referencesBeforeResult.As<int>();

                for (var i = 0; i < 1000; i++)
                {
                    Assert.IsFalse(algorithm.RequestBrokerageAccountGroupAllocationUpdate(
                        "GroupA",
                        allocations,
                        BrokerageAccountSnapshot.Unavailable));
                }

                using var referencesAfterResult = (PyObject)sys.getrefcount(key);
                Assert.AreEqual(referencesBefore, referencesAfterResult.As<int>());
            }
        }

        private AlgorithmPythonWrapper GetAlgorithm(string code)
        {
            code = $"{_baseCode}{Environment.NewLine}    {code}";

            using (Py.GIL())
            {
                PyModule.FromString("Test_AlgorithmPythonWrapper", code);
                return new AlgorithmPythonWrapper("Test_AlgorithmPythonWrapper");
            }
        }
    }
}
