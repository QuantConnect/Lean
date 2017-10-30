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
using Newtonsoft.Json.Linq;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.GDAXDownloader
{
    /// <summary>
    /// GDAX Data Downloader class 
    /// </summary>
    public class GDAXDownloader : IDataDownloader
    {
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
            var returnData = new List<BaseData>();

            if (resolution != Resolution.Hour)
            {
                throw new ArgumentException("Only hourly data is currently supported.");
            }

            var counter = startUtc;
            const string url = "http://api.gdax.com/products/{0}/candles?start={1}&end={2}&granularity={3}";

            while (counter <= endUtc)
            {
                System.Threading.Thread.Sleep(1000);
                Log.Trace("Getting " + counter.ToShortDateString() + " data..");

                DateTime endDate = counter.AddDays(1);
                var granularity = 3600;

                var requestURL = string.Format(url, symbol.Value, counter.ToShortDateString(), endDate.ToShortDateString(), granularity);
                var request = (HttpWebRequest)WebRequest.Create(requestURL);
                request.UserAgent = ".NET Framework Test Client";

                var response = (HttpWebResponse)request.GetResponse();
                var encoding = ASCIIEncoding.ASCII;

                string data;
                using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                {
                    data = reader.ReadToEnd();
                }
                if (data.Length > 0)
                {
                    var a = JArray.Parse(data);

                    foreach (var token in a.Children())
                    {
                        var barValues = JArray.Parse(token.ToString());

                        for (int i = 0; i < barValues.Count; i++)
                        {
                            var dt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(barValues[0].ToString())).DateTime;
                            var tradeBar = new TradeBar()
                            {
                                Time = dt,
                                Symbol = symbol,
                                Low = decimal.Parse(barValues[1].ToString()),
                                High = decimal.Parse(barValues[2].ToString()),
                                Open = decimal.Parse(barValues[3].ToString()),
                                Close = decimal.Parse(barValues[4].ToString()),
                                Volume = decimal.Parse(barValues[5].ToString()),
                                Value = decimal.Parse(barValues[4].ToString()),
                                DataType = MarketDataType.TradeBar,
                                Period = Time.OneHour,
                                EndTime = dt.AddHours(1)
                            };
                            returnData.Add(tradeBar);
                        }
                    }
                }
                counter = counter.AddDays(1);
            }
            return returnData;
        }
    }
}
