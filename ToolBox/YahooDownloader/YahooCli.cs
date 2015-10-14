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

namespace QuantConnect.ToolBox.YahooDownloader
{
    class YahooCli
    {
        /// <summary>
        /// Yahoo Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// Original by @chrisdk2015, tidied by @jaredbroad
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: YahooDownloader SYMBOL");
                Console.WriteLine("Usage: Place the data into your LEAN Data directory: /data/equity/usa/daily/SYMBOL.zip");
                Console.WriteLine("SYMBOL = eg SPY");
                Environment.Exit(1);
            }

            //Command line inputs: symbol
            var symbol = args[0];
            var dataDirectory = Config.Get("data-directory", "../../../Data");

            try
            {
                //Get Yahoo Downloader:
                var yahooDownloader = new YahooDataDownloader();
                var enumerableYahoo = yahooDownloader.Get(new Symbol(symbol), SecurityType.Equity, Resolution.Daily, DateTime.MinValue, DateTime.UtcNow);

                //Get LEAN Data Writer:
                var writer = new LeanDataWriter(SecurityType.Equity, Resolution.Daily, symbol, dataDirectory, "usa");
                writer.Write(enumerableYahoo);
            }
            catch (Exception err)
            {
                Log.Error("YahooDownloader(): Error: " + err.Message);
            }
        }
    }
}
