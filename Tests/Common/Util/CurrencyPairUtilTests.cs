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
        [Test]
        public void DecomposeThrowsOnNullSymbol()
        {
            Symbol symbol = null;
            string baseCurrency, quoteCurrency;

            Assert.Throws<ArgumentException>(
                () => CurrencyPairUtil.DecomposeCurrencyPair(symbol, out baseCurrency, out quoteCurrency));
        }

        [Test]
        public void CurrencyPairDualForex()
        {
            var currencyPair = Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM);

            Assert.AreEqual("USD", currencyPair.CurrencyPairDual("EUR"));
            Assert.AreEqual("EUR", currencyPair.CurrencyPairDual("USD"));
        }

        [Test]
        public void CurrencyPairDualCrypto()
        {
            var currencyPair = Symbol.Create("ETHBTC", SecurityType.Crypto, Market.Bitfinex);

            Assert.AreEqual("BTC", currencyPair.CurrencyPairDual("ETH"));
            Assert.AreEqual("ETH", currencyPair.CurrencyPairDual("BTC"));
        }

        [Test]
        public void CurrencyPairDualThrowsOnWrongKnownSymbol()
        {
            var currencyPair = Symbol.Create("ETHBTC", SecurityType.Crypto, Market.Bitfinex);

            Assert.Throws<ArgumentException>(() => currencyPair.CurrencyPairDual("ZRX"));
        }

        [Test]
        public void ComparePairWorksCorrectly()
        {
            var ethusd = Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Bitfinex);
            var btcusd = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex);

            var eurusd = Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM);
            var usdeur = Symbol.Create("USDEUR", SecurityType.Forex, Market.FXCM);

            Assert.AreEqual(CurrencyPairUtil.Match.ExactMatch, ethusd.ComparePair(ethusd));
            Assert.AreEqual(CurrencyPairUtil.Match.ExactMatch, ethusd.ComparePair("ETH", "USD"));

            Assert.AreEqual(CurrencyPairUtil.Match.InverseMatch, eurusd.ComparePair(usdeur));
            Assert.AreEqual(CurrencyPairUtil.Match.InverseMatch, eurusd.ComparePair("USD", "EUR"));

            Assert.AreEqual(CurrencyPairUtil.Match.NoMatch, ethusd.ComparePair(btcusd));
            Assert.AreEqual(CurrencyPairUtil.Match.NoMatch, ethusd.ComparePair("BTC", "USD"));
        }

        [Test]
        public void PairContainsCurrencyWorksCorrectly()
        {
            var ethusd = Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Bitfinex);

            Assert.IsTrue(ethusd.PairContainsCurrency("ETH"));
            Assert.IsTrue(ethusd.PairContainsCurrency("USD"));
            Assert.IsFalse(ethusd.PairContainsCurrency("ZRX"));
            Assert.IsFalse(ethusd.PairContainsCurrency("BTC"));
        }
    }
}
