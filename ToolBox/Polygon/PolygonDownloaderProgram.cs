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
using System.Net;
using System.Threading.Tasks;

// QuantConnect
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.Polygon
{
    public class PolygonDownloaderProgram
    {
        /// <summary>
        /// Primary entry point to the program. This program only supports SecurityType.Equity
        /// </summary>
        public static void PolygonDownloader(IList<string> tickers, string securityTypeString, string market, string resolutionString, 
                                             DateTime fromDate, DateTime toDate, string apiName, string apiKey, string apiResultsLimit, string apiDownloadThreads)
        {
            if (tickers.IsNullOrEmpty() || securityTypeString.IsNullOrEmpty() || market.IsNullOrEmpty() || resolutionString.IsNullOrEmpty() || apiName.IsNullOrEmpty() || apiKey.IsNullOrEmpty() || apiResultsLimit.IsNullOrEmpty() || apiDownloadThreads.IsNullOrEmpty())
            {
                Console.WriteLine("PolygonDownloader ERROR: '--tickers=' or '--security-type=' or '--market=' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg SPY,AAPL");
                Console.WriteLine("--security-type=Equity");
                Console.WriteLine("--market=usa");
                Console.WriteLine("--resolution=Minute/Hour/Daily");
                Console.WriteLine("--polygon-api-key=xxxxxxxxxxxxxxxxx");
                Console.WriteLine("--polygon-api-results-limit=10000");
                Console.WriteLine("--polygon-download-threads=24");
                Environment.Exit(1);
            }

            try
            {
                // Convert method parameters to Integer
                int _numberOfApiDownloadThreads = Convert.ToInt32(apiDownloadThreads, System.Globalization.CultureInfo.GetCultureInfo("en-US"));


                // Load settings from command line
                var resolution = (Resolution)Enum.Parse(typeof(Resolution), resolutionString);
                var securityType = (SecurityType)Enum.Parse(typeof(SecurityType), securityTypeString);

                // Polygon.io does not support Crypto historical quotes
                var tickTypes = new List<TickType>();

                // Load settings from config.json
                var dataDirectory = Config.Get("data-folder", Config.Get("data-directory", @"../../../Data/"));

                // ---------------------------------------------------------------------
                // Daily Bars
                //
                // This Polygon API for daily bars is not implemented yet
                // https://polygon.io/docs/get_v1_open-close__stocksTicker___date__anchor
                // If you are working on the daily bars API, please read this !   https://polygon.io/blog/aggs-api-updates/
                // Thank you for working on this project !  : )
                // ---------------------------------------------------------------------


                // Setup the system for the specific API    //TODO: Polygon-> Fix this to be extensible to all of the other APIs 
                if (apiName == "HistoricTrades")
                {
                    if (securityTypeString == "Equity")
                    {
                        tickTypes = new List<TickType> { TickType.Trade };
                    }
                    else
                    {
                        Log.Trace("PolygonDownloader ERROR: --api-name=HistoricTrades requires a parameter --security-type=Equity");
                        Environment.Exit(1);
                    }
                }
                else if (apiName == "HistoricQuotes")
                {
                    if (securityTypeString == "Equity")
                    {
                        tickTypes = new List<TickType> { TickType.Quote };
                    }
                    else
                    {
                        Log.Trace("PolygonDownloader ERROR: --api-name=HistoricQuotes requires a parameter --security-type=Equity");
                        Environment.Exit(1);
                    }
                }
                else if (apiName == "HistoricTradesDailyBars")
                {
                    if (securityTypeString == "Equity")
                    {
                        tickTypes = new List<TickType> { TickType.Trade };
                    }
                    else
                    {
                        Log.Trace("PolygonDownloader ERROR: --api-name=HistoricTradesDailyBars requires a parameter --security-type=Equity");
                        Environment.Exit(1);
                    }
                }
                else
                {
                    Log.Trace("PolygonDownloader ERROR: At this time, API is not implemented. Use --api-name=HistoricTrades, --api-name=HistoricQuotes");
                    Environment.Exit(1);
                }

                // Create an instance of the downloader
                using (var downloader = new PolygonDataDownloader())
                {
                    // Server side parameters for multithreaded simultaneous API connections
                    ServicePointManager.DefaultConnectionLimit = _numberOfApiDownloadThreads;
                    int symbolDownloadThreads = 1;
                    int dateDownloadThreads = 1;

                    // Balance the thread usage;
                    if (tickers.Count >= _numberOfApiDownloadThreads) { symbolDownloadThreads = _numberOfApiDownloadThreads; }
                    else { symbolDownloadThreads = tickers.Count; dateDownloadThreads = (_numberOfApiDownloadThreads - symbolDownloadThreads); }

                    // Parallel iteration of the symbols
                    // Lean classes are NOT thread safe ... don't use Parallel.ForEach here until everything is thread safe
                    //Parallel.ForEach(tickers, new ParallelOptions { MaxDegreeOfParallelism = symbolDownloadThreads }, (ticker) =>
                    foreach (string ticker in tickers)
                    {
                        var symbol = Symbol.Create(ticker, securityType, market);

                        // Get the correct time zones
                        var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
                        var exchangeTimeZone = marketHoursDatabase.GetExchangeHours(market, symbol, securityType).TimeZone;
                        var dataTimeZone = marketHoursDatabase.GetDataTimeZone(market, symbol, securityType);

                        // Set the UTC times for correctness of the architecture
                        var utcStartDate = fromDate.ConvertToUtc(dataTimeZone);  // TimeZones.NewYork);
                        var utcEndDate = toDate.ConvertToUtc(dataTimeZone);  // TimeZones.NewYork);
                        var numberOfDays = (utcEndDate - utcStartDate).TotalDays;

                        // Parallel iteration of the each day to download the data
                        //Parallel.ForEach(Enumerable.Range(0, Convert.ToInt32(numberOfDays)), new ParallelOptions { MaxDegreeOfParallelism = dateDownloadThreads }, i =>
                        // Lean classes are NOT thread safe ... don't use Parallel.ForEach here until everything is thread safe
                        // Behavior is noticed in Tick.cs which is used for downloading data and also backtesting .. perhaps this should be abstracted separately
                        for (int i=0; i <= numberOfDays; i++)
                        {
                            foreach (var tickType in tickTypes)
                            {
                                // Download the data
                                var data = downloader.Get(symbol, resolution, utcStartDate.AddDays(i), utcStartDate.AddDays(i + 1), tickType 
                                                          ,apiKey,apiResultsLimit,apiDownloadThreads).ToArray();

                                if ((data != null) && (data.Length != 0))
                                {
                                    // Save the data to file system
                                    var writer = new LeanDataWriter(resolution, symbol, dataDirectory, tickType);
                                    writer.Write(data);
                                }
                            }

                        }
                        //);   // Time ParallelForEach for loop ending

                    }
                    //);      // Symbol Parallel ParallelForEach loop ending
                }

            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }
    }
}
