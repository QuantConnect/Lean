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
using System.Globalization;
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.KrakenDownloader
{
    class Program
    {
        /// <summary>
        /// Kraken Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// By @matthewsedam
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: KrakenDownloader PAIRS RESOLUTION FROMDATE TODATE");
                Console.WriteLine("SYMBOLS = eg XXBTZUSD,XETHZUSD");
                Console.WriteLine("RESOLUTION = Minute/Hour/Daily/Tick");
                Console.WriteLine("FROMDATE = yyyymmdd");
                Console.WriteLine("TODATE = yyyymmdd");
                Environment.Exit(1);
            }

            try
            {
                var pairs = args[0].Split(',');
                var resolution = (Resolution)Enum.Parse(typeof(Resolution), args[1]);
                var startDate = DateTime.ParseExact(args[2], "yyyyMMdd", CultureInfo.InvariantCulture);
                var endDate = DateTime.ParseExact(args[3], "yyyyMMdd", CultureInfo.InvariantCulture);

                // Load settings from config.json and create downloader
                var dataDirectory = Config.Get("data-directory", "../../../Data");
                var downloader = new KrakenDataDownloader();

                foreach (var pair in pairs)
                {
                    // Download data
                    var pairObject = Symbol.Create(pair, SecurityType.Crypto, Market.Kraken);
                    var data = downloader.Get(pairObject, resolution, startDate, endDate);

                    // Write data
                    var writer = new LeanDataWriter(resolution, pairObject, dataDirectory);
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
