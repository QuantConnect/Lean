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
using System.IO;
using System.Net;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.GoogleDownloader
{
    /// <summary>
    /// Google Data Downloader class
    /// </summary>
    public class GoogleDataDownloader : IDataDownloader
    {
        // q = SYMBOL
        // startdate = Mon+D%2C+YYYY, e.g. Jan+1%2C+2005
        // enddate = Mon+D%2C+YYYY, e.g. Sep+29%2C+2016
        private const string UrlPrototypeDaily = @"https://finance.google.com/finance/historical?q={0}&startdate={1}&enddate={2}&output=csv";
        private enum ConvertMonths
        {
            Jan = 1,
            Feb = 2,
            Mar = 3,
            Apr = 4,
            May = 5,
            Jun = 6,
            Jul = 7,
            Aug = 8,
            Sep = 9,
            Oct = 10,
            Nov = 11,
            Dec = 12
        };

        private IEnumerable<BaseData> GetDaily(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            var startdate = ((ConvertMonths)startUtc.Month).ToString()
                + @"+" + startUtc.Day.ToString()
                + @"%2C+" + startUtc.Year.ToString();
            var enddate = ((ConvertMonths)endUtc.Month).ToString()
                + @"+" + endUtc.Day.ToString()
                + @"%2C+" + endUtc.Year.ToString();

            // Create the Google formatted URL.
            var url = string.Format(UrlPrototypeDaily, symbol.Value, startdate, enddate);

            // Download the data from Google.
            string[] lines;
            using (var client = new WebClient())
            {
                var data = client.DownloadString(url);
                lines = data.Split('\n');
            }

            // first line is header
            var currentLine = 1;

            while (currentLine < lines.Length - 1)
            {
                // Format: Date,Open,High,Low,Close,Volume
                var columns = lines[currentLine].Split(',');

                // date format: DD-Mon-YY, e.g. 27-Sep-16
                var DMY = columns[0].Split('-');

                // date = 20160927
                var day = DMY[0].ToInt32();
                var month = (int)Enum.Parse(typeof(ConvertMonths), DMY[1]);
                var year = (DMY[2].ToInt32() > 70) ? 1900 + DMY[2].ToInt32() : 2000 + DMY[2].ToInt32();
                var time = new DateTime(year, month, day, 0, 0, 0);

                // occasionally, the columns will have a '-' instead of a proper value
                List<Decimal?> ohlc = new List<Decimal?>()
                {
                    columns[1] != "-" ? (Decimal?)columns[1].ToDecimal() : null,
                    columns[2] != "-" ? (Decimal?)columns[2].ToDecimal() : null,
                    columns[3] != "-" ? (Decimal?)columns[3].ToDecimal() : null,
                    columns[4] != "-" ? (Decimal?)columns[4].ToDecimal() : null
                };

                if (ohlc.Where(val => val == null).Count() > 0)
                {
                    // let's try hard to fix any issues as good as we can
                    // this code assumes that there is at least 1 good value
                    if (ohlc[1] == null) ohlc[1] = ohlc.Where(val => val != null).Max();
                    if (ohlc[2] == null) ohlc[2] = ohlc.Where(val => val != null).Min();
                    if (ohlc[0] == null) ohlc[0] = ohlc.Where(val => val != null).Average();
                    if (ohlc[3] == null) ohlc[3] = ohlc.Where(val => val != null).Average();

                    Log.Error(string.Format("Corrupt bar on {0}: {1},{2},{3},{4}. Saved as {5},{6},{7},{8}.",
                        columns[0], columns[1], columns[2], columns[3], columns[4],
                        ohlc[0], ohlc[1], ohlc[2], ohlc[3]));
                }

                long volume = columns[5].ToInt64();

                yield return new TradeBar(time, symbol, (Decimal)ohlc[0], (Decimal)ohlc[1], (Decimal)ohlc[2], (Decimal)ohlc[3], volume, resolution.ToTimeSpan());

                currentLine++;
            }
        }

        // q = SYMBOL
        // i = resolution in seconds
        // p = period in days
        // ts = start time
        // Strangely Google forces CHLO format instead of normal OHLC.
        private const string UrlPrototypeMinuteOrHour = @"https://finance.google.com/finance/getprices?q={0}&i={1}&p={2}d&f=d,c,h,l,o,v&ts={3}";

        private IEnumerable<BaseData> GetMinuteOrHour(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            var numberOfDays = (int)(endUtc - startUtc).TotalDays;
            var resolutionSeconds = (int)resolution.ToTimeSpan().TotalSeconds;
            var endUnixTime = ToUnixTime(endUtc);

            // Create the Google formatted URL.
            var url = string.Format(UrlPrototypeMinuteOrHour, symbol.Value, resolutionSeconds, numberOfDays, endUnixTime);

            // Download the data from Google.
            string[] lines;
            using (var client = new WebClient())
            {
                var data = client.DownloadString(url);
                lines = data.Split('\n');
            }

            // First 7 lines are headers 
            var currentLine = 7;

            while (currentLine < lines.Length - 1)
            {
                var firstPass = true;

                // Each day google starts date time at 930am and then 
                // has 390 minutes over the day. Look for the starter rows "a".
                var columns = lines[currentLine].Split(',');
                var startTime = FromUnixTime(columns[0].Remove(0, 1).ToInt64());

                while (currentLine < lines.Length - 1)
                {
                    var str = lines[currentLine].Split(',');
                    if (str.Length < 6)
                        throw new InvalidDataException("Short record: " + str);

                    // If its the start of a new day, break out of this sub-loop.
                    var titleRow = str[0][0] == 'a';
                    if (titleRow && !firstPass) 
                        break;

                    firstPass = false;

                    // Build the current datetime, from the row offset
                    var time = startTime.AddSeconds(resolutionSeconds * (titleRow ? 0 : str[0].ToInt64()));

                    // Bar: d0, c1, h2, l3, o4, v5
                    var open = str[4].ToDecimal();
                    var high = str[2].ToDecimal();
                    var low = str[3].ToDecimal();
                    var close = str[1].ToDecimal();
                    var volume = str[5].ToInt64();

                    currentLine++;

                    yield return new TradeBar(time, symbol, open, high, low, close, volume, resolution.ToTimeSpan());
                }
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

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Resolution of the data request</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            if (symbol.ID.SecurityType != SecurityType.Equity)
                throw new NotSupportedException("SecurityType not available: " + symbol.ID.SecurityType);

            if (endUtc < startUtc)
                throw new ArgumentException("The end date must be greater or equal than the start date.");

            switch (resolution)
            {
                case Resolution.Minute:
                case Resolution.Hour:
                    return GetMinuteOrHour(symbol, resolution, startUtc, endUtc);

                case Resolution.Daily:
                    return GetDaily(symbol, resolution, startUtc, endUtc);

                default:
                    throw new NotSupportedException("Resolution not available: " + resolution);
            }
        }
    }
}
