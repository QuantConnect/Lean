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
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class CurrenciesTests
    {
        [TestCase("")]
        [TestCase(null)]
        public void GetCurrencySymbolHandlesNullOrEmpty(string currency)
        {
            Assert.AreEqual("", Currencies.GetCurrencySymbol(currency));
        }

        [TestCase(SecurityType.Forex, Market.FXCM)]
        [TestCase(SecurityType.Forex, Market.Oanda)]
        public void HasCurrencySymbolForEachForexPair(SecurityType securityType, string market)
        {
            var symbols = SymbolPropertiesDatabase
                .FromDataFolder()
                .GetSymbolPropertiesList(market, securityType)
                .Select(x => x.Key.Symbol);

            foreach (var symbol in symbols)
            {
                string baseCurrency, quoteCurrency;
                Forex.DecomposeCurrencyPair(symbol, out baseCurrency, out quoteCurrency);

                Assert.IsTrue(!string.IsNullOrWhiteSpace(Currencies.GetCurrencySymbol(baseCurrency)), "Missing currency symbol for: " + baseCurrency);
                Assert.IsTrue(!string.IsNullOrWhiteSpace(Currencies.GetCurrencySymbol(quoteCurrency)), "Missing currency symbol for: " + quoteCurrency);
            }
        }

        [TestCase(SecurityType.Crypto, Market.Coinbase)]
        [TestCase(SecurityType.Crypto, Market.Bitfinex)]
        public void HasCurrencySymbolForEachCryptoPair(SecurityType securityType, string market)
        {
            var symbols = SymbolPropertiesDatabase
                .FromDataFolder()
                .GetSymbolPropertiesList(market, securityType)
                .Select(x => Symbol.Create(x.Key.Symbol, securityType, market));

            foreach (var symbol in symbols)
            {
                var symbolProperties = SymbolPropertiesDatabase.FromDataFolder().GetSymbolProperties(market, symbol, securityType, Currencies.USD);

                string baseCurrency, quoteCurrency;
                Crypto.DecomposeCurrencyPair(symbol, symbolProperties, out baseCurrency, out quoteCurrency);

                Assert.IsTrue(!string.IsNullOrWhiteSpace(Currencies.GetCurrencySymbol(baseCurrency)), "Missing currency symbol for: " + baseCurrency);
                Assert.IsTrue(!string.IsNullOrWhiteSpace(Currencies.GetCurrencySymbol(quoteCurrency)), "Missing currency symbol for: " + quoteCurrency);
            }
        }

        [TestCase(Currencies.USD)]
        [TestCase(Currencies.EUR)]
        [TestCase("BTC")]
        [TestCase("ADA")]
        public void ReturnsSymbolForCurrencyWithSymbol(string currency)
        {
            Assert.AreNotEqual(currency, Currencies.GetCurrencySymbol(currency));
        }

        [TestCase("ABC")]
        [TestCase("XYZ")]
        [TestCase("CUR")]
        public void ReturnsTickerForUnknownCurrency(string currency)
        {
            Assert.AreEqual(currency, Currencies.GetCurrencySymbol(currency));
        }

        [Test]
        public void ParsesValuesWithCurrency(
            [ValueSource(nameof(CurrencySymbols))] string currencySymbol,
            [Values("10,000.1", "10000.1", "1.00001e4")] string value)
        {
            decimal result = 0;
            decimal result2;
            string valueWithCurrency = currencySymbol + value;
            Assert.DoesNotThrow(() => result = Currencies.Parse(valueWithCurrency));
            Assert.IsTrue(Currencies.TryParse(valueWithCurrency, out result2));
            Assert.AreEqual(10000.1m, result);
            Assert.AreEqual(result, result2);
        }

        [Test]
        public void CannotParseInvalidValuesWithCurrency(
            [ValueSource(nameof(CurrencySymbols))] string currencySymbol,
            [Values("10.000.1", "10.000,1", "1.00001A4", "")] string value)
        {
            string valueWithCurrency = currencySymbol + value;
            Assert.Throws<ArgumentException>(() => Currencies.Parse(valueWithCurrency));
            Assert.IsFalse(Currencies.TryParse(valueWithCurrency, out _));
        }

        static IEnumerable<string> CurrencySymbols =>
            // Currencies with known symbols
            Currencies.CurrencySymbols.Values.Distinct()
            // Currencies without known symbols
            .Concat(new [] { "BUSD", "BNT", "ARS", "VES" });
    }
}
