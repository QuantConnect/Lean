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
            string basec, quotec;

            Assert.Throws<ArgumentException>(() => CurrencyPairUtil.DecomposeCurrencyPair(symbol, out basec, out quotec),
                                             "Currency pair must not be null");
        }

        [Test]
        public void CurrencyPairDualForex()
        {
            var currencyPair = Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM);

            Assert.AreEqual(CurrencyPairUtil.CurrencyPairDual(currencyPair, "EUR"), "USD");
            Assert.AreEqual(CurrencyPairUtil.CurrencyPairDual(currencyPair, "USD"), "EUR");
        }

        [Test]
        public void CurrencyPairDualCrypto()
        {
            var currencyPair = Symbol.Create("ETHBTC", SecurityType.Crypto, Market.Bitfinex);

            Assert.AreEqual(CurrencyPairUtil.CurrencyPairDual(currencyPair, "ETH"), "BTC");
            Assert.AreEqual(CurrencyPairUtil.CurrencyPairDual(currencyPair, "BTC"), "ETH");
        }

        [Test]
        public void CurrencyPairDualThrowsOnWrongKnownSymbol()
        {
            var currencyPair = Symbol.Create("ETHBTC", SecurityType.Crypto, Market.Bitfinex);

            Assert.Throws<ArgumentException>(() => CurrencyPairUtil.CurrencyPairDual(currencyPair, "ZRX"),
                                             "The knownSymbol ZRX isn't contained in currencyPair ETHBTC.");
        }

        [Test]
        public void ComparePairWorksCorrectly()
        {
            var ethusd = Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Bitfinex);
            var btcusd = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex);

            var eurusd = Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM);
            var usdeur = Symbol.Create("USDEUR", SecurityType.Forex, Market.FXCM);

            Assert.AreEqual(ethusd.ComparePair(ethusd), CurrencyPairUtil.Match.ExactMatch);
            Assert.AreEqual(ethusd.ComparePair("ETH", "USD"), CurrencyPairUtil.Match.ExactMatch);

            Assert.AreEqual(eurusd.ComparePair(usdeur), CurrencyPairUtil.Match.InverseMatch);
            Assert.AreEqual(eurusd.ComparePair("USD", "EUR"), CurrencyPairUtil.Match.InverseMatch);

            Assert.AreEqual(ethusd.ComparePair(btcusd), CurrencyPairUtil.Match.NoMatch);
            Assert.AreEqual(ethusd.ComparePair("BTC", "USD"), CurrencyPairUtil.Match.NoMatch);
        }

        [Test]
        public void PairContainsCodeWorksCorrectly()
        {
            var ethusd = Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Bitfinex);

            Assert.AreEqual(CurrencyPairUtil.PairContainsCode(ethusd, "ETH"), true);
            Assert.AreEqual(CurrencyPairUtil.PairContainsCode(ethusd, "USD"), true);
            Assert.AreEqual(CurrencyPairUtil.PairContainsCode(ethusd, "ZRX"), false);
            Assert.AreEqual(CurrencyPairUtil.PairContainsCode(ethusd, "BTC"), false);
        }

    }
}
