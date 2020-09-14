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

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SymbolPropertiesDatabaseTests
    {
        [Test]
        public void LoadsLotSize()
        {
            var db = SymbolPropertiesDatabase.FromDataFolder();

            var symbolProperties = db.GetSymbolProperties(Market.FXCM, "EURGBP", SecurityType.Forex, "GBP");

            Assert.AreEqual(symbolProperties.LotSize, 1000);
        }

        [Test]
        public void LoadsQuoteCurrency()
        {
            var db = SymbolPropertiesDatabase.FromDataFolder();

            var symbolProperties = db.GetSymbolProperties(Market.FXCM, "EURGBP", SecurityType.Forex, "GBP");

            Assert.AreEqual(symbolProperties.QuoteCurrency, "GBP");
        }

        [Test]
        public void LoadsDefaultLotSize()
        {
            var defaultSymbolProperties = SymbolProperties.GetDefault(Currencies.USD);

            Assert.AreEqual(defaultSymbolProperties.LotSize, 1);
        }

        [TestCase(Market.FXCM, SecurityType.Forex)]
        [TestCase(Market.Oanda, SecurityType.Forex)]
        [TestCase(Market.GDAX, SecurityType.Crypto)]
        [TestCase(Market.Bitfinex, SecurityType.Crypto)]
        public void BaseCurrencyIsNotEqualToQuoteCurrency(string market, SecurityType securityType)
        {
            var db = SymbolPropertiesDatabase.FromDataFolder();

            var spList = db.GetSymbolPropertiesList(market, securityType).ToList();
            Assert.IsNotEmpty(spList);

            foreach (var kvp in spList)
            {
                var quoteCurrency = kvp.Value.QuoteCurrency;
                var baseCurrency = kvp.Key.Symbol.Substring(0, kvp.Key.Symbol.Length - quoteCurrency.Length);

                Assert.AreNotEqual(baseCurrency, quoteCurrency);
            }
        }

        [TestCase(Market.FXCM, SecurityType.Cfd)]
        [TestCase(Market.Oanda, SecurityType.Cfd)]
        [TestCase(Market.CBOE, SecurityType.Future)]
        [TestCase(Market.CBOT, SecurityType.Future)]
        [TestCase(Market.CME, SecurityType.Future)]
        [TestCase(Market.COMEX, SecurityType.Future)]
        [TestCase(Market.ICE, SecurityType.Future)]
        [TestCase(Market.NYMEX, SecurityType.Future)]
        [TestCase(Market.SGX, SecurityType.Future)]
        [TestCase(Market.HKFE, SecurityType.Future)]
        public void GetSymbolPropertiesListIsNotEmpty(string market, SecurityType securityType)
        {
            var db = SymbolPropertiesDatabase.FromDataFolder();

            var spList = db.GetSymbolPropertiesList(market, securityType).ToList();

            Assert.IsNotEmpty(spList);
        }

        [TestCase(Market.USA, SecurityType.Equity)]
        [TestCase(Market.USA, SecurityType.Option)]
        public void GetSymbolPropertiesListHasOneRow(string market, SecurityType securityType)
        {
            var db = SymbolPropertiesDatabase.FromDataFolder();

            var spList = db.GetSymbolPropertiesList(market, securityType).ToList();

            Assert.AreEqual(1, spList.Count);
            Assert.IsTrue(spList[0].Key.Symbol.Contains("*"));
        }
    }
}
