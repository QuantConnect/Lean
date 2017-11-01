/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2017 QuantConnect Corporation.
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
using QuantConnect.Data;
using System.Net;
using Newtonsoft.Json;
using QuantConnect.Data.Market;

namespace QuantConnect.ToolBox.KrakenDownloader
{
    /// <summary>
    /// Kraken Data Downloader class
    /// </summary>
    class KrakenDataDownloader : IDataDownloader
    {
        private const string UrlPrototype = @"https://api.kraken.com/0/public/Trades?pair={0}&since={1}";

        /// <summary>
        /// Get historical data enumerable for a trading pair, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Resolution of the data request</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            if (endUtc < startUtc)
            {
                throw new ArgumentException("The end date must be greater or equal than the start date.");
            }

            if (resolution != Resolution.Tick)
            {
                throw new NotSupportedException("Only Tick Resolution is supported.");
            }

            var startUnixTime = ToUnixTime(startUtc) * 1000000000; // Multiply by 10^9 per Kraken API
            var endUnixTime = ToUnixTime(endUtc) * 1000000000;
            var url = string.Format(UrlPrototype, symbol.Value, startUnixTime);
            List<List<string>> data;

            using (var client = new WebClient())
            {
                var response = client.DownloadString(url);
                dynamic result = JsonConvert.DeserializeObject<dynamic>(response);
                if (result.error.Count != 0)
                {
                    throw new Exception("Error in Kraken API: " + result.error[0]);
                }

                data = result.result[symbol.Value].ToObject<List<List<string>>>();

                var last = Convert.ToInt64(result.result.last);
                while (last < endUnixTime)
                {
                    url = string.Format(UrlPrototype, symbol.Value, last);
                    response = client.DownloadString(url);
                    result = JsonConvert.DeserializeObject<dynamic>(response);

                    var errorCount = 0;
                    while (result.error.Count != 0 && errorCount < 10)
                    {
                        errorCount++;
                        System.Threading.Thread.Sleep(6000);
                        response = client.DownloadString(url);
                        result = JsonConvert.DeserializeObject<dynamic>(response);
                    }

                    if (result.error.Count != 0 && errorCount >= 10)
                    {
                        throw new Exception("Error in Kraken API: " + result.error[0]);
                    }

                    List<List<string>> newData = result.result[symbol.Value].ToObject<List<List<string>>>();
                    data.AddRange(newData);
                    last = Convert.ToInt64(result.result.last);
                }
            }

            foreach (var i in data)
            {
                var time = FromUnixTime(Convert.ToInt64(i[2].Split('.')[0]));
                if (time > endUtc)
                {
                    break;
                }

                var value = Decimal.Parse(i[0]);

                yield return new Tick
                {
                    Time = time,
                    Symbol = symbol,
                    Value = value,
                    AskPrice = value,
                    BidPrice = value,
                    TickType = TickType.Trade
                };
            }
        }

        /// <summary>
        /// Convert a DateTime object into a Unix time long value
        /// </summary>
        /// <param name="utcDateTime">The DateTime object (UTC)</param>
        /// <returns>A Unix long time value.</returns>
        /// <remarks>When we move to NET 4.6, we can replace this with DateTimeOffset.ToUnixTimeSeconds()</remarks>
        private static long ToUnixTime(DateTime utcDateTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(utcDateTime - epoch).TotalSeconds;
        }

        /// <summary>
        /// Convert a Unix time long value into a DateTime object
        /// </summary>
        /// <param name="unixTime">Unix long time.</param>
        /// <returns>A DateTime value (UTC)</returns>
        /// <remarks>When we move to NET 4.6, we can replace this with DateTimeOffset.FromUnixTimeSeconds()</remarks>
        private static DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }
    }
}
