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

using System.Web;
using NUnit.Framework;
using QuantConnect.Api;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace QuantConnect.Tests.API
{
    [TestFixture]
    public class AuthenticationTests
    {
        [Test, Explicit("Requires api creds")]
        public void Link()
        {
            var link = Authentication.Link("authenticate");

            var response = link.DownloadData();

            Assert.IsNotNull(response);

            var jobject = JObject.Parse(response);
            Assert.IsTrue(jobject["success"].ToObject<bool>());
        }

        [Test]
        public void PopulateQueryString()
        {
            var payload = new { SomeArray = new[] { 1, 2, 3 }, Symbol = "SPY", Parameters = new Dictionary<string, int>() { { "Quantity", 10 } } };

            var queryString = HttpUtility.ParseQueryString(string.Empty);
            Authentication.PopulateQueryString(queryString, new[] { new KeyValuePair<string, object>("command", payload) });

            Assert.AreEqual("command%5bSomeArray%5d%5b0%5d=1&command%5bSomeArray%5d%5b1%5d=2&command%5bSomeArray%5d%5b2%5d=3&command%5bSymbol%5d=SPY&command%5bParameters%5d%5bQuantity%5d=10", queryString.ToString());
        }
    }
}
