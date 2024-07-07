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

using System.Collections.Generic;
using NUnit.Framework;
using Python.Runtime;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class PythonCollectionsTests
    {
        private static dynamic containsKeyTest;
        private static dynamic containsTest;
        private static string testModule =
            @"
def ContainsTest(key, collection):
    if key in collection.Keys:
        return True
    return False

def ContainsKeyTest(key, collection):
    return collection.ContainsKey(key)
";

        [OneTimeSetUp]
        public void Setup()
        {
            using (Py.GIL())
            {
                var pyModule = PyModule.FromString("module", testModule);
                containsTest = pyModule.GetAttr("ContainsTest");
                containsKeyTest = pyModule.GetAttr("ContainsKeyTest");
            }
        }

        [TestCase("AAPL", false)]
        [TestCase("SPY", true)]
        public void Contains(string key, bool expected)
        {
            using (Py.GIL())
            {
                var dic = new Dictionary<string, object> { { "SPY", new object() } };
                Assert.AreEqual(expected, (bool)containsTest(key, dic));
            }
        }

        [TestCase("AAPL", false)]
        [TestCase("SPY", true)]
        public void ContainsKey(string key, bool expected)
        {
            using (Py.GIL())
            {
                var dic = new Dictionary<string, object> { { "SPY", new object() } };
                Assert.AreEqual(expected, (bool)containsKeyTest(key, dic));
            }
        }
    }
}
