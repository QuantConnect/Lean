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
using System.Globalization;
using System.IO;
using System.Linq;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.QuantQuoteConverter
{
    public static class QuantQuoteConverterProgram
    {
        /// <summary>
        /// Primary entry point to the program
        /// </summary>
        public static void QuantQuoteConverter(string destinationDirectory, string sourceDirectory, string resolution)
        {
            //Document the process:
            Console.WriteLine("QuantConnect.ToolBox: QuantQuote Converter: ");
            Console.WriteLine("==============================================");
            Console.WriteLine("The QuantQuote converter transforms QuantQuote orders into the LEAN Algorithmic Trading Engine Data Format.");
            Console.WriteLine("Parameters required: --source-dir= --destination-dir= --resolution=");
            Console.WriteLine("   1> Source Directory of Unzipped QuantQuote Data.");
            Console.WriteLine("   2> Destination Directory of LEAN Data Folder. (Typically located under Lean/Data)");
            Console.WriteLine("   3> Resolution of your QuantQuote data. (either minute, second or tick)");
            Console.WriteLine(" ");
            Console.WriteLine("NOTE: THIS WILL OVERWRITE ANY EXISTING FILES.");
            if(sourceDirectory.IsNullOrEmpty() || destinationDirectory.IsNullOrEmpty() || resolution.IsNullOrEmpty())
            {
                Console.WriteLine("1. Source QuantQuote source directory: ");
                sourceDirectory = (Console.ReadLine() ?? "");
                Console.WriteLine("2. Destination LEAN Data directory: ");
                destinationDirectory = (Console.ReadLine() ?? "");
                Console.WriteLine("3. Enter Resolution (minute/second/tick): ");
                resolution = (Console.ReadLine() ?? "");
                resolution = resolution.ToLower();
            }

            //Validate the user input:
            Validate(sourceDirectory, destinationDirectory, resolution);

            //Remove the final slash to make the path building easier:
            sourceDirectory = StripFinalSlash(sourceDirectory);
            destinationDirectory = StripFinalSlash(destinationDirectory);

            //Count the total files to process:
            Console.WriteLine("Counting Files...");
            var count = 0;
            var totalCount = GetCount(sourceDirectory);
            Console.WriteLine("Processing {0} Files ...", totalCount);

            //Enumerate files
            foreach (var directory in Directory.EnumerateDirectories(sourceDirectory))
            {
                var date = GetDate(directory);
                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    var symbol = GetSymbol(file);
                    var fileContents = File.ReadAllText(file);
                    var data = new Dictionary<string, string> { { string.Format("{0}_{1}_Trade_Second.csv", date.ToString("yyyyMMdd"), symbol), fileContents } };

                    var fileDestination = string.Format("{0}/equity/{1}/{2}/{3}_trade.zip",  destinationDirectory, resolution, symbol, date.ToString("yyyyMMdd"));

                    if (!Compression.ZipData(fileDestination, data))
                    {
                        Error("Error: Could not convert to Lean zip file.");
                    }
                    else
                    {
                        Console.WriteLine("Successfully processed {0} of {1} files: {2}", count, totalCount, fileDestination);
                    }
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
            foreach (var directory in Directory.EnumerateDirectories(sourceDirectory))
            {
                count += Directory.EnumerateFiles(directory, "*.csv").Count();
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
            return DateTime.ParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture);
        }


        /// <summary>
        /// Extract the symbol from the path
        /// </summary>
        private static string GetSymbol(string filePath)
        {
            var splits = filePath.Split('/', '\\');
            var file = splits[splits.Length - 1];
            file = file.Trim( '.', '/', '\\');
            file = file.Replace("table_", "");
            return file.Replace(".csv", "");
        }

        /// <summary>
        /// Validate the users input and throw error if not valid
        /// </summary>
        private static void Validate(string sourceDirectory, string destinationDirectory, string resolution)
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
            if (resolution != "minute" && resolution != "second" && resolution != "tick")
            {
                Error("Error: Resolution specified is not supported. Please enter tick, second or minute");
            }
        }
    }
}
