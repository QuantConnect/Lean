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
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Logging;
using QuantConnect.Util;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using System.Linq;

namespace QuantConnect.ToolBox.IEX
{
    public static class IEXDownloaderProgram
    {
        /// <summary>
        /// Primary entry point to the program. This program only supports FOREX for now.
        /// </summary>
        public static void IEXDownloader(IList<string> tickers, string resolution, DateTime fromDate, DateTime toDate, string apiKey)
        {
            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("IEXDownloader ERROR: '--tickers=' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg SPY,AAPL");
                Console.WriteLine("--resolution=Minute/Daily");
                Environment.Exit(1);
            }
            if (apiKey.IsNullOrEmpty())
            {
                Console.WriteLine("QuandlBitfinexDownloader ERROR: '--api-key=' parameter is missing");
                Environment.Exit(1);
            }

            try
            {
                // Load settings from command line
                var castResolution = (Resolution)Enum.Parse(typeof(Resolution), resolution);

                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");
                var startDate = fromDate.ConvertToUtc(TimeZones.NewYork);
                var endDate = toDate.ConvertToUtc(TimeZones.NewYork);
                
                // Create an instance of the downloader
                const string market = Market.USA;
                SecurityType securityType = SecurityType.Equity;

                using (var downloader = new IEXDataDownloader(apiKey = apiKey))
                {
                    foreach (var ticker in tickers)
                    {
                        // Download the data
                        var symbolObject = Symbol.Create(ticker, securityType, market);
                        var data = downloader.Get(symbolObject, castResolution, startDate, endDate);

                        // Save the data
                        var writer = new LeanDataWriter(castResolution, symbolObject, dataDirectory);
                        writer.Write(data);
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);       
            }
            Console.ReadLine();
        }
    }
}
