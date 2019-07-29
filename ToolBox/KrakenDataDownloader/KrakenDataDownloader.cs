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
using System.Globalization;
using QuantConnect.Data;
using System.Net;
using Newtonsoft.Json;
using QuantConnect.Data.Market;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.KrakenDownloader
{
    /// <summary>
    /// Kraken Data Downloader class
    /// </summary>
    public class KrakenDataDownloader : IDataDownloader
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

            var startUnixTime = Convert.ToInt64(Time.DateTimeToUnixTimeStamp(startUtc) * 1000000000); // Multiply by 10^9 per Kraken API
            var endUnixTime = Convert.ToInt64(Time.DateTimeToUnixTimeStamp(endUtc) * 1000000000);
            var url = string.Format(CultureInfo.InvariantCulture, UrlPrototype, symbol.Value, startUnixTime);
            List<List<string>> data;

            using (var client = new WebClient())
            {
                var rateGate = new RateGate(10, TimeSpan.FromMinutes(1)); // 10 calls per minute for Kraken API

                rateGate.WaitToProceed();
                var response = client.DownloadString(url);
                dynamic result = JsonConvert.DeserializeObject<dynamic>(response);
                if (result.error.Count != 0)
                {
                    throw new Exception("Error in Kraken API: " + result.error[0]);
                }

                data = result.result[symbol.Value].ToObject<List<List<string>>>();

                foreach (var i in data)
                {
                    var time = Time.UnixTimeStampToDateTime(Parse.Double(i[2].Split('.')[0]));
                    if (time > endUtc)
                    {
                        break;
                    }

                    var value = Parse.Decimal(i[0]);
                    var volume = Parse.Decimal(i[1]);

                    yield return new Tick
                    {
                        Value = value,
                        Time = time,
                        DataType = MarketDataType.Tick,
                        Symbol = symbol,
                        TickType = TickType.Trade,
                        Quantity = volume,
                        Exchange = "kraken"
                    };
                }

                var last = Convert.ToInt64(result.result.last);
                while (last < endUnixTime)
                {
                    url = string.Format(UrlPrototype, symbol.Value, last);

                    rateGate.WaitToProceed();
                    response = client.DownloadString(url);
                    result = JsonConvert.DeserializeObject<dynamic>(response);

                    var errorCount = 0;
                    while (result.error.Count != 0 && errorCount < 10)
                    {
                        errorCount++;
                        rateGate.WaitToProceed();
                        response = client.DownloadString(url);
                        result = JsonConvert.DeserializeObject<dynamic>(response);
                    }

                    if (result.error.Count != 0 && errorCount >= 10)
                    {
                        throw new Exception("Error in Kraken API: " + result.error[0]);
                    }

                    data = result.result[symbol.Value].ToObject<List<List<string>>>();

                    foreach (var i in data)
                    {
                        var time = Time.UnixTimeStampToDateTime(Parse.Double(i[2].Split('.')[0]));
                        if (time > endUtc)
                        {
                            break;
                        }

                        var value = Parse.Decimal(i[0]);
                        var volume = Parse.Decimal(i[1]);

                        yield return new Tick
                        {
                            Value = value,
                            Time = time,
                            DataType = MarketDataType.Tick,
                            Symbol = symbol,
                            TickType = TickType.Trade,
                            Quantity = volume,
                            Exchange = "kraken"
                        };
                    }

                    last = Convert.ToInt64(result.result.last);
                }
            }
        }
    }
}
