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

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class ExchangeTest
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
        [TestCase("BSE", "BSE")]
        [TestCase("NSX", "C")]
        [TestCase("FINRA", "D")]
        [TestCase("ISE", "I")]
        [TestCase("CSE", "M")]
        [TestCase("CBOE", "W")]
        [TestCase("EDGA", "J")]
        [TestCase("EDGX", "K")]
        [TestCase("AMEX", "A")]
        [TestCase("UNKNOWN", "")]
        [TestCase("", "")]
        [TestCase(null, "")]
        // option exchanges not found for default equity
        [TestCase("ISE_GEMINI", "")]
        [TestCase("ISE_MERCURY", "")]
        [TestCase("OPRA", "")]
        public void ExchangeCorrectlyReturnedAsSingleLetter(
            string exchange,
            string expectedExchangeCode
        )
        {
            var primaryExchange = exchange.GetPrimaryExchange();
            Assert.AreEqual(expectedExchangeCode, primaryExchange.Code);
            Assert.AreEqual(primaryExchange, primaryExchange.Code.GetPrimaryExchange());
        }

        [TestCase("ISE_GEMINI", "H", SecurityType.Option)]
        [TestCase("ISE_MERCURY", "J", SecurityType.Option)]
        [TestCase("OPRA", "O", SecurityType.Option)]
        public void ExchangeCorrectlyReturnedAsSingleLetterSecurityType(
            string exchange,
            string expectedExchange,
            SecurityType securityType
        )
        {
            Assert.AreEqual(
                expectedExchange,
                exchange.GetPrimaryExchangeCodeGetPrimaryExchange(securityType)
            );
        }

        [TestCaseSource(nameof(ExchangeCases))]
        public void StringExchangeCorrectlyReturnedAsSingleLetter(
            Exchange expectedExchange,
            string exchange,
            string _,
            SecurityType securityType
        )
        {
            Assert.AreEqual(expectedExchange, exchange.GetPrimaryExchange(securityType));
        }

        [TestCaseSource(nameof(ExchangeCases))]
        public void ExchangeAsString(
            Exchange exchange,
            string _,
            string expectedExchange,
            SecurityType __
        )
        {
            Assert.AreEqual(expectedExchange, exchange.ToString());
        }

        [TestCaseSource(nameof(EqualityExchangeCases))]
        public void Equality(bool expectedResult, Exchange left, Exchange right)
        {
            Assert.AreEqual(expectedResult, left == right);
            if (left != null)
            {
                Assert.AreEqual(expectedResult, left.Equals(right));
            }
            if (left != null && right != null)
            {
                Assert.AreEqual(expectedResult, left.GetHashCode() == right.GetHashCode());
            }
        }

        [Test]
        public void RoundTripSerialization_unknown()
        {
            var serialized = JsonConvert.SerializeObject(Exchange.UNKNOWN);
            var deserialized = JsonConvert.DeserializeObject<Exchange>(serialized);

            Assert.AreEqual(Exchange.UNKNOWN, deserialized);
        }

        [Test]
        public void RoundTripSerialization()
        {
            var serialized = JsonConvert.SerializeObject(Exchange.C2);
            var deserialized = JsonConvert.DeserializeObject<Exchange>(serialized);

            Assert.AreEqual(Exchange.C2, deserialized);
        }

        private static TestCaseData[] EqualityExchangeCases()
        {
            return new[]
            {
                new TestCaseData(true, Exchange.UNKNOWN, null),
                new TestCaseData(true, null, Exchange.UNKNOWN),
                new TestCaseData(true, null, null),
                new TestCaseData(true, Exchange.NASDAQ, Exchange.NASDAQ),
                new TestCaseData(true, Exchange.NYSE, Exchange.NYSE),
                new TestCaseData(false, Exchange.UNKNOWN, Exchange.NYSE),
                new TestCaseData(false, null, Exchange.NYSE),
                new TestCaseData(false, Exchange.NSX, Exchange.NSE),
                new TestCaseData(false, Exchange.NASDAQ_PSX, Exchange.NASDAQ),
                new TestCaseData(false, Exchange.NASDAQ, Exchange.NASDAQ_BX),
            };
        }

        private static TestCaseData[] ExchangeCases()
        {
            return new[]
            {
                new TestCaseData(Exchange.UNKNOWN, null, "", SecurityType.Base),
                new TestCaseData(Exchange.NASDAQ, "Q", "NASDAQ", SecurityType.Equity),
                new TestCaseData(Exchange.NASDAQ_BX, "B", "NASDAQ_BX", SecurityType.Equity),
                new TestCaseData(Exchange.NASDAQ_PSX, "X", "NASDAQ_PSX", SecurityType.Equity),
                new TestCaseData(Exchange.BATS, "Z", "BATS", SecurityType.Equity),
                new TestCaseData(Exchange.BATS_Y, "Y", "BATS_Y", SecurityType.Equity),
                new TestCaseData(Exchange.ARCA, "P", "ARCA", SecurityType.Equity),
                new TestCaseData(Exchange.NYSE, "N", "NYSE", SecurityType.Equity),
                new TestCaseData(Exchange.NSX, "C", "NSE", SecurityType.Equity),
                new TestCaseData(Exchange.FINRA, "D", "FINRA", SecurityType.Equity),
                new TestCaseData(Exchange.ISE, "I", "ISE", SecurityType.Equity),
                new TestCaseData(Exchange.CSE, "M", "CSE", SecurityType.Equity),
                new TestCaseData(Exchange.CBOE, "W", "CBOE", SecurityType.Equity),
                new TestCaseData(Exchange.EDGA, "J", "EDGA", SecurityType.Equity),
                new TestCaseData(Exchange.EDGX, "K", "EDGX", SecurityType.Equity),
                new TestCaseData(Exchange.AMEX, "A", "AMEX", SecurityType.Equity),
                new TestCaseData(Exchange.NSX, "C", "NSE", SecurityType.Equity),
                new TestCaseData(Exchange.BSE, "BSE", "BSE", SecurityType.Equity),
                new TestCaseData(Exchange.UNKNOWN, "O", "", SecurityType.Equity),
                new TestCaseData(Exchange.UNKNOWN, "H", "", SecurityType.Equity),
                new TestCaseData(Exchange.EDGA, "J", "EDGA", SecurityType.Equity),
                new TestCaseData(Exchange.OPRA, "O", "OPRA", SecurityType.Option),
                new TestCaseData(Exchange.ISE_GEMINI, "H", "ISE_GEMINI", SecurityType.Option),
                new TestCaseData(Exchange.ISE_MERCURY, "J", "ISE_MERCURY", SecurityType.Option),
            };
        }
    }
}
