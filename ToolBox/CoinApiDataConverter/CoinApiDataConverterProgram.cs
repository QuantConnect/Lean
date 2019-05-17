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
    public static class CoinApiDataConverterProgram
    {
        /// <summary>
        /// List of supported exchanges
        /// </summary>
        private static readonly HashSet<string> SupportedMarkets = new[]
        {
            Market.GDAX,
            Market.Bitfinex
        }.ToHashSet();

        /// <summary>
        /// CoinAPI data converter entry point.
        /// </summary>
        /// <param name="sourceDirectory">The source directory where all CoinAPI raw files are stored.</param>
        /// <exception cref="ArgumentException">Source folder does not exists.</exception>
        /// <remarks>This converter will automatically convert data for every exchange, date and tick type contained in each raw data file in the sourceDirectory</remarks>
        public static void CoinApiDataConverter(string sourceDirectory)
        {
            var folderPath = new DirectoryInfo(sourceDirectory);
            if (!folderPath.Exists)
            {
                throw new ArgumentException($"CoinApiDataConverter(): Source folder not found: {folderPath.FullName}");
            }

            var stopwatch = Stopwatch.StartNew();

            var coinapiDataReader = new CoinApiDataReader();

            foreach (var fileName in folderPath.EnumerateFiles("*.tar"))
            {
                Log.Trace($"CoinApiDataConverter(): Starting data conversion from source file: {fileName.Name}...");

                using (var stream = new FileStream(fileName.FullName, FileMode.Open))
                {
                    using (var tar = new TarInputStream(stream))
                    {
                        TarEntry entry;
                        while ((entry = tar.GetNextEntry()) != null)
                        {
                            if (entry.IsDirectory) continue;

                            try
                            {
                                ProcessEntry(coinapiDataReader, tar, entry);
                            }
                            catch (Exception e)
                            {
                                Log.Error(e, $"CoinApiDataConverter(): Error processing entry: {entry.Name}");
                            }
                        }
                    }
                }
            }

            Log.Trace($"CoinApiDataConverter(): Finished in {stopwatch.Elapsed}");
        }

        private static void ProcessEntry(CoinApiDataReader coinapiDataReader, TarInputStream tar, TarEntry entry)
        {
            var entryData = coinapiDataReader.GetCoinApiEntryData(tar, entry);

            if (!SupportedMarkets.Contains(entryData.Symbol.ID.Market))
            {
                // only convert data for supported exchanges
                return;
            }

            // materialize the enumerable into a list, since we need to enumerate over it twice
            var ticks = coinapiDataReader.ProcessCoinApiEntry(tar, entryData).ToList();

            var writer = new LeanDataWriter(Resolution.Tick, entryData.Symbol, Globals.DataFolder, entryData.TickType);
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
