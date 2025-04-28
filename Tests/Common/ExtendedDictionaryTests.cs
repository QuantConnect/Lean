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

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Statistics;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class ExtendedDictionaryTests
    {
        [Test]
        public void RunPythonDictionaryFeatureRegressionAlgorithm()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("PythonDictionaryFeatureRegressionAlgorithm",
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, "3"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "349.409%"},
                    {"Drawdown", "2.600%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "1.940%"},
                    {"Sharpe Ratio", "10.771"},
                    {"Probabilistic Sharpe Ratio", "66.098%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "0.571"},
                    {"Beta", "1.247"},
                    {"Annual Standard Deviation", "0.282"},
                    {"Annual Variance", "0.079"},
                    {"Information Ratio", "14.457"},
                    {"Tracking Error", "0.073"},
                    {"Treynor Ratio", "2.433"},
                    {"Total Fees", "$0.00"},
                    {"OrderListHash", "a91c50e19b6b9ee19007be4555029779"}
                },
                Language.Python,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                initialCash: 100000);
        }

        [Test]
        public void ExtendedDictionaryBehavesAsPythonDictionary()
        {
            using var _ = Py.GIL();

            var module = PyModule.FromString("ExtendedDictionaryBehavesAsPythonDictionary",
                    @"
from QuantConnect.Tests.Common import ExtendedDictionaryTests

def contains(dictionary, key):
    return key in dictionary

def get(dictionary, key):
    return dictionary.get(key)

def keys(dictionary):
    return dictionary.keys()

def values(dictionary):
    return dictionary.values()

def pop(dictionary, key):
    return dictionary.pop(key)
");

            var dict = new TestDictionary
            {
                ["a"] = 1,
                ["b"] = 2,
                ["c"] = 3
            };
            using var pyDict = dict.ToPython();

            var expectedKeys = new[] { "a", "b", "c" };
            var keys = module.InvokeMethod("keys", pyDict).GetAndDispose<List<string>>();
            CollectionAssert.AreEquivalent(expectedKeys, keys);

            var expectedValues = new[] { 1, 2, 3 };
            var values = module.InvokeMethod("values", pyDict).GetAndDispose<List<int>>();
            CollectionAssert.AreEquivalent(expectedValues, values);

            foreach (var (key, value) in keys.Zip(values))
            {
                using var pyKey = key.ToPython();
                Assert.IsTrue(module.InvokeMethod("contains", pyDict, pyKey).As<bool>());
                Assert.AreEqual(value, module.InvokeMethod("get", pyDict, pyKey).As<int>());
            }

            using var pyNonExistingKey = "d".ToPython();
            Assert.IsFalse(module.InvokeMethod("contains", pyDict, pyNonExistingKey).As<bool>());
            Assert.IsFalse(module.InvokeMethod("contains", pyDict, PyObject.None).As<bool>());

            using var pyExistingKey = keys[0].ToPython();
            using var pyExistingValue = values[0].ToPython();
            var popped = module.InvokeMethod("pop", pyDict, pyExistingKey).As<int>();
            Assert.AreEqual(1, popped);
            Assert.IsFalse(module.InvokeMethod("contains", pyDict, pyExistingKey).As<bool>());
        }

        public class TestDictionary : ExtendedDictionary<string, int>
        {
            private readonly Dictionary<string, int> _data = new();

            public override int Count => _data.Count;

            public override bool IsReadOnly => false;

            public override int this[string key]
            {
                get => _data[key];
                set => _data[key] = value;
            }

            protected override IEnumerable<string> GetKeys => _data.Keys;

            protected override IEnumerable<int> GetValues => _data.Values;

            public override bool TryGetValue(string key, out int value)
            {
                return _data.TryGetValue(key, out value);
            }

            public override bool ContainsKey(string key)
            {
                return _data.ContainsKey(key);
            }

            public override bool Remove(string key)
            {
                return _data.Remove(key);
            }
        }
    }
}
