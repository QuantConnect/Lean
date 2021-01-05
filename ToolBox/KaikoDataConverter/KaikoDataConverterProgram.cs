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

using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// <param name="exchange">The exchange to process, if not defined, all exchanges will be processed.</param>
        /// <exception cref="ArgumentException">Source folder does not exists.</exception>
        /// <remarks>This converter will process automatically data for every exchange and for both tick types if the raw data files are available in the sourceDirectory</remarks>
        public static void KaikoDataConverter(string sourceDirectory, string date, string exchange = "")
        {
            var timer = new Stopwatch();
            timer.Start();
            var folderPath = new DirectoryInfo(sourceDirectory);
            if (!folderPath.Exists)
            {
                throw new ArgumentException($"Source folder {folderPath.FullName} not found");
            }

            exchange = exchange != string.Empty && exchange.ToLowerInvariant() == "gdax" ? "coinbase" : exchange;

            var processingDate = Parse.DateTimeExact(date, DateFormat.EightCharacter);
            foreach (var filePath in folderPath.EnumerateFiles("*.zip"))
            {
                // Do not process exchanges other than the one defined.
                if (exchange != string.Empty && !filePath.Name.ToLowerInvariant().Contains(exchange.ToLowerInvariant())) continue;

                Log.Trace($"KaikoDataConverter(): Starting data conversion from source {filePath.Name} for date {processingDate:yyyy_MM_dd}... ");
                using (var zip = new ZipFile(filePath.FullName))
                {
                    var targetDayEntries = zip.Entries.Where(e => e.FileName.Contains($"{processingDate.ToStringInvariant("yyyy_MM_dd")}")).ToList();

                    if (!targetDayEntries.Any())
                    {
                        Log.Error($"KaikoDataConverter(): Date {processingDate:yyyy_MM_dd} not found in source file {filePath.FullName}.");
                    }

                    foreach (var zipEntry in targetDayEntries)
                    {
                        var nameParts = zipEntry.FileName.Split(new char[] { '/' }).Last().Split(new char[] { '_' });
                        var market = nameParts[0] == "Coinbase" ? "GDAX" : nameParts[0];
                        var ticker = nameParts[1];
                        var tickType = nameParts[2] == "trades" ? TickType.Trade : TickType.Quote;
                        var symbol = Symbol.Create(ticker, SecurityType.Crypto, market);

                        Log.Trace($"KaikoDataConverter(): Processing {symbol.Value} {tickType}");

                        // Generate ticks from raw data and write them to disk

                        var reader = new KaikoDataReader(symbol, tickType);
                        var ticks = reader.GetTicksFromZipEntry(zipEntry);

                        var writer = new LeanDataWriter(Resolution.Tick, symbol, Globals.DataFolder, tickType);
                        writer.Write(ticks);

                        try
                        {
                            Log.Trace($"KaikoDataConverter(): Starting consolidation for {symbol.Value} {tickType}");
                            List<TickAggregator> consolidators = new List<TickAggregator>();

                            if (tickType == TickType.Trade)
                            {
                                consolidators.AddRange(new[]
                                {
                                    new TradeTickAggregator(Resolution.Second),
                                    new TradeTickAggregator(Resolution.Minute),
                                });
                            }
                            else
                            {
                                consolidators.AddRange(new[]
                                {
                                    new QuoteTickAggregator(Resolution.Second),
                                    new QuoteTickAggregator(Resolution.Minute),
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
                                writer = new LeanDataWriter(consolidator.Resolution, symbol, Globals.DataFolder, tickType);
                                writer.Write(consolidator.Flush());
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
    }
}