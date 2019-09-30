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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.NseMarketDataConverter
{
    public static class NseMarketDataConverterProgram
    {
        /// <summary>
        /// Supports data from http://TrueData.in
        /// </summary>
        public static void NseMarketDataConverter(string sourceDirectory, string destinationDirectory)
        {
            //Document the process:
            Console.WriteLine("QuantConnect.ToolBox: NseMarketData Converter: ");
            Console.WriteLine("==============================================");
            Console.WriteLine("The NseMarketData converter transforms NseMarketData orders into the LEAN Algorithmic Trading Engine Data Format.");
            Console.WriteLine("Parameters required: --source-dir= --destination-dir= ");
            Console.WriteLine("   1> Source Directory of Unzipped NSE Data.");
            Console.WriteLine("   2> Destination Directory of LEAN Data Folder. (Typically located under Lean/Data)");
            Console.WriteLine(" ");
            Console.WriteLine("NOTE: THIS WILL OVERWRITE ANY EXISTING FILES.");
            if (sourceDirectory.IsNullOrEmpty() || destinationDirectory.IsNullOrEmpty())
            {
                Console.WriteLine("1. Source NSE source directory: ");
                sourceDirectory = (Console.ReadLine() ?? "");
                Console.WriteLine("2. Destination LEAN Data directory: ");
                destinationDirectory = (Console.ReadLine() ?? "");
            }

            //Validate the user input:
            Validate(sourceDirectory, destinationDirectory);

            //Remove the final slash to make the path building easier:
            sourceDirectory = StripFinalSlash(sourceDirectory);
            destinationDirectory = StripFinalSlash(destinationDirectory);

            //iterate over all the dates

            //Count the total files to process:
            Console.WriteLine("Counting Files..." + sourceDirectory);
            var count = 0;
            var totalCount = GetCount(sourceDirectory);
            Console.WriteLine("Processing {0} Files ...", totalCount);

            //Enumerate folders by date as all folders are named as dates
            foreach (var dateDirectory in Directory.EnumerateDirectories(sourceDirectory))
            {
                var date = GetDate(dateDirectory);
                var dateDirectoryExtension = dateDirectory + "\\1\\Equity";
                foreach (var file in Directory.EnumerateFiles(dateDirectoryExtension))
                {
                    var symbol = GetSymbol(file);
                    var fileContents = File.ReadAllText(file);
                    string[] stringSeparators = new string[] { "\n" };
                    string[] lines = fileContents.Split(stringSeparators, StringSplitOptions.None);
                    var datawriter = new LeanDataWriter(Resolution.Minute, symbol, destinationDirectory);
                    IList<TradeBar> fileEnum = new List<TradeBar>();
                    foreach (string line in lines)
                    {
                        string[] separators = new string[] { "," };
                        string[] linearray = line.Split(separators, StringSplitOptions.None);
                        if (linearray.Length > 2)
                        {
                            String newline = linearray[0] + " ";
                            newline += linearray[1];
                            newline += ":00.0000";
                            var Time = Parse.DateTimeExact(newline, DateFormat.Forex);
                            var open = Parse.Decimal(linearray[2]);
                            var high = Parse.Decimal(linearray[3]);
                            var low = Parse.Decimal(linearray[4]);
                            var close = Parse.Decimal(linearray[5]);
                            var volume = linearray[6].LongCount();
                            var linedata = new TradeBar(Time, symbol, open, high, low, close, volume);
                            fileEnum.Add(linedata);
                        }
                    }
                    datawriter.Write(fileEnum);
                    count++;
                }
            }
            Console.ReadKey();
        }


        /// <summary>
        /// Application error: display error and then stop conversion
        /// </summary>
        /// <param name="error">Error string</param>
        private static void Error(string error)
        {
            Console.WriteLine(error);
            Console.ReadKey();
            Environment.Exit(0);
        }

        /// <summary>
        /// Get the count of the files to process
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <returns></returns>
        private static int GetCount(string sourceDirectory)
        {
            var count = 0;
            foreach (var date in Directory.EnumerateDirectories(sourceDirectory))
            {
                StripFinalSlash(date);
                count += Directory.EnumerateFiles(date + "\\1\\Equity\\", "*").Count();
            }
            return count;
        }

        /// <summary>
        /// Remove the final slash to make path building easier
        /// </summary>
        private static string StripFinalSlash(string directory)
        {
            return directory.Trim('/', '\\');
        }

        /// <summary>
        /// Get the date component of tie file path.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static DateTime GetDate(string date)
        {
            var splits = date.Split('/', '\\');
            var dateString = splits[splits.Length - 1].Replace("allstocks_", "");
            return Parse.DateTimeExact(dateString, "yyyyMMdd");
        }


        /// <summary>
        /// Extract the symbol from the path
        /// </summary>
        private static Symbol GetSymbol(string filePath)
        {
            var splits = filePath.Split('/', '\\');
            var file = splits[splits.Length - 1];
            file = file.Trim('.', '/', '\\');
            //This switch case is for the symbols names with spaces in their file names
            switch (file)
            {
                case "NIFTY GS 4 8YR.csv":
                    file = "NIFTYGS4TO8YR";
                    break;
                case "NIFTY GS 8 13YR.csv":
                    file = "NIFTYGS8TO13YR";
                    break;
                case "NIFTY GS 10YR CLN.csv":
                    file = "NIFTYGS10YRCLN";
                    break;
                case "NIFTY GS 10YR.csv":
                    file = "NIFTYGS10YR";
                    break;
                case "NIFTY GS 11 15YR.csv":
                    file = "NIFTYGS11TO15YR";
                    break;
                case "NIFTY GS 15YRPLUS.csv":
                    file = "NIFTYGS15YRPLUS";
                    break;
                case "NIFTY GS COMPSITE.csv":
                    file = "NIFTYGSCOMPOSITE";
                    break;
                default:
                    break;
            }
            return Symbol.Create(file.Replace(".csv", ""), SecurityType.Equity, Market.NSE);
        }

        /// <summary>
        /// Validate the users input and throw error if not valid
        /// </summary>
        private static void Validate(string sourceDirectory, string destinationDirectory)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory))
            {
                Error("Error: Please enter a valid source directory.");
            }
            if (string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Error("Error: Please enter a valid destination directory.");
            }
            if (!Directory.Exists(sourceDirectory))
            {
                Error("Error: Source directory does not exist.");
            }
            if (!Directory.Exists(destinationDirectory))
            {
                Error("Error: Destination directory does not exist.");
            }
        }
    }
}
