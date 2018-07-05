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
using System.Globalization;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System.Threading;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.GDAXDownloader
{
    public static class GDAXDownloaderProgram
    {
        /// <summary>
        /// GDAX Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// </summary>
        public static void GDAXDownloader(IList<string> tickers, string resolution, DateTime fromDate, DateTime toDate)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("GDAXDownloader ERROR: '--tickers=' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=ETH-USD,ETH-BTC,BTC-USD,etc.");
                Console.WriteLine("--resolution=Second/Minute/Hour/Daily");
                Environment.Exit(1);
            }
            var castResolution = (Resolution) Enum.Parse(typeof(Resolution), resolution);
            try
            {
                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");
                //todo: will download any exchange but always save as gdax
                // Create an instance of the downloader
                const string market = Market.GDAX;
                var downloader = new GDAXDownloader();
                foreach (var ticker in tickers)
                {
                    // Download the data
                    var symbolObject = Symbol.Create(ticker, SecurityType.Crypto, market);
                    var data = downloader.Get(symbolObject, castResolution, fromDate, toDate);

                    // Save the data
                    var writer = new LeanDataWriter(castResolution, symbolObject, dataDirectory, TickType.Trade);
                    var distinctData = data.GroupBy(i => i.Time, (key, group) => group.First()).ToArray();

                    writer.Write(distinctData);
                }

                Log.Trace("Finish data download. Press any key to continue..");

            }
            catch (Exception err)
            {
                Log.Error(err);
                Log.Trace(err.Message);
                Log.Trace(err.StackTrace);
            }
            Console.ReadLine();
        }
    }
}
