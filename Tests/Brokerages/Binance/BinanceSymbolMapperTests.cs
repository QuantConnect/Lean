using NUnit.Framework;
using QuantConnect.Brokerages.Binance;
using QuantConnect.Brokerages.Binance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Brokerages.Binance
{
    [TestFixture]
    public class BinanceSymbolMapperTests
    {
        private BinanceSymbolMapper mapper;

        [SetUp]
        public void Setup()
        {
            mapper = new BinanceSymbolMapper();
        }

        #region data
        public TestCaseData[] CryptoPairs => new[]
        {
            new TestCaseData("ethusdt"),
            new TestCaseData("btcusdt"),
            new TestCaseData("ethbtc")
        };

        public TestCaseData[] RawCryptoSymbols => new[]
        {
            new TestCaseData("ETHUSDT", SecurityType.Crypto, Market.Binance),
            new TestCaseData("ETHBTC", SecurityType.Crypto, Market.Binance),
            new TestCaseData("BTCUSDT", SecurityType.Crypto, Market.Binance),
        };

        public TestCaseData[] CryptoSymbols => new[]
        {
            new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance)),
            new TestCaseData(Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Binance)),
            new TestCaseData(Symbol.Create("ETHBTC", SecurityType.Crypto, Market.Binance))
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
            new TestCaseData("eth-usd", SecurityType.Crypto, Market.Binance),
            new TestCaseData("BTC/USD", SecurityType.Crypto, Market.Binance),
            new TestCaseData("eurusd", SecurityType.Crypto, Market.GDAX),
            new TestCaseData("gbpusd", SecurityType.Forex, Market.Binance),
            new TestCaseData("usdjpy", SecurityType.Forex, Market.FXCM),
            new TestCaseData("btceth", SecurityType.Crypto, Market.Binance)
        };

        public TestCaseData[] UnknownSecurityType => new[]
        {
            new TestCaseData("BTCUSDT", SecurityType.Forex, Market.Binance),
        };

        public TestCaseData[] UnknownMarket => new[]
        {
            new TestCaseData("ethusdt", SecurityType.Crypto, Market.GDAX)
        };

        #endregion

        [Test]
        [TestCaseSource("CryptoPairs")]
        public void ReturnsCryptoSecurityType(string pair)
        {
            Assert.AreEqual(SecurityType.Crypto, mapper.GetBrokerageSecurityType(pair));
            var symbol = mapper.GetLeanSymbol(pair);
            Assert.AreEqual(SecurityType.Crypto, mapper.GetLeanSecurityType(symbol.Value));
        }

        [Test]
        [TestCaseSource("CryptoPairs")]
        public void ReturnsCorrectLeanSymbol(string pair)
        {
            var symbol = mapper.GetLeanSymbol(pair);
            Assert.AreEqual(pair.LazyToUpper(), symbol.Value);
            Assert.AreEqual(SecurityType.Crypto, symbol.ID.SecurityType);
            Assert.AreEqual(Market.Binance, symbol.ID.Market);
        }

        [Test]
        [TestCaseSource("RawCryptoSymbols")]
        public void ReturnsCorrectLeanSymbol(string pair, SecurityType type, string market)
        {
            var symbol = mapper.GetLeanSymbol(pair);
            Assert.AreEqual(pair.LazyToUpper(), symbol.Value);
            Assert.AreEqual(SecurityType.Crypto, symbol.ID.SecurityType);
            Assert.AreEqual(Market.Binance, symbol.ID.Market);
        }

        [Test]
        [TestCaseSource("CryptoSymbols")]
        public void ReturnsCorrectBrokerageSymbol(Symbol symbol)
        {
            Assert.AreEqual(symbol.Value.LazyToUpper(), mapper.GetBrokerageSymbol(symbol));
        }

        [Test]
        [TestCaseSource("CurrencyPairs")]
        public void ThrowsOnCurrencyPairs(string pair)
        {
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSecurityType(pair));
        }

        [Test]
        public void ThrowsOnNullOrEmptySymbols()
        {
            string ticker = null;
            Assert.IsFalse(mapper.IsKnownBrokerageSymbol(ticker));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(ticker, SecurityType.Crypto, Market.Binance));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSecurityType(ticker));

            ticker = "";
            Assert.IsFalse(mapper.IsKnownBrokerageSymbol(ticker));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(ticker, SecurityType.Crypto, Market.Binance));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSecurityType(ticker));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(ticker, SecurityType.Crypto, Market.Binance)));
        }

        [Test]
        [TestCaseSource("UnknownSymbols")]
        public void ThrowsOnUnknownSymbols(string pair, SecurityType type, string market)
        {
            Assert.IsFalse(mapper.IsKnownBrokerageSymbol(pair));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(pair, type, market));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(pair, type, market)));
        }

        [Test]
        [TestCaseSource("UnknownSecurityType")]
        public void ThrowsOnUnknownSecurityType(string pair, SecurityType type, string market)
        {
            Assert.IsTrue(mapper.IsKnownBrokerageSymbol(pair));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(pair, type, market));
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(Symbol.Create(pair, type, market)));
        }

        [Test]
        [TestCaseSource("UnknownMarket")]
        public void ThrowsOnUnknownMarket(string pair, SecurityType type, string market)
        {
            Assert.IsTrue(mapper.IsKnownBrokerageSymbol(pair));
            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(pair, type, market));
            Assert.AreEqual(pair.LazyToUpper(), mapper.GetBrokerageSymbol(Symbol.Create(pair, type, market)));
        }
    }
}
