using NUnit.Framework;
using QuantConnect.Configuration;
using System.Collections.Generic;
using System;

namespace QuantConnect.Tests.API
{
    [TestFixture, Explicit("Requires configured api access and available backtest node to run on")]
    public class ObjectStoreTests: ApiTestBase
    {
        private readonly string _organizationId = Config.Get("job-organization-id");
        private const string _key = "/Ricardo";

        [Test]
        public void GetObjectStoreWorksAsExpected()
        {
            var keys = new List<string>()
            {
                "/Ronit2",
                "/Ronit3"
            };

            var result = ApiClient.GetObjectStore(_organizationId, keys);
            Assert.IsTrue(result.Success);
        }

        [Test]
        public void SetObjectStoreWorksAsExpected()
        {
            var data = new byte[3] { 1, 2, 3 };


            var result = ApiClient.SetObjectStore(_organizationId, _key, data);
            Assert.IsTrue(result.Success);
        }

        [Test]
        public void DeleteObjectStoreWorksAsExpected()
        {
            SetObjectStoreWorksAsExpected();
            var result = ApiClient.DeleteObjectStore(_organizationId, _key);
            Assert.IsTrue(result.Success);
        }

        [Test]
        public void ListObjectStoreWorksAsExpected()
        {
            var path = "/";

            var result = ApiClient.ListObjectStore(_organizationId, path);
            Assert.IsTrue(result.Success);
        }
    }
}
