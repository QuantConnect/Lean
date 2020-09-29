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
using QuantConnect.ToolBox.BinanceDownloader.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace QuantConnect.ToolBox.BinanceDownloader
{
    /// <summary>
    /// Binance implementation of <see cref="IExchangeInfoDownloader"/>
    /// </summary>
    public class BinanceExchangeInfoDownloader : IExchangeInfoDownloader
    {
        /// <summary>
        /// Market name
        /// </summary>
        public string Market => QuantConnect.Market.Binance;

        /// <summary>
        /// Pulling data from a remote source
        /// </summary>
        /// <returns>Enumerable of exchange info</returns>
        public IEnumerable<string> Get()
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.binance.com/api/v3/exchangeInfo");

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                var exchangeInfo = JsonConvert.DeserializeObject<ExchangeInfo>(reader.ReadToEnd());

                foreach (var symbol in exchangeInfo.Symbols.OrderBy(x => x.Name))
                {
                    if (!symbol.IsSpotTradingAllowed && !symbol.IsMarginTradingAllowed)
                    {
                        // exclude derivatives
                        continue;
                    }

                    var priceFilter = symbol.Filters
                        .First(f => f.GetValue("filterType").ToString() == "PRICE_FILTER")
                        .GetValue("tickSize");

                    var lotFilter = symbol.Filters
                        .First(f => f.GetValue("filterType").ToString() == "LOT_SIZE")
                        .GetValue("stepSize");


                    yield return $"binance,{symbol.Name},crypto,{symbol.Name},{symbol.QuoteAsset},1,{priceFilter},{lotFilter},{symbol.Name}";
                }
            }
        }
    }
}
