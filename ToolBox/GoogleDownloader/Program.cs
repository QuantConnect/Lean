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
using System.IO;
using System.IO.Compression;
using System.Net;
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
            //q=SYMBOL
            //i=resolution in seconds
            //p=period in days
            // Strangely Google forces CHLO format instead of normal OHLC.
            const string urlPrototype = @"http://www.google.com/finance/getprices?q={0}&i={1}&p={2}d&f=d,c,h,l,o,v";

            if (args.Length != 3)
            {
                Console.WriteLine("Usage: GoogleDownloader SYMBOL RESOLUTION PERIOD");
                Console.WriteLine("SYMBOL = eg SPY");
                Console.WriteLine("RESOLUTION = 60 for minute intraday data");
                Console.WriteLine("PERIOD = 10 for 10 days intraday data");
                Environment.Exit(1);
            }
            var symbol = args[0];
            var resolution = int.Parse(args[1]);
            var period = args[2];
            
            // Create the Google formatted URL.
            var url = string.Format(urlPrototype, symbol, resolution, period);
            Log.Trace("GoogleDownloader.Main(): Downloading " + period + " days of " + resolution + "sec bars for symbol: " + symbol);

            try
            {
                //Download the data from Google.
                string[] lines;
                using (var client = new WebClient())
                {
                    var data = client.DownloadString(url);
                    lines = data.Split('\n');
                }
                Log.Trace("GoogleDownloader.Main(): Downloaded " + lines.Length + " lines of data.");

                //First 7 lines are headers 
                var currentLine = 7;
                var tempPath = Path.GetTempPath();

                while (true)
                {
                    //If there's no more data available 
                    if (currentLine >= lines.Length - 1) break;

                    var fullpath = "";
                    var zipFilename = "";
                    var filename = "";
                    var firstPass = true;

                    //Each day google starts date time at 930am and then 
                    //has 390 minutes over the day. Look for the starter rows "a".
                    var columns = lines[currentLine].Split(',');
                    long startTime = int.Parse(columns[0].Remove(0, 1));
                    var unixTime = FromUnixTime(startTime);

                    //Convert from "a1231231" to "20120101"
                    var dateString = unixTime.ToString("yyyyMMdd");

                    //Create the filename & zipname to write to:
                    filename = string.Format("{0}_minute_trade.csv", dateString);
                    zipFilename = string.Format("{0}_trade.zip", dateString);
                    fullpath = Path.Combine(tempPath, filename);

                    //Delete if already exsits, open the file for writing.
                    File.Delete(fullpath);
                    Log.Trace("GoogleDownloader.Main(): Starting work on " + dateString + "...");

                    //Build the file.
                    using (var swfile = new StreamWriter(fullpath))
                    {
                        while (true)
                        {
                            //If there's no more data available.
                            if (currentLine >= lines.Length - 1) break;

                            var str = lines[currentLine].Split(',');
                            if (str.Length < 6)
                            {
                                Log.Trace("GoogleDownloader.Main(): Short record: " + str);
                                break;
                            }

                            //If its the start of a new day, break out of this sub-loop.
                            var titleRow = str[0][0] == 'a';
                            if (titleRow && !firstPass) break;
                            firstPass = false;

                            //Build the current minute position, from the start of day + minutes * 60,000ms. 
                            var time = startTime + resolution * (titleRow ? 0 : int.Parse(str[0]));

                            //Bar: d0 , c1, h2, l3, o4, v5
                            var open = decimal.Parse(str[4]) * 10000;
                            var high = decimal.Parse(str[2]) * 10000;
                            var low = decimal.Parse(str[3]) * 10000;
                            var close = decimal.Parse(str[1]) * 10000;
                            var volume = decimal.Parse(str[5]);

                            // Write the line, time is milliseconds since midnight.
                            swfile.WriteLine("{0},{1},{2},{3},{4},{5}", (time - startTime)*1000, (int)open, (int)high, (int)low, (int)close, volume);
                            currentLine += 1;
                        }
                    }

                    //Write the zip output.
                    CreateZip(fullpath, zipFilename, filename);
                }
            }
            catch (Exception err)
            {
                Log.Error("GoogleDownloader.Main(): Error: " + err.Message);
            }
            Log.Trace("Completed.");
            Console.ReadKey();
        }


        /// <summary>
        /// Create a new zip from the string file.
        /// </summary>
        /// <param name="fullpath">Path of the file output</param>
        /// <param name="zipfilename">File name of the zip</param>
        /// <param name="file">File contents</param>
        private static void CreateZip(string fullpath, string zipfilename, string file)
        {
            //string zipfile = symbol + ".zip";
            var curdir = Directory.GetCurrentDirectory();
            var zipfullpath = Path.Combine(curdir, zipfilename);
            Log.Trace("GoogleDownloader.CreateZip(): Writing Zip: " + fullpath);
            File.Delete(zipfullpath);
            using (var zip = ZipFile.Open(zipfullpath, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(fullpath, file);
            }
        }


        /// <summary>
        /// Convert a long time into a date time object
        /// </summary>
        /// <param name="unixTime">Unix long time.</param>
        /// <returns>DateTime object</returns>
        private static DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }
    }
}
