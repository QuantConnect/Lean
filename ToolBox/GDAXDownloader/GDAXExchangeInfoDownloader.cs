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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Logging;
using QuantConnect.ToolBox.GDAXDownloader.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.ToolBox.GDAXDownloader
{
    /// <summary>
    /// GDAX implementation of <see cref="IExchangeInfoDownloader"/>
    /// </summary>
    public class GDAXExchangeInfoDownloader : IExchangeInfoDownloader
    {
        private readonly Dictionary<string, string> _idNameMapping = new();

        /// <summary>
        /// Market name
        /// </summary>
        public string Market => QuantConnect.Market.GDAX;

        /// <summary>
        /// Creats an instance of the class
        /// </summary>
        public GDAXExchangeInfoDownloader()
        {
            _idNameMapping = GetCurrencyDetails();
        }

        /// <summary>
        /// Pulling data from a remote source
        /// </summary>
        /// <returns>Enumerable of exchange info</returns>
        public IEnumerable<string> Get()
        {
            const string url = "https://api.exchange.coinbase.com/products";
            Dictionary<string, string> headers = new() { { "User-Agent", ".NET Client" } };
            var json = url.DownloadData(headers);
            var exchangeInfo = JsonConvert.DeserializeObject<List<Product>>(json);
            foreach (var product in exchangeInfo.OrderBy(x => x.ID.Replace("-", string.Empty)))
            {
                // market,symbol,type,description,quote_currency,contract_multiplier,minimum_price_variation,lot_size,market_ticker,minimum_order_size
                var symbol = product.ID.Replace("-", string.Empty);
                var description = $"{_idNameMapping[product.BaseCurrency]}-{_idNameMapping[product.QuoteCurrency]}";
                var quoteCurrency = product.QuoteCurrency;
                var contractMultiplier = 1;
                var minimum_price_variation = product.QuoteIncrement;
                var lot_size = product.BaseIncrement;
                var marketTicker = product.ID;
                var minimum_order_size = product.BaseMinSize;
                yield return $"{Market},{symbol},crypto,{description},{quoteCurrency},{contractMultiplier},{minimum_price_variation},{lot_size},{marketTicker},{minimum_order_size}";
            }
        }

        /// <summary>
        /// Fetch currency details
        /// </summary>
        /// <returns>Enumerable of exchange info</returns>
        private static Dictionary<string, string> GetCurrencyDetails()
        {
            Dictionary<string, string> idNameMapping = new();
            var url = $"https://api.exchange.coinbase.com/currencies";
            Dictionary<string, string> headers = new() { { "User-Agent", ".NET Framework Test Client" } };
            var json = url.DownloadData(headers);
            var jObject = JToken.Parse(json);
            foreach (var currency in jObject)
            {
                try
                {
                    var id = currency.SelectToken("id").ToString();
                    idNameMapping[id] = currency.SelectToken("name").ToString();
                }
                catch (Exception e)
                {
                    Log.Trace($"GDAXExchangeInfoDownloader.GetCurrencyNameById(): {e}");
                }
            }
            return idNameMapping;
        }
    }
}
