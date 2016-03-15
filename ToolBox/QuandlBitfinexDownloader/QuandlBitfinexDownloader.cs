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
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.ToolBox.QuandlBitfinexDownloader
{
    /// <summary>
    /// Quandl Bitfinex Data Downloader class 
    /// </summary>
    public class QuandlBitfinexDownloader : IDataDownloader
    {
        private readonly string _apiKey;
        private readonly decimal _scaleFactor;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuandlBitfinexDownloader"/> class
        /// </summary>
        /// <param name="apiKey">The quandl api key</param>
        /// <param name="scaleFactor">Scale factor used to scale the data, useful for changing the BTC units</param>
        public QuandlBitfinexDownloader(string apiKey, int scaleFactor = 100)
        {
            _apiKey = apiKey;
            _scaleFactor = scaleFactor;
        }

        /// <summary>
        /// Get historical data enumerable for Bitfinex from Quandl
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Only Daily is supported</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            if (resolution != Resolution.Daily)
            {
                throw new ArgumentException("Only daily data is currently supported.");
            }

            const string collapse = "daily";

            var url = "https://www.quandl.com/api/v3/datasets/BCHARTS/BITFINEXUSD.csv?order=asc&collapse=" + collapse + "&api_key=" + _apiKey + "&start_date="
                + startUtc.ToString("yyyy-MM-dd");
            using (var cl = new WebClient())
            {
                var data = cl.DownloadString(url);

                // skip the header line
                foreach (var item in data.Split('\n').Skip(1))
                {
                    var line = item.Split(',');
                    if (line.Length != 8)
                    {
                        continue;
                    }

                    var bar = new TradeBar
                    {
                        Time = DateTime.Parse(line[0]),
                        Open = decimal.Parse(line[1])/_scaleFactor,
                        High = decimal.Parse(line[2])/_scaleFactor,
                        Low = decimal.Parse(line[3])/_scaleFactor,
                        Close = decimal.Parse(line[4])/_scaleFactor,
                        Value = decimal.Parse(line[7])/_scaleFactor,
                        Volume = (long) (decimal.Parse(line[5])*_scaleFactor),
                        Symbol = symbol,
                        DataType = MarketDataType.TradeBar,
                        Period = Time.OneDay
                    };

                    yield return bar;
                }
            }

        }

    }
}
