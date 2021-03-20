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
using QuantConnect.ToolBox.CoinApi;

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture, Explicit("These tests require a CoinAPI api key.")]
    public class CoinApiSymbolMapperTests
    {
        [TestCase("COINBASE_SPOT_BTC_USD", "BTCUSD", Market.GDAX)]
        [TestCase("COINBASE_SPOT_BCH_USD", "BCHUSD", Market.GDAX)]
        [TestCase("BITFINEX_SPOT_BTC_USD", "BTCUSD", Market.Bitfinex)]
        [TestCase("BITFINEX_SPOT_BCHABC_USD", "BCHUSD", Market.Bitfinex)]
        [TestCase("BITFINEX_SPOT_BCHSV_USD", "BSVUSD", Market.Bitfinex)]
        [TestCase("BITFINEX_SPOT_ABS_USD", "ABYSSUSD", Market.Bitfinex)]
        public void ReturnsCorrectLeanSymbol(string coinApiSymbolId, string leanTicker, string market)
        {
            var mapper = new CoinApiSymbolMapper();

            var symbol = mapper.GetLeanSymbol(coinApiSymbolId, SecurityType.Crypto, string.Empty);

            Assert.AreEqual(leanTicker, symbol.Value);
            Assert.AreEqual(SecurityType.Crypto, symbol.ID.SecurityType);
            Assert.AreEqual(market, symbol.ID.Market);
        }

        [TestCase("BTCUSD", Market.GDAX, "COINBASE_SPOT_BTC_USD")]
        [TestCase("BCHUSD", Market.GDAX, "COINBASE_SPOT_BCH_USD")]
        [TestCase("BTCUSD", Market.Bitfinex, "BITFINEX_SPOT_BTC_USD")]
        [TestCase("BCHUSD", Market.Bitfinex, "BITFINEX_SPOT_BCHABC_USD")]
        [TestCase("BSVUSD", Market.Bitfinex, "BITFINEX_SPOT_BCHSV_USD")]
        [TestCase("ABYSSUSD", Market.Bitfinex, "BITFINEX_SPOT_ABS_USD")]
        public void ReturnsCorrectBrokerageSymbol(string leanTicker, string market, string coinApiSymbolId)
        {
            var mapper = new CoinApiSymbolMapper();

            var symbol = Symbol.Create(leanTicker, SecurityType.Crypto, market);

            var symbolId = mapper.GetBrokerageSymbol(symbol);

            Assert.AreEqual(coinApiSymbolId, symbolId);
        }
    }
}
