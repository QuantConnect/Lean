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
using ICSharpCode.SharpZipLib.Tar;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.CoinApiDataConverter
{
    /// <summary>
    /// Console application for converting CoinApi raw data into Lean data format for high resolutions (tick, second and minute)
    /// </summary>
    public class CoinApiDataConverterProgram
    {
        /// <summary>
        /// List of supported exchanges
        /// </summary>
        private static readonly HashSet<string> SupportedMarkets = new[]
        {
            Market.GDAX,
            Market.Bitfinex
        }.ToHashSet();

        private DirectoryInfo _rawDataFolder;
        private DirectoryInfo _destinationFolder;
        private DateTime _processingDate;
        private string _market;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="date"></param>
        /// <param name="market"></param>
        /// <param name="rawDataFolder"></param>
        /// <param name="destinationFolder"></param>
        public CoinApiDataConverterProgram(DateTime date, string market, string rawDataFolder, string destinationFolder)
        {
            _market = market;
            _processingDate = date;
            _rawDataFolder = new DirectoryInfo(Path.Combine(rawDataFolder));
            if (!_rawDataFolder.Exists)
            {
                throw new ArgumentException($"CoinApiDataConverter(): Source folder not found: {_rawDataFolder.FullName}");
            }

            _destinationFolder = new DirectoryInfo(destinationFolder);
            _destinationFolder.Create();

            if (!SupportedMarkets.Contains(market.ToLower()))
            {
                throw new ArgumentException($"CoinApiDataConverter(): Market/Exchangue {market} not supported, yet. Supported Markets/Exchangues are {string.Join(" ", SupportedMarkets)}", market);
            }
        }

        /// <summary>
        /// CoinAPI data converter entry point.
        /// </summary>
        /// <param name="sourceDirectory">The source directory where all CoinAPI raw files are stored.</param>
        /// <exception cref="ArgumentException">Source folder does not exists.</exception>
        /// <remarks>This converter will automatically convert data for every exchange, date and tick type contained in each raw data file in the sourceDirectory</remarks>
        public bool Run()
        {
            var stopwatch = Stopwatch.StartNew();
            var coinapiDataReader = new CoinApiDataReader();

            foreach (var file in _rawDataFolder.EnumerateFiles("*.gz"))
            {
                Log.Trace($"CoinApiDataConverter(): Starting data conversion from source file: {file.Name}...");
                try
                {
                    ProcessEntry(coinapiDataReader, file);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"CoinApiDataConverter(): Error processing entry: {file.Name}");
                    return false;
                }
            }

            Log.Trace($"CoinApiDataConverter(): Finished in {stopwatch.Elapsed}");
            return true;
        }

        private void ProcessEntry(CoinApiDataReader coinapiDataReader, FileInfo file)
        {
            var entryData = coinapiDataReader.GetCoinApiEntryData(file, _processingDate, _market);

            if (!SupportedMarkets.Contains(entryData.Symbol.ID.Market))
            {
                // only convert data for supported exchanges
                return;
            }

            // materialize the enumerable into a list, since we need to enumerate over it twice
            var ticks = coinapiDataReader.ProcessCoinApiEntry(entryData, file).ToList();

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
                writer = new LeanDataWriter(consolidator.Resolution, entryData.Symbol, Globals.DataFolder, entryData.TickType);
                writer.Write(consolidator.Flush());
            }
        }
    }
}
