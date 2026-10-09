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

using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Api;

namespace QuantConnect.Tests.API
{
    [TestFixture]
    public class OrganizationResponseTests
    {
        [TestCase("{\"organization\":{\"type\":\"team\",\"data\":{\"signedTime\":null,\"current\":false},\"objectStoreExportDerivative\":true},\"success\":true}", true)]
        [TestCase("{\"organization\":{\"type\":\"team\",\"data\":{\"signedTime\":null,\"current\":false},\"objectStoreExportDerivative\":false},\"success\":true}", false)]
        [TestCase("{\"organization\":{\"type\":\"team\",\"data\":{\"signedTime\":null,\"current\":false}},\"success\":true}", false)]
        public void DeserializesObjectStoreExportDerivative(string response, bool expected)
        {
            var organization = JsonConvert.DeserializeObject<OrganizationResponse>(response).Organization;

            Assert.AreEqual("team", organization.Type);
            Assert.AreEqual(expected, organization.ObjectStoreExportDerivative);
        }
    }
}
