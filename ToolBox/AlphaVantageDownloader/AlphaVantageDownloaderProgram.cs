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
using QuantConnect.Util;
using QuantConnect.Data;
using QuantConnect.Logging;
using System.Collections.Generic;

namespace QuantConnect.ToolBox.AlphaVantageDownloader
{
    public static class AlphaVantageDownloaderProgram
    {
        /// <summary>
        /// Primary entry point to the program.
        /// </summary>
        public static void AlphaVantageDownloader(List<string> tickers, string resolution, DateTime fromDate, DateTime toDate, string apiKey)
        {
            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("AlphaVantageDownloader ERROR: '--tickers=' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg SPY,AAPL");
                Console.WriteLine("--resolution=Minute/Hour/Daily");
                Environment.Exit(1);
            }
            try
            {
                var castResolution = (Resolution)Enum.Parse(typeof(Resolution), resolution);
                var startDate = fromDate.ConvertToUtc(TimeZones.NewYork);
                var endDate = toDate.ConvertToUtc(TimeZones.NewYork);

                // fix end date
                endDate = new DateTime(Math.Min(endDate.Ticks, DateTime.Now.AddDays(-1).Ticks));

                using var downloader = new AlphaVantageDataDownloader(apiKey);
                foreach (var ticker in tickers)
                {
                    // Download the data
                    var symbol = Symbol.Create(ticker, SecurityType.Equity, Market.USA);
                    var data = downloader.Get(new DataDownloaderGetParameters(symbol, castResolution, startDate, endDate));

                    // Save the data
                    var writer = new LeanDataWriter(castResolution, symbol, Globals.DataFolder);
                    writer.Write(data);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }
    }
}
