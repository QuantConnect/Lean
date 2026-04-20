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
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using QuantConnect.Api;

namespace QuantConnect.Tests.API
{
    [TestFixture, Explicit("Requires configured api access and available backtest node to run on"), Parallelizable(ParallelScope.Fixtures)]
    public class ObjectStoreTests : ApiTestBase
    {
        private const string _ciTestFolder = "/CI_TEST";
        private const string _key = _ciTestFolder + "/Ricardo";
        private readonly byte[] _data = new byte[3] { 1, 2, 3 };

        private static readonly string[] _keysToSetUp = new[]
        {
            "/filename.zip",
            "/mm_test.csv",
            "/model",
            "/trades_test.json",
            "/profile_results.json",
            "/CustomData/placeholder.txt",
            "/log.txt",
            "/l1_model.p",
            "/latency_1_False.txt",
            "/portfolio-targets2.csv",
            "/Regressor",
            "/example_data_2.zip"
        };

        [OneTimeSetUp]
        public void SetUpObjectStoreFiles()
        {
            foreach (var key in _keysToSetUp)
            {
                ApiClient.SetObjectStore(TestOrganization, _ciTestFolder + key, _data);
            }
        }

        [OneTimeTearDown]
        public void TearDownObjectStoreFiles()
        {
            ApiClient.DeleteObjectStore(TestOrganization, _ciTestFolder);
        }

        [TestCaseSource(nameof(GetObjectStoreWorksAsExpectedTestCases))]
        public void GetObjectStoreWorksAsExpected(string testName, List<string> keys, bool isSuccessExpected)
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

        [TestCase(_ciTestFolder + "/filename.zip", true)]
        [TestCase(_ciTestFolder + "/orats_2024-02-32.json", false)]
        [TestCase(_ciTestFolder + "/mrm8488", false)]
        [TestCase(_ciTestFolder + "/mm_test.csv", true)]
        [TestCase(_ciTestFolder + "/model", true)]
        [TestCase(_ciTestFolder + "/trades_test.json", true)]
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
            new object[] { "Two keys present", new List<string> { _ciTestFolder + "/trades_test.json", _ciTestFolder + "/profile_results.json" }, true},
            new object[] { "No key is given", new List<string> {}, false},
            new object[] { "One key is present and the other one not", new List<string> { _ciTestFolder + "/trades_test.json", _ciTestFolder + "/orats_2024-02-32.json" }, true},
            new object[] { "The key is not present", new List<string> { _ciTestFolder + "/orats_2024-02-32.json" }, false},
            new object[] { "The type of the object store file is directory", new List<string> { _ciTestFolder + "/CustomData" }, true},
            new object[] { "The type of the object store file is text/plain", new List<string> { _ciTestFolder + "/log.txt" }, true},
            new object[] { "The type of the object store file is application/octet-stream", new List<string> { _ciTestFolder + "/model" }, true},
            new object[] { "The type of the object store file is P", new List<string> { _ciTestFolder + "/l1_model.p" }, true},
            new object[] { "Heavy object store files", new List<string> {
                _ciTestFolder + "/latency_1_False.txt",
                _ciTestFolder + "/portfolio-targets2.csv",
                _ciTestFolder + "/Regressor",
                _ciTestFolder + "/example_data_2.zip"
            }, true}
        };
    }
}
