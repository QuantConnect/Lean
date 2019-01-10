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

using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using ZipFile = Ionic.Zip.ZipFile;

namespace QuantConnect.ToolBox.KaikoDataConverter
{
    /// <summary>
    /// Console application for converting a single day of Kaiko data into Lean data format for high resolutions (tick, second and minute)
    /// </summary>
    public static class KaikoDataConverterProgram
    {
        /// <summary>
        /// Kaiko data converter entry point.
        /// </summary>
        /// <param name="sourceDirectory">The source directory where all Kaiko zipped files are stored..</param>
        /// <param name="date">The date to process.</param>
        /// <exception cref="ArgumentException">Source folder does not exists.</exception>
        /// <remarks>This converter will process automatically data for every exchange and for both tick types if the raw data files are available in the sourceDirectory</remarks>
        public static void KaikoDataConverter(string sourceDirectory, string date)
        {
            var timer = new Stopwatch();
            timer.Start();
            var folderPath = new DirectoryInfo(sourceDirectory);
            if (!folderPath.Exists)
            {
                throw new ArgumentException($"Source folder {folderPath.FullName} not found");
            }

            var processingDate = DateTime.ParseExact(date, DateFormat.EightCharacter, CultureInfo.InvariantCulture);
            foreach (var filePath in folderPath.EnumerateFiles("*.zip"))
            {
                Log.Trace($"KaikoDataConverter(): Starting data conversion from source {filePath.Name} for date {processingDate:yyyy_MM_dd}... ");
                using (var zip = new ZipFile(filePath.FullName))
                {
                    var targetDayEntries = zip.Entries.Where(e => e.FileName.Contains($"{processingDate:yyyy_MM_dd}"));
                        
                    if (targetDayEntries.Count() == 0)
                    {
                        Log.Error($"KaikoDataConverter(): Date {processingDate:yyyy_MM_dd} not found in source file {filePath.FullName}.");
                    }

                    foreach (var zipEntry in targetDayEntries)
                    {
                        var nameParts = zipEntry.FileName.Split(new char[] { '/' }).Last().Split(new char[] { '_' });
                        var exchange = nameParts[0] == "Coinbase" ? "GDAX" : nameParts[0];
                        var ticker = nameParts[1];
                        var tickType = nameParts[2] == "trades" ? TickType.Trade : TickType.Quote;
                        var symbol = Symbol.Create(ticker, SecurityType.Crypto, exchange);

                        Log.Trace($"KaikoDataConverter(): Processing {symbol.Value} {tickType}");

                        // Generate ticks from raw data and write them to disk
                        var ticks = KaikoDataReader.GetTicksFromZipEntry(zipEntry, symbol, tickType);
                        var writer = new LeanDataWriter(Resolution.Tick, symbol, Globals.DataFolder, tickType);
                        writer.Write(ticks);

                        try
                        {
                            Log.Trace($"KaikoDataConverter(): Starting consolidation for {symbol.Value} {tickType}");
                            var consolidators = GetHighResolutionDataAggregatorsForTickType(tickType);

                            foreach (var tick in ticks)
                            {
                                foreach (var consolidator in consolidators)
                                {
                                    consolidator.Consolidator.Update(tick);
                                }
                            }

                            foreach (var consolidator in consolidators)
                            {
                                WriteTicksForResolution(symbol, consolidator.Resolution, tickType, consolidator.Flush());
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error($"KaikoDataConverter(): Error processing entry {zipEntry.FileName}. Exception {e}");
                        }
                    }
                }
            }
            Log.Trace($"KaikoDataConverter(): Finished in {timer.Elapsed}");
        }

        /// <summary>
        /// Gets the aggregators for minute and second resolutions.
        /// </summary>
        /// <param name="tickType">The tick type (Trades/Quotes)</param>
        /// <returns></returns>
        private static List<TickAggregator> GetHighResolutionDataAggregatorsForTickType(TickType tickType)
        {
            if (tickType == TickType.Quote)
            {
                return new List<TickAggregator>
                {
                    new QuoteTickAggregator(Resolution.Second),
                    new QuoteTickAggregator(Resolution.Minute),
                };
            }

            return new List<TickAggregator>
            {
                new TradeTickAggregator(Resolution.Second),
                new TradeTickAggregator(Resolution.Minute),
            };
        }

        /// <summary>
        /// Use the lean data writer to write the ticks for a specific resolution
        /// </summary>
        /// <param name="symbol">The symbol these ticks represent</param>
        /// <param name="resolution">The resolution that should be written</param>
        /// <param name="tickType">The tick type (Trades/Quotes) </param>
        /// <param name="bars">The aggregated bars being written to disk</param>
        private static void WriteTicksForResolution(Symbol symbol, Resolution resolution, TickType tickType, List<BaseData> bars)
        {
            var writer = new LeanDataWriter(resolution, symbol, Globals.DataFolder, tickType);
            writer.Write(bars);
        }
    }
}