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
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.GoogleDownloader
{
    class Program
    {
        /// <summary>
        /// QuantConnect Google Downloader For LEAN Algorithmic Trading Engine.
        /// Original by @chrisdk2015, tidied by @jaredbroad
        /// </summary>
        public static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: GoogleDownloader SYMBOL RESOLUTION PERIOD");
                Console.WriteLine("SYMBOL = eg SPY");
                Console.WriteLine("RESOLUTION = Minute/Hour");
                Console.WriteLine("PERIOD = 10 for 10 days intraday data");
                Environment.Exit(1);
            }

            try
            {
                // Load settings from command line
                var symbol = args[0];
                var resolution = (Resolution)Enum.Parse(typeof(Resolution), args[1]);
                var period = args[2].ToInt32();

                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");

                // Download the data
                var downloader = new GoogleDataDownloader();
                var data = downloader.Get(new Symbol(symbol), SecurityType.Equity, resolution, DateTime.UtcNow.AddDays(-period), DateTime.UtcNow);

                // Save the data
                var writer = new LeanDataWriter(SecurityType.Equity, resolution, symbol, dataDirectory, "usa");
                writer.Write(data);
            }
            catch (Exception err)
            {
                Log.Error("GoogleDownloader(): Error: " + err.Message);
            }
        }

    }
}
