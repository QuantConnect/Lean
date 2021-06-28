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
using QuantConnect.Brokerages.TDAmeritrade;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.TDAmeritradeDownloader
{
    public static class TDAmeritradeDownloaderProgram
    {
        /// <summary>
        /// Kraken Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// By @matthewsedam
        /// </summary>
        public static void TDAmeritradeDownloader(IList<string> tickers, string resolution, DateTime startDate, DateTime endDate, string securityType)
        {
            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty() || securityType.IsNullOrEmpty())
            {
                Console.WriteLine("TDAmeritradeDownloader ERROR: '--tickers=' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg XXBTZUSD,XETHZUSD");
                Console.WriteLine("--resolution=Minute/Hour/Daily");
                Environment.Exit(1);
            }

            try
            {
                var castResolution = (Resolution)Enum.Parse(typeof(Resolution), resolution);
                var castSecurityType = (SecurityType)Enum.Parse(typeof(SecurityType), securityType);

                // Load settings from config.json and create downloader
                var dataDirectory = Config.Get("data-directory", "../../../Data");

                var downloader = new TDAmeritradeDataDownloader();
                var symbolMapper = new TDAmeritradeSymbolMapper();

                foreach (var ticker in tickers)
                {
                    // Download data
                    var pairObject = symbolMapper.GetLeanSymbol(ticker, castSecurityType, Market.USA);
                    var data = downloader.Get(pairObject, castResolution, startDate, endDate);

                    // Write data
                    var writer = new LeanDataWriter(castResolution, pairObject, dataDirectory);
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
