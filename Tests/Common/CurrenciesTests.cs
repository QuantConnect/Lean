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
using QuantConnect.Securities.Forex;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class CurrenciesTests
    {
        [Test]
        public void HasCurrencySymbolForEachPair()
        {
            var allPairs = Currencies.CurrencyPairs.Concat(Currencies.CfdCurrencyPairs);
            foreach (var currencyPair in allPairs)
            {
                string quotec, basec;
                Forex.DecomposeCurrencyPair(currencyPair, out basec, out quotec);
                Assert.IsTrue(Currencies.CurrencySymbols.ContainsKey(basec), "Missing currency symbol for: " + basec);
                Assert.IsTrue(Currencies.CurrencySymbols.ContainsKey(quotec), "Missing currency symbol for: " + quotec);
            }
        }

        [Test]
        public void HasCryptoCurrencySymbolForEachPair()
        {
            var allPairs = Currencies.CryptoCurrencyPairs;
            foreach (var currencyPair in allPairs)
            {
                Assert.IsTrue(Currencies.CurrencySymbols.Keys.Any(c => currencyPair.StartsWith(c)), "Missing currency symbol for: " + currencyPair);
                Assert.IsTrue(Currencies.CurrencySymbols.Keys.Any(c => currencyPair.EndsWith(c)), "Missing currency symbol for: " + currencyPair);
            }
        }
    }
}
