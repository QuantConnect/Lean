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
using QuantConnect.Lean.Engine.DataFeeds;
using DateTime = System.DateTime;
using Log = QuantConnect.Logging.Log;
using QuantConnect.Data.UniverseSelection;
using static QuantConnect.Data.UniverseSelection.CoarseFundamentalDataProvider;
using QuantConnect.Data.Fundamental;

namespace QuantConnect.ToolBox.CoarseUniverseGenerator
{
    /// <summary>
    /// Coarse
    /// </summary>
    public class CoarseUniverseGeneratorProgram
    {
        /// <summary>
        /// Has fundamental data source
        /// </summary>
        public const FundamentalProperty HasFundamentalSource = FundamentalProperty.CompanyReference_CompanyId;

        private static readonly object _lock = new object();
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
            var dataProvider = new DefaultDataProvider();
            var mapFileProvider = new LocalDiskMapFileProvider();
            mapFileProvider.Initialize(dataProvider);
            var factorFileProvider = new LocalDiskFactorFileProvider();
            factorFileProvider.Initialize(mapFileProvider, dataProvider);
            FundamentalService.Initialize(dataProvider, nameof(CoarseFundamentalDataProvider), false);
            var generator = new CoarseUniverseGeneratorProgram(dailyDataFolder, destinationFolder, Market.USA, blackListedTickersFile, reservedWordPrefix, mapFileProvider, factorFileProvider);
            return generator.Run(out _, out _);
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
        public CoarseUniverseGeneratorProgram(
            DirectoryInfo dailyDataFolder,
            DirectoryInfo destinationFolder,
            string market,
            FileInfo blackListedTickersFile,
            string reservedWordsPrefix,
            IMapFileProvider mapFileProvider,
            IFactorFileProvider factorFileProvider,
            bool debugEnabled = false)
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
        public bool Run(out ConcurrentDictionary<SecurityIdentifier, List<CoarseFundamental>> coarsePerSecurity, out DateTime[] dates)
        {
            var startTime = DateTime.UtcNow;
            var success = true;
            Log.Trace($"CoarseUniverseGeneratorProgram.ProcessDailyFolder(): Processing: {_dailyDataFolder.FullName}");

            var symbolsProcessed = 0;
            var filesRead = 0;
            var dailyFilesNotFound = 0;
            var coarseFilesGenerated = 0;

            var mapFileResolver = _mapFileProvider.Get(new AuxiliaryDataKey(_market, SecurityType.Equity));

            var result = coarsePerSecurity = new();
            dates = Array.Empty<DateTime>();

            var blackListedTickers = new HashSet<string>();
            if (_blackListedTickersFile.Exists)
            {
                blackListedTickers = File.ReadAllLines(_blackListedTickersFile.FullName).ToHashSet();
            }

            var securityIdentifierContexts = PopulateSidContex(mapFileResolver, blackListedTickers);
            var dailyPricesByTicker = new ConcurrentDictionary<string, List<TradeBar>>();
            var outputCoarseContent = new ConcurrentDictionary<DateTime, List<CoarseFundamental>>();

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };
            try
            {
                Parallel.ForEach(securityIdentifierContexts, parallelOptions, sidContext =>
                {
                    var coarseForSecurity = new List<CoarseFundamental>();
                    var symbol = new Symbol(sidContext.SID, sidContext.LastTicker);
                    var symbolCount = Interlocked.Increment(ref symbolsProcessed);
                    Log.Debug($"CoarseUniverseGeneratorProgram.Run(): Processing {symbol} with tickers: '{string.Join(",", sidContext.Tickers)}'");
                    var factorFile = _factorFileProvider.Get(symbol);

                    // Populate dailyPricesByTicker with all daily data by ticker for all tickers of this security.
                    foreach (var ticker in sidContext.Tickers)
                    {
                        var pathFile = Path.Combine(_dailyDataFolder.FullName, $"{ticker}.zip");
                        var dailyFile = new FileInfo(pathFile);
                        if (!dailyFile.Exists)
                        {
                            Log.Debug($"CoarseUniverseGeneratorProgram.Run(): {dailyFile.FullName} not found, looking for daily data in data folder");

                            dailyFile = new FileInfo(Path.Combine(Globals.DataFolder, "equity", "usa", "daily", $"{ticker}.zip"));
                            if (!dailyFile.Exists)
                            {
                                Log.Error($"CoarseUniverseGeneratorProgram.Run(): {dailyFile} not found!");
                                Interlocked.Increment(ref dailyFilesNotFound);
                                continue;
                            }
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

                        // Get daily data only for the time the ticker was
                        foreach (var tradeBar in tickerDailyData.Where(tb => tb.Time >= startDate && tb.Time <= endDate))
                        {
                            var coarseFundamental = GenerateFactorFileRow(ticker, sidContext, factorFile as CorporateFactorProvider, tradeBar);
                            coarseForSecurity.Add(coarseFundamental);

                            outputCoarseContent.AddOrUpdate(tradeBar.Time,
                                new List<CoarseFundamental> { coarseFundamental },
                                (time, list) =>
                            {
                                lock (list)
                                {
                                    list.Add(coarseFundamental);
                                    return list;
                                }
                            });
                        }
                    }

                    if(coarseForSecurity.Count > 0)
                    {
                        result[sidContext.SID] = coarseForSecurity;
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
                    File.WriteAllLines(filePath, coarseByDate.Value.Select(x => CoarseFundamental.ToRow(x)).OrderBy(cr => cr));
                    var filesCount = Interlocked.Increment(ref coarseFilesGenerated);
                    if (filesCount % 1000 == 0)
                    {
                        var elapsed = DateTime.UtcNow - startWriting;
                        Log.Trace($"CoarseUniverseGeneratorProgram.Run(): Processed {filesCount} in {elapsed:g} at {filesCount / elapsed.TotalSeconds:F2} files/second ");
                    }
                });

                dates = outputCoarseContent.Keys.OrderBy(x => x).ToArray();
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
        private static CoarseFundamental GenerateFactorFileRow(string ticker, SecurityIdentifierContext sidContext, CorporateFactorProvider factorFile, TradeBar tradeBar)
        {
            var date = tradeBar.Time;
            var factorFileRow = factorFile?.GetScalingFactors(date);
            var dollarVolume = Math.Truncate((double)(tradeBar.Close * tradeBar.Volume));
            var priceFactor = factorFileRow?.PriceFactor.Normalize() ?? 1m;
            var splitFactor = factorFileRow?.SplitFactor.Normalize() ?? 1m;
            var hasFundamentalData = CheckFundamentalData(date, sidContext.SID);

            // sid,symbol,close,volume,dollar volume,has fundamental data,price factor,split factor
            return new CoarseFundamentalSource
            {
                Symbol = new Symbol(sidContext.SID, ticker),
                Value = tradeBar.Close.Normalize(),
                Time = date,
                VolumeSetter = decimal.ToInt64(tradeBar.Volume),
                DollarVolumeSetter = dollarVolume,
                PriceFactorSetter = priceFactor,
                SplitFactorSetter = splitFactor,
                HasFundamentalDataSetter = hasFundamentalData
            };
        }

        /// <summary>
        /// Checks if there is fundamental data for
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="sid">The security identifier.</param>
        /// <returns>True if fundamental data is available</returns>
        private static bool CheckFundamentalData(DateTime date, SecurityIdentifier sid)
        {
            return !string.IsNullOrEmpty(FundamentalService.Get<string>(date, sid, HasFundamentalSource));
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
            Log.Trace("CoarseUniverseGeneratorProgram.PopulateSidContex(): Generating SID context from QuantQuote's map files.");
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
