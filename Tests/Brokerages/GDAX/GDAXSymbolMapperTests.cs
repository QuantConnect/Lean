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
using QuantConnect.Brokerages.GDAX;
using System;

namespace QuantConnect.Tests.Brokerages.GDAX
{
    [TestFixture]
    public class GDAXSymbolMapperTests
    {
        #region Data

        private static TestCaseData[] BrokerageSymbols => new[]
        {
            new TestCaseData("ETH-USD", "ETHUSD"),
            new TestCaseData("ETH-BTC", "ETHBTC"),
            new TestCaseData("BTC-USD", "BTCUSD"),
            new TestCaseData("BTC-USDC", "BTCUSDC"),
            new TestCaseData("ATOM-USD", "ATOMUSD")
        };

        private static TestCaseData[] CryptoSymbols => new[]
        {
            new TestCaseData(Symbol.Create("ETHUSD", SecurityType.Crypto, Market.GDAX), "ETH-USD"),
            new TestCaseData(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX), "BTC-USD"),
            new TestCaseData(Symbol.Create("ETHBTC", SecurityType.Crypto, Market.GDAX), "ETH-BTC"),
            new TestCaseData(Symbol.Create("BTCUSDC", SecurityType.Crypto, Market.GDAX), "BTC-USDC"),
            new TestCaseData(Symbol.Create("ATOMUSD", SecurityType.Crypto, Market.GDAX), "ATOM-USD")
        };

        private static TestCaseData[] CurrencyPairs => new[]
        {
            new TestCaseData(""),
            new TestCaseData("EURUSD"),
            new TestCaseData("GBP-USD"),
            new TestCaseData("USD-JPY")
        };

        private static TestCaseData[] UnknownSymbols => new[]
        {
            new TestCaseData("AAA-BBB", SecurityType.Crypto, Market.GDAX),
            new TestCaseData("USD-BTC", SecurityType.Crypto, Market.GDAX),
            new TestCaseData("EUR-USD", SecurityType.Crypto, Market.GDAX),
            new TestCaseData("GBP-USD", SecurityType.Crypto, Market.GDAX),
            new TestCaseData("USD-JPY", SecurityType.Crypto, Market.GDAX),
            new TestCaseData("BTC-ETH", SecurityType.Crypto, Market.GDAX),
            new TestCaseData("USDC-BTC", SecurityType.Crypto, Market.GDAX)
        };

        private static TestCaseData[] UnknownSecurityType => new[]
        {
            new TestCaseData("BTC-USD", SecurityType.Forex, Market.GDAX)
        };

        private static TestCaseData[] UnknownMarket => new[]
        {
            new TestCaseData("ETH-USD", SecurityType.Crypto, Market.Bitfinex)
        };

        #endregion

        [TestCaseSource(nameof(BrokerageSymbols))]
        public void ReturnsCryptoSecurityType(string brokerageSymbol, string leanSymbol)
        {
            var mapper = new GDAXSymbolMapper();

            Assert.AreEqual(SecurityType.Crypto, mapper.GetBrokerageSecurityType(brokerageSymbol));
            var symbol = mapper.GetLeanSymbol(brokerageSymbol);
            Assert.AreEqual(leanSymbol, symbol.Value);
            Assert.AreEqual(SecurityType.Crypto, mapper.GetLeanSecurityType(symbol.Value));
            Assert.AreEqual(Market.GDAX, symbol.ID.Market);
        }

        [TestCaseSource(nameof(BrokerageSymbols))]
        public void ReturnsCorrectLeanSymbol(string brokerageSymbol, string leanSymbol)
        {
            var mapper = new GDAXSymbolMapper();

            var symbol = mapper.GetLeanSymbol(brokerageSymbol);
            Assert.AreEqual(leanSymbol, symbol.Value);
            Assert.AreEqual(SecurityType.Crypto, symbol.ID.SecurityType);
            Assert.AreEqual(Market.GDAX, symbol.ID.Market);
        }

        [TestCaseSource(nameof(CryptoSymbols))]
        public void ReturnsCorrectBrokerageSymbol(Symbol symbol, string brokerageSymbol)
        {
            var mapper = new GDAXSymbolMapper();

            Assert.AreEqual(brokerageSymbol, mapper.GetBrokerageSymbol(symbol));
        }

        [TestCaseSource(nameof(CurrencyPairs))]
        public void ThrowsOnCurrencyPairs(string brokerageSymbol)
        {
            var mapper = new GDAXSymbolMapper();

            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSecurityType(brokerageSymbol));
        }

        [Test]
        public void ThrowsOnNullOrEmptySymbols()
        {
            var mapper = new GDAXSymbolMapper();

            string ticker = null;
            Assert.IsFalse(mapper.IsKnownBrokerageSymbol(ticker));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(ticker, SecurityType.Crypto, Market.GDAX));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSecurityType(ticker));

            ticker = "";
            Assert.IsFalse(mapper.IsKnownBrokerageSymbol(ticker));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(ticker, SecurityType.Crypto, Market.GDAX));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSecurityType(ticker));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(ticker, SecurityType.Crypto, Market.GDAX)));
        }

        [TestCaseSource(nameof(UnknownSymbols))]
        public void ThrowsOnUnknownSymbols(string brokerageSymbol, SecurityType type, string market)
        {
            var mapper = new GDAXSymbolMapper();

            Assert.IsFalse(mapper.IsKnownBrokerageSymbol(brokerageSymbol));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(brokerageSymbol, type, market));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(brokerageSymbol.Replace("-", ""), type, market)));
        }

        [TestCaseSource(nameof(UnknownSecurityType))]
        public void ThrowsOnUnknownSecurityType(string brokerageSymbol, SecurityType type, string market)
        {
            var mapper = new GDAXSymbolMapper();

            Assert.IsTrue(mapper.IsKnownBrokerageSymbol(brokerageSymbol));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(brokerageSymbol, type, market));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(brokerageSymbol.Replace("-", ""), type, market)));
        }

        [TestCaseSource(nameof(UnknownMarket))]
        public void ThrowsOnUnknownMarket(string brokerageSymbol, SecurityType type, string market)
        {
            var mapper = new GDAXSymbolMapper();

            Assert.IsTrue(mapper.IsKnownBrokerageSymbol(brokerageSymbol));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(brokerageSymbol, type, market));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(brokerageSymbol.Replace("-", ""), type, market)));
        }
    }
}
