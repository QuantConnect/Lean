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

namespace QuantConnect.ToolBox.DukascopyDownloader
{
    class Program
    {
        /// <summary>
        /// Primary entry point to the program
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: DukascopyDownloader SYMBOL RESOLUTION FROMDATE TODATE");
                Console.WriteLine("SYMBOL = eg EURUSD");
                Console.WriteLine("RESOLUTION = Tick/Second/Minute/Hour/Daily");
                Console.WriteLine("FROMDATE = yyyymmdd");
                Console.WriteLine("TODATE = yyyymmdd");
                Environment.Exit(1);
            }

            try
            {
                // Load settings from command line
                var symbol = args[0];
                var resolution = (Resolution)Enum.Parse(typeof(Resolution), args[1]);
                var startDate = DateTime.ParseExact(args[2], "yyyyMMdd", CultureInfo.InvariantCulture);
                var endDate = DateTime.ParseExact(args[3], "yyyyMMdd", CultureInfo.InvariantCulture);

                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");

                // Download the data
                var downloader = new DukascopyDataDownloader();

                if (!downloader.HasSymbol(symbol))
                    throw new ArgumentException("The symbol " + symbol + " is not available.");

                var securityType = downloader.GetSecurityType(symbol);
                var data = downloader.Get(new Symbol(symbol), securityType, resolution, startDate, endDate);

                // Save the data
                var writer = new LeanDataWriter(securityType, resolution, symbol, dataDirectory, "dukascopy");
                writer.Write(data);
            }
            catch (Exception err)
            {
                Log.Error("DukascopyDownloader(): {0}", err.Message);
            }
        }

    }
}
