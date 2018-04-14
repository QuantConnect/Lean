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
using System.Linq;
using System.Threading;
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.IBDownloader
{
    class Program
    {
        /// <summary>
        /// Primary entry point to the program. This program only supports FOREX for now.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length != 5)
            {
                Console.WriteLine("Usage: QuantConnect.ToolBox SYMBOLS RESOLUTION FROMDATE TODATE");
                Console.WriteLine("SYMBOLS = eg EURUSD,USDJPY");
                Console.WriteLine("RESOLUTION = Second/Minute/Hour/Daily/All");
                Console.WriteLine("FROMDATE = yyyymmdd");
                Console.WriteLine("TODATE = yyyymmdd");
                Environment.Exit(1);
            }
            try
            {
                // Load settings from command line
                var tickers = args[1].Split(',');
                var allResolutions = args[2].ToLower() == "all";
                var resolution = allResolutions ? Resolution.Second : (Resolution)Enum.Parse(typeof(Resolution), args[2]);
                var startDate = DateTime.ParseExact(args[3], "yyyyMMdd", CultureInfo.InvariantCulture).ConvertToUtc(TimeZones.NewYork);
                var endDate = DateTime.ParseExact(args[4], "yyyyMMdd", CultureInfo.InvariantCulture).ConvertToUtc(TimeZones.NewYork);

                // fix end date 
                endDate = new DateTime(Math.Min(endDate.Ticks, DateTime.Now.AddDays(-1).Ticks));

                // Max number of histoy days
                int maxDays = 1;
                if (!allResolutions)
                {
                    switch (resolution)
                    {
                        case Resolution.Daily:
                            maxDays = 365;
                            break;
                        case Resolution.Hour:
                            maxDays = 30;
                            break;
                        case Resolution.Minute:
                            maxDays = 10;
                            break;
                    }
                }

                // Load settings from config.json
                var dataDirectory = Config.Get("data-folder", "../../../Data");

                // Create IB Broker Gateway Runner
                InteractiveBrokersGatewayRunner.StartFromConfiguration();

                // Only FOREX for now
                SecurityType securityType = SecurityType.Forex;
                string market = Market.FXCM; 


                using (var downloader = new IBDataDownloader())
                {
                    foreach (var ticker in tickers)
                    {
                        // Download the data
                        var symbol = Symbol.Create(ticker, securityType, market);

                        var auxEndDate = startDate.AddDays(maxDays); 
                        auxEndDate = new DateTime(Math.Min(auxEndDate.Ticks, endDate.Ticks));

                        while (startDate < auxEndDate)
                        {
                            var data = downloader.Get(symbol, resolution, startDate, auxEndDate);
                            var bars = data.Cast<QuoteBar>().ToList();

                            if (allResolutions)
                            {
                                // Save the data (second resolution)
                                var writer = new LeanDataWriter(resolution, symbol, dataDirectory);
                                writer.Write(bars);

                                // Save the data (other resolutions)
                                foreach (var res in new[] { Resolution.Minute, Resolution.Hour, Resolution.Daily })
                                {
                                    var resData = downloader.AggregateBars(symbol, bars, res.ToTimeSpan());

                                    writer = new LeanDataWriter(res, symbol, dataDirectory);
                                    writer.Write(resData);
                                }
                            }
                            else
                            {
                                // Save the data (single resolution)
                                var writer = new LeanDataWriter(resolution, symbol, dataDirectory);
                                writer.Write(data);
                            }

                            startDate  = auxEndDate;
                            auxEndDate = auxEndDate.AddDays(maxDays);
                            auxEndDate = new DateTime(Math.Min(auxEndDate.Ticks, endDate.Ticks));
                        }
                    }

                }

            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            finally
            {

                if (InteractiveBrokersGatewayRunner.IsRunning())
                {
                    InteractiveBrokersGatewayRunner.Stop();
                }
            }

        }
    }
}
