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
using QuantConnect.ToolBox.GDAXDownloader.Models;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace QuantConnect.ToolBox.GDAXDownloader
{
    /// <summary>
    /// GDAX implementation of <see cref="IExchangeInfoDownloader"/>
    /// </summary>
    public class GDAXExchangeInfoDownloader : IExchangeInfoDownloader
    {
        /// <summary>
        /// Market name
        /// </summary>
        public string Market => QuantConnect.Market.GDAX;

        /// <summary>
        /// Pulling data from a remote source
        /// </summary>
        /// <returns>Enumerable of exchange info</returns>
        public IEnumerable<string> Get()
        {
            const string url = "https://api.exchange.coinbase.com/products";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.AllowAutoRedirect = true;
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Headers["Accept"] = "application/json";
            request.Headers["User-Agent"] = ".NET Framework Test Client";
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                var exchangeInfo = JsonConvert.DeserializeObject<List<Product>>(json);
                foreach (var product in exchangeInfo)
                {
                    // market,symbol,type,description,quote_currency,contract_multiplier,minimum_price_variation,lot_size,market_ticker,minimum_order_size,price_magnifier
                    var symbol = product.ID.Replace("-", string.Empty);
                    var quoteCurrency = product.QuoteCurrency;
                    var contractMultiplier = 1;
                    var minimum_price_variation = product.QuoteIncrement;
                    var lot_size = product.BaseMinSize;
                    var marketTicker = product.ID;
                    var minimum_order_size = product.BaseMinSize;
                    yield return $"gdax,{symbol},crypto,description,{quoteCurrency},{contractMultiplier},{minimum_price_variation},{lot_size},{marketTicker},{minimum_order_size}";
                }
            }
        }
    }
}
