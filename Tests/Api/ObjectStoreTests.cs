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

namespace QuantConnect.Tests.API
{
    [TestFixture, Explicit("Requires configured api access and available backtest node to run on")]
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

        [TestCase("/orats_2024-02-29.json", true)]
        [TestCase("/cli-projects.zip", true)]
        [TestCase("/orats_2024-02-32.json", false)]
        [TestCase("/mrm8488", false)]
        [TestCase("/ETF_constrain_Alex.csv", true)]
        [TestCase("/model", true)]
        [TestCase("/dividend_20240312.json", true)]
        public void GetObjectStorePropertiesWorksAsExpected(string key, bool isSuccessExpected)
        {
            var result = ApiClient.GetObjectStoreProperties(TestOrganization, key);
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
            var result = ApiClient.SetObjectStore(TestOrganization, _key, _data);
            Assert.IsTrue(result.Success);
            var objectsBefore = ApiClient.ListObjectStore(TestOrganization, _key);

            result = ApiClient.DeleteObjectStore(TestOrganization, _key);
            Assert.IsTrue(result.Success);

            var objectsAfter = ApiClient.ListObjectStore(TestOrganization, _key);
            Assert.AreNotEqual(objectsAfter.ObjectStorageUsed, objectsBefore.ObjectStorageUsed);

            result = ApiClient.DeleteObjectStore(TestOrganization, _key);
            Assert.IsFalse(result.Success);
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
            new object[] { new List<string> { "/orats_2024-02-17.json", "/orats_2024-02-29.json" }, true}, // Two keys present
            new object[] { new List<string> {}, false}, // No key is given
            new object[] { new List<string> { "/orats_2024-02-17.json", "/orats_2024-02-32.json" }, true}, // One key is present and the other one not
            new object[] { new List<string> { "/orats_2024-02-32.json" }, false}, // The key is not present
            new object[] { new List<string> { "/mrm8488" }, true}, // The type of the object store file is directory
            new object[] { new List<string> { "/ETF_constrain_Alex.csv" }, true}, // The type of the object store file is text/plain
            new object[] { new List<string> { "/model" }, true}, // The type of the object store file is application/octet-stream
            new object[] { new List<string> { "/dividend_20240312.json" }, true}, // The type of the object store file is application/x-empty
            new object[] { new List<string> {
                "/cli-projects.zip",
                "/500MB_big_file.txt",
                "/orats_2024-01-31.json",
                "/orats_2024-03-06.json"
            }, true} // Heavy object store files
        };
    }
}
