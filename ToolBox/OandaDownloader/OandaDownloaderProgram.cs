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
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.OandaDownloader
{
    public static class OandaDownloaderProgram
    {
        /// <summary>
        /// Primary entry point to the program
        /// </summary>
        public static void OandaDownloader(IList<string> tickers, string resolution, DateTime startDate, DateTime endDate)
        {
            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("OandaDownloader ERROR: '--tickers=' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg EURUSD,USDJPY");
                Console.WriteLine("--resolution=Second/Minute/Hour/Daily/All");
                Environment.Exit(1);
            }

            try
            {
                // Load settings from command line
                var allResolutions = resolution.ToLowerInvariant() == "all";
                var castResolution = allResolutions ? Resolution.Second : (Resolution)Enum.Parse(typeof(Resolution), resolution);

                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");
                var accessToken = Config.Get("access-token", "73eba38ad5b44778f9a0c0fec1a66ed1-44f47f052c897b3e1e7f24196bbc071f");
                var accountId = Config.Get("account-id", "621396");

                // Create an instance of the downloader
                const string market = Market.Oanda;
                var downloader = new OandaDataDownloader(accessToken, accountId);

                foreach (var ticker in tickers)
                {
                    if (!downloader.HasSymbol(ticker))
                        throw new ArgumentException("The ticker " + ticker + " is not available.");
                }

                foreach (var ticker in tickers)
                {
                    // Download the data
                    var securityType = downloader.GetSecurityType(ticker);
                    var symbol = Symbol.Create(ticker, securityType, market);

                    var data = downloader.Get(symbol, castResolution, startDate, endDate);

                    if (allResolutions)
                    {
                        var bars = data.Cast<QuoteBar>().ToList();

                        // Save the data (second resolution)
                        var writer = new LeanDataWriter(castResolution, symbol, dataDirectory);
                        writer.Write(bars);

                        // Save the data (other resolutions)
                        foreach (var res in new[] { Resolution.Minute, Resolution.Hour, Resolution.Daily })
                        {
                            var resData = downloader.AggregateBars(symbol, bars, res.ToTimeSpan());

                            writer = new LeanDataWriter(res, symbol, dataDirectory);
                            writer.Write(resData);
                        }
                    }
                    else
                    {
                        // Save the data (single resolution)
                        var writer = new LeanDataWriter(castResolution, symbol, dataDirectory);
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
