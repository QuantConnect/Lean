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
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class CurrencyPairUtilTests
    {
        [TestCaseSource(nameof(decomposeSuccessCases))]
        public void DecomposeDecomposesAllCurrencyPairTypes(
            Symbol symbol,
            string expectedBaseCurrency,
            string expectedQuoteCurrency
        )
        {
            string actualBaseCurrency;
            string actualQuoteCurrency;

            CurrencyPairUtil.DecomposeCurrencyPair(
                symbol,
                out actualBaseCurrency,
                out actualQuoteCurrency
            );

            Assert.AreEqual(expectedBaseCurrency, actualBaseCurrency);
            Assert.AreEqual(expectedQuoteCurrency, actualQuoteCurrency);
        }

        [TestCaseSource(nameof(decomposeThrowCases))]
        public void DecomposeThrowsOnNonCurrencyPair(Symbol symbol)
        {
            string baseCurrency,
                quoteCurrency;

            Assert.Throws<ArgumentException>(
                () =>
                    CurrencyPairUtil.DecomposeCurrencyPair(
                        symbol,
                        out baseCurrency,
                        out quoteCurrency
                    )
            );
        }

        [TestCaseSource(nameof(isDecomposableCases))]
        public void IsDecomposableWorksCorrectly(Symbol symbol, bool expectedResult)
        {
            Assert.AreEqual(expectedResult, CurrencyPairUtil.IsDecomposable(symbol));
        }

        [Test]
        public void CurrencyPairDualForex()
        {
            var currencyPair = Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM);

            Assert.AreEqual("USD", currencyPair.CurrencyPairDual("EUR"));
            Assert.AreEqual("EUR", currencyPair.CurrencyPairDual("USD"));
            Assert.AreEqual("USD", CurrencyPairUtil.CurrencyPairDual("EUR", "USD", "EUR"));
            Assert.AreEqual("EUR", CurrencyPairUtil.CurrencyPairDual("EUR", "USD", "USD"));
        }

        [Test]
        public void CurrencyPairDualCfd()
        {
            var currencyPair = Symbol.Create("XAGUSD", SecurityType.Cfd, Market.Oanda);

            Assert.AreEqual("XAG", currencyPair.CurrencyPairDual("USD"));
            Assert.AreEqual("USD", currencyPair.CurrencyPairDual("XAG"));
            Assert.AreEqual("XAG", CurrencyPairUtil.CurrencyPairDual("XAG", "USD", "USD"));
            Assert.AreEqual("USD", CurrencyPairUtil.CurrencyPairDual("XAG", "USD", "XAG"));
        }

        [Test]
        public void CurrencyPairDualCrypto()
        {
            var currencyPair = Symbol.Create("ETHBTC", SecurityType.Crypto, Market.Bitfinex);

            Assert.AreEqual("BTC", currencyPair.CurrencyPairDual("ETH"));
            Assert.AreEqual("ETH", currencyPair.CurrencyPairDual("BTC"));
            Assert.AreEqual("BTC", CurrencyPairUtil.CurrencyPairDual("ETH", "BTC", "ETH"));
            Assert.AreEqual("ETH", CurrencyPairUtil.CurrencyPairDual("ETH", "BTC", "BTC"));
        }

        [Test]
        public void CurrencyPairDualReturnsNullOnWrongKnownSymbol()
        {
            var currencyPair = Symbol.Create("ETHBTC", SecurityType.Crypto, Market.Bitfinex);

            Assert.AreEqual(null, currencyPair.CurrencyPairDual("ZRX"));
            Assert.AreEqual(null, CurrencyPairUtil.CurrencyPairDual("ETH", "BTC", "ZRX"));
        }

        [Test]
        public void ComparePairWorksCorrectly()
        {
            var ethusd = Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Bitfinex);
            var eurusd = Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM);

            Assert.AreEqual(CurrencyPairUtil.Match.ExactMatch, ethusd.ComparePair("ETH", "USD"));
            Assert.AreEqual(CurrencyPairUtil.Match.InverseMatch, eurusd.ComparePair("USD", "EUR"));
            Assert.AreEqual(CurrencyPairUtil.Match.NoMatch, ethusd.ComparePair("BTC", "USD"));
        }

        [Test]
        public void ComparePairWorksCorrectlyStableCoinsExactMatch()
        {
            var btcusdt = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Binance);
            var btcxchf = Symbol.Create("BTCXCHF", SecurityType.Crypto, Market.Bitfinex);

            Assert.AreEqual(CurrencyPairUtil.Match.ExactMatch, btcusdt.ComparePair("BTC", "USD"));
            Assert.AreEqual(CurrencyPairUtil.Match.ExactMatch, btcxchf.ComparePair("BTC", "CHF"));
        }

        [Test]
        public void ComparePairWorksCorrectlyStableCoinsInverseMatch()
        {
            var btcusdt = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Binance);
            var btcxchf = Symbol.Create("BTCXCHF", SecurityType.Crypto, Market.Bitfinex);

            Assert.AreEqual(CurrencyPairUtil.Match.InverseMatch, btcusdt.ComparePair("USD", "BTC"));
            Assert.AreEqual(CurrencyPairUtil.Match.InverseMatch, btcxchf.ComparePair("CHF", "BTC"));
        }

        [Test]
        public void ComparePairWorksCorrectlyStableCoinsNoMatch()
        {
            var btceur = Symbol.Create("BTCEUR", SecurityType.Crypto, Market.Binance);
            var btcidr = Symbol.Create("BTCIDR", SecurityType.Crypto, Market.Binance);

            Assert.AreEqual(CurrencyPairUtil.Match.NoMatch, btceur.ComparePair("BTC", "USD"));
            Assert.AreEqual(CurrencyPairUtil.Match.NoMatch, btcidr.ComparePair("BTC", "EUR"));
        }

        [Test]
        public void ComparePairWorksCorrectlyWithDoubleStableCoinMatch()
        {
            var idrusd = Symbol.Create("IDRUSD", SecurityType.Crypto, Market.Binance);

            Assert.AreEqual(CurrencyPairUtil.Match.ExactMatch, idrusd.ComparePair("IDRT", "USDT"));
        }

        /// <summary>
        /// DecomposeCurrencyPair test cases with successful results:
        /// symbol, expectedBaseCurrency, expectedQuoteCurrency
        /// </summary>
        private static object[][] decomposeSuccessCases =
        {
            new object[] { Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM), "EUR", "USD" },
            new object[]
            {
                Symbol.Create("NZDSGD", SecurityType.Forex, Market.Oanda),
                "NZD",
                "SGD"
            },
            new object[] { Symbol.Create("XAGUSD", SecurityType.Cfd, Market.FXCM), "XAG", "USD" },
            new object[]
            {
                Symbol.Create("US30USD", SecurityType.Cfd, Market.Oanda),
                "US30",
                "USD"
            },
            new object[]
            {
                Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex),
                "BTC",
                "USD"
            },
            new object[]
            {
                Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Binance),
                "BTC",
                "USDT"
            }
        };

        /// <summary>
        /// DecomposeCurrencyPair test cases where method should throw:
        /// symbol
        /// </summary>
        private static object[][] decomposeThrowCases =
        {
            new object[] { null },
            new object[] { Symbol.Empty },
            new object[] { Symbols.SPY },
            new object[] { Symbols.SPY_C_192_Feb19_2016 },
            new object[] { Symbols.Fut_SPY_Feb19_2016 }
        };

        /// <summary>
        /// IsDecomposable test cases:
        /// symbol, expectedResult
        /// </summary>
        private static object[][] isDecomposableCases =
        {
            // Forex, CFD and crypto are usually decomposable
            new object[] { Symbols.EURUSD, true },
            new object[] { Symbols.XAGUSD, true },
            new object[] { Symbols.BTCUSD, true },
            // CFD, but ticker doesn't end with quote currency, so no way to extract base currency
            new object[] { Symbol.Create("AU200AUD", SecurityType.Cfd, Market.FXCM), false },
            // Obviously not decomposable
            new object[] { null, false },
            new object[] { Symbol.Empty, false },
            // Other security types, also not decomposable
            new object[] { Symbols.SPY, false },
            new object[] { Symbols.SPY_C_192_Feb19_2016, false },
            new object[] { Symbols.Fut_SPY_Feb19_2016, false }
        };
    }
}
