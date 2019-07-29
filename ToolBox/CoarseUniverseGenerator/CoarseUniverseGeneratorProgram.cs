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
                    updateTime = Parse.TimeSpan(jtoken.Value<string>());
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

            do
            {
                ProcessEquityDirectories(dataDirectory, ignoreMaplessSymbols);
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
        public static IEnumerable<string> ProcessEquityDirectories(string dataDirectory, bool ignoreMaplessSymbols)
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

                var factorFileProvider = new LocalDiskFactorFileProvider();
                var files = ProcessDailyFolder(dailyFolder, coarseFolder, MapFileResolver.Create(mapFileFolder), factorFileProvider, exclusions, ignoreMaplessSymbols);
                foreach (var file in files)
                {
                    yield return file;
                }
            }
        }

        /// <summary>
        /// Iterates each daily file in the specified <paramref name="dailyFolder"/> and adds a line for each
        /// day to the appropriate coarse file
        /// </summary>
        /// <param name="dailyFolder">The folder with daily data.</param>
        /// <param name="coarseFolder">The coarse output folder.</param>
        /// <param name="mapFileResolver">The map file resolver.</param>
        /// <param name="factorFileProvider">The factor file provider.</param>
        /// <param name="exclusions">The symbols to be excluded from processing.</param>
        /// <param name="ignoreMapless">Ignore the symbols without a map file.</param>
        /// <param name="symbolResolver">Function used to provide symbol resolution. Default resolution uses the zip file name to resolve
        /// the symbol, specify null for this behavior.</param>
        /// <returns>Collection with the names of the newly generated coarse files.</returns>
        /// <exception cref="Exception">
        /// Unable to resolve market for daily folder: " + dailyFolder
        /// or
        /// Unable to resolve fundamental path for coarse folder: " + coarseFolder
        /// </exception>
        public static ICollection<string> ProcessDailyFolder(string dailyFolder, string coarseFolder, MapFileResolver mapFileResolver, IFactorFileProvider factorFileProvider,
            HashSet<string> exclusions, bool ignoreMapless, Func<string, string> symbolResolver = null)
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

            var marketDirectoryInfo = new DirectoryInfo(dailyFolder).Parent;
            if (marketDirectoryInfo == null)
            {
                throw new Exception($"Unable to resolve market for daily folder: {dailyFolder}");
            }
            var market = marketDirectoryInfo.Name.ToLowerInvariant();

            var fundamentalDirectoryInfo = new DirectoryInfo(coarseFolder).Parent;
            if (fundamentalDirectoryInfo == null)
            {
                throw new Exception($"Unable to resolve fundamental path for coarse folder: {coarseFolder}");
            }
            var fineFundamentalFolder = Path.Combine(marketDirectoryInfo.FullName, "fundamental", "fine");

            // open up each daily file to get the values and append to the daily coarse files
            foreach (var file in Directory.EnumerateFiles(dailyFolder, "*.zip"))
            {
                try
                {
                    var ticker = Path.GetFileNameWithoutExtension(file);
                    var fineAvailableDates = Enumerable.Empty<DateTime>();

                    var tickerFineFundamentalFolder = Path.Combine(fineFundamentalFolder, ticker);
                    if (Directory.Exists(tickerFineFundamentalFolder))
                    {
                        fineAvailableDates = Directory.GetFiles(tickerFineFundamentalFolder, "*.zip")
                        .Select(f => DateTime.ParseExact(Path.GetFileNameWithoutExtension(f), DateFormat.EightCharacter, CultureInfo.InvariantCulture))
                        .ToList();
                    }

                    if (ticker == null)
                    {
                        Log.Trace("CoarseGenerator.ProcessDailyFolder(): Unable to resolve symbol from file: {0}", file);
                        continue;
                    }

                    if (symbolResolver != null)
                    {
                        ticker = symbolResolver(ticker);
                    }

                    ticker = ticker.ToUpperInvariant();

                    if (exclusions != null && exclusions.Contains(ticker))
                    {
                        Log.Trace("Excluded symbol: {0}", ticker);
                        continue;
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

                            if (ignoreMapless && !checkedForMapFile)
                            {
                                checkedForMapFile = true;
                                if (!mapFileResolver.ResolveMapFile(ticker, date).Any())
                                {
                                    // if the resolved map file has zero entries then it's a mapless symbol
                                    maplessCount++;
                                    break;
                                }
                            }

                            var close = Parse.Decimal(csv[4]) / scaleFactor;
                            var volume = Parse.Long(csv[5]);

                            var dollarVolume = close * volume;

                            var coarseFile = Path.Combine(coarseFolder, date.ToStringInvariant("yyyyMMdd") + ".csv");
                            dates.Add(date);

                            // try to resolve a map file and if found, regen the sid
                            var sid = SecurityIdentifier.GenerateEquity(SecurityIdentifier.DefaultDate, ticker, market);
                            var mapFile = mapFileResolver.ResolveMapFile(ticker, date);
                            if (!mapFile.IsNullOrEmpty())
                            {
                                // if available, us the permtick in the coarse files, because of this, we need
                                // to update the coarse files each time new map files are added/permticks change
                                sid = SecurityIdentifier.GenerateEquity(mapFile.FirstDate, mapFile.OrderBy(x => x.Date).First().MappedSymbol, market);
                            }

                            if (mapFile == null && ignoreMapless)
                            {
                                // if we're ignoring mapless files then we should always be able to resolve this
                                Log.Error($"CoarseGenerator.ProcessDailyFolder(): Unable to resolve map file for {ticker} as of {date.ToStringInvariant("d")}");
                                continue;
                            }

                            // get price and split factors from factor files
                            var symbol = new Symbol(sid, ticker);
                            var factorFile = factorFileProvider.Get(symbol);
                            var factorFileRow = factorFile?.GetScalingFactors(date);
                            var priceFactor = factorFileRow?.PriceFactor ?? 1m;
                            var splitFactor = factorFileRow?.SplitFactor ?? 1m;


                            // Check if security has fine file within a trailing month for a date-ticker set.
                            // There are tricky cases where a folder named by a ticker can have data for multiple securities.
                            // e.g  GOOG -> GOOGL (GOOG T1AZ164W5VTX) / GOOCV -> GOOG (GOOCV VP83T1ZUHROL) case.
                            // The fine data in the 'fundamental/fine/goog' folder will be for 'GOOG T1AZ164W5VTX' up to the 2014-04-02 and for 'GOOCV VP83T1ZUHROL' afterward.
                            // Therefore, date before checking if the security has fundamental data for a date, we need to filter the fine files the map's first date.
                            var firstDate = mapFile?.FirstDate ?? DateTime.MinValue;
                            var hasFundamentalDataForDate = fineAvailableDates.Where(d => d >= firstDate).Any(d => date.AddMonths(-1) <= d && d <= date);

                            // The following section handles mergers and acquisitions cases.
                            // e.g. YHOO -> AABA (YHOO R735QTJ8XC9X)
                            // The dates right after the acquisition, valid fine fundamental data for AABA are still under the former ticker folder.
                            // Therefore if no fine fundamental data is found in the 'fundamental/fine/aaba' folder, it searches into the 'yhoo' folder.
                            if (mapFile != null && mapFile.Count() > 2 && !hasFundamentalDataForDate)
                            {
                                var previousTicker = mapFile.LastOrDefault(m => m.Date < date)?.MappedSymbol;
                                if (previousTicker != null)
                                {
                                    var previousTickerFineFundamentalFolder = Path.Combine(fineFundamentalFolder, previousTicker);
                                    if (Directory.Exists(previousTickerFineFundamentalFolder))
                                    {
                                        var previousTickerFineAvailableDates = Directory.GetFiles(previousTickerFineFundamentalFolder, "*.zip")
                                            .Select(f => DateTime.ParseExact(Path.GetFileNameWithoutExtension(f), DateFormat.EightCharacter, CultureInfo.InvariantCulture))
                                            .ToList();
                                        hasFundamentalDataForDate = previousTickerFineAvailableDates.Where(d => d >= firstDate).Any(d => date.AddMonths(-1) <= d && d <= date);
                                    }
                                }
                            }

                            // sid,symbol,close,volume,dollar volume,has fundamental data,price factor,split factor
                            var coarseFileLine = $"{sid},{ticker},{close},{volume},{Math.Truncate(dollarVolume)},{hasFundamentalDataForDate},{priceFactor},{splitFactor}";

                            StreamWriter writer;
                            if (!writers.TryGetValue(coarseFile, out writer))
                            {
                                writer = new StreamWriter(new FileStream(coarseFile, FileMode.Create, FileAccess.Write, FileShare.Write));
                                writers[coarseFile] = writer;
                            }
                            writer.WriteLine(coarseFileLine);
                        }
                    }

                    if (symbols % 1000 == 0)
                    {
                        Log.Trace($"CoarseGenerator.ProcessDailyFolder(): Completed processing {symbols} symbols. Current elapsed: {(DateTime.UtcNow - start).TotalSeconds.ToStringInvariant("0.00")} seconds");
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

            Log.Trace($"CoarseGenerator.ProcessDailyFolder(): Processed {symbols} symbols into {dates.Count} coarse files in {(stop - start).TotalSeconds.ToStringInvariant("0.00")} seconds");
            Log.Trace($"CoarseGenerator.ProcessDailyFolder(): Excluded {maplessCount} mapless symbols.");

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
