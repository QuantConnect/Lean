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
using System.Linq;
using System.Globalization;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System.Threading;

namespace QuantConnect.ToolBox.GDAXDownloader
{
    class Program
    {
        /// <summary>
        /// GDAX Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// </summary>
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            if (args.Length < 3)
            {
                Console.WriteLine("Usage: GDAX Downloader SYMBOL RESOLUTION FROMDATE TODATE");
                Console.WriteLine("SYMBOL   = ETH-USD, ETH-BTC, BTC-USD etc.");
                Console.WriteLine("RESOLUTION   = Second/Minute/Hour/Daily");
                Console.WriteLine("FROMDATE = yyyyMMdd HH:mm:ss");
                Console.WriteLine("TODATE = yyyyMMdd HH:mm:ss");
                Environment.Exit(1);
            }

            try
            {
                var resolution = (Resolution)Enum.Parse(typeof(Resolution), args[1]);
                // Load settings from command line
                var startDate = DateTime.ParseExact(args[2], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
                var endDate = DateTime.UtcNow;
                if (args[3] != null)
                {
                    endDate = DateTime.ParseExact(args[3], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
                }

                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");
                //todo: will download any exchange but always save as gdax
                // Create an instance of the downloader
                const string market = Market.GDAX;
                var downloader = new GDAXDownloader();

                // Download the data
                var symbolObject = Symbol.Create(args[0], SecurityType.Crypto, market);
                var data = downloader.Get(symbolObject, resolution, startDate, endDate);

                // Save the data

                var writer = new LeanDataWriter(resolution, symbolObject, dataDirectory, TickType.Trade);
                var distinctData = data.GroupBy(i => i.Time, (key, group) => group.First()).ToArray();

                writer.Write(distinctData);

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
