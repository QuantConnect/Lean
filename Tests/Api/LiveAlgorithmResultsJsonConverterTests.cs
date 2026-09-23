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

using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Api;

namespace QuantConnect.Tests.API
{
    [TestFixture]
    public class LiveAlgorithmResultsJsonConverterTests
    {
        [TestCase("deploymentDetails")]
        [TestCase("DeploymentDetails")]
        public void DeserializesDeploymentDetails(string name)
        {
            var result = Deserialize($@"""{name}"": {{ ""Account"": ""U1234567"", ""environment"": ""paper"" }},");

            CollectionAssert.AreEquivalent(
                new Dictionary<string, string> { { "Account", "U1234567" }, { "environment", "paper" } },
                result.DeploymentDetails);
        }

        [Test]
        public void DeploymentDetailsAreOptional()
        {
            // deployments running an older Lean, or whose brokerage shares none, report nothing
            var result = Deserialize();

            Assert.IsNull(result.DeploymentDetails);
            // the rest is still deserialized
            Assert.AreEqual("DeployId", result.DeployId);
            Assert.AreEqual("Running", result.Status);
            CollectionAssert.AreEquivalent(new Dictionary<string, string> { { "Unrealized", "0" } }, result.RuntimeStatistics);
        }

        [Test]
        public void ServerStatisticsAreStillDeserialized()
        {
            var result = Deserialize(@"""serverStatistics"": { ""CPU Usage"": ""1%"" },");

            CollectionAssert.AreEquivalent(new Dictionary<string, string> { { "CPU Usage", "1%" } }, result.ServerStatistics);
        }

        private static LiveAlgorithmResults Deserialize(string extraFields = "")
        {
            var json = $@"{{
                ""success"": true,
                ""message"": """",
                ""status"": ""Running"",
                ""deployId"": ""DeployId"",
                ""cloneId"": 1,
                ""launched"": ""2026-09-14T00:00:00Z"",
                ""stopped"": null,
                ""brokerage"": ""Paper Trading"",
                ""securityTypes"": ""Equity"",
                ""projectName"": ""ProjectName"",
                ""datacenter"": ""Datacenter"",
                ""public"": false,
                ""files"": [],
                ""charts"": {{}},
                {extraFields}
                ""runtimeStatistics"": {{ ""Unrealized"": ""0"" }}
            }}";

            return JsonConvert.DeserializeObject<LiveAlgorithmResults>(json, new LiveAlgorithmResultsJsonConverter());
        }
    }
}
