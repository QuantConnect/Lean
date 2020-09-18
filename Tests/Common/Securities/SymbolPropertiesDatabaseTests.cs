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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
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

        #region GDAX brokerage

        [Test, Explicit]
        public void FetchSymbolPropertiesFromGdax()
        {
            const string urlCurrencies = "https://api.pro.coinbase.com/currencies";
            const string urlProducts = "https://api.pro.coinbase.com/products";

            var sb = new StringBuilder();

            using (var wc = new WebClient())
            {
                var jsonCurrencies = wc.DownloadString(urlCurrencies);
                var rowsCurrencies = JsonConvert.DeserializeObject<List<GdaxCurrency>>(jsonCurrencies);
                var currencyDescriptions = rowsCurrencies.ToDictionary(x => x.Id, x => x.Name);

                var jsonProducts = wc.DownloadString(urlProducts);

                var rowsProducts = JsonConvert.DeserializeObject<List<GdaxProduct>>(jsonProducts);
                foreach (var row in rowsProducts.OrderBy(x => x.Id))
                {
                    string baseDescription, quoteDescription;
                    if (!currencyDescriptions.TryGetValue(row.BaseCurrency, out baseDescription))
                    {
                        baseDescription = row.BaseCurrency;
                    }
                    if (!currencyDescriptions.TryGetValue(row.QuoteCurrency, out quoteDescription))
                    {
                        quoteDescription = row.QuoteCurrency;
                    }

                    sb.AppendLine("gdax," +
                                  $"{row.BaseCurrency}{row.QuoteCurrency}," +
                                  "crypto," +
                                  $"{baseDescription}-{quoteDescription}," +
                                  $"{row.QuoteCurrency}," +
                                  "1," +
                                  $"{row.QuoteIncrement.NormalizeToStr()}," +
                                  $"{row.BaseIncrement.NormalizeToStr()}," +
                                  $"{row.Id}");
                }
            }

            Console.WriteLine(sb.ToString());
        }

        public class GdaxCurrency
        {
            [JsonProperty("id")] 
            public string Id { get; set; }

            [JsonProperty("name")] 
            public string Name { get; set; }

            [JsonProperty("min_size")]
            public decimal MinSize { get; set; }
        }

        public class GdaxProduct
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("base_currency")]
            public string BaseCurrency { get; set; }

            [JsonProperty("quote_currency")]
            public string QuoteCurrency { get; set; }

            [JsonProperty("base_min_size")]
            public decimal BaseMinSize { get; set; }

            [JsonProperty("base_max_size")]
            public decimal BaseMaxSize { get; set; }

            [JsonProperty("quote_increment")]
            public decimal QuoteIncrement { get; set; }

            [JsonProperty("base_increment")]
            public decimal BaseIncrement { get; set; }

            [JsonProperty("display_name")]
            public string DisplayName { get; set; }

            [JsonProperty("min_market_funds")]
            public decimal MinMarketFunds { get; set; }

            [JsonProperty("max_market_funds")]
            public decimal MaxMarketFunds { get; set; }

            [JsonProperty("margin_enabled")]
            public bool MarginEnabled { get; set; }

            [JsonProperty("post_only")]
            public bool PostOnly { get; set; }

            [JsonProperty("limit_only")]
            public bool LimitOnly { get; set; }

            [JsonProperty("cancel_only")]
            public bool CancelOnly { get; set; }

            [JsonProperty("trading_disabled")]
            public bool TradingDisabled { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("status_message")]
            public string StatusMessage { get; set; }
        }

        #endregion

    }
}
