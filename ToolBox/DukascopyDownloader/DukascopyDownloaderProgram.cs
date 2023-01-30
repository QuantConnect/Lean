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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Configuration;

namespace QuantConnect.ToolBox.DukascopyDownloader
{
    public static class DukascopyDownloaderProgram
    {
        /// <summary>
        /// Primary entry point to the program
        /// </summary>
        public static void DukascopyDownloader(IList<string> tickers, string resolution, DateTime startDate, DateTime endDate)
        {
            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("DukascopyDownloader ERROR: '--tickers=' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg EURUSD,USDJPY");
                Console.WriteLine("--resolution=Tick/Second/Minute/Hour/Daily/All");
                Environment.Exit(1);
            }

            try
            {
                // Load settings from command line
                var allResolutions = resolution.ToLowerInvariant() == "all";
                var castResolution = allResolutions ? Resolution.Tick : (Resolution)Enum.Parse(typeof(Resolution), resolution);

                // Load settings from config.json
                var dataDirectory = "../../../Data";

                // Download the data
                var downloader = new DukascopyDataDownloader();

                foreach (var ticker in tickers)
                {
                    if (!downloader.HasSymbol(ticker))
                        throw new ArgumentException("The ticker " + ticker + " is not available.");
                }

                foreach (var ticker in tickers)
                {
                    var securityType = downloader.GetSecurityType(ticker);
                    var symbolObject = Symbol.Create(ticker, securityType, Market.Dukascopy);
                    var data = downloader.Get(new DataDownloaderGetParameters(symbolObject, castResolution, startDate, endDate, TickType.Quote));

                    if (allResolutions)
                    {
                        var ticks = data.Cast<Tick>().ToList();

                        // Save the data (tick resolution)
                        var writer = new LeanDataWriter(castResolution, symbolObject, dataDirectory);
                        writer.Write(ticks);

                        // Save the data (other resolutions)
                        foreach (var res in new[] { Resolution.Second, Resolution.Minute, Resolution.Hour, Resolution.Daily })
                        {
                            var resData = LeanData.AggregateTicks(ticks, symbolObject, res.ToTimeSpan());

                            writer = new LeanDataWriter(res, symbolObject, dataDirectory);
                            writer.Write(resData);
                        }
                    }
                    else
                    {
                        // Save the data (single resolution)
                        var writer = new LeanDataWriter(castResolution, symbolObject, dataDirectory);
                        writer.Write(data);
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }
    }
}
