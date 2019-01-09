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

                        var rawData = KaikoCryptoReader.GetRawDataFromEntry(zipEntry);
                        var ticks = tickType == TickType.Trade ? ParseKaikoTradeFile(rawData, symbol) : ParseKaikoQuoteFile(rawData, symbol);
                        Log.Trace($"KaikoDataConverter(): Processing {symbol.Value} {tickType}");

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

                            Log.Trace($"KaikoDataConverter(): Consolidation finished for {symbol.Value} {tickType}");
                            Log.Trace($"KaikoDataConverter(): Save minute and second files for {symbol.Value} {tickType}");

                            foreach (var consolidator in consolidators)
                            {
                                WriteTicksForResolution(symbol, consolidator.Resolution, tickType, consolidator.Flush());
                            }

                            Log.Trace($"KaikoDataConverter(): Save tick files for {symbol.Value} {tickType}");

                            var writer = new LeanDataWriter(Resolution.Tick, symbol, Globals.DataFolder, tickType);
                            writer.Write(ticks);

                            Log.Trace($"KaikoDataConverter(): Tick files saved for {symbol.Value} {tickType}");
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

        public static List<TickAggregator> GetHighResolutionDataAggregatorsForTickType(TickType tickType)
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
        /// <param name="tickType">The tpye (Trades/Quotes) </param>
        /// <param name="bars">The aggregated bars being written to disk</param>
        public static void WriteTicksForResolution(Symbol symbol, Resolution resolution, TickType tickType, List<BaseData> bars)
        {
            var writer = new LeanDataWriter(resolution, symbol, Globals.DataFolder, tickType);
            writer.Write(bars);
        }

        /// <summary>
        /// Parse order book information for Kaiko data files
        /// </summary>
        /// <param name="symbol">The symbol being converted</param>
        /// <param name="unzippedFile">The path to the unzipped file</param>
        /// <returns>Lean quote ticks representing the Kaiko data</returns>
        public static IEnumerable<Tick> ParseKaikoQuoteFile(IEnumerable<string> rawDataLines, Symbol symbol)
        {
            var headerLine = rawDataLines.First();
            var headerCsv = headerLine.ToCsv();
            var typeColumn = headerCsv.FindIndex(x => x == "type");
            var dateColumn = headerCsv.FindIndex(x => x == "date");
            var priceColumn = headerCsv.FindIndex(x => x == "price");
            var quantityColumn = headerCsv.FindIndex(x => x == "amount");

            long currentEpoch = 0;
            var currentEpochTicks = new List<KaikoTick>();

            foreach (var line in rawDataLines.Skip(1))
            {
                if (line == null || line == string.Empty) continue;

                var lineParts = line.Split(',');

                var tickEpoch = Convert.ToInt64(lineParts[dateColumn]);

                decimal quantity;
                decimal price;

                try
                {
                    quantity = ParseScientificNotationToDecimal(lineParts, quantityColumn);
                    price = ParseScientificNotationToDecimal(lineParts, priceColumn);
                }
                catch (Exception ex)
                {
                    Log.Error($"KaikoDataConverter.ParseKaikoQuoteFile(): Raw data corrupted. Line {string.Join(" ", lineParts)}, Exception {ex}");
                    continue;
                }

                var currentTick = new KaikoTick
                {
                    TickType = TickType.Quote,
                    Time = Time.UnixMillisecondTimeStampToDateTime(tickEpoch),
                    Quantity = quantity,
                    Value = price,
                    OrderDirection = lineParts[typeColumn]
                };

                if (currentEpoch != tickEpoch)
                {
                    var quoteTick = CreateQuoteTick(symbol, Time.UnixMillisecondTimeStampToDateTime(currentEpoch), currentEpochTicks);

                    if (quoteTick != null) yield return quoteTick;

                    currentEpochTicks.Clear();
                    currentEpoch = tickEpoch;
                }

                currentEpochTicks.Add(currentTick);
            }
        }

        /// <summary>
        /// Take a minute snapshot of order book information and make a single Lean quote tick
        /// </summary>
        /// <param name="symbol">The symbol being processed</param>
        /// <param name="date">The data being processed</param>
        /// <param name="currentEpcohTicks">The snapshot of bid/ask Kaiko data</param>
        /// <returns>A single Lean quote tick</returns>
        public static Tick CreateQuoteTick(Symbol symbol, DateTime date, List<KaikoTick> currentEpcohTicks)
        {
            // lowest ask
            var bestAsk = currentEpcohTicks.Where(x => x.OrderDirection == "a")
                                        .OrderBy(x => x.Value)
                                        .FirstOrDefault();

            // highest bid
            var bestBid = currentEpcohTicks.Where(x => x.OrderDirection == "b")
                                        .OrderByDescending(x => x.Value)
                                        .FirstOrDefault();

            if (bestAsk == null && bestBid == null)
            {
                // Did not have enough data to create a tick
                return null;
            }

            var tick = new Tick()
            {
                Symbol = symbol,
                Time = date,
                TickType = TickType.Quote
            };

            if (bestBid != null)
            {
                tick.BidPrice = bestBid.Price;
                tick.BidSize = bestBid.Quantity;
            }

            if (bestAsk != null)
            {
                tick.AskPrice = bestAsk.Price;
                tick.AskSize = bestAsk.Quantity;
            }

            return tick;
        }

        /// <summary>
        /// Parse a kaiko trade file
        /// </summary>
        /// <param name="symbol">The symbol being processed</param>
        /// <param name="unzippedFile">The path to the unzipped file</param>
        /// <returns>Lean Ticks in the Kaiko file</returns>
        public static IEnumerable<Tick> ParseKaikoTradeFile(IEnumerable<string> rawDataLines, Symbol symbol)
        {
            var headerLine = rawDataLines.First();
            var headerCsv = headerLine.ToCsv();
            var dateColumn = headerCsv.FindIndex(x => x == "date");
            var priceColumn = headerCsv.FindIndex(x => x == "price");
            var quantityColumn = headerCsv.FindIndex(x => x == "amount");

            foreach (var line in rawDataLines.Skip(1))
            {
                if (line == null || line == string.Empty) continue;

                var lineParts = line.Split(',');

                decimal quantity;
                decimal price;

                try
                {
                    quantity = ParseScientificNotationToDecimal(lineParts, quantityColumn);
                    price = ParseScientificNotationToDecimal(lineParts, priceColumn);
                }
                catch (Exception ex)
                {
                    Log.Error($"KaikoDataConverter.ParseKaikoTradeFile(): Raw data corrupted. Line {string.Join(" ", lineParts)}, Exception {ex}");
                    continue;
                }

                yield return new Tick
                {
                    Symbol = symbol,
                    TickType = TickType.Trade,
                    Time = Time.UnixMillisecondTimeStampToDateTime(Convert.ToInt64(lineParts[dateColumn])),
                    Quantity = quantity,
                    Value = price
                };
            }
        }

        /// <summary>
        /// Parse the quantity field of the kaiko ticks - can sometimes be expressed in scientific notation
        /// </summary>
        /// <param name="lineParts">The line from the Kaiko file</param>
        /// <param name="column">The index of the quantity column </param>
        /// <returns>The quantity as a decimal</returns>
        public static decimal ParseScientificNotationToDecimal(string[] lineParts, int column)
        {
            var value = lineParts[column];
            if (value.Contains("e"))
            {
                return Decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            return Convert.ToDecimal(lineParts[column], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Simple class to add order direction to Tick
        /// used for aggregating Kaiko order book snapshots
        /// </summary>
        public class KaikoTick : Tick
        {
            public string OrderDirection { get; set; }
        }
    }
}