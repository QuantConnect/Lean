using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Configuration;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// Base test class for Api tests, provides the setup needed for all api tests
    /// </summary>
    public class ApiTestBase
    {
        internal int TestAccount;
        internal string TestToken;
        internal string TestOrganization;
        internal string DataFolder;
        internal Api.Api ApiClient;

        /// <summary>
        /// Run once before any RestApiTests
        /// </summary>
        [OneTimeSetUp]
        public void Setup()
        {
            TestAccount = Config.GetInt("job-user-id", 1);
            TestToken = Config.Get("api-access-token", "ec87b337ac970da4cbea648f24f1c851");
            TestOrganization = Config.Get("job-organization-id", "EnterOrgHere");
            DataFolder = Config.Get("data-folder");

            ApiClient = new Api.Api();
            ApiClient.Initialize(TestAccount, TestToken, DataFolder);
        }
    }
}
