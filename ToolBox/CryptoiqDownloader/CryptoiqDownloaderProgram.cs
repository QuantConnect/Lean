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
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.CryptoiqDownloader
{
    public static class CryptoiqDownloaderProgram
    {
        /// <summary>
        /// Cryptoiq Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// </summary>
        public static void CryptoiqDownloader(IList<string> tickers, string exchange, DateTime startDate, DateTime endDate)
        {
            if (exchange.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("CryptoiqDownloader ERROR: '--exchange=' or '--tickers=' parameter is missing");
                Environment.Exit(1);
            }

            try
            {
                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");
                //todo: will download any exchange but always save as gdax
                // Create an instance of the downloader
                const string market = Market.GDAX;
                var downloader = new CryptoiqDownloader(exchange);

                foreach (var ticker in tickers)
                {
                    // Download the data
                    var symbolObject = Symbol.Create(ticker, SecurityType.Crypto, market);
                    var data = downloader.Get(symbolObject, Resolution.Tick, startDate, endDate);

                    // Save the data
                    var writer = new LeanDataWriter(Resolution.Tick, symbolObject, dataDirectory, TickType.Quote);
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