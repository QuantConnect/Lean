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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuantConnect.Logging;
using QuantConnect.ToolBox.CoinApi;
using QuantConnect.Util;

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
        private readonly string _market;

        /// <summary>
        /// CoinAPI data converter.
        /// </summary>
        /// <param name="date">the processing date.</param>
        /// <param name="market">the exchange/market.</param>
        /// <param name="rawDataFolder">path to the raw data folder.</param>
        /// <param name="destinationFolder">destination of the newly generated files.</param>
        public CoinApiDataConverter(DateTime date, string market, string rawDataFolder, string destinationFolder)
        {
            _market = market;
            _processingDate = date;
            _rawDataFolder = new DirectoryInfo(Path.Combine(rawDataFolder, SecurityType.Crypto.ToLower(), market.ToLowerInvariant(), date.ToStringInvariant(DateFormat.EightCharacter)));
            if (!_rawDataFolder.Exists)
            {
                throw new ArgumentException($"CoinApiDataConverter(): Source folder not found: {_rawDataFolder.FullName}");
            }

            _destinationFolder = new DirectoryInfo(destinationFolder);
            _destinationFolder.Create();

            if (!SupportedMarkets.Contains(market.ToLowerInvariant()))
            {
                throw new ArgumentException($"CoinApiDataConverter(): Market/Exchange {market} not supported, yet. Supported Markets/Exchanges are {string.Join(" ", SupportedMarkets)}", market);
            }
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
            var fileToProcess = _rawDataFolder.EnumerateFiles("*.gz")
                .Where(f => f.Name.Split('_').Length == 4)
                .DistinctBy(
                    x =>
                    {
                        var parts = x.Name.Split('-').Take(2);
                        return string.Join("-", parts);
                    }
                );

            Parallel.ForEach(fileToProcess,(file, loopState) =>
                {
                    Log.Trace($"CoinApiDataConverter(): Starting data conversion from source file: {file.Name}...");
                    try
                    {
                        ProcessEntry(new CoinApiDataReader(symbolMapper), file);
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
            var entryData = coinapiDataReader.GetCoinApiEntryData(file, _processingDate, _market);

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
