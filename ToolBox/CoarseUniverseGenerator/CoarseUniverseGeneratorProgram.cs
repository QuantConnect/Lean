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
using System.Threading;
using Ionic.Zip;
using Newtonsoft.Json.Linq;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Util;
using Log = QuantConnect.Logging.Log;

namespace QuantConnect.ToolBox.CoarseUniverseGenerator
{
    public static class CoarseUniverseGeneratorProgram
    {
        private const string ExclusionsFile = "exclusions.txt";

        /// <summary>
        /// This program generates the coarse files requires by lean for universe selection.
        /// Universe selection is planned to happen in two stages, the first stage, the 'coarse'
        /// stage serves to cull the set using coarse filters, such as price, market, and dollar volume.
        /// Later we'll support full fundamental data such as ratios and financial statements, and these
        /// would be run AFTER the initial coarse filter
        ///
        /// The files are generated from LEAN formatted daily trade bar equity files
        /// </summary>
        public static void CoarseUniverseGenerator()
        {
            // read out the configuration file
            JToken jtoken;
            var config = JObject.Parse(File.ReadAllText("CoarseUniverseGenerator/config.json"));

            var ignoreMaplessSymbols = false;
            var updateMode = false;
            var updateTime = TimeSpan.Zero;
            DateTime? startDate = null;
            if (config.TryGetValue("update-mode", out jtoken))
            {
                updateMode = jtoken.Value<bool>();
                if (config.TryGetValue("update-time-of-day", out jtoken))
                {
                    updateTime = TimeSpan.Parse(jtoken.Value<string>());
                }
            }

            var dataDirectory = Globals.DataFolder;
            if (config.TryGetValue("data-directory", out jtoken))
            {
                dataDirectory = jtoken.Value<string>();
            }

            //Ignore symbols without a map file:
            // Typically these are nothing symbols (NASDAQ test symbols, or symbols listed for a few days who aren't actually ever traded).
            if (config.TryGetValue("ignore-mapless", out jtoken))
            {
                ignoreMaplessSymbols = jtoken.Value<bool>();
            }

            if (config.TryGetValue("coarse-universe-generator-start-date", out jtoken))
            {
                string startDateStr = jtoken.Value<string>();
                startDate = DateTime.ParseExact(startDateStr, "yyyyMMdd", null);
                Log.Trace("Generating coarse data from {0}", startDate);
            }

            do
            {
                ProcessEquityDirectories(dataDirectory, ignoreMaplessSymbols, startDate);
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
        /// <param name="ignoreMaplessSymbols">Ignore symbols without a QuantQuote map file.</param>
        public static IEnumerable<string> ProcessEquityDirectories(string dataDirectory, bool ignoreMaplessSymbols, DateTime? startDate)
        {
            var exclusions = ReadExclusionsFile(ExclusionsFile);

            var equity = Path.Combine(dataDirectory, "equity");
            foreach (var directory in Directory.EnumerateDirectories(equity))
            {
                var dailyFolder = Path.Combine(directory, "daily");
                var mapFileFolder = Path.Combine(directory, "map_files");
                var coarseFolder = Path.Combine(directory, "fundamental", "coarse");
                if (!Directory.Exists(coarseFolder))
                {
                    Directory.CreateDirectory(coarseFolder);
                }

                var lastProcessedDate = startDate ?? GetLastProcessedDate(coarseFolder);
                var factorFileProvider = new LocalDiskFactorFileProvider();
                var files = ProcessDailyFolder(dailyFolder, coarseFolder, MapFileResolver.Create(mapFileFolder), factorFileProvider, exclusions, ignoreMaplessSymbols, lastProcessedDate);
                foreach (var file in files)
                {
                    yield return file;
                }
            }
        }

        /// <summary>
        /// Iterates each daily file in the specified <paramref name="dailyFolder"/> and adds a line for each
        /// day to the approriate coarse file
        /// </summary>
        /// <param name="dailyFolder">The folder with daily data</param>
        /// <param name="coarseFolder">The coarse output folder</param>
        /// <param name="mapFileResolver"></param>
        /// <param name="exclusions">The symbols to be excluded from processing</param>
        /// <param name="ignoreMapless">Ignore the symbols without a map file.</param>
        /// <param name="startDate">The starting date for processing</param>
        /// <param name="symbolResolver">Function used to provide symbol resolution. Default resolution uses the zip file name to resolve
        /// the symbol, specify null for this behavior.</param>
        /// <returns>A collection of the generated coarse files</returns>
        public static ICollection<string> ProcessDailyFolder(string dailyFolder, string coarseFolder, MapFileResolver mapFileResolver, IFactorFileProvider factorFileProvider,
            HashSet<string> exclusions, bool ignoreMapless, DateTime startDate, Func<string, string> symbolResolver = null)
        {
            const decimal scaleFactor = 10000m;

            Log.Trace("Processing: {0}", dailyFolder);

            var start = DateTime.UtcNow;

            // load map files into memory

            var symbols = 0;
            var maplessCount = 0;
            var dates = new HashSet<DateTime>();

            // instead of opening/closing these constantly, open them once and dispose at the end (~3x speed improvement)
            var writers = new Dictionary<string, StreamWriter>();

            var dailyFolderDirectoryInfo = new DirectoryInfo(dailyFolder).Parent;
            if (dailyFolderDirectoryInfo == null)
            {
                throw new Exception("Unable to resolve market for daily folder: " + dailyFolder);
            }
            var market = dailyFolderDirectoryInfo.Name.ToLower();

            var fundamentalDirectoryInfo = new DirectoryInfo(coarseFolder).Parent;
            if (fundamentalDirectoryInfo == null)
            {
                throw new Exception("Unable to resolve fundamental path for coarse folder: " + coarseFolder);
            }
            var fineFundamentalFolder = Path.Combine(fundamentalDirectoryInfo.FullName, "fine");

            var mapFileProvider = new LocalDiskMapFileProvider();

            // open up each daily file to get the values and append to the daily coarse files
            foreach (var file in Directory.EnumerateFiles(dailyFolder, "*.zip"))
            {
                try
                {
                    var symbol = Path.GetFileNameWithoutExtension(file);
                    if (symbol == null)
                    {
                        Log.Trace("CoarseGenerator.ProcessDailyFolder(): Unable to resolve symbol from file: {0}", file);
                        continue;
                    }

                    if (symbolResolver != null)
                    {
                        symbol = symbolResolver(symbol);
                    }

                    symbol = symbol.ToUpper();

                    if (exclusions.Contains(symbol))
                    {
                        Log.Trace("Excluded symbol: {0}", symbol);
                        continue;
                    }

                    // check if symbol has any fine fundamental data
                    var firstFineSymbolDate = DateTime.MaxValue;
                    if (Directory.Exists(fineFundamentalFolder))
                    {
                        var fineSymbolFolder = Path.Combine(fineFundamentalFolder, symbol.ToLower());

                        var firstFineSymbolFileName = Directory.Exists(fineSymbolFolder) ? Directory.GetFiles(fineSymbolFolder).OrderBy(x => x).FirstOrDefault() : string.Empty;
                        if (firstFineSymbolFileName.Length > 0)
                        {
                            firstFineSymbolDate = DateTime.ParseExact(Path.GetFileNameWithoutExtension(firstFineSymbolFileName), "yyyyMMdd", CultureInfo.InvariantCulture);
                        }
                    }

                    ZipFile zip;
                    using (var reader = Compression.Unzip(file, out zip))
                    {
                        var checkedForMapFile = false;

                        symbols++;
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            //20150625.csv
                            var csv = line.Split(',');
                            var date = DateTime.ParseExact(csv[0], DateFormat.TwelveCharacter, CultureInfo.InvariantCulture);

                            // spin past old data
                            if (date < startDate) continue;

                            if (ignoreMapless && !checkedForMapFile)
                            {
                                checkedForMapFile = true;
                                if (!mapFileResolver.ResolveMapFile(symbol, date).Any())
                                {
                                    // if the resolved map file has zero entries then it's a mapless symbol
                                    maplessCount++;
                                    break;
                                }
                            }

                            var close = decimal.Parse(csv[4])/scaleFactor;
                            var volume = long.Parse(csv[5]);

                            var dollarVolume = close * volume;

                            var coarseFile = Path.Combine(coarseFolder, date.ToString("yyyyMMdd") + ".csv");
                            dates.Add(date);

                            // try to resolve a map file and if found, regen the sid
                            var sid = SecurityIdentifier.GenerateEquity(SecurityIdentifier.DefaultDate, symbol, market);
                            var mapFile = mapFileResolver.ResolveMapFile(symbol, date);
                            if (!mapFile.IsNullOrEmpty())
                            {
                                // if available, us the permtick in the coarse files, because of this, we need
                                // to update the coarse files each time new map files are added/permticks change
                                sid = SecurityIdentifier.GenerateEquity(mapFile.FirstDate, mapFile.OrderBy(x => x.Date).First().MappedSymbol, market);
                            }
                            if (mapFile == null && ignoreMapless)
                            {
                                // if we're ignoring mapless files then we should always be able to resolve this
                                Log.Error(string.Format("CoarseGenerator.ProcessDailyFolder(): Unable to resolve map file for {0} as of {1}", symbol, date.ToShortDateString()));
                                continue;
                            }

                            // check if symbol has fine fundamental data for the current date
                            var hasFundamentalDataForDate = date >= firstFineSymbolDate;

                            // get price and split factors from factor files
                            var leanSymbol = new Symbol(sid, symbol);
                            var factorFile = factorFileProvider.Get(leanSymbol);
                            var factorFileRow = factorFile?.GetScalingFactors(date);
                            var priceFactor = factorFileRow?.PriceFactor ?? 1m;
                            var splitFactor = factorFileRow?.SplitFactor ?? 1m;

                            // sid,symbol,close,volume,dollar volume,has fundamental data,price factor,split factor
                            var coarseFileLine = $"{sid},{symbol},{close},{volume},{Math.Truncate(dollarVolume)},{hasFundamentalDataForDate},{priceFactor},{splitFactor}";

                            StreamWriter writer;
                            if (!writers.TryGetValue(coarseFile, out writer))
                            {
                                writer = new StreamWriter(new FileStream(coarseFile, FileMode.Create, FileAccess.Write, FileShare.Write));
                                writers[coarseFile] = writer;
                            }
                            writer.WriteLine(coarseFileLine);
                        }
                    }

                    if (symbols%1000 == 0)
                    {
                        Log.Trace("CoarseGenerator.ProcessDailyFolder(): Completed processing {0} symbols. Current elapsed: {1} seconds", symbols, (DateTime.UtcNow - start).TotalSeconds.ToString("0.00"));
                    }
                }
                catch (Exception err)
                {
                    // log the error and continue with the process
                    Log.Error(err.ToString());
                }
            }

            Log.Trace("CoarseGenerator.ProcessDailyFolder(): Saving {0} coarse files to disk", dates.Count);

            // dispose all the writers at the end of processing
            foreach (var writer in writers)
            {
                writer.Value.Dispose();
            }

            var stop = DateTime.UtcNow;

            Log.Trace("CoarseGenerator.ProcessDailyFolder(): Processed {0} symbols into {1} coarse files in {2} seconds", symbols, dates.Count, (stop - start).TotalSeconds.ToString("0.00"));
            Log.Trace("CoarseGenerator.ProcessDailyFolder(): Excluded {0} mapless symbols.", maplessCount);

            return writers.Keys;
        }

        /// <summary>
        /// Reads the specified exclusions file into a new hash set.
        /// Returns an empty set if the file does not exist
        /// </summary>
        public static HashSet<string> ReadExclusionsFile(string exclusionsFile)
        {
            var exclusions = new HashSet<string>();
            if (File.Exists(exclusionsFile))
            {
                var excludedSymbols = File.ReadLines(exclusionsFile).Select(x => x.Trim()).Where(x => !x.StartsWith("#"));
                exclusions = new HashSet<string>(excludedSymbols, StringComparer.InvariantCultureIgnoreCase);
                Log.Trace("CoarseGenerator.ReadExclusionsFile(): Loaded {0} symbols into the exclusion set", exclusions.Count);
            }
            return exclusions;
        }

        /// <summary>
        /// Resolves the start date that should be used in the <see cref="ProcessDailyFolder"/>. This will
        /// be equal to the latest file date (20150101.csv) plus one day
        /// </summary>
        /// <param name="coarseDirectory">The directory containing the coarse files</param>
        /// <returns>The last coarse file date plus one day if exists, else DateTime.MinValue</returns>
        public static DateTime GetLastProcessedDate(string coarseDirectory)
        {
            var lastProcessedDate = (
                from coarseFile in Directory.EnumerateFiles(coarseDirectory)
                let date = TryParseCoarseFileDate(coarseFile)
                where date != null
                // we'll start on the following day
                select date.Value.AddDays(1)
                ).DefaultIfEmpty(DateTime.MinValue).Max();

            return lastProcessedDate;
        }

        private static DateTime? TryParseCoarseFileDate(string coarseFile)
        {
            try
            {
                var dateString = Path.GetFileNameWithoutExtension(coarseFile);
                return DateTime.ParseExact(dateString, "yyyyMMdd", null);
            }
            catch
            {
                return null;
            }
        }
    }
}
