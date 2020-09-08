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

using System.Linq;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class CurrenciesTests
    {
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

        [TestCase(SecurityType.Crypto, Market.GDAX)]
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
    }
}
