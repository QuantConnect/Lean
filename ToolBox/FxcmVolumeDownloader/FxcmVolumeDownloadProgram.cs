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
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.FxcmVolumeDownload
{
    public static class FxcmVolumeDownloadProgram
    {
        public static void FxcmVolumeDownload(IList<string> tickers, string resolution, DateTime startDate, DateTime endDate)
        {
            var isUpdate = false;
            if (resolution.IsNullOrEmpty())
            {
                if (!tickers.IsNullOrEmpty())
                {
                    var _tickers = tickers.First().ToLowerInvariant();
                    if (_tickers == "all" || _tickers == "update")
                    {
                        if (_tickers == "update")
                        {
                            isUpdate = true;
                        }

                        tickers = new List<string> { "EURUSD","USDJPY","GBPUSD","USDCHF","EURCHF","AUDUSD","USDCAD",
                                "NZDUSD","EURGBP","EURJPY","GBPJPY","EURAUD","EURCAD","AUDJPY" };
                        resolution = "all";
                    }
                }
                else
                {
                    Console.WriteLine("Usage:\n\t" +
                                      "FxcmVolumeDownloader all\t will download data for all available pair for the three resolutions.\n\t" +
                                      "FxcmVolumeDownloader update\t will download just last day data for all pair and resolutions already downloaded.");
                    Console.WriteLine("Usage: FxcmVolumeDownloader --tickers= --resolution= --from-date= --to-date=");
                    Console.WriteLine("--tickers=eg EURUSD,USDJPY\n" +
                                      "\tAvailable pairs:\n" +
                                      "\tEURUSD, USDJPY, GBPUSD, USDCHF, EURCHF, AUDUSD, USDCAD,\n" +
                                      "\tNZDUSD, EURGBP, EURJPY, GBPJPY, EURAUD, EURCAD, AUDJPY");
                    Console.WriteLine("--resolution=Minute/Hour/Daily/All");
                    Environment.Exit(exitCode: 1);
                }
            }

            try
            {
                Log.DebuggingEnabled = true;
                Log.LogHandler = new CompositeLogHandler(new ConsoleLogHandler(), new FileLogHandler("FxcmFxVolumeDownloader.log", useTimestampPrefix: false));

                var resolutions = new[] { Resolution.Daily };
                if (resolution.ToLowerInvariant() == "all")
                {
                    resolutions = new[] { Resolution.Daily, Resolution.Hour, Resolution.Minute };
                }
                else
                {
                    resolutions[0] = (Resolution)Enum.Parse(typeof(Resolution), resolution);
                }

                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");

                var downloader = new FxcmVolumeDownloader(dataDirectory);
                foreach (var ticker in tickers)
                {
                    var symbol = Symbol.Create(ticker, SecurityType.Base, Market.FXCM);
                    foreach (var _resolution in resolutions)
                    {
                        downloader.Run(symbol, _resolution, startDate, endDate, isUpdate);
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