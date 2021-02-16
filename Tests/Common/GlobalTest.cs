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

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class GlobalTest
    {
        [TestCase("NASDAQ", "Q")]
        [TestCase("NASDAQ BX", "B")]
        [TestCase("NASDAQ PSX", "X")]
        [TestCase("BATS", "Z")]
        [TestCase("BATS Z", "Z")]
        [TestCase("BATS Y", "Y")]
        [TestCase("ARCA", "P")]
        [TestCase("NYSE", "N")]
        [TestCase("NSE", "C")]
        [TestCase("FINRA", "D")]
        [TestCase("ISE", "I")]
        [TestCase("CSE", "M")]
        [TestCase("CBOE", "W")]
        [TestCase("EDGA", "J")]
        [TestCase("EDGX", "K")]
        [TestCase("AMEX", "A")]
        [TestCase("", null)]
        [TestCase(null, null)]
        public void ExchangeCorrectlyReturnedAsSingleLetter(string exchange, string expectedExchange)
        {
            Assert.AreEqual(expectedExchange, exchange.GetPrimaryExchangeAsSingleCharacter());
        }
    }
}
