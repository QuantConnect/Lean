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
using org.apache.log4j;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.FxcmDownloader
{
    public static class FxcmDownloaderProgram
    {
        /// <summary>
        /// Primary entry point to the program
        /// </summary>
        public static void FxcmDownloader(IList<string> tickers, string resolution, DateTime startDate, DateTime endDate)
        {
            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("FxcmDownloader ERROR: '--tickers=' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg EURUSD,USDJPY");
                Console.WriteLine("--resolution=Second/Minute/Hour/Daily/All");
                Environment.Exit(1);
            }


            try
            {
                Logger.getRootLogger().setLevel(Level.ERROR);
                BasicConfigurator.configure(new FileAppender(new SimpleLayout(), "FxcmDownloader.log", append: false));

                var allResolutions = resolution.ToLowerInvariant() == "all";
                var castResolution = allResolutions ? Resolution.Tick : (Resolution)Enum.Parse(typeof(Resolution), resolution);
                endDate = endDate.AddDays(1).AddMilliseconds(-1);


                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");
                var server = Config.Get("fxcm-server", "http://www.fxcorporate.com/Hosts.jsp");
                var terminal = Config.Get("fxcm-terminal", "Demo");
                var userName = Config.Get("fxcm-user-name", "username");
                var password = Config.Get("fxcm-password", "password");

                // Download the data
                const string market = Market.FXCM;
                var downloader = new FxcmDataDownloader(server, terminal, userName, password);

                foreach (var ticker in tickers)
                {
                    if (!downloader.HasSymbol(ticker))
                        throw new ArgumentException("The ticker " + ticker + " is not available.");
                }

                foreach (var ticker in tickers)
                {
                    var securityType = downloader.GetSecurityType(ticker);
                    var symbol = Symbol.Create(ticker, securityType, market);

                    var data = downloader.Get(symbol, castResolution, startDate, endDate);

                    if (allResolutions)
                    {
                        var ticks = data.Cast<Tick>().ToList();

                        // Save the data (second resolution)
                        var writer = new LeanDataWriter(castResolution, symbol, dataDirectory);
                        writer.Write(ticks);

                        // Save the data (other resolutions)
                        foreach (var res in new[] { Resolution.Second, Resolution.Minute, Resolution.Hour, Resolution.Daily })
                        {
                            var resData = FxcmDataDownloader.AggregateTicks(symbol, ticks, res.ToTimeSpan());

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
