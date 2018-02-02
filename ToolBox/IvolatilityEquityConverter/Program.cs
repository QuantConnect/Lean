/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * 
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
using System.Globalization;
using System.IO;
using System.Linq;
using ikvm.extensions;
using QuantConnect.Data.Market;

namespace QuantConnect.ToolBox.IvolatilityEquityConverter
{
    public class Program
    {
        const int TimeField = 0;
        private const int PriceBidField = 5;
        private const int SizeBidField = 11;
        private const int PriceAskField = 6;
        private const int SizeAskField = 12;

        private const int PriceLastField = 7;

//        private const int SizeLastField = 13;
        private const int VolumeField = 17;

        static string _destinationDirectory = "";
        static string _sourceDirectory;
        static Resolution _resolution = Resolution.Minute;
        static readonly string[] Separators = {","};

        public static void Main(string[] args)
        {
            Console.WriteLine("QuantConnect.ToolBox: IVolatility Converter: ");
            Console.WriteLine("==============================================");
            Console.WriteLine(
                "The IVolatility converter transforms IVolatility orders into the LEAN Algorithmic Trading Engine Data Format.");
            Console.WriteLine("Two parameters are required: ");
            Console.WriteLine("   1> Source archived IVolatility Data.");
            Console.WriteLine("   2> Destination Directory of LEAN Data Folder. (Typically located under Lean/Data)");
            Console.WriteLine("   3> Resolution of your IVolatility data. (min,hour,day)");
            Console.WriteLine(" ");
            Console.WriteLine("NOTE: THIS WILL OVERWRITE ANY EXISTING FILES.");

            if (args.Length == 3)
            {
                _sourceDirectory = args[0];
                _destinationDirectory = args[1];
                _resolution = ParseResolution(args[2]);
            }
            else
            {
                Console.WriteLine("1. Ivolatility equity data source directory: ");
                _sourceDirectory = (Console.ReadLine() ?? "");
                Console.WriteLine("2. Destination LEAN Data directory: ");
                _destinationDirectory = (Console.ReadLine() ?? "");
                Console.WriteLine("3. Enter resolution of source data (min, hour, day): ");
                _resolution = ParseResolution(Console.ReadLine() ?? "");
            }

            //Count the total files to process:
            Console.WriteLine("Counting Files..." + _sourceDirectory);
            var count = 1;
            var files = GetFiles(_sourceDirectory);
            Console.WriteLine("Processing {0} Files ...", files.Length);

            foreach (var file in files)
            {
                Console.WriteLine("Processing {0} of {1} files ...", count, files.Length);
                var symbol = GetSymbol(file);
                var streamProvider = StreamProvider.ForExtension(Path.GetExtension(file));
                var inputStream = streamProvider.Open(file).First();
                var streamReader = new StreamReader(inputStream);
                var datawriter = new LeanDataWriter(_resolution, symbol, _destinationDirectory);
                IList<TradeBar> fileBars = new List<TradeBar>();
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    var linearray = ParseCsv(line);
                    if (linearray.Length <= 2) continue;
                    var time = DateTime.ParseExact(linearray[TimeField], DateFormat.UI,
                        CultureInfo.InvariantCulture);
                    var priceBid = Decimal.Parse(linearray[PriceBidField]);
                    var sizeBid = Decimal.Parse(linearray[SizeBidField]);
                    var priceAsk = Decimal.Parse(linearray[PriceAskField]);
                    var sizeAsk = Decimal.Parse(linearray[SizeAskField]);
                    var priceLast = Decimal.Parse(linearray[PriceLastField]);
                    //var sizeLast = Decimal.Parse(linearray[sizeLastField]);
                    var volume = Decimal.Parse(linearray[VolumeField]);
                    var tradeBar = new TradeBar(time, symbol, priceLast, priceLast, priceLast, priceLast, 0);
                    tradeBar.Update(priceLast, priceBid, priceAsk, volume, sizeBid, sizeAsk);
                    fileBars.Add(tradeBar);
                }
                datawriter.Write(fileBars);
                count++;
            }

            Console.ReadKey();
        }

        /// <summary>
        /// Extract the symbol from the path stocks_spy_627_20170427.csv
        /// Symbol is the 2nd parameter
        /// </summary>
        private static Symbol GetSymbol(string fileName)
        {
            var splits = fileName.Split('_');
            var ticker = splits[2].toLowerCase();
            return Symbol.Create(ticker, SecurityType.Equity, Market.USA);
        }

        /// <summary>
        /// Convert config resolution from string to enum
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        private static Resolution ParseResolution(string entry)
        {
            switch (entry.Trim().toLowerCase())
            {
                case "min":
                    return Resolution.Minute;
                case "hour":
                    return Resolution.Hour;
                case "day":
                    return Resolution.Daily;
                default:
                    return Resolution.Minute;
            }
        }

        /// <summary>
        /// Get the count of the files to process
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static string[] GetFiles(string src)
        {
            return Directory.GetFiles(src, "stocks_*.csv.gz", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Parses a CSV line in the most naive case for comma separated values. Does not handle quotes and escapes.
        /// </summary>
        /// <param name="csvLine"></param>
        /// <returns></returns>
        private static string[] ParseCsv(string csvLine)
        {
            return csvLine.Split(Separators, StringSplitOptions.None);
        }
    }
}