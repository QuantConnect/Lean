/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using NUnit.Framework;
using QuantConnect.ToolBox.Polygon;

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture]
    public class PolygonSymbolMapperTests
    {
        [TestCase("SPY", SecurityType.Equity, Market.USA, "SPY")]
        [TestCase("EURUSD", SecurityType.Forex, Market.FXCM, "EUR/USD")]
        [TestCase("BTCUSD", SecurityType.Crypto, Market.GDAX, "BTC-USD")]
        public void ReturnsCorrectLeanSymbol(string leanSymbol, SecurityType securityType, string market, string brokerageSymbol)
        {
            var mapper = new PolygonSymbolMapper();

            var symbol = mapper.GetLeanSymbol(brokerageSymbol, securityType, market);

            Assert.AreEqual(leanSymbol, symbol.Value);
            Assert.AreEqual(securityType, symbol.ID.SecurityType);
            Assert.AreEqual(market, symbol.ID.Market);
        }

        [TestCase("SPY", SecurityType.Equity, Market.USA, "SPY")]
        [TestCase("EURUSD", SecurityType.Forex, Market.FXCM, "EUR/USD")]
        [TestCase("BTCUSD", SecurityType.Crypto, Market.GDAX, "BTC-USD")]
        public void ReturnsCorrectBrokerageSymbol(string leanSymbol, SecurityType securityType, string market, string brokerageSymbol)
        {
            var symbol = Symbol.Create(leanSymbol, securityType, market);

            var mapper = new PolygonSymbolMapper();

            var symbolId = mapper.GetBrokerageSymbol(symbol);

            Assert.AreEqual(brokerageSymbol, symbolId);
        }
    }
}
