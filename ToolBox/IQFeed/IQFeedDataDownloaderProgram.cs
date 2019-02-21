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
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.IQFeed
{
    public class IQFeedDataDownloaderProgram
    {
        /// <summary>
        /// Primary entry point to the program.
        /// </summary>
        public static void IQFeedDataDownloaderProgramConverter(IList<string> tickers, string resolution, DateTime fromDate,
            DateTime toDate)
        {
            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("IQFeedDataDownloaderProgram ERROR: '--tickers=' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg BTCUSD");
                Console.WriteLine("--resolution=Second/Minute/Hour/Daily/All");
                Environment.Exit(1);
            }

            try
            {
                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");

                // Create an instance of the downloader
                var downloader = new IQFeedDataDownloader();
                foreach (var ticker in tickers)
                {
                    // Download the data
                    var symbol = downloader.GetSymbol(ticker);
                    var data = downloader.Get(symbol, Resolution.Daily, fromDate, DateTime.UtcNow);

                    // Save the data
                    var writer = new LeanDataWriter(Resolution.Daily, symbol, dataDirectory, TickType.Quote);
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
