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
 *
*/

using System;
using NUnit.Framework;
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture]
    public class IdentitySymbolMapperTests
    {
        [Test]
        public void ReturnsCorrectLeanSymbol()
        {
            var mapper = new IdentitySymbolMapper(Market.USA);

            var symbol = mapper.GetLeanSymbol("AAPL");
            Assert.AreEqual("AAPL", symbol.Value);
            Assert.AreEqual(SecurityType.Equity, symbol.ID.SecurityType);
            Assert.AreEqual(Market.USA, symbol.ID.Market);
        }

        [Test]
        public void ReturnsCorrectBrokerageSymbol()
        {
            var mapper = new IdentitySymbolMapper(Market.USA);

            var symbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var brokerageSymbol = mapper.GetBrokerageSymbol(symbol);
            Assert.AreEqual("AAPL", brokerageSymbol);
        }

        [Test]
        public void ThrowsOnNullOrEmptySymbol()
        {
            var mapper = new IdentitySymbolMapper(Market.USA);

            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(null));

            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(""));

            var symbol = Symbol.Empty;
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(symbol));

            symbol = null;
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(symbol));

            symbol = Symbol.Create("", SecurityType.Equity, Market.USA);
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(symbol));
        }


    }
}
