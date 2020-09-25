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
using QuantConnect.Brokerages.Binance;
using System;

namespace QuantConnect.Tests.Brokerages.Binance
{
    [TestFixture]
    public class BinanceSymbolMapperTests
    {
        private BinanceSymbolMapper _mapper;

        [SetUp]
        public void Setup()
        {
            _mapper = new BinanceSymbolMapper();
        }

        [Test]
        [TestCaseSource(nameof(CryptoPairs))]
        public void ReturnsCryptoSecurityType(string pair)
        {
            Assert.AreEqual(SecurityType.Crypto, _mapper.GetBrokerageSecurityType(pair));
            var symbol = _mapper.GetLeanSymbol(pair);
            Assert.AreEqual(SecurityType.Crypto, _mapper.GetLeanSecurityType(symbol.Value));
        }

        [Test]
        [TestCaseSource(nameof(CryptoPairs))]
        public void ReturnsCorrectLeanSymbol(string pair)
        {
            var symbol = _mapper.GetLeanSymbol(pair);
            Assert.AreEqual(pair.LazyToUpper(), symbol.Value);
            Assert.AreEqual(SecurityType.Crypto, symbol.ID.SecurityType);
            Assert.AreEqual(Market.Binance, symbol.ID.Market);
        }

        [Test]
        [TestCaseSource(nameof(RawCryptoSymbols))]
        public void ReturnsCorrectLeanSymbol(string pair, SecurityType type, string market)
        {
            var symbol = _mapper.GetLeanSymbol(pair);
            Assert.AreEqual(pair.LazyToUpper(), symbol.Value);
            Assert.AreEqual(SecurityType.Crypto, symbol.ID.SecurityType);
            Assert.AreEqual(Market.Binance, symbol.ID.Market);
        }

        [Test]
        [TestCaseSource(nameof(CryptoSymbols))]
        public void ReturnsCorrectBrokerageSymbol(Symbol symbol)
        {
            Assert.AreEqual(symbol.Value.LazyToUpper(), _mapper.GetBrokerageSymbol(symbol));
        }

        [Test]
        [TestCaseSource(nameof(CurrencyPairs))]
        public void ThrowsOnCurrencyPairs(string pair)
        {
            Assert.Throws<ArgumentException>(() => _mapper.GetBrokerageSecurityType(pair));
        }

        [Test]
        public void ThrowsOnNullOrEmptySymbols()
        {
            string ticker = null;
            Assert.IsFalse(_mapper.IsKnownBrokerageSymbol(ticker));
            Assert.Throws<ArgumentException>(() => _mapper.GetLeanSymbol(ticker, SecurityType.Crypto, Market.Binance));
            Assert.Throws<ArgumentException>(() => _mapper.GetBrokerageSecurityType(ticker));

            ticker = "";
            Assert.IsFalse(_mapper.IsKnownBrokerageSymbol(ticker));
            Assert.Throws<ArgumentException>(() => _mapper.GetLeanSymbol(ticker, SecurityType.Crypto, Market.Binance));
            Assert.Throws<ArgumentException>(() => _mapper.GetBrokerageSecurityType(ticker));
            Assert.Throws<ArgumentException>(() => _mapper.GetBrokerageSymbol(Symbol.Create(ticker, SecurityType.Crypto, Market.Binance)));
        }

        [Test]
        [TestCaseSource(nameof(UnknownSymbols))]
        public void ThrowsOnUnknownSymbols(string pair, SecurityType type, string market)
        {
            Assert.IsFalse(_mapper.IsKnownBrokerageSymbol(pair));
            Assert.Throws<ArgumentException>(() => _mapper.GetLeanSymbol(pair, type, market));
            Assert.Throws<ArgumentException>(() => _mapper.GetBrokerageSymbol(Symbol.Create(pair, type, market)));
        }

        [Test]
        [TestCaseSource(nameof(UnknownSecurityType))]
        public void ThrowsOnUnknownSecurityType(string pair, SecurityType type, string market)
        {
            Assert.IsTrue(_mapper.IsKnownBrokerageSymbol(pair));
            Assert.Throws<ArgumentException>(() => _mapper.GetLeanSymbol(pair, type, market));
            Assert.Throws<ArgumentException>(() => _mapper.GetBrokerageSymbol(Symbol.Create(pair, type, market)));
        }

        [Test]
        [TestCaseSource(nameof(UnknownMarket))]
        public void ThrowsOnUnknownMarket(string pair, SecurityType type, string market)
        {
            Assert.IsTrue(_mapper.IsKnownBrokerageSymbol(pair));
            Assert.Throws<ArgumentException>(() => _mapper.GetLeanSymbol(pair, type, market));
            Assert.AreEqual(pair.LazyToUpper(), _mapper.GetBrokerageSymbol(Symbol.Create(pair, type, market)));
        }

        #region Data

        private static TestCaseData[] CryptoPairs => new[]
        {
            new TestCaseData("ETHUSDT"),
            new TestCaseData("BTCUSDT"),
            new TestCaseData("ETHBTC")
        };

        private static TestCaseData[] RawCryptoSymbols => new[]
        {
            new TestCaseData("ETHUSDT", SecurityType.Crypto, Market.Binance),
            new TestCaseData("ETHBTC", SecurityType.Crypto, Market.Binance),
            new TestCaseData("BTCUSDT", SecurityType.Crypto, Market.Binance),
        };

        private static TestCaseData[] CryptoSymbols => new[]
        {
            new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance)),
            new TestCaseData(Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Binance)),
            new TestCaseData(Symbol.Create("ETHBTC", SecurityType.Crypto, Market.Binance))
        };

        private static TestCaseData[] CurrencyPairs => new[]
        {
            new TestCaseData(""),
            new TestCaseData("EURUSD"),
            new TestCaseData("GBPUSD"),
            new TestCaseData("USDJPY")
        };

        private static TestCaseData[] UnknownSymbols => new[]
        {
            new TestCaseData("eth-usd", SecurityType.Crypto, Market.Binance),
            new TestCaseData("BTC/USD", SecurityType.Crypto, Market.Binance),
            new TestCaseData("eurusd", SecurityType.Crypto, Market.GDAX),
            new TestCaseData("gbpusd", SecurityType.Forex, Market.Binance),
            new TestCaseData("usdjpy", SecurityType.Forex, Market.FXCM),
            new TestCaseData("btceth", SecurityType.Crypto, Market.Binance)
        };

        private static TestCaseData[] UnknownSecurityType => new[]
        {
            new TestCaseData("BTCUSDT", SecurityType.Forex, Market.Binance),
        };

        private static TestCaseData[] UnknownMarket => new[]
        {
            new TestCaseData("ETHUSDT", SecurityType.Crypto, Market.GDAX)
        };

        #endregion
    }
}
