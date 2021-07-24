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
using System.IO;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;
using System.Diagnostics;
using QuantConnect.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using QuantConnect.ToolBox.CoinApi;

namespace QuantConnect.ToolBox.CoinApiDataConverter
{
    /// <summary>
    /// Console application for converting CoinApi raw data into Lean data format for high resolutions (tick, second and minute)
    /// </summary>
    public class CoinApiDataConverter
    {
        /// <summary>
        /// List of supported exchanges
        /// </summary>
        private static readonly HashSet<string> SupportedMarkets = new[]
        {
            Market.GDAX,
            Market.Bitfinex
        }.ToHashSet();

        private readonly DirectoryInfo _rawDataFolder;
        private readonly DirectoryInfo _destinationFolder;
        private readonly DateTime _processingDate;

        /// <summary>
        /// CoinAPI data converter.
        /// </summary>
        /// <param name="date">the processing date.</param>
        /// <param name="rawDataFolder">path to the raw data folder.</param>
        /// <param name="destinationFolder">destination of the newly generated files.</param>
        public CoinApiDataConverter(DateTime date, string rawDataFolder, string destinationFolder)
        {
            _processingDate = date;
            _rawDataFolder = new DirectoryInfo(Path.Combine(rawDataFolder, SecurityType.Crypto.ToLower(), "coinapi"));
            if (!_rawDataFolder.Exists)
            {
                throw new ArgumentException($"CoinApiDataConverter(): Source folder not found: {_rawDataFolder.FullName}");
            }

            _destinationFolder = new DirectoryInfo(destinationFolder);
            _destinationFolder.Create();
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        /// <returns></returns>
        public bool Run()
        {
            var stopwatch = Stopwatch.StartNew();

            var symbolMapper = new CoinApiSymbolMapper();
            var success = true;

            // There were cases of files with with an extra suffix, following pattern:
            // <TickType>-<ID>-<Exchange>_SPOT_<BaseCurrency>_<QuoteCurrency>_<ExtraSuffix>.csv.gz
            // Those cases should be ignored for SPOT prices.
            var tradesFolder = new DirectoryInfo(
                Path.Combine(
                    _rawDataFolder.FullName, 
                    "trades", 
                    _processingDate.ToStringInvariant(DateFormat.EightCharacter)));

            var quotesFolder = new DirectoryInfo(
                Path.Combine(
                    _rawDataFolder.FullName,
                    "quotes",
                    _processingDate.ToStringInvariant(DateFormat.EightCharacter)));

            // Distinct by tick type and first two parts of the raw file name, separated by '-'.
            // This prevents us from double processing the same ticker twice, in case we're given
            // two raw data files for the same symbol. Related: https://github.com/QuantConnect/Lean/pull/3262
            var apiDataReader = new CoinApiDataReader(symbolMapper);
            var filesToProcessCandidates = tradesFolder.EnumerateFiles("*.gz")
                .Concat(quotesFolder.EnumerateFiles("*.gz"))
                .Where(f => f.Name.Contains("SPOT"))
                .Where(f => f.Name.Split('_').Length == 4)
                .ToList();

            var filesToProcessKeys = new HashSet<string>();
            var filesToProcess = new List<FileInfo>();

            foreach (var candidate in filesToProcessCandidates)
            {
                try
                {
                    var key = candidate.Directory.Parent.Name + apiDataReader.GetCoinApiEntryData(candidate, _processingDate).Symbol.ID;
                    if (filesToProcessKeys.Add(key))
                    {
                        // Separate list from HashSet to preserve ordering of viable candidates
                        filesToProcess.Add(candidate);
                    }
                }
                catch (Exception err)
                {
                    // Most likely the exchange isn't supported. Log exception message to avoid excessive stack trace spamming in console output 
                    Log.Error(err.Message);
                }
            }

            Parallel.ForEach(filesToProcess, (file, loopState) =>
                {
                    Log.Trace($"CoinApiDataConverter(): Starting data conversion from source file: {file.Name}...");
                    try
                    {
                        ProcessEntry(apiDataReader, file);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, $"CoinApiDataConverter(): Error processing entry: {file.Name}");
                        success = false;
                        loopState.Break();
                    }
                }
            );

            Log.Trace($"CoinApiDataConverter(): Finished in {stopwatch.Elapsed}");
            return success;
        }

        /// <summary>
        /// Processes the entry.
        /// </summary>
        /// <param name="coinapiDataReader">The coinapi data reader.</param>
        /// <param name="file">The file.</param>
        private void ProcessEntry(CoinApiDataReader coinapiDataReader, FileInfo file)
        {
            var entryData = coinapiDataReader.GetCoinApiEntryData(file, _processingDate);

            if (!SupportedMarkets.Contains(entryData.Symbol.ID.Market))
            {
                // only convert data for supported exchanges
                return;
            }

            // materialize the enumerable into a list, since we need to enumerate over it twice
            var ticks = coinapiDataReader.ProcessCoinApiEntry(entryData, file).OrderBy(t => t.Time).ToList();

            var writer = new LeanDataWriter(Resolution.Tick, entryData.Symbol, _destinationFolder.FullName, entryData.TickType);
            writer.Write(ticks);

            Log.Trace($"CoinApiDataConverter(): Starting consolidation for {entryData.Symbol.Value} {entryData.TickType}");
            var consolidators = new List<TickAggregator>();

            if (entryData.TickType == TickType.Trade)
            {
                consolidators.AddRange(new[]
                {
                    new TradeTickAggregator(Resolution.Second),
                    new TradeTickAggregator(Resolution.Minute)
                });
            }
            else
            {
                consolidators.AddRange(new[]
                {
                    new QuoteTickAggregator(Resolution.Second),
                    new QuoteTickAggregator(Resolution.Minute)
                });
            }

            foreach (var tick in ticks)
            {
                if (tick.Suspicious)
                {
                    // When CoinAPI loses connectivity to the exchange, they indicate
                    // it in the data by providing a value of `-1` for bid/ask price.
                    // We will keep it in tick data, but will remove it from consolidated data.
                    continue;
                }
                
                foreach (var consolidator in consolidators)
                {
                    consolidator.Update(tick);
                }
            }

            foreach (var consolidator in consolidators)
            {
                writer = new LeanDataWriter(consolidator.Resolution, entryData.Symbol, _destinationFolder.FullName, entryData.TickType);
                writer.Write(consolidator.Flush());
            }
        }
    }
}
