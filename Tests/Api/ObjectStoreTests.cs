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

namespace QuantConnect.Tests.API
{
    [TestFixture, Explicit("Requires configured api access and available backtest node to run on")]
    public class ObjectStoreTests: ApiTestBase
    {
        private const string _key = "/Ricardo";

        [Test]
        public void GetObjectStoreWorksAsExpected()
        {
            var keys = new List<string>()
            {
                "/Ronit2",
                "/Ronit3"
            };

            var result = ApiClient.GetObjectStore(TestOrganization, keys);
            Assert.IsTrue(result.Success);
        }

        [Test]
        public void SetObjectStoreWorksAsExpected()
        {
            var data = new byte[3] { 1, 2, 3 };


            var result = ApiClient.SetObjectStore(TestOrganization, _key, data);
            Assert.IsTrue(result.Success);
        }

        [Test]
        public void DeleteObjectStoreWorksAsExpected()
        {
            SetObjectStoreWorksAsExpected();
            var objectsBefore = ApiClient.ListObjectStore(TestOrganization, _key);

            var result = ApiClient.DeleteObjectStore(TestOrganization, _key);
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
        }
    }
}
