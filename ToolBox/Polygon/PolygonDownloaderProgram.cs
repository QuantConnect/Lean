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

//QuantConnect
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
        public static void PolygonDownloader(IList<string> tickers, string securityTypeString, string market, string resolutionString, DateTime fromDate, DateTime toDate)
        {
            if (tickers.IsNullOrEmpty() || securityTypeString.IsNullOrEmpty() || market.IsNullOrEmpty() || resolutionString.IsNullOrEmpty())
            {
                Console.WriteLine("PolygonDownloader ERROR: '--tickers=' or '--security-type=' or '--market=' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg SPY,AAPL");
                Console.WriteLine("--security-type=Equity");
                Console.WriteLine("--market=usa");
                Console.WriteLine("--resolution=Minute/Hour/Daily");
                Environment.Exit(1);
            }

            try
            {
                // Load settings from command line
                var resolution = (Resolution)Enum.Parse(typeof(Resolution), resolutionString);
                var securityType = (SecurityType)Enum.Parse(typeof(SecurityType), securityTypeString);

                // Polygon.io does not support Crypto historical quotes
                var tickTypes = securityType == SecurityType.Crypto 
                    ? new List<TickType> { TickType.Trade } 
                    : SubscriptionManager.DefaultDataTypes()[securityType];

                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");

                // Get hours
                var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

                // Create an instance of the downloader
                using (var downloader = new PolygonDataDownloader())
                {
                    // Server side parameters for multithreaded simultaneous API connections
                    ServicePointManager.DefaultConnectionLimit = 4;

                    foreach (var ticker in tickers)
                    {
                        var symbol = Symbol.Create(ticker, securityType, market);
                        
                        // Get the correct time zones
                        var exchangeTimeZone = marketHoursDatabase.GetExchangeHours(market, symbol, securityType).TimeZone;
                        var dataTimeZone = marketHoursDatabase.GetDataTimeZone(market, symbol, securityType);

                        // Set the UTC times for correctness of the architecture
                        var utcStartDate = fromDate.ConvertToUtc(dataTimeZone);  // TimeZones.NewYork);
                        var utcEndDate = toDate.ConvertToUtc(dataTimeZone);  // TimeZones.NewYork);
                        var numberOfDays = (utcEndDate - utcStartDate).TotalDays;

                        // Iterate through each day, which is the Polygon.IO api architecture
                        for (int i = 0; i <= numberOfDays; i++)
                        {
                            foreach (var tickType in tickTypes)
                            {
                                // Download the data
                                var data = downloader.Get(symbol, resolution, utcStartDate.AddDays(i), utcEndDate.AddDays(i + 1), tickType)
                                    .Select(x =>
                                        {
                                            x.Time = x.Time.ConvertTo(exchangeTimeZone, dataTimeZone);
                                            return x;
                                        }
                                    );

                                if ((data != null))
                                {
                                    // Save the data
                                    var writer = new LeanDataWriter(resolution, symbol, dataDirectory, tickType);
                                    writer.Write(data);
                                }
                            }
                        }
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
