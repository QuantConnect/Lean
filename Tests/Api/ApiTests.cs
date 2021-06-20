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
using QuantConnect.Api;
using System.IO;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// API Object tests
    /// Tests APIs ability to connect to Web API
    /// </summary>
    [TestFixture, Explicit("Requires configured api access")]
    public class ApiTest : ApiTestBase
    {
        /// <summary>
        /// Test successfully authenticating with the ApiConnection using valid credentials.
        /// </summary>
        [Test]
        public void ApiConnectionWillAuthenticate_ValidCredentials_Successfully()
        {
            var connection = new ApiConnection(TestAccount, TestToken);
            Assert.IsTrue(connection.Connected);
        }

        /// <summary>
        /// Test successfully authenticating with the API using valid credentials.
        /// </summary>
        [Test]
        public void ApiWillAuthenticate_ValidCredentials_Successfully()
        {
            var api = new Api.Api();
            api.Initialize(TestAccount, TestToken, DataFolder);
            Assert.IsTrue(api.Connected);
        }

        /// <summary>
        /// Test that the ApiConnection will reject invalid credentials
        /// </summary>
        [Test]
        public void ApiConnectionWillAuthenticate_InvalidCredentials_Unsuccessfully()
        {
            var connection = new ApiConnection(TestAccount, "");
            Assert.IsFalse(connection.Connected);
        }

        /// <summary>
        /// Test that the Api will reject invalid credentials
        /// </summary>
        [Test]
        public void ApiWillAuthenticate_InvalidCredentials_Unsuccessfully()
        {
            var api = new Api.Api();
            api.Initialize(TestAccount, "", DataFolder);
            Assert.IsFalse(api.Connected);
        }

        [TestCase("C:\\Data")]
        [TestCase("C:\\Data\\")]
        [TestCase("C:/Data/")]
        [TestCase("C:/Data")]
        public void FormattingPathForDataRequestsAreCorrect(string dataFolder)
        {
            var api = new Api.Api();
            api.Initialize(TestAccount, TestToken, dataFolder);

            var dataToDownload = "forex/oanda/daily/eurusd.zip";
            var path = Path.Combine(dataFolder, dataToDownload);

            var result = api.FormatPathForDataRequest(path);
            Assert.AreEqual(dataToDownload, result);
        }
    }
}
