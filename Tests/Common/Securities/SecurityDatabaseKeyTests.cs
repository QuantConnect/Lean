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
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityDatabaseKeyTests
    {
        [Test]
        public void ConstructorWithNoWildcards()
        {
            var key = new SecurityDatabaseKey(Market.USA, "SPY", SecurityType.Equity);
            Assert.AreEqual(SecurityType.Equity, key.SecurityType);
            Assert.AreEqual(Market.USA, key.Market);
            Assert.AreEqual("SPY", key.Symbol);
        }

        [Test]
        public void ConstructorWithNullSymbolConvertsToWildcard()
        {
            var key = new SecurityDatabaseKey(Market.USA, null, SecurityType.Equity);
            Assert.AreEqual(SecurityType.Equity, key.SecurityType);
            Assert.AreEqual(Market.USA, key.Market);
            Assert.AreEqual("[*]", key.Symbol);
        }

        [Test]
        public void ConstructorWithEmptySymbolConvertsToWildcard()
        {
            var key = new SecurityDatabaseKey(Market.USA, string.Empty, SecurityType.Equity);
            Assert.AreEqual(SecurityType.Equity, key.SecurityType);
            Assert.AreEqual(Market.USA, key.Market);
            Assert.AreEqual("[*]", key.Symbol);
        }

        [Test]
        public void ConstructorWithNullMarketConvertsToWildcard()
        {
            var key = new SecurityDatabaseKey(null, "SPY", SecurityType.Equity);
            Assert.AreEqual(SecurityType.Equity, key.SecurityType);
            Assert.AreEqual("[*]", key.Market);
            Assert.AreEqual("SPY", key.Symbol);
        }

        [Test]
        public void ConstructorWithEmptyMarketConvertsToWildcard()
        {
            var key = new SecurityDatabaseKey(string.Empty, "SPY", SecurityType.Equity);
            Assert.AreEqual(SecurityType.Equity, key.SecurityType);
            Assert.AreEqual("[*]", key.Market);
            Assert.AreEqual("SPY", key.Symbol);
        }

        [Test]
        public void ParsesKeyProperly()
        {
            const string input = "Equity-usa-SPY";
            var key = SecurityDatabaseKey.Parse(input);
            Assert.AreEqual(SecurityType.Equity, key.SecurityType);
            Assert.AreEqual(Market.USA, key.Market);
            Assert.AreEqual("SPY", key.Symbol);
        }

        [Test]
        public void ParsesWildcardSymbol()
        {
            const string input = "Equity-usa-[*]";
            var key = SecurityDatabaseKey.Parse(input);
            Assert.AreEqual(SecurityType.Equity, key.SecurityType);
            Assert.AreEqual(Market.USA, key.Market);
            Assert.AreEqual("[*]", key.Symbol);
        }

        [Test]
        public void ParsesWildcardMarket()
        {
            const string input = "Equity-[*]-SPY";
            var key = SecurityDatabaseKey.Parse(input);
            Assert.AreEqual(SecurityType.Equity, key.SecurityType);
            Assert.AreEqual("[*]", key.Market);
            Assert.AreEqual("SPY", key.Symbol);
        }

        [Test]
        public void EqualityMembersAreCaseInsensitive()
        {
            var key = new SecurityDatabaseKey("uSa", "SPY", SecurityType.Equity);
            var key2 = new SecurityDatabaseKey("UsA", "spy", SecurityType.Equity);

            Assert.AreEqual(key, key2);
            Assert.AreEqual(key.GetHashCode(), key2.GetHashCode());
        }

        [Test, ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "as a SecurityType")]
        public void ThrowsOnWildcardSecurityType()
        {
            const string input = "[*]-usa-SPY";
            SecurityDatabaseKey.Parse(input);
        }

        [Test, ExpectedException(typeof (FormatException), MatchType = MessageMatch.Contains, ExpectedMessage = "expected format")]
        public void ThrowsOnInvalidFormat()
        {
            const string input = "Equity-[*]";
            SecurityDatabaseKey.Parse(input);
        }
    }
}
