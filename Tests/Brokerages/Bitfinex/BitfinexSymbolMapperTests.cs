using NUnit.Framework;
using QuantConnect.Brokerages.Bitfinex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture]
    public class BitfinexSymbolMapperTests
    {
        #region data
        public TestCaseData[] CryptoPairs => new[]
        {
            new TestCaseData("ethusd"),
            new TestCaseData("btcusd"),
            new TestCaseData("ethbtc")
        };

        public TestCaseData[] RawCryptoSymbols => new[]
        {
            new TestCaseData("ETHUSD", SecurityType.Crypto, Market.Bitfinex),
            new TestCaseData("ETHBTC", SecurityType.Crypto, Market.Bitfinex),
            new TestCaseData("BTCUSD", SecurityType.Crypto, Market.Bitfinex),
        };

        public TestCaseData[] CryptoSymbols => new[]
        {
            new TestCaseData(Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Bitfinex)),
            new TestCaseData(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex)),
            new TestCaseData(Symbol.Create("ETHBTC", SecurityType.Crypto, Market.Bitfinex))
        };

        public TestCaseData[] CurrencyPairs => new[]
        {
            new TestCaseData(""),
            new TestCaseData("eurusd"),
            new TestCaseData("gbpusd"),
            new TestCaseData("usdjpy")
        };

        public TestCaseData[] UnknownSymbols => new[]
        {
            new TestCaseData("eth-usd", SecurityType.Crypto, Market.Bitfinex),
            new TestCaseData("BTC/USD", SecurityType.Crypto, Market.Bitfinex),
            new TestCaseData("eurusd", SecurityType.Crypto, Market.GDAX),
            new TestCaseData("gbpusd", SecurityType.Forex, Market.Bitfinex),
            new TestCaseData("usdjpy", SecurityType.Forex, Market.FXCM),
            new TestCaseData("btceth", SecurityType.Crypto, Market.Bitfinex)
        };

        public TestCaseData[] UnknownSecurityType => new[]
        {
            new TestCaseData("BTCUSD", SecurityType.Forex, Market.Bitfinex),
        };

        public TestCaseData[] UnknownMarket => new[]
        {
            new TestCaseData("ethusd", SecurityType.Crypto, Market.GDAX)
        };

        #endregion

        [Test]
        [TestCaseSource("CryptoPairs")]
        public void ReturnsCryptoSecurityType(string pair)
        {
            var mapper = new BitfinexSymbolMapper();

            Assert.AreEqual(SecurityType.Crypto, mapper.GetBrokerageSecurityType(pair));
            var symbol = mapper.GetLeanSymbol(pair);
            Assert.AreEqual(SecurityType.Crypto, mapper.GetLeanSecurityType(symbol.Value));
        }

        [Test]
        [TestCaseSource("CryptoPairs")]
        public void ReturnsCorrectLeanSymbol(string pair)
        {
            var mapper = new BitfinexSymbolMapper();

            var symbol = mapper.GetLeanSymbol(pair);
            Assert.AreEqual(pair.ToUpper(), symbol.Value);
            Assert.AreEqual(SecurityType.Crypto, symbol.ID.SecurityType);
            Assert.AreEqual(Market.Bitfinex, symbol.ID.Market);
        }

        [Test]
        [TestCaseSource("RawCryptoSymbols")]
        public void ReturnsCorrectLeanSymbol(string pair, SecurityType type, string market)
        {
            var mapper = new BitfinexSymbolMapper();

            var symbol = mapper.GetLeanSymbol(pair);
            Assert.AreEqual(pair.ToUpper(), symbol.Value);
            Assert.AreEqual(SecurityType.Crypto, symbol.ID.SecurityType);
            Assert.AreEqual(Market.Bitfinex, symbol.ID.Market);
        }

        [Test]
        [TestCaseSource("CryptoSymbols")]
        public void ReturnsCorrectBrokerageSymbol(Symbol symbol)
        {
            var mapper = new BitfinexSymbolMapper();

            Assert.AreEqual(symbol.Value.ToUpper(), mapper.GetBrokerageSymbol(symbol));
        }

        [Test]
        [TestCaseSource("CurrencyPairs")]
        public void ThrowsOnCurrencyPairs(string pair)
        {
            var mapper = new BitfinexSymbolMapper();

            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSecurityType(pair));
        }

        [Test]
        public void ThrowsOnNullOrEmptySymbols()
        {
            var mapper = new BitfinexSymbolMapper();

            string ticker = null;
            Assert.IsFalse(mapper.IsKnownBrokerageSymbol(ticker));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(ticker, SecurityType.Crypto, Market.Bitfinex));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSecurityType(ticker));

            ticker = "";
            Assert.IsFalse(mapper.IsKnownBrokerageSymbol(ticker));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(ticker, SecurityType.Crypto, Market.Bitfinex));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSecurityType(ticker));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(ticker, SecurityType.Crypto, Market.Bitfinex)));
        }

        [Test]
        [TestCaseSource("UnknownSymbols")]
        public void ThrowsOnUnknownSymbols(string pair, SecurityType type, string market)
        {
            var mapper = new BitfinexSymbolMapper();

            Assert.IsFalse(mapper.IsKnownBrokerageSymbol(pair));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(pair, type, market));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(pair, type, market)));
        }

        [Test]
        [TestCaseSource("UnknownSecurityType")]
        public void ThrowsOnUnknownSecurityType(string pair, SecurityType type, string market)
        {
            var mapper = new BitfinexSymbolMapper();

            Assert.IsTrue(mapper.IsKnownBrokerageSymbol(pair));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(pair, type, market));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(pair, type, market)));
        }

        [Test]
        [TestCaseSource("UnknownMarket")]
        public void ThrowsOnUnknownMarket(string pair, SecurityType type, string market)
        {
            var mapper = new BitfinexSymbolMapper();

            Assert.IsTrue(mapper.IsKnownBrokerageSymbol(pair));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(pair, type, market));
            Assert.AreEqual(pair.ToUpper(), mapper.GetBrokerageSymbol(Symbol.Create(pair, type, market)));
        }
    }
}
