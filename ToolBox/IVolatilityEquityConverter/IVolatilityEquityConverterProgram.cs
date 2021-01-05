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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.IVolatilityEquityConverter
{
    public static class IVolatilityEquityConverterProgram
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
        static string _sourceDirectory = "";
        static string _sourceMetaDirectory = "";
        static Resolution _resolution = Resolution.Minute;
        static readonly string[] Separators = {","};

        public static void IVolatilityEquityConverter(string sourceDirectory, string sourceMetaDirectory, string destinationDirectory, string resolution)
        {
            Console.WriteLine("QuantConnect.ToolBox: IVolatility Converter: ");
            Console.WriteLine("==============================================");
            Console.WriteLine(
                "The IVolatility converter transforms IVolatility orders into the LEAN Algorithmic Trading Engine Data Format.");
            Console.WriteLine("Parameters required: --source-dir= --source-meta-dir= --destination-dir= --resolution=");
            Console.WriteLine("   1> Source archived IVolatility data.");
            Console.WriteLine("   2> Source archived IVolatility meta data.");
            Console.WriteLine("   3> Destination Directory of LEAN Data Folder. (Typically located under Lean/Data)");
            Console.WriteLine("   4> Resolution of your IVolatility data. (min,hour,day)");
            Console.WriteLine(" ");
            Console.WriteLine("NOTE: THIS WILL OVERWRITE ANY EXISTING FILES.");

            if (!(sourceDirectory.IsNullOrEmpty() || sourceMetaDirectory.IsNullOrEmpty()
                  || destinationDirectory.IsNullOrEmpty() || resolution.IsNullOrEmpty()))
            {
                _sourceDirectory = sourceDirectory;
                _sourceMetaDirectory = sourceMetaDirectory;
                _destinationDirectory = destinationDirectory;
                _resolution = ParseResolution(resolution);
            }
            else
            {
                Console.WriteLine("1. Ivolatility equity data source directory: ");
                _sourceDirectory = (Console.ReadLine() ?? "");
                Console.WriteLine("2. Ivolatility equity data source directory: ");
                _sourceMetaDirectory = (Console.ReadLine() ?? "");
                Console.WriteLine("3. Destination LEAN Data directory: ");
                _destinationDirectory = (Console.ReadLine() ?? "");
                Console.WriteLine("4. Enter resolution of source data (Minute, Hour, Daily): ");
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
                IList<TradeBar> fileTradeBars = new List<TradeBar>();
//                IList<QuoteBar> fileQuoteBars = new List<QuoteBar>();
                var tradeDataWriter = new LeanDataWriter(_resolution, symbol, _destinationDirectory);
//                var quoteDataWriter = new LeanDataWriter(_resolution, symbol, _destinationDirectory, TickType.Quote);
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    var linearray = ParseCsv(line);
                    if (linearray.Length <= 2) continue;
                    var time = DateTime.ParseExact(linearray[TimeField], DateFormat.UI,
                        CultureInfo.InvariantCulture);
                    var priceBid = Parse.Decimal(linearray[PriceBidField]);
                    var sizeBid = Parse.Decimal(linearray[SizeBidField]);
                    var priceAsk = Parse.Decimal(linearray[PriceAskField]);
                    var sizeAsk = Parse.Decimal(linearray[SizeAskField]);
                    var priceLast = Parse.Decimal(linearray[PriceLastField]);
                    //var sizeLast = linearray[sizeLastField]);
                    var volume = Parse.Decimal(linearray[VolumeField]);
                    var tradeBar = new TradeBar(time, symbol, priceLast, priceLast, priceLast, priceLast, 0);
                    tradeBar.Update(priceLast, priceBid, priceAsk, volume, sizeBid, sizeAsk);
                    var bidBar = new Bar(priceBid, priceBid, priceBid, priceBid);
                    var askBar = new Bar(priceAsk, priceAsk, priceAsk, priceAsk);
                    var quoteBar = new QuoteBar(time, symbol, bidBar, sizeBid, askBar, sizeAsk,
                        TimeSpan.FromSeconds(60));
                    fileTradeBars.Add(tradeBar);
//                    fileQuoteBars.Add(quoteBar);
                }
                tradeDataWriter.Write(fileTradeBars);
                //TODO: implement quote bars in LeanDataWriter for minute equity resolution
//                quoteDataWriter.Write(fileQuoteBars);
                count++;
            }
        }

        /// <summary>
        /// Extract the symbol from the path stocks_spy_627_20170427.csv
        /// Symbol is the 2nd parameter
        /// </summary>
        private static Symbol GetSymbol(string fileName)
        {
            var splits = fileName.Split('_');
            var ticker = splits[1].toLowerCase();
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
                case "minute":
                    return Resolution.Minute;
                case "hour":
                    return Resolution.Hour;
                case "daily":
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

        /// <summary>
        /// Generate a LEAN Factor File from IVol Dividends.csv and Splits.csv files
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="factorFilePath"></param>
        private static void GenerateFactorFile(Symbol symbol, string factorFilePath)
        {
            var splitFile = _sourceMetaDirectory + Path.DirectorySeparatorChar + symbol.Value + "Splits.csv";
            var dividendFile = _sourceMetaDirectory + Path.DirectorySeparatorChar + symbol.Value + "Dividends.csv";

            var list1 = PopulateSplitsAndSpecialDividends(splitFile);
            var list2 = PopulateRegularDividends(dividendFile);
            var parsedFactors = list1.Concat(list2).ToList();

            parsedFactors.OrderByDescending(x => x.Time);

            var factorFileGenerator = new FactorFileGenerator(symbol, factorFilePath);
            factorFileGenerator.CreateFactorFile(parsedFactors).WriteToCsv(symbol);
        }

        /// <summary>
        /// Parse regular cash dividends from IVol Dividends.csv to LEAN format
        /// </summary>
        /// <param name="dividendFile"></param>
        private static List<BaseData> PopulateRegularDividends(string dividendFile)
        {
            var parsed = new List<BaseData>();
            if (!File.Exists(dividendFile)) return parsed;
            var dividendsLines = File.ReadAllLines(dividendFile);
            foreach (var d in dividendsLines.Skip(1))
            {
                var line = ParseCsv(d);
                parsed.Add(new Dividend
                {
                    Time = DateTime.ParseExact(line[1].Replace("-", String.Empty), DateFormat.EightCharacter,
                        CultureInfo.InvariantCulture),
                    Value = Parse.Decimal(line[3])
                });
            }
            return parsed;
        }

        /// <summary>
        /// Read Ivolatility Split file and convert to Lean BaseData convention.
        /// IVol split file also contains irregular cash and stock dividends, which should be indexed as dividends, not splits
        /// </summary>
        /// <param name="splitFile"></param>
        /// <exception cref="Exception"></exception>
        private static List<BaseData> PopulateSplitsAndSpecialDividends(string splitFile)
        {
            var parsed = new List<BaseData>();

            if (!File.Exists(splitFile)) return parsed;

            var splitsLines = File.ReadAllLines(splitFile);
            foreach (var s in splitsLines.Skip(1))
            {
                var line = ParseCsv(s);
                var time = DateTime.ParseExact(line[1].Replace("-", String.Empty),
                    DateFormat.EightCharacter, CultureInfo.InvariantCulture);
                //0 - split, 1 - irregular cash dividend, 2 - stock dividend
                switch (line[2])
                {
                    case "0":
                        parsed.Add(new Split
                        {
                            Time = time,
                            Value = ParseSplitAmt(line[3])
                        });
                        break;
                    case "1":
                    case "2":
                        parsed.Add(new Dividend
                        {
                            Time = time,
                            Value = Parse.Decimal(line[4])
                        });
                        break;

                    default:
                        throw new Exception("Unsupported split type: " + line[2]);
                }
            }
            return parsed;
        }

        /// <summary>
        /// Lean uses the inverse of IVol convention.
        /// Ivol defines factor as split factor (1.5 for 3:2 split, 2 for 2/1 splits etc.),
        /// LEAN Defines as 0.5 for 2/1 splits
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        private static decimal ParseSplitAmt(string amount)
        {
            return 1 / Parse.Decimal(amount);
        }
    }
}