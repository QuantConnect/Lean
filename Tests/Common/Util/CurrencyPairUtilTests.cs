﻿/*
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
        /// <summary>
        /// String version of Currency.MaxCharactersPerCurrencyPair, multiplied by 2. It needs to be string so that it's compile time const.
        /// </summary>
        private const string MaxCharactersPerCurrencyPair = "12";

        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "Currency pairs must not be null, length minimum of 6 and maximum of " + MaxCharactersPerCurrencyPair + ".")]
        public void DecomposeThrowsOnSymbolTooShort()
        {
            string symbol = "12345";
            Assert.AreEqual(5, symbol.Length);
            string basec, quotec;
            CurrencyPairUtil.DecomposeCurrencyPair(symbol, out basec, out quotec);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "Currency pairs must not be null, length minimum of 6 and maximum of " + MaxCharactersPerCurrencyPair + ".")]
        public void DecomposeThrowsOnSymbolTooLong()
        {
            string symbol = "";

            for(int i = 0 ; i < Currencies.MaxCharactersPerCurrencyPair + 1; i++)
                symbol += "X";

            Assert.AreEqual(symbol.Length, Currencies.MaxCharactersPerCurrencyPair + 1);

            string basec, quotec;
            CurrencyPairUtil.DecomposeCurrencyPair(symbol, out basec, out quotec);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "Currency pairs must not be null, length minimum of 6 and maximum of " + MaxCharactersPerCurrencyPair + ".")]
        public void DecomposeThrowsOnNullSymbol()
        {
            string symbol = null;
            string basec, quotec;
            CurrencyPairUtil.DecomposeCurrencyPair(symbol, out basec, out quotec);
        }

        [Test]
        public void CurrencyPairDualForex()
        {
            string currencyPair = "EURUSD";

            Assert.AreEqual(CurrencyPairUtil.CurrencyPairDual(currencyPair, "EUR"), "USD");
            Assert.AreEqual(CurrencyPairUtil.CurrencyPairDual(currencyPair, "USD"), "EUR");
        }

        [Test]
        public void CurrencyPairDualCrypto()
        {
            string currencyPair = "ETHBTC";

            Assert.AreEqual(CurrencyPairUtil.CurrencyPairDual(currencyPair, "ETH"), "BTC");
            Assert.AreEqual(CurrencyPairUtil.CurrencyPairDual(currencyPair, "BTC"), "ETH");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "The knownSymbol ZRX isn't contained in currencyPair ETHBTC.")]
        public void CurrencyPairDualThrowsOnWrongKnownSymbol()
        {
            string currencyPair = "ETHBTC";

            CurrencyPairUtil.CurrencyPairDual(currencyPair, "ZRX");
        }

        [Test]
        public void ComparePairWorksCorrectly()
        {
            Assert.AreEqual("ETHUSD".ComparePair("ETHUSD"), CurrencyPairUtil.Match.ExactMatch);
            Assert.AreEqual("ETHUSD".ComparePair("ETH", "USD"), CurrencyPairUtil.Match.ExactMatch);

            Assert.AreEqual("ETHUSD".ComparePair("USDETH"), CurrencyPairUtil.Match.InverseMatch);
            Assert.AreEqual("ETHUSD".ComparePair("USD", "ETH"), CurrencyPairUtil.Match.InverseMatch);

            Assert.AreEqual("ETHUSD".ComparePair("BTCUSD"), CurrencyPairUtil.Match.NoMatch);
            Assert.AreEqual("ETHUSD".ComparePair("BTC", "USD"), CurrencyPairUtil.Match.NoMatch);
        }

        [Test]
        public void PairContainsCodeWorksCorrectly()
        {
            Assert.AreEqual(CurrencyPairUtil.PairContainsCode("ETHUSD", "ETH"), true);
            Assert.AreEqual(CurrencyPairUtil.PairContainsCode("ETHUSD", "USD"), true);
            Assert.AreEqual(CurrencyPairUtil.PairContainsCode("ETHUSD", "ZRX"), false);
            Assert.AreEqual(CurrencyPairUtil.PairContainsCode("ETHUSD", "BTC"), false);
        }

    }
}
