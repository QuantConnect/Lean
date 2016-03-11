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
using System.Net;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using Newtonsoft.Json;
using System.Linq;

namespace QuantConnect.ToolBox.CryptoiqDownloader
{
    /// <summary>
    /// Cryptoiq Data Downloader class 
    /// </summary>
    public class CryptoiqDownloader : IDataDownloader
    {

        private string _exchange;
        private bool _useDivisor;
        const decimal divisor = 100m;

        /// <summary>
        /// Creates instance of downloader
        /// </summary>
        /// <param name="exchange"></param>
        /// <param name="useDivisor"></param>
        public CryptoiqDownloader(string exchange = "bitfinex", bool useDivisor = false)
        {
            _exchange = exchange;
            _useDivisor = useDivisor;
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Only Tick is currently supported</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {

            if (resolution != Resolution.Tick)
            {
                throw new ArgumentException("Only tick data is currently supported.");
            }


            DateTime counter = startUtc;
            int hour = 1;
            var url = "http://cryptoiq.io/api/marketdata/ticker/{3}/{2}/{0}/{1}";

            while (counter <= endUtc)
            {
                // Console.WriteLine(counter.ToString());
                while (hour < 24)
                {
                    // Console.WriteLine(hour.ToString());
                    string request = String.Format(url, counter.ToString("yyyy-MM-dd"), hour.ToString(), symbol.Value, _exchange);

                    using (var cl = new WebClient())
                    {
                       var data = cl.DownloadString(request);

                        var mbtc = JsonConvert.DeserializeObject<List<CryptoiqBitcoin>>(data);
                        mbtc = mbtc.OrderBy(m => m.Time).ToList();
                        foreach (var item in mbtc)
                        {
                            yield return new Tick
                            {
                                Time = item.Time,
                                Symbol = symbol,
                                Value = _useDivisor ? item.Last / divisor: item.Last,
                                AskPrice = _useDivisor ? item.Ask / divisor : item.Ask,
                                BidPrice = _useDivisor ? item.Bid / divisor : item.Bid,
                                TickType = QuantConnect.TickType.Quote
                            };
                        }
                        hour++;
                    }
                }
                counter = counter.AddDays(1);
                hour = 0;
            }
        }

    }
}