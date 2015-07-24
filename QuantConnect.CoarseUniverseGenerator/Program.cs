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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Ionic.Zip;
using Newtonsoft.Json.Linq;
using QuantConnect.ToolBox;

namespace QuantConnect.CoarseUniverseGenerator
{
    public static class Program
    {
        /// <summary>
        /// This program generates the coarse files requires by lean for universe selection.
        /// Universe selection is planned to happen in two stages, the first stage, the 'coarse'
        /// stage serves to cull the set using coarse filters, such as price, market, and dollar volume.
        /// Later we'll support full fundamental data such as ratios and financial statements, and these
        /// would be run AFTER the initial coarse filter
        /// 
        /// The files are generated from LEAN formatted daily trade bar equity files
        /// </summary>
        /// <param name="args">Unused argument</param>
        public static void Main(string[] args)
        {
            // read out the configuration file
            JToken jtoken;
            var config = JObject.Parse(File.ReadAllText("config.json"));
            if (!config.TryGetValue("data-directory", out jtoken))
            {
                throw new Exception("Specify 'data-directory' in config.json");
            }
            var dataDirectory = jtoken.Value<string>();

            var updateMode = false;
            var updateTime = TimeSpan.Zero;
            if (config.TryGetValue("update-mode", out jtoken))
            {
                updateMode = jtoken.Value<bool>();
                if (config.TryGetValue("update-time-of-day", out jtoken))
                {
                    updateTime = TimeSpan.Parse(jtoken.Value<string>());
                }
            }

            do
            {
                ProcessEquityDirectories(dataDirectory);
            }
            while (WaitUntilTimeInUpdateMode(updateMode, updateTime));
        }

        /// <summary>
        /// If we're in update mode, pause the thread until the next update time
        /// </summary>
        /// <param name="updateMode">True for update mode, false for run-once</param>
        /// <param name="updateTime">The time of day updates should be performed</param>
        /// <returns>True if in update mode, otherwise false</returns>
        private static bool WaitUntilTimeInUpdateMode(bool updateMode, TimeSpan updateTime)
        {
            if (!updateMode) return false;

            var now = DateTime.Now;
            var timeUntilNextProcess = (now.Date.AddDays(1).Add(updateTime) - now);
            Thread.Sleep((int)timeUntilNextProcess.TotalMilliseconds);
            return true;
        }

        /// <summary>
        /// Iterates over each equity directory and aggregates the data into the coarse file
        /// </summary>
        /// <param name="dataDirectory">The Lean /Data directory</param>
        private static void ProcessEquityDirectories(string dataDirectory)
        {
            var equity = Path.Combine(dataDirectory, "equity");
            foreach (var directory in Directory.EnumerateDirectories(equity))
            {
                var dailyFolder = Path.Combine(directory, "daily");
                var coarseFolder = Path.Combine(directory, "fundamental", "coarse");
                if (!Directory.Exists(coarseFolder))
                {
                    Directory.CreateDirectory(coarseFolder);
                }

                var start = Directory.EnumerateFiles(coarseFolder)
                    .Select(x => ParseDateFromCoarseFilename(Path.GetFileName(x)))
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();

                ProcessDailyFolder(dailyFolder, coarseFolder, start);
            }
        }

        /// <summary>
        /// Iterates each daily file in the specified <paramref name="dailyFolder"/> and adds a line for each
        /// day to the approriate coarse file
        /// </summary>
        /// <param name="dailyFolder">The folder with daily data</param>
        /// <param name="coarseFolder">The coarse output folder</param>
        /// <param name="start">The start time, this is resolve by finding the most recent written coarse file</param>
        private static void ProcessDailyFolder(string dailyFolder, string coarseFolder, DateTime start)
        {
            const decimal scaleFactor = 10000m;

            Console.WriteLine(DateTime.UtcNow.ToString("o") + ": Processing: " + dailyFolder);

            var stopwatch = Stopwatch.StartNew();

            var symbols = 0;
            var dates = new HashSet<DateTime>();

            // instead of opening/closing these constantly, open them once and dispose at the end (~3x speed improvement)
            var writers = new Dictionary<string, StreamWriter>();

            // open up each daily file to get the values and append to the daily coarse files
            foreach (var file in Directory.EnumerateFiles(dailyFolder))
            {
                symbols++;
                ZipFile zip;
                using (var reader = Compression.Unzip(file, out zip))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var csv = line.Split(',');
                        var date = DateTime.ParseExact(csv[0], DateFormat.TwelveCharacter, CultureInfo.InvariantCulture);
                        if (date <= start) continue;

                        var close = decimal.Parse(csv[4])/scaleFactor;
                        var volume = long.Parse(csv[5]);

                        var dollarVolume = close*volume;

                        var symbol = Path.GetFileNameWithoutExtension(file);
                        if (symbol == null)
                        {
                            Console.WriteLine(DateTime.UtcNow.ToString("o") + ": Unable to resolve symbol from file: " + file);
                            continue;
                        }

                        var coarseFile = Path.Combine(coarseFolder, date.ToString("yyyyMMdd") + ".csv");
                        dates.Add(date);

                        // symbol,close,dollar volume
                        var coarseFileLine = symbol.ToUpper() + "," + close + "," + dollarVolume;

                        StreamWriter writer;
                        if (!writers.TryGetValue(coarseFile, out writer))
                        {
                            writer = new StreamWriter(new FileStream(coarseFile, FileMode.Append, FileAccess.Write, FileShare.Write));
                            writers[coarseFile] = writer;
                        }
                        writer.WriteLine(coarseFileLine);
                    }
                }

                if (symbols%10 == 0)
                {
                    Console.WriteLine(DateTime.UtcNow.ToString("o") + ": Completed processing {0} symbols. Current elapsed: " + stopwatch.Elapsed.TotalSeconds.ToString("0.00"), symbols);
                }
            }

            // dispose all the writers at the end of processing
            foreach (var writer in writers)
            {
                writer.Value.Dispose();
            }

            stopwatch.Stop();

            Console.WriteLine(DateTime.UtcNow.ToString("o") + ": Processed {0} symbols into {1} coarse files in {2}", symbols, dates.Count, stopwatch.Elapsed.TotalSeconds.ToString("0.00"));
        }

        /// <summary>
        /// Parses a date time from a coarse file name
        /// </summary>
        /// <param name="filename">The coarse file name to be parsed</param>
        /// <returns>The timestamp in the filename</returns>
        private static DateTime ParseDateFromCoarseFilename(string filename)
        {
            return DateTime.ParseExact(filename.Substring(0, DateFormat.EightCharacter.Length), DateFormat.EightCharacter, CultureInfo.InvariantCulture);
        }
    }
}
