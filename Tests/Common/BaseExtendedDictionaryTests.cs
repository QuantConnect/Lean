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
using NUnit.Framework;
using Common.Util;
using Python.Runtime;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class BaseExtendedDictionaryTests
    {
        [Test]
        public void AddAndGetItemsWorksCorrectly()
        {
            var dict = new BaseExtendedDictionary<string, string>();

            dict.Add("key1", "value1");
            dict["key2"] = "value2";

            Assert.AreEqual("value1", dict["key1"]);
            Assert.AreEqual("value2", dict["key2"]);
            Assert.AreEqual(2, dict.Count);
        }

        [Test]
        public void ContainsKeyExistingKeyReturnsTrue()
        {
            var dict = new BaseExtendedDictionary<int, string>();
            dict.Add(1, "one");

            Assert.IsTrue(dict.ContainsKey(1));
            Assert.IsFalse(dict.ContainsKey(2));
        }

        [Test]
        public void TryGetValueExistingKeyReturnsTrueAndValue()
        {
            var dict = new BaseExtendedDictionary<string, decimal>();
            dict["price"] = 100.5m;

            bool result = dict.TryGetValue("price", out decimal value);

            Assert.IsTrue(result);
            Assert.AreEqual(100.5m, value);
        }

        [Test]
        public void TryGetValueNonExistingKeyReturnsFalseAndDefault()
        {
            var dict = new BaseExtendedDictionary<string, decimal>();

            bool result = dict.TryGetValue("nonexistent", out decimal value);

            Assert.IsFalse(result);
            Assert.AreEqual(0, value);
        }

        [Test]
        public void RemoveExistingKeyRemovesItem()
        {
            var dict = new BaseExtendedDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);

            bool removed = dict.Remove("a");

            Assert.IsTrue(removed);
            Assert.AreEqual(1, dict.Count);
            Assert.IsFalse(dict.ContainsKey("a"));
            Assert.IsTrue(dict.ContainsKey("b"));
        }

        [Test]
        public void ClearRemovesAllItems()
        {
            var dict = new BaseExtendedDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);

            dict.Clear();

            Assert.AreEqual(0, dict.Count);
            Assert.IsFalse(dict.ContainsKey("a"));
            Assert.IsFalse(dict.ContainsKey("b"));
        }

        [Test]
        public void GetMethodExistingKeyReturnsValue()
        {
            var dict = new BaseExtendedDictionary<string, string>();
            dict["test"] = "success";

            var result = dict.get("test");

            Assert.AreEqual("success", result);
        }

        [Test]
        public void GetMethodNonExistingKeyReturnsDefault()
        {
            var dict = new BaseExtendedDictionary<string, int>();

            var result = dict.get("nonexistent");

            Assert.AreEqual(0, result);
        }

        [Test]
        public void KeysAndValuesPropertiesReturnCorrectCollections()
        {
            var dict = new BaseExtendedDictionary<int, string>();
            dict[1] = "one";
            dict[2] = "two";

            var keys = dict.Keys.ToList();
            var values = dict.Values.ToList();

            Assert.AreEqual(2, keys.Count);
            Assert.AreEqual(2, values.Count);
            Assert.Contains(1, keys);
            Assert.Contains(2, keys);
            Assert.Contains("one", values);
            Assert.Contains("two", values);
        }

        [Test]
        public void ConstructorWithInitialDictionaryCopiesData()
        {
            var initialDict = new Dictionary<string, int>
            {
                {"a", 1},
                {"b", 2}
            };

            var extendedDict = new BaseExtendedDictionary<string, int>(initialDict);

            Assert.AreEqual(2, extendedDict.Count);
            Assert.AreEqual(1, extendedDict["a"]);
            Assert.AreEqual(2, extendedDict["b"]);
        }

        [Test]
        public void ConstructorWithDataAndKeySelectorPopulatesDictionary()
        {
            var data = new List<string> { "apple", "banana" };

            var dict = new BaseExtendedDictionary<string, string>(
                data,
                fruit => fruit.Substring(0, 1) // key is first letter
            );

            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual("apple", dict["a"]);
            Assert.AreEqual("banana", dict["b"]);
        }

        [Test]
        public void EnumerationWorksCorrectly()
        {
            var dict = new BaseExtendedDictionary<int, string>();
            dict[1] = "one";
            dict[2] = "two";

            var items = new List<KeyValuePair<int, string>>();
            foreach (var kvp in dict)
            {
                items.Add(kvp);
            }

            Assert.AreEqual(2, items.Count);
            Assert.IsTrue(items.Any(kvp => kvp.Key == 1 && kvp.Value == "one"));
            Assert.IsTrue(items.Any(kvp => kvp.Key == 2 && kvp.Value == "two"));
        }

        [Test]
        public void CopyToCopiesItemsToArray()
        {
            var dict = new BaseExtendedDictionary<int, string>();
            dict[1] = "one";
            dict[2] = "two";

            var array = new KeyValuePair<int, string>[2];
            dict.CopyTo(array, 0);

            Assert.AreEqual(2, array.Length);
            Assert.IsTrue(array.Contains(new KeyValuePair<int, string>(1, "one")));
            Assert.IsTrue(array.Contains(new KeyValuePair<int, string>(2, "two")));
        }

        [Test]
        public void ReadOnlyExtendedDictionaryReturnsTrueForIsReadOnly()
        {
            var dict = new ReadOnlyExtendedDictionary<string, int>();
            Assert.IsTrue(dict.IsReadOnly);
        }

        [Test]
        public void ReadOnlyExtendedDictionaryThrowsInvalidOperationExceptionForIndexerSet()
        {
            var dict = new ReadOnlyExtendedDictionary<string, int>(new Dictionary<string, int> { { "test", 1 } });

            Assert.Throws<InvalidOperationException>(() => dict["test"] = 2);
        }

        [Test]
        public void ReadOnlyExtendedDictionaryThrowsInvalidOperationExceptionForClear()
        {
            var dict = new ReadOnlyExtendedDictionary<string, int>();
            Assert.Throws<InvalidOperationException>(() => dict.Clear());
        }

        [Test]
        public void ReadOnlyExtendedDictionaryThrowsInvalidOperationExceptionForRemoveByKey()
        {
            var dict = new ReadOnlyExtendedDictionary<string, int>();
            Assert.Throws<InvalidOperationException>(() => dict.Remove("anykey"));
        }

        [Test]
        public void ReadOnlyExtendedDictionaryThrowsInvalidOperationExceptionForAddByKeyValue()
        {
            var dict = new ReadOnlyExtendedDictionary<string, int>();
            Assert.Throws<InvalidOperationException>(() => dict.Add("newkey", 123));
        }

        [Test]
        public void BaseExtendedDictionaryBehavesAsPythonDictionary()
        {
            using var _ = Py.GIL();

            var module = PyModule.FromString("BaseExtendedDictionaryBehavesAsPythonDictionary",
                    @"
def contains(dictionary, key):
    return key in dictionary

def get(dictionary, key):
    return dictionary.get(key)

def keys(dictionary):
    return dictionary.keys()

def items(dictionary):
    return list(dictionary.items())

def values(dictionary):
    return dictionary.values()

def pop(dictionary, key):
    return dictionary.pop(key)

def setdefault(dictionary, key, default_value):
    return dictionary.setdefault(key, default_value)

def update(dictionary, other_dict):
    dictionary.update(other_dict)
");

            var dict = new BaseExtendedDictionary<string, int>
            {
                ["a"] = 1,
                ["b"] = 2,
                ["c"] = 3
            };
            using var pyDict = dict.ToPython();

            // Test keys()
            var expectedKeys = new[] { "a", "b", "c" };
            var keys = module.InvokeMethod("keys", pyDict).GetAndDispose<List<string>>();
            CollectionAssert.AreEquivalent(expectedKeys, keys);

            // Test values()
            var expectedValues = new[] { 1, 2, 3 };
            var values = module.InvokeMethod("values", pyDict).GetAndDispose<List<int>>();
            CollectionAssert.AreEquivalent(expectedValues, values);

            // Test items() method
            using var itemsResult = module.InvokeMethod("items", pyDict);
            Assert.IsNotNull(itemsResult);
            var itemsLength = PythonEngine.Eval($"len({itemsResult.Repr()})").As<int>();
            Assert.AreEqual(3, itemsLength);

            // Test contains and get
            foreach (var (key, value) in keys.Zip(values))
            {
                using var pyKey = key.ToPython();
                Assert.IsTrue(module.InvokeMethod("contains", pyDict, pyKey).As<bool>());
                Assert.AreEqual(value, module.InvokeMethod("get", pyDict, pyKey).As<int>());
            }

            // Test non-existing key
            using var pyNonExistingKey = "d".ToPython();
            Assert.IsFalse(module.InvokeMethod("contains", pyDict, pyNonExistingKey).As<bool>());

            // Test pop
            using var pyExistingKey = keys[0].ToPython();
            var popped = module.InvokeMethod("pop", pyDict, pyExistingKey).As<int>();
            Assert.AreEqual(1, popped);
            Assert.IsFalse(module.InvokeMethod("contains", pyDict, pyExistingKey).As<bool>());

            // Test setdefault with existing key
            using var pyExistingKey2 = keys[1].ToPython();
            var setdefaultExisting = module.InvokeMethod("setdefault", pyDict, pyExistingKey2, 999.ToPython()).As<int>();
            Assert.AreEqual(2, setdefaultExisting); // Should return existing value

            // Test setdefault with new key
            using var pyNewKey = "new".ToPython();
            using var pyDefaultValue = 100.ToPython();
            var setdefaultNew = module.InvokeMethod("setdefault", pyDict, pyNewKey, pyDefaultValue).As<int>();
            Assert.AreEqual(100, setdefaultNew);
            Assert.IsTrue(module.InvokeMethod("contains", pyDict, pyNewKey).As<bool>());

            // Test update
            using var updateDict = new PyDict();
            updateDict.SetItem("x".ToPython(), 10.ToPython());
            updateDict.SetItem("y".ToPython(), 20.ToPython());
            module.InvokeMethod("update", pyDict, updateDict);

            Assert.AreEqual(10, module.InvokeMethod("get", pyDict, "x".ToPython()).As<int>());
            Assert.AreEqual(20, module.InvokeMethod("get", pyDict, "y".ToPython()).As<int>());
        }
    }
}
