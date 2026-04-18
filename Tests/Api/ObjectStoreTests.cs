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
using QuantConnect.Configuration;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using QuantConnect.Api;

namespace QuantConnect.Tests.API
{
    [TestFixture, Explicit("Requires configured api access and available backtest node to run on"), Parallelizable(ParallelScope.Fixtures)]
    public class ObjectStoreTests: ApiTestBase
    {
        private const string _key = "/Ricardo";
        private readonly byte[] _data = new byte[3] { 1, 2, 3 };

        [TestCaseSource(nameof(GetObjectStoreWorksAsExpectedTestCases))]
        public void GetObjectStoreWorksAsExpected(List<string> keys, bool isSuccessExpected)
        {
            var path = Directory.GetCurrentDirectory() + "/StoreObjectFolder/";
            var result = ApiClient.GetObjectStore(TestOrganization, keys, path);
            if (isSuccessExpected)
            {
                Assert.IsTrue(result);
                DirectoryAssert.Exists(path);
                Assert.IsTrue(keys.Where(x => File.Exists(path + x) || Directory.Exists(path + x)).Any()); // For some test cases, just one of the keys is present in the Object Store.
            }
            else
            {
                Assert.IsFalse(result);
            }
        }

        [TestCase("/filename.zip", true)]
        [TestCase("/orats_2024-02-32.json", false)]
        [TestCase("/mrm8488", false)]
        [TestCase("/mm_test.csv", true)]
        [TestCase("/model", true)]
        [TestCase("/trades_test.json", true)]
        public void GetObjectStorePropertiesWorksAsExpected(string key, bool isSuccessExpected)
        {
            var result = ApiClient.GetObjectStoreProperties(TestOrganization, key);
            var stringRepresentation = result.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
            if (isSuccessExpected)
            {
                Assert.IsTrue(result.Success);
            }
            else
            {
                Assert.IsFalse(result.Success);
            }
        }

        [Test]
        public void SetObjectStoreWorksAsExpected()
        {
            var result = ApiClient.DeleteObjectStore(TestOrganization, _key);
            Assert.IsFalse(result.Success);

            result = ApiClient.SetObjectStore(TestOrganization, _key, _data);
            Assert.IsTrue(result.Success);

            result = ApiClient.DeleteObjectStore(TestOrganization, _key);
            Assert.IsTrue(result.Success);
        }

        [Test]
        public void DeleteObjectStoreWorksAsExpected()
        {
            var result = ApiClient.SetObjectStore(TestOrganization, _key + "/test1.txt", _data);
            var result2 = ApiClient.SetObjectStore(TestOrganization, _key + "/test2.txt", _data);
            Assert.IsTrue(result.Success);
            var objectsBefore = ApiClient.ListObjectStore(TestOrganization, _key);
            var stringRepresentation = objectsBefore.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
            var numberOfObjectsBefore = objectsBefore.Objects.Count;
            var totalSizeBefore = objectsBefore.Objects.Select(o => o.Size).Sum();

            result = ApiClient.DeleteObjectStore(TestOrganization, _key + "/test1.txt");
            Assert.IsTrue(result.Success);

            ListObjectStoreResponse objectsAfter;
            var time = DateTime.UtcNow;
            do
            {
                objectsAfter = ApiClient.ListObjectStore(TestOrganization, _key);
            } while (objectsAfter.Objects.Count == numberOfObjectsBefore && DateTime.UtcNow < time.AddMinutes(10));

            var totalSizeAfter = objectsAfter.Objects.Select(o => o.Size).Sum();
            Assert.IsTrue(totalSizeAfter < totalSizeBefore);

            result = ApiClient.DeleteObjectStore(TestOrganization, _key);
            Assert.IsTrue(result.Success);
        }

        [Test]
        public void ListObjectStoreWorksAsExpected()
        {
            var path = "/";

            var result = ApiClient.ListObjectStore(TestOrganization, path);
            Assert.IsTrue(result.Success);
            Assert.IsNotEmpty(result.Objects);
            Assert.AreEqual(path, result.Path);
        }

        private static object[] GetObjectStoreWorksAsExpectedTestCases =
        {
            new object[] { new List<string> { "/trades_test.json", "/profile_results.json" }, true}, // Two keys present
            new object[] { new List<string> {}, false}, // No key is given
            new object[] { new List<string> { "/trades_test.json", "/orats_2024-02-32.json" }, true}, // One key is present and the other one not
            new object[] { new List<string> { "/orats_2024-02-32.json" }, false}, // The key is not present
            new object[] { new List<string> { "/CustomData" }, true}, // The type of the object store file is directory
            new object[] { new List<string> { "/log.txt" }, true}, // The type of the object store file is text/plain
            new object[] { new List<string> { "/model" }, true}, // The type of the object store file is application/octet-stream
            new object[] { new List<string> { "/l1_model.p" }, true}, // The type of the object store file is P
            new object[] { new List<string> {
                "/latency_1_False.txt",
                "/portfolio-targets2.csv",
                "/Regressor",
                "/example_data_2.zip"
            }, true} // Heavy object store files
        };
    }
}
