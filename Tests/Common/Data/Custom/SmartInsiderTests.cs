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
using QuantConnect.Data.Custom.SmartInsider;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class SmartInsiderTests
    {
        [Test]
        public void ErrorGetsMappedToSatisfyStockVesting()
        {
            var intentionLine = "BIXYZ	Downwards Revision	20190101	20190101	USXYZ		1	Some Random Industry																		US	Off Market Agreement	Issuer	Missing Lookup Formula for BuybackHoldingTypeId 10.00										";
            var transactionLine = "BIXYZ	Downwards Revision	20190101	20190101	USXYZ		1	Some Random Industry																				Off Market Agreement	Issuer	Missing Lookup Formula for BuybackHoldingTypeId 10.00																														";

            var intention = new SmartInsiderIntention(intentionLine);
            var transaction = new SmartInsiderTransaction(transactionLine);

            Assert.IsTrue(intention.ExecutionHolding.HasValue);
            Assert.IsTrue(transaction.ExecutionHolding.HasValue);
            Assert.AreEqual(intention.ExecutionHolding, SmartInsiderExecutionHolding.SatisfyStockVesting);
            Assert.AreEqual(transaction.ExecutionHolding, SmartInsiderExecutionHolding.SatisfyStockVesting);
        }
    }
}
