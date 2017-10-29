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
using System.Text;

namespace QuantConnect.ToolBox.GDAXDownloader
{
    /// <summary>
    /// Cryptoiq Data Downloader class 
    /// </summary>
    public class GDAXDownloader : IDataDownloader
    {
        private readonly string _exchange;

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoiqDownloader"/> class
        /// </summary>
        /// <param name="exchange">The bitcoin exchange</param>
        /// <param name="scaleFactor">Scale factor used to scale the data, useful for changing the BTC units</param>
        public GDAXDownloader(string exchange = Market.GDAX)
        {
            _exchange = exchange;
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
            List<BaseData> returnData = new List<BaseData>();

            if (resolution != Resolution.Hour)
            {
                throw new ArgumentException("Only hourly data is currently supported.");
            }

            var counter = startUtc;
            const string url = "http://api.gdax.com/products/{0}/candles?start={1}&end={2}&granularity={3}";

            while (counter <= endUtc)
            {
                System.Threading.Thread.Sleep(1000);
                Console.WriteLine("Getting " + counter.ToShortDateString() + " data..");
                DateTime endDate = counter.AddDays(1);
                int granularity = 3600;

                var requestURL = string.Format(url, symbol.Value, counter.ToShortDateString(), endDate.ToShortDateString(), granularity);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestURL);
                request.UserAgent = ".NET Framework Test Client";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                var encoding = ASCIIEncoding.ASCII;

                string data;
                using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                {
                    data = reader.ReadToEnd();
                }

                data = data.Remove(0, 2);
                data = data.Substring(0,data.Length - 2);

                string[] bars = data.Split(new string[] { "],[" },StringSplitOptions.RemoveEmptyEntries);

                foreach(var bar in bars.Distinct())
                {
                    var items = bar.Split(new char[] { ',' });
                    TradeBar tradeBar = new TradeBar()
                    {
                        Time =  new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(long.Parse(items[0])).ToLocalTime(),
                        Symbol = symbol,
                        Low = decimal.Parse(items[1]),
                        High = decimal.Parse(items[2]),
                        Open = decimal.Parse(items[3]),
                        Close = decimal.Parse(items[4]),
                        Volume = decimal.Parse(items[5]),
                        Value = decimal.Parse(items[4]),
                        DataType = MarketDataType.TradeBar,
                        Period = Time.OneHour,
                        EndTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(long.Parse(items[0])).ToLocalTime().AddHours(1)
                     };
                     returnData.Add(tradeBar);
                }
            
                counter = counter.AddDays(1);
            }
            return returnData;
        }
    }
}