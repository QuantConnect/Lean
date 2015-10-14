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

namespace QuantConnect.ToolBox.YahooDownloader
{

    /// <summary>
    /// Yahoo Data Downloader class for 
    /// </summary>
    public class YahooDataDownloader : IDataDownloader
    {
        //Initialize
        private string _urlPrototype = @"http://ichart.finance.yahoo.com/table.csv?s={0}&a={1}&b={2}&c={3}&d={4}&e={5}&f={6}&g={7}&ignore=.csv";

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="type">Security type</param>
        /// <param name="resolution">Resolution of the data request</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, SecurityType type, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            // We subtract one day to make sure we have data from yahoo
            var finishMonth = endUtc.Month;
            var finishDay = endUtc.Subtract(TimeSpan.FromDays(1)).Day;
            var finishYear = endUtc.Year;
            var url = string.Format(_urlPrototype, symbol, 01, 01, 1990, finishMonth, finishDay, finishYear, "d");

            using (var cl = new WebClient())
            {
                var data = cl.DownloadString(url);
                var lines = data.Split('\n');

                for (var i = lines.Length - 1; i >= 1; i--)
                {
                    var str = lines[i].Split(',');
                    if (str.Length < 6) continue;
                    var ymd = str[0].Split('-');
                    var year = Convert.ToInt32(ymd[0]);
                    var month = Convert.ToInt32(ymd[1]);
                    var day = Convert.ToInt32(ymd[2]);
                    var open = decimal.Parse(str[1]);
                    var high = decimal.Parse(str[2]);
                    var low = decimal.Parse(str[3]);
                    var close = decimal.Parse(str[4]);
                    var volume = int.Parse(str[5]);
                    yield return new TradeBar(new DateTime(year, month, day), symbol, open, high, low, close, volume, TimeSpan.FromDays(1));
                }
            }
        }
    }
}
