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
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture]
    public class SymbolPropertiesDatabaseSymbolMapperTests
    {
        [TestCaseSource(nameof(BrokerageSymbols))]
        public void ReturnsCryptoSecurityType(string market, string brokerageSymbol, string leanSymbol)
        {
            var mapper = new SymbolPropertiesDatabaseSymbolMapper(market);

            var symbol = mapper.GetLeanSymbol(brokerageSymbol, SecurityType.Crypto, market);
            Assert.AreEqual(leanSymbol, symbol.Value);
            Assert.AreEqual(market, symbol.ID.Market);
        }

        [TestCaseSource(nameof(BrokerageSymbols))]
        public void ReturnsCorrectLeanSymbol(string market, string brokerageSymbol, string leanSymbol)
        {
            var mapper = new SymbolPropertiesDatabaseSymbolMapper(market);

            var symbol = mapper.GetLeanSymbol(brokerageSymbol, SecurityType.Crypto, market);
            Assert.AreEqual(leanSymbol, symbol.Value);
            Assert.AreEqual(SecurityType.Crypto, symbol.ID.SecurityType);
            Assert.AreEqual(market, symbol.ID.Market);
        }

        [TestCaseSource(nameof(CryptoSymbols))]
        public void ReturnsCorrectBrokerageSymbol(Symbol symbol, string brokerageSymbol)
        {
            var mapper = new SymbolPropertiesDatabaseSymbolMapper(symbol.ID.Market);

            Assert.AreEqual(brokerageSymbol, mapper.GetBrokerageSymbol(symbol));
        }

        [TestCaseSource(nameof(CurrencyPairs))]
        public void ThrowsOnCurrencyPairs(string market, string brokerageSymbol)
        {
            var mapper = new SymbolPropertiesDatabaseSymbolMapper(market);

            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSecurityType(brokerageSymbol));
        }

        [TestCase(Market.Coinbase)]
        [TestCase(Market.Bitfinex)]
        [TestCase(Market.Binance)]
        public void ThrowsOnNullOrEmptySymbols(string market)
        {
            var mapper = new SymbolPropertiesDatabaseSymbolMapper(market);

            string ticker = null;
            Assert.IsFalse(mapper.IsKnownBrokerageSymbol(ticker));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(ticker, SecurityType.Crypto, market));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSecurityType(ticker));

            ticker = "";
            Assert.IsFalse(mapper.IsKnownBrokerageSymbol(ticker));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(ticker, SecurityType.Crypto, market));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSecurityType(ticker));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(ticker, SecurityType.Crypto, market)));
        }

        [TestCaseSource(nameof(UnknownSymbols))]
        public void ThrowsOnUnknownSymbols(string brokerageSymbol, SecurityType type, string market)
        {
            var mapper = new SymbolPropertiesDatabaseSymbolMapper(market);

            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(brokerageSymbol, type, market));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(brokerageSymbol.Replace("-", ""), type, market)));
        }

        [TestCaseSource(nameof(UnknownSecurityType))]
        public void ThrowsOnUnknownSecurityType(string brokerageSymbol, SecurityType type, string market)
        {
            var mapper = new SymbolPropertiesDatabaseSymbolMapper(market);

            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(brokerageSymbol, type, market));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(brokerageSymbol.Replace("-", ""), type, market)));
        }

        [TestCaseSource(nameof(UnknownMarket))]
        public void ThrowsOnUnknownMarket(string brokerageSymbol, SecurityType type, string market)
        {
            var mapper = new SymbolPropertiesDatabaseSymbolMapper(market);

            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(brokerageSymbol, type, market));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(brokerageSymbol.Replace("-", ""), type, market)));
        }

        private static TestCaseData[] BrokerageSymbols => new[]
        {
            new TestCaseData(Market.Coinbase, "ETH-USD", "ETHUSD"),
            new TestCaseData(Market.Coinbase, "ETH-BTC", "ETHBTC"),
            new TestCaseData(Market.Coinbase, "BTC-USD", "BTCUSD"),
            new TestCaseData(Market.Coinbase, "BTC-USDC", "BTCUSDC"),
            new TestCaseData(Market.Coinbase, "ATOM-USD", "ATOMUSD"),

            new TestCaseData(Market.Bitfinex, "tBTCUSD", "BTCUSD"),
            new TestCaseData(Market.Bitfinex, "tBTCUST", "BTCUSDT"),
            new TestCaseData(Market.Bitfinex, "tETHUSD", "ETHUSD"),
            new TestCaseData(Market.Bitfinex, "tADAUST", "ADAUSDT"),
            new TestCaseData(Market.Bitfinex, "tCOMP:USD", "COMPUSD"),
            new TestCaseData(Market.Bitfinex, "tCOMP:UST", "COMPUSDT"),

            new TestCaseData(Market.Binance, "ETHUSDT", "ETHUSDT"),
            new TestCaseData(Market.Binance, "ETHBTC", "ETHBTC"),
            new TestCaseData(Market.Binance, "BTCUSDT", "BTCUSDT"),
            new TestCaseData(Market.Binance, "ATOMTUSD", "ATOMTUSD"),
            new TestCaseData(Market.Binance, "ATOMUSDC", "ATOMUSDC"),
            new TestCaseData(Market.Binance, "ATOMUSDT", "ATOMUSDT")
        };

        private static TestCaseData[] CryptoSymbols => new[]
        {
            new TestCaseData(Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Coinbase), "ETH-USD"),
            new TestCaseData(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Coinbase), "BTC-USD"),
            new TestCaseData(Symbol.Create("ETHBTC", SecurityType.Crypto, Market.Coinbase), "ETH-BTC"),
            new TestCaseData(Symbol.Create("BTCUSDC", SecurityType.Crypto, Market.Coinbase), "BTC-USDC"),
            new TestCaseData(Symbol.Create("ATOMUSD", SecurityType.Crypto, Market.Coinbase), "ATOM-USD"),

            new TestCaseData(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex), "tBTCUSD"),
            new TestCaseData(Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Bitfinex), "tBTCUST"),
            new TestCaseData(Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Bitfinex), "tETHUSD"),
            new TestCaseData(Symbol.Create("ADAUSDT", SecurityType.Crypto, Market.Bitfinex), "tADAUST"),
            new TestCaseData(Symbol.Create("COMPUSD", SecurityType.Crypto, Market.Bitfinex), "tCOMP:USD"),
            new TestCaseData(Symbol.Create("COMPUSDT", SecurityType.Crypto, Market.Bitfinex), "tCOMP:UST"),

            new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), "ETHUSDT"),
            new TestCaseData(Symbol.Create("ETHBTC", SecurityType.Crypto, Market.Binance), "ETHBTC"),
            new TestCaseData(Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Binance), "BTCUSDT"),
            new TestCaseData(Symbol.Create("ATOMTUSD", SecurityType.Crypto, Market.Binance), "ATOMTUSD"),
            new TestCaseData(Symbol.Create("ATOMUSDC", SecurityType.Crypto, Market.Binance), "ATOMUSDC"),
            new TestCaseData(Symbol.Create("ATOMUSDT", SecurityType.Crypto, Market.Binance), "ATOMUSDT")
        };

        private static TestCaseData[] CurrencyPairs => new[]
        {
            new TestCaseData(Market.Coinbase, ""),
            new TestCaseData(Market.Coinbase, "EURUSD"),
            new TestCaseData(Market.Coinbase, "GBP-USD"),
            new TestCaseData(Market.Coinbase, "USD-JPY"),

            new TestCaseData(Market.Bitfinex, ""),
            new TestCaseData(Market.Bitfinex, "EURUSD"),
            new TestCaseData(Market.Bitfinex, "GBP-USD"),
            new TestCaseData(Market.Bitfinex, "USD-JPY"),

            new TestCaseData(Market.Binance, ""),
            new TestCaseData(Market.Binance, "EURUSD"),
            new TestCaseData(Market.Binance, "GBPUSD"),
            new TestCaseData(Market.Binance, "USDJPY")
        };

        private static TestCaseData[] UnknownSymbols => new[]
        {
            new TestCaseData("AAA-BBB", SecurityType.Crypto, Market.Coinbase),
            new TestCaseData("USD-BTC", SecurityType.Crypto, Market.Coinbase),
            new TestCaseData("EUR-USD", SecurityType.Crypto, Market.Coinbase),
            new TestCaseData("GBP-USD", SecurityType.Crypto, Market.Coinbase),
            new TestCaseData("USD-JPY", SecurityType.Crypto, Market.Coinbase),
            new TestCaseData("BTC-ETH", SecurityType.Crypto, Market.Coinbase),
            new TestCaseData("USDC-BTC", SecurityType.Crypto, Market.Coinbase),

            new TestCaseData("USD-BTC", SecurityType.Crypto, Market.Bitfinex),
            new TestCaseData("EUR-USD", SecurityType.Crypto, Market.Bitfinex),
            new TestCaseData("GBP-USD", SecurityType.Crypto, Market.Bitfinex),
            new TestCaseData("USD-JPY", SecurityType.Crypto, Market.Bitfinex),
            new TestCaseData("BTC-ETH", SecurityType.Crypto, Market.Bitfinex),
            new TestCaseData("USDC-BTC", SecurityType.Crypto, Market.Bitfinex),

            new TestCaseData("AAABBB", SecurityType.Crypto, Market.Binance),
            new TestCaseData("USDBTC", SecurityType.Crypto, Market.Binance),
            new TestCaseData("EURUSD", SecurityType.Crypto, Market.Binance),
            new TestCaseData("GBPUSD", SecurityType.Crypto, Market.Binance),
            new TestCaseData("USDJPY", SecurityType.Crypto, Market.Binance),
            new TestCaseData("BTCETH", SecurityType.Crypto, Market.Binance),
            new TestCaseData("BTCUSD", SecurityType.Crypto, Market.Binance)
        };

        private static TestCaseData[] UnknownSecurityType => new[]
        {
            new TestCaseData("BTC-USD", SecurityType.Forex, Market.Coinbase)
        };

        private static TestCaseData[] UnknownMarket => new[]
        {
            new TestCaseData("ETH-USD", SecurityType.Crypto, Market.USA)
        };
    }
}
