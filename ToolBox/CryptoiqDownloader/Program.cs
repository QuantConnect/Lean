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
using System.Globalization;
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.CryptoiqDownloader
{
    class Program
    {
        /// <summary>
        /// Cryptoiq Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length == 3)
            {
                args = new [] { args[0], DateTime.UtcNow.ToString("yyyyMMdd"), args[1], args[2] };
            }
            else if (args.Length < 4)
            {
                Console.WriteLine("Usage: CryptoiqDownloader FROMDATE TODATE EXCHANGE SYMBOL");
                Console.WriteLine("FROMDATE = yyyymmdd");
                Console.WriteLine("TODATE = yyyymmdd");
                Environment.Exit(1);
            }

            try
            {
                // Load settings from command line
                var startDate = DateTime.ParseExact(args[0], "yyyyMMdd", CultureInfo.InvariantCulture);
                var endDate = DateTime.ParseExact(args[1], "yyyyMMdd", CultureInfo.InvariantCulture);

                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");
                //todo: will download any exchange but always save as gdax
                // Create an instance of the downloader
                const string market = Market.GDAX;
                var downloader = new CryptoiqDownloader(args[2]);

                // Download the data
                var symbolObject = Symbol.Create(args[3], SecurityType.Crypto, market);
                var data = downloader.Get(symbolObject, Resolution.Tick, startDate, endDate);

                // Save the data
                var writer = new LeanDataWriter(Resolution.Tick, symbolObject, dataDirectory, TickType.Quote);
                writer.Write(data);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }
    }
}