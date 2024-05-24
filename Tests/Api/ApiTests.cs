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
using System.IO;
using NUnit.Framework;
using QuantConnect.Api;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// API Object tests
    /// Tests APIs ability to connect to Web API
    /// </summary>
    [TestFixture]
    public class ApiTest : ApiTestBase
    {
        /// <summary>
        /// Test successfully authenticating with the ApiConnection using valid credentials.
        /// </summary>
        [Test, Explicit("Requires configured api access")]
        public void ApiConnectionWillAuthenticate_ValidCredentials_Successfully()
        {
            var connection = new ApiConnection(TestAccount, TestToken);
            Assert.IsTrue(connection.Connected);
        }

        /// <summary>
        /// Test successfully authenticating with the API using valid credentials.
        /// </summary>
        [Test, Explicit("Requires configured api access")]
        public void ApiWillAuthenticate_ValidCredentials_Successfully()
        {
            using var api = new Api.Api();
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
            using var api = new Api.Api();
            api.Initialize(TestAccount, "", DataFolder);
            Assert.IsFalse(api.Connected);
        }

        [Test]
        public void NullDataFolder()
        {
            using var api = new Api.Api();
            Assert.DoesNotThrow(() => api.Initialize(TestAccount, "", null));
        }

        [TestCase("C:\\Data", "forex/oanda/daily/eurusd.zip")]
        [TestCase("C:\\Data\\", "forex/oanda/daily/eurusd.zip")]
        [TestCase("C:/Data/", "forex/oanda/daily/eurusd.zip")]
        [TestCase("C:/Data", "forex/oanda/daily/eurusd.zip")]
        [TestCase("C:/Data", "forex\\oanda\\daily\\eurusd.zip")]
        [TestCase("C:/Data/", "forex\\oanda\\daily\\eurusd.zip")]
        [TestCase("C:\\Data\\", "forex\\oanda\\daily\\eurusd.zip")]
        [TestCase("C:\\Data", "forex\\oanda\\daily\\eurusd.zip")]
        public void FormattingPathForDataRequestsAreCorrect(string dataFolder, string dataToDownload)
        {
            var path = Path.Combine(dataFolder, dataToDownload);

            var result = Api.Api.FormatPathForDataRequest(path, dataFolder);
            Assert.AreEqual(dataToDownload.Replace("\\", "/", StringComparison.InvariantCulture), result);
        }

        [TestCase("Authorization", "AWS4-HMAC-SHA256 Credential=AKIAIOSFODNN7EXAMPLE/20130524/us-east-1/s3/aws4_request, SignedHeaders=host;range;x-amz-date,Signature=EXAMPLE_SIGNATURE")]
        [TestCase("Custom-Header", "Custom header value")]
        public void DownloadBytesAllowsUserDefinedHeaders(string headerKey, string headerValue)
        {
            using var api = new Api.Api();

            var headers = new List<KeyValuePair<string, string>>() { new(headerKey, headerValue) };
            Assert.DoesNotThrow(() => api.Download("https://www.dropbox.com/s/ggt6blmib54q36e/CAPE.csv?dl=1", headers, "", ""));
        }
    }
}
