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
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Api;

namespace QuantConnect.Tests.API
{
    [TestFixture]
    public class AccountTests
    {
        [TestCase("{\"organizationId\":\"organizationID\",\"creditBalance\":111.56,\"card\":{\"brand\":\"visa\",\"expiration\":\"12\\/27\",\"last4\":\"0001\"},\"success\":true}", true)]
        [TestCase("{\"organizationId\":\"organizationID\",\"creditBalance\":111.56,\"card\":null,\"success\":true}", false)]
        public void Deserialize(string response, bool hasCard)
        {
            var account = JsonConvert.DeserializeObject<Account>(response);

            Assert.AreEqual("organizationID", account.OrganizationId);
            Assert.AreEqual(111.56m, account.CreditBalance);

            if (hasCard)
            {
                Assert.AreEqual(1, account.Card.LastFourDigits);
                Assert.AreEqual("visa", account.Card.Brand);
                Assert.AreEqual(new DateTime(2027, 12, 1), account.Card.Expiration);
            }
            else
            {
                Assert.IsNull(account.Card);
            }
        }
    }
}
