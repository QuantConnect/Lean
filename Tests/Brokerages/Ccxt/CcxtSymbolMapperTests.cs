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
using QuantConnect.Brokerages.Ccxt;

namespace QuantConnect.Tests.Brokerages.Ccxt
{
    [TestFixture]
    public class CcxtSymbolMapperTests
    {
        [TestCase("binance", Market.Binance)]
        [TestCase("bittrex", Market.Bittrex)]
        [TestCase("coinbasepro", Market.GDAX)]
        [TestCase("ftx", Market.Ftx)]
        [TestCase("gateio", Market.GateIo)]
        [TestCase("kraken", Market.Kraken)]
        public void ReturnsCorrectLeanSymbol(string exchangeName, string expectedMarket)
        {
            var mapper = new CcxtSymbolMapper(exchangeName);

            var symbol = mapper.GetLeanSymbol("BTC/USD");

            Assert.AreEqual("BTCUSD", symbol.ID.Symbol);
            Assert.AreEqual(SecurityType.Crypto, symbol.SecurityType);
            Assert.AreEqual(expectedMarket, symbol.ID.Market);
        }

        [TestCase("binance", "BTCUSDT", Market.Binance, "BTC/USDT")]
        [TestCase("bittrex", "ETHBTC", Market.Bittrex, "ETH/BTC")]
        [TestCase("coinbasepro", "BTCUSD", Market.GDAX, "BTC/USD")]
        [TestCase("ftx", "ETHBTC", Market.Ftx, "ETH/BTC")]
        [TestCase("gateio", "ETHBTC", Market.GateIo, "ETH/BTC")]
        [TestCase("kraken", "ETHBTC", Market.Kraken, "ETH/BTC")]
        public void ReturnsCorrectBrokerageSymbol(string exchangeName, string leanTicker, string market, string expectedBrokerageTicker)
        {
            var mapper = new CcxtSymbolMapper(exchangeName);

            var symbol = Symbol.Create(leanTicker, SecurityType.Crypto, market);

            var brokerageSymbol = mapper.GetBrokerageSymbol(symbol);

            Assert.AreEqual(expectedBrokerageTicker, brokerageSymbol);
        }
    }
}
