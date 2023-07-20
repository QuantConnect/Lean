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
            Config.Set("job-user-id", "200374");
            Config.Set("api-access-token", "f2d0a4af7ad8731c3e387475944e27c92566b78c32b709d835504ca00a6fb8e7");
            Config.Set("job-organization-id", "d6d62db48592c72e67b534553413b691");

            TestAccount = Config.GetInt("job-user-id", 1);
            TestToken = Config.Get("api-access-token", "EnterTokenHere");
            TestOrganization = Config.Get("job-organization-id", "EnterOrgHere");
            DataFolder = Config.Get("data-folder");

            ApiClient = new Api.Api();
            ApiClient.Initialize(TestAccount, TestToken, DataFolder);
        }
    }
}
