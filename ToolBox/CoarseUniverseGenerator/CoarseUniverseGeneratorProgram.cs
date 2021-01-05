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

using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DateTime = System.DateTime;
using Log = QuantConnect.Logging.Log;

namespace QuantConnect.ToolBox.CoarseUniverseGenerator
{
    /// <summary>
    /// Coarse
    /// </summary>
    public class CoarseUniverseGeneratorProgram
    {
        private readonly DirectoryInfo _dailyDataFolder;
        private readonly DirectoryInfo _destinationFolder;
        private readonly IMapFileProvider _mapFileProvider;
        private readonly IFactorFileProvider _factorFileProvider;
        private readonly string _market;
        private readonly FileInfo _blackListedTickersFile;

        /// <summary>
        /// Runs the Coarse universe generator with default values.
        /// </summary>
        /// <returns></returns>
        public static bool CoarseUniverseGenerator()
        {
            var dailyDataFolder = new DirectoryInfo(Path.Combine(Globals.DataFolder, SecurityType.Equity.SecurityTypeToLower(), Market.USA, Resolution.Daily.ResolutionToLower()));
            var destinationFolder = new DirectoryInfo(Path.Combine(Globals.DataFolder, SecurityType.Equity.SecurityTypeToLower(), Market.USA, "fundamental", "coarse"));
            var blackListedTickersFile = new FileInfo("blacklisted-tickers.txt");
            var reservedWordPrefix = Config.Get("reserved-words-prefix", "quantconnect-");
            var mapFileProvider = new LocalDiskMapFileProvider();
            var factorFileProvider = new LocalDiskFactorFileProvider(mapFileProvider);
            var generator = new CoarseUniverseGeneratorProgram(dailyDataFolder, destinationFolder, Market.USA, blackListedTickersFile, reservedWordPrefix, mapFileProvider, factorFileProvider);
            return generator.Run();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoarseUniverseGeneratorProgram"/> class.
        /// </summary>
        /// <param name="dailyDataFolder">The daily data folder.</param>
        /// <param name="destinationFolder">The destination folder.</param>
        /// <param name="market">The market.</param>
        /// <param name="blackListedTickersFile">The black listed tickers file.</param>
        /// <param name="reservedWordsPrefix">The reserved words prefix.</param>
        /// <param name="mapFileProvider">The map file provider.</param>
        /// <param name="factorFileProvider">The factor file provider.</param>
        /// <param name="debugEnabled">if set to <c>true</c> [debug enabled].</param>
        public CoarseUniverseGeneratorProgram(DirectoryInfo dailyDataFolder, DirectoryInfo destinationFolder, string market, FileInfo blackListedTickersFile, string reservedWordsPrefix,
            IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, bool debugEnabled = false)
        {
            _blackListedTickersFile = blackListedTickersFile;
            _market = market;
            _factorFileProvider = factorFileProvider;
            _mapFileProvider = mapFileProvider;
            _destinationFolder = destinationFolder;
            _dailyDataFolder = dailyDataFolder;
            Log.DebuggingEnabled = debugEnabled;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        /// <returns></returns>
        public bool Run()
        {
            var startTime = DateTime.UtcNow;
            var success = true;
            Log.Trace($"CoarseUniverseGeneratorProgram.ProcessDailyFolder(): Processing: {_dailyDataFolder.FullName}");

            var symbolsProcessed = 0;
            var filesRead = 0;
            var dailyFilesNotFound = 0;
            var coarseFilesGenerated = 0;

            var mapFileResolver = _mapFileProvider.Get(_market);

            var blackListedTickers = new HashSet<string>();
            if (_blackListedTickersFile.Exists)
            {
                blackListedTickers = File.ReadAllLines(_blackListedTickersFile.FullName).ToHashSet();
            }

            var marketFolder = _dailyDataFolder.Parent;
            var fineFundamentalFolder = new DirectoryInfo(Path.Combine(marketFolder.FullName, "fundamental", "fine"));
            if (!fineFundamentalFolder.Exists)
            {
                Log.Error($"CoarseUniverseGenerator.Run(): FAIL, Fine Fundamental folder not found at {fineFundamentalFolder}! ");
                return false;
            }

            var securityIdentifierContexts = PopulateSidContex(mapFileResolver, blackListedTickers);
            var dailyPricesByTicker = new ConcurrentDictionary<string, List<TradeBar>>();
            var outputCoarseContent = new ConcurrentDictionary<DateTime, List<string>>();

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 };

            try
            {
                Parallel.ForEach(securityIdentifierContexts, parallelOptions, sidContext =>
                {
                    var symbol = new Symbol(sidContext.SID, sidContext.LastTicker);
                    var symbolCount = Interlocked.Increment(ref symbolsProcessed);
                    Log.Debug($"CoarseUniverseGeneratorProgram.Run(): Processing {symbol}");
                    var factorFile = _factorFileProvider.Get(symbol);

                    // Populate dailyPricesByTicker with all daily data by ticker for all tickers of this security.
                    foreach (var ticker in sidContext.Tickers)
                    {
                        var dailyFile = new FileInfo(Path.Combine(_dailyDataFolder.FullName, $"{ticker}.zip"));
                        if (!dailyFile.Exists)
                        {
                            Log.Error($"CoarseUniverseGeneratorProgram.Run(): {dailyFile} not found!");
                            Interlocked.Increment(ref dailyFilesNotFound);
                            continue;
                        }

                        if (!dailyPricesByTicker.ContainsKey(ticker))
                        {
                            dailyPricesByTicker.AddOrUpdate(ticker, ParseDailyFile(dailyFile));
                            Interlocked.Increment(ref filesRead);
                        }
                    }

                    // Look for daily data for each ticker of the actual security
                    for (int mapFileRowIndex = sidContext.MapFileRows.Length - 1; mapFileRowIndex >= 1; mapFileRowIndex--)
                    {
                        var ticker = sidContext.MapFileRows[mapFileRowIndex].Item2.ToLowerInvariant();
                        var endDate = sidContext.MapFileRows[mapFileRowIndex].Item1;
                        var startDate = sidContext.MapFileRows[mapFileRowIndex - 1].Item1;
                        List<TradeBar> tickerDailyData;
                        if (!dailyPricesByTicker.TryGetValue(ticker, out tickerDailyData))
                        {
                            Log.Error($"CoarseUniverseGeneratorProgram.Run(): Daily data for ticker {ticker.ToUpperInvariant()} not found!");
                            continue;
                        }

                        var tickerFineFundamentalFolder = Path.Combine(fineFundamentalFolder.FullName, ticker);
                        var fineAvailableDates = Enumerable.Empty<DateTime>();
                        if (Directory.Exists(tickerFineFundamentalFolder))
                        {
                            fineAvailableDates = Directory.GetFiles(tickerFineFundamentalFolder, "*.zip")
                                .Select(f => DateTime.ParseExact(Path.GetFileNameWithoutExtension(f), DateFormat.EightCharacter, CultureInfo.InvariantCulture))
                                .ToList();
                        }

                        // Get daily data only for the time the ticker was
                        foreach (var tradeBar in tickerDailyData.Where(tb => tb.Time >= startDate && tb.Time <= endDate))
                        {
                            var coarseRow = GenerateFactorFileRow(ticker, sidContext, factorFile, tradeBar, fineAvailableDates, fineFundamentalFolder);
                            List<string> tempList;

                            outputCoarseContent.AddOrUpdate(tradeBar.Time,
                                new List<string> { coarseRow },
                                (time, list) =>
                            {
                                lock (list)
                                {
                                    list.Add(coarseRow);
                                    return list;
                                }
                            });
                        }
                    }

                    if (symbolCount % 1000 == 0)
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        Log.Trace($"CoarseUniverseGeneratorProgram.Run(): Processed {symbolCount} in {elapsed:g} at {symbolCount / elapsed.TotalMinutes:F2} symbols/minute ");
                    }
                });

                _destinationFolder.Create();
                var startWriting = DateTime.UtcNow;
                Parallel.ForEach(outputCoarseContent, coarseByDate =>
                {
                    var filename = $"{coarseByDate.Key.ToString(DateFormat.EightCharacter, CultureInfo.InvariantCulture)}.csv";
                    var filePath = Path.Combine(_destinationFolder.FullName, filename);
                    Log.Debug($"CoarseUniverseGeneratorProgram.Run(): Saving {filename} with {coarseByDate.Value.Count} entries.");
                    File.WriteAllLines(filePath, coarseByDate.Value.OrderBy(cr => cr));
                    var filesCount = Interlocked.Increment(ref coarseFilesGenerated);
                    if (filesCount % 1000 == 0)
                    {
                        var elapsed = DateTime.UtcNow - startWriting;
                        Log.Trace($"CoarseUniverseGeneratorProgram.Run(): Processed {filesCount} in {elapsed:g} at {filesCount / elapsed.TotalSeconds:F2} files/second ");
                    }
                });

                Log.Trace($"\n\nTotal of {coarseFilesGenerated} coarse files generated in {DateTime.UtcNow - startTime:g}:\n" +
                          $"\t => {filesRead} daily data files read.\n");
            }
            catch (Exception e)
            {
                Log.Error(e, $"CoarseUniverseGeneratorProgram.Run(): FAILED!");
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Generates the factor file row.
        /// </summary>
        /// <param name="ticker">The ticker.</param>
        /// <param name="sidContext">The sid context.</param>
        /// <param name="factorFile">The factor file.</param>
        /// <param name="tradeBar">The trade bar.</param>
        /// <param name="fineAvailableDates">The fine available dates.</param>
        /// <param name="fineFundamentalFolder">The fine fundamental folder.</param>
        /// <returns></returns>
        private static string GenerateFactorFileRow(string ticker, SecurityIdentifierContext sidContext, FactorFile factorFile, TradeBar tradeBar, IEnumerable<DateTime> fineAvailableDates, DirectoryInfo fineFundamentalFolder)
        {
            var date = tradeBar.Time;
            var factorFileRow = factorFile?.GetScalingFactors(date);
            var dollarVolume = Math.Truncate(tradeBar.Close * tradeBar.Volume);
            var priceFactor = factorFileRow?.PriceFactor.Normalize() ?? 1m;
            var splitFactor = factorFileRow?.SplitFactor.Normalize() ?? 1m;
            bool hasFundamentalData = CheckFundamentalData(date, sidContext.MapFile, fineAvailableDates, fineFundamentalFolder);

            // sid,symbol,close,volume,dollar volume,has fundamental data,price factor,split factor
            var coarseFileLine = $"{sidContext.SID},{ticker.ToUpperInvariant()},{tradeBar.Close.Normalize()},{tradeBar.Volume.Normalize()},{Math.Truncate(dollarVolume)},{hasFundamentalData},{priceFactor},{splitFactor}";
            return coarseFileLine;
        }

        /// <summary>
        /// Checks if there is fundamental data for
        /// </summary>
        /// <param name="ticker">The ticker.</param>
        /// <param name="date">The date.</param>
        /// <param name="mapFile">The map file.</param>
        /// <param name="fineAvailableDates"></param>
        /// <param name="fineFundamentalFolder">The fine fundamental folder.</param>
        /// <returns></returns>
        private static bool CheckFundamentalData(DateTime date, MapFile mapFile, IEnumerable<DateTime> fineAvailableDates, DirectoryInfo fineFundamentalFolder)
        {
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
                    var previousTickerFineFundamentalFolder = Path.Combine(fineFundamentalFolder.FullName, previousTicker);
                    if (Directory.Exists(previousTickerFineFundamentalFolder))
                    {
                        var previousTickerFineAvailableDates = Directory.GetFiles(previousTickerFineFundamentalFolder, "*.zip")
                            .Select(f => DateTime.ParseExact(Path.GetFileNameWithoutExtension(f), DateFormat.EightCharacter, CultureInfo.InvariantCulture))
                            .ToList();
                        hasFundamentalDataForDate = previousTickerFineAvailableDates.Where(d => d >= firstDate).Any(d => date.AddMonths(-1) <= d && d <= date);
                    }
                }
            }

            return hasFundamentalDataForDate;
        }

