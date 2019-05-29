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
using QuantConnect.ToolBox.CoinApiDataConverter;

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture]
    class CoinApiBitfinexSymbolMapperTest
    {
        [Test]
        public void ReturnsCorrectLeanSymbol()
        {
            var brokerageSymbol = "ANIO_USD";

            var mapper = new CoinApiBitfinexSymbolMapper();

            var symbol = mapper.GetLeanSymbol(brokerageSymbol, SecurityType.Crypto, Market.Bitfinex);
            Assert.AreEqual("NIOUSD", symbol.Value);
            Assert.AreEqual(SecurityType.Crypto, symbol.ID.SecurityType);
            Assert.AreEqual(Market.Bitfinex, symbol.ID.Market);
        }

        [Test]
        public void ReturnsCorrectBrokerageSymbol()
        {
            var symbol = Symbol.Create("QTMUST", SecurityType.Crypto, Market.Bitfinex);

            var mapper = new CoinApiBitfinexSymbolMapper();
            var coinApiSymbol = mapper.GetBrokerageSymbol(symbol);

            Assert.AreEqual("QTUM_USDT", coinApiSymbol);
        }
    }
}
