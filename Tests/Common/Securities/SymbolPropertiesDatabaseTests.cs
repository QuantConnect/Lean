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
using QuantConnect.Logging;
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
            var symbol = Symbol.Create("EURGBP", SecurityType.Forex, Market.FXCM);
            var symbolProperties = db.GetSymbolProperties(symbol.ID.Market, symbol, symbol.SecurityType, "GBP");

            Assert.AreEqual(symbolProperties.LotSize, 1000);
        }

        [Test]
        public void LoadsQuoteCurrency()
        {
            var db = SymbolPropertiesDatabase.FromDataFolder();
            var symbol = Symbol.Create("EURGBP", SecurityType.Forex, Market.FXCM);
            var symbolProperties = db.GetSymbolProperties(symbol.ID.Market, symbol, symbol.SecurityType, "GBP");

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

            Log.Trace(sb.ToString());
        }

        private class GdaxCurrency
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("min_size")]
            public decimal MinSize { get; set; }
        }

        private class GdaxProduct
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

        #region Bitfinex brokerage

        [Test, Explicit]
        public void FetchSymbolPropertiesFromBitfinex()
        {
            const string urlExchangePairs = "https://api-pub.bitfinex.com/v2/conf/pub:list:pair:exchange";
            const string urlMarginPairs = "https://api-pub.bitfinex.com/v2/conf/pub:list:pair:margin";
            const string urlCurrencyMap = "https://api-pub.bitfinex.com/v2/conf/pub:map:currency:sym";
            const string urlCurrencyLabels = "https://api-pub.bitfinex.com/v2/conf/pub:map:currency:label";
            const string urlSymbolDetails = "https://api.bitfinex.com/v1/symbols_details";

            var sb = new StringBuilder();

            using (var wc = new WebClient())
            {
                var jsonExchangePairs = wc.DownloadString(urlExchangePairs);
                var exchangePairs = JsonConvert.DeserializeObject<List<List<string>>>(jsonExchangePairs)[0];

                var jsonMarginPairs = wc.DownloadString(urlMarginPairs);
                var marginPairs = JsonConvert.DeserializeObject<List<List<string>>>(jsonMarginPairs)[0];

                var jsonCurrencyMap = wc.DownloadString(urlCurrencyMap);
                var rowsCurrencyMap = JsonConvert.DeserializeObject<List<List<List<string>>>>(jsonCurrencyMap)[0];
                var currencyMap = rowsCurrencyMap
                    .ToDictionary(row => row[0], row => row[1].ToUpperInvariant());

                var jsonCurrencyLabels = wc.DownloadString(urlCurrencyLabels);
                var rowsCurrencyLabels = JsonConvert.DeserializeObject<List<List<List<string>>>>(jsonCurrencyLabels)[0];
                var currencyLabels = rowsCurrencyLabels
                    .ToDictionary(row => row[0], row => row[1]);

                var jsonSymbolDetails = wc.DownloadString(urlSymbolDetails);
                var symbolDetails = JsonConvert.DeserializeObject<List<BitfinexSymbolDetails>>(jsonSymbolDetails);
                var minimumPriceIncrements = symbolDetails
                    .ToDictionary(x => x.Pair.ToUpperInvariant(), x => (decimal)Math.Pow(10, -x.PricePrecision));

                foreach (var pair in exchangePairs.Union(marginPairs).OrderBy(x => x))
                {
                    string baseCurrency, quoteCurrency;
                    if (pair.Contains(":"))
                    {
                        var parts = pair.Split(':');
                        baseCurrency = parts[0];
                        quoteCurrency = parts[1];
                    }
                    else if (pair.Length == 6)
                    {
                        baseCurrency = pair.Substring(0, 3);
                        quoteCurrency = pair.Substring(3);
                    }
                    else
                    {
                        // should never happen
                        Log.Trace($"Skipping pair with unknown format: {pair}");
                        continue;
                    }

                    string baseDescription, quoteDescription;
                    if (!currencyLabels.TryGetValue(baseCurrency, out baseDescription))
                    {
                        Log.Trace($"Base currency description not found: {baseCurrency}");
                        baseDescription = baseCurrency;
                    }
                    if (!currencyLabels.TryGetValue(quoteCurrency, out quoteDescription))
                    {
                        Log.Trace($"Quote currency description not found: {quoteCurrency}");
                        quoteDescription = quoteCurrency;
                    }

                    var description = baseDescription + "-" + quoteDescription;

                    string newBaseCurrency, newQuoteCurrency;
                    if (currencyMap.TryGetValue(baseCurrency, out newBaseCurrency))
                    {
                        baseCurrency = newBaseCurrency;
                    }
                    if (currencyMap.TryGetValue(quoteCurrency, out newQuoteCurrency))
                    {
                        quoteCurrency = newQuoteCurrency;
                    }

                    // skip test symbols
                    if (quoteCurrency.StartsWith("TEST"))
                    {
                        continue;
                    }

                    var leanTicker = $"{baseCurrency}{quoteCurrency}";

                    decimal minimumPriceIncrement;
                    if (!minimumPriceIncrements.TryGetValue(pair, out minimumPriceIncrement))
                    {
                        minimumPriceIncrement = 0.00001m;
                    }

                    const decimal lotSize = 0.00000001m;

                    sb.AppendLine("bitfinex," +
                                  $"{leanTicker}," +
                                  "crypto," +
                                  $"{description}," +
                                  $"{quoteCurrency}," +
                                  "1," +
                                  $"{minimumPriceIncrement.NormalizeToStr()}," +
                                  $"{lotSize.NormalizeToStr()}," +
                                  $"t{pair}");
                }
            }

            Log.Trace(sb.ToString());
        }

        private class BitfinexSymbolDetails
        {
            [JsonProperty("pair")]
            public string Pair { get; set; }

            [JsonProperty("price_precision")]
            public int PricePrecision { get; set; }

            [JsonProperty("initial_margin")]
            public decimal InitialMargin { get; set; }

            [JsonProperty("minimum_margin")]
            public decimal MinimumMargin { get; set; }

            [JsonProperty("maximum_order_size")]
            public decimal MaximumOrderSize { get; set; }

            [JsonProperty("minimum_order_size")]
            public decimal MinimumOrderSize { get; set; }

            [JsonProperty("expiration")]
            public string Expiration { get; set; }
        }

        #endregion

        [TestCase("ES", Market.CME, 50, 0.25)]
        [TestCase("ZB", Market.CBOT, 1000, 0.015625)]
        [TestCase("ZW", Market.CBOT, 5000, 0.00125)]
        [TestCase("SI", Market.COMEX, 5000, 0.001)]
        public void ReadsFuturesOptionsEntries(string ticker, string market, int expectedMultiplier, double expectedMinimumPriceFluctuation)
        {
            var future = Symbol.CreateFuture(ticker, market, SecurityIdentifier.DefaultDate);
            var option = Symbol.CreateOption(
                future,
                market,
                default(OptionStyle),
                default(OptionRight),
                default(decimal),
                SecurityIdentifier.DefaultDate);

            var db = SymbolPropertiesDatabase.FromDataFolder();
            var results = db.GetSymbolProperties(market, option, SecurityType.FutureOption, "USD");

            Assert.AreEqual((decimal)expectedMultiplier, results.ContractMultiplier);
            Assert.AreEqual((decimal)expectedMinimumPriceFluctuation, results.MinimumPriceVariation);
        }
    }
}