        /// <summary>
        /// Parses the daily file.
        /// </summary>
        /// <param name="dailyFile">The daily file.</param>
        /// <returns></returns>
        private static List<TradeBar> ParseDailyFile(FileInfo dailyFile)
        {
            var scaleFactor = 1 / 10000m;

            var output = new List<TradeBar>();
            using (var fileStream = dailyFile.OpenRead())
            using (var stream = Compression.UnzipStreamToStreamReader(fileStream))
            {
                while (!stream.EndOfStream)
                {
                    var tradeBar = new TradeBar
                    {
                        Time = stream.GetDateTime(),
                        Open = stream.GetDecimal() * scaleFactor,
                        High = stream.GetDecimal() * scaleFactor,
                        Low = stream.GetDecimal() * scaleFactor,
                        Close = stream.GetDecimal() * scaleFactor,
                        Volume = stream.GetDecimal()
                    };
                    output.Add(tradeBar);
                }
            }

            return output;
        }

        /// <summary>
        /// Populates the sid contex.
        /// </summary>
        /// <param name="mapFileResolver">The map file resolver.</param>
        /// <param name="exclusions">The exclusions.</param>
        /// <returns></returns>
        private IEnumerable<SecurityIdentifierContext> PopulateSidContex(MapFileResolver mapFileResolver, HashSet<string> exclusions)
        {
            Log.Trace($"CoarseUniverseGeneratorProgram.PopulateSidContex(): Generating SID context from QuantQuote's map files.");
            foreach (var mapFile in mapFileResolver)
            {
                if (exclusions.Contains(mapFile.Last().MappedSymbol))
                {
                    continue;
                }

                yield return new SecurityIdentifierContext(mapFile, _market);
            }
        }
    }
}