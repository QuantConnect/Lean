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
using NUnit.Framework;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// Tests API account and organizations endpoints
    /// </summary>
    [TestFixture, Explicit("Requires configured api access")]
    public class OrganizationTests : ApiTestBase
    {
        [Test]
        public void ReadAccount()
        {
            var account = ApiClient.ReadAccount();

            Assert.IsTrue(account.Success);
            Assert.IsNotEmpty(account.OrganizationId);
            Assert.IsNotNull(account.Card);
            Assert.AreNotEqual(default(DateTime), account.Card.Expiration);
            Assert.IsNotEmpty(account.Card.Brand);
            Assert.AreNotEqual(0, account.Card.LastFourDigits);
        }

        [Test]
        public void ReadOrganization()
        {
            var organization = ApiClient.ReadOrganization(TestOrganization);

            Assert.AreNotEqual(default(DateTime), organization.DataAgreement.Signed);
            Assert.AreNotEqual(0, organization.DataAgreement.EpochSignedTime);
            Assert.AreNotEqual(0, organization.Credit.Balance);
            Assert.AreNotEqual(0, organization.Products.Count);
        }
    }
}
