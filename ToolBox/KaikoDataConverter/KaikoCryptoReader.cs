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

using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ZipEntry = Ionic.Zip.ZipEntry;

namespace QuantConnect.ToolBox.KaikoDataConverter
{
    /// <summary>
    /// Decompress single entry from Kaiko crypto raw data.
    /// </summary>
    public class KaikoDataReader
    {
        private Symbol _symbol;
        private TickType _tickType;

        /// <summary>
        /// Initializes a new instance of the <see cref="KaikoDataReader"/> class.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="tickType">Type of the tick.</param>
        public KaikoDataReader(Symbol symbol, TickType tickType)
        {
            _symbol = symbol;
            _tickType = tickType;
        }

        /// <summary>
        /// Gets the ticks from Kaiko file zip entry.
        /// </summary>
        /// <param name="zipEntry">The zip entry.</param>
        /// <returns></returns>
        public IEnumerable<BaseData> GetTicksFromZipEntry(ZipEntry zipEntry)
        {
            var rawData = GetRawDataStreamFromEntry(zipEntry);
            return _tickType == TickType.Trade ? ParseKaikoTradeFile(rawData) : ParseKaikoQuoteFile(rawData);
        }

        /// <summary>
        /// Gets the raw data from entry.
        /// </summary>
        /// <param name="zipEntry">The zip entry.</param>
        /// <returns>IEnumerable with the zip entry content.</returns>
        private IEnumerable<string> GetRawDataStreamFromEntry(ZipEntry zipEntry)
        {
            using (var outerStream = new StreamReader(zipEntry.OpenReader()))
            using (var innerStream = new GZipStream(outerStream.BaseStream, CompressionMode.Decompress))
            using (var outputStream = new StreamReader(innerStream))
            {
                string line;
                while ((line = outputStream.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        /// <summary>
        /// Parse order book information for Kaiko data files
        /// </summary>
        /// <param name="rawDataLines">The raw data lines.</param>
        /// <returns>
        /// IEnumerable of ticks representing the Kaiko data
        /// </returns>
        private IEnumerable<Tick> ParseKaikoQuoteFile(IEnumerable<string> rawDataLines)
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

                var tickEpoch = Parse.Long(lineParts[dateColumn]);

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
                    var quoteTick = CreateQuoteTick(Time.UnixMillisecondTimeStampToDateTime(currentEpoch), currentEpochTicks);

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
        /// <param name="date">The data being processed</param>
        /// <param name="currentEpcohTicks">The snapshot of bid/ask Kaiko data</param>
        /// <returns>A single Lean quote tick</returns>
        private Tick CreateQuoteTick(DateTime date, List<KaikoTick> currentEpcohTicks)
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
                Symbol = _symbol,
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
        /// <param name="unzippedFile">The path to the unzipped file</param>
        /// <returns>Lean Ticks in the Kaiko file</returns>
        private IEnumerable<Tick> ParseKaikoTradeFile(IEnumerable<string> rawDataLines)
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
                    Symbol = _symbol,
                    TickType = TickType.Trade,
                    Time = Time.UnixMillisecondTimeStampToDateTime(Parse.Long(lineParts[dateColumn])),
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
        private static decimal ParseScientificNotationToDecimal(string[] lineParts, int column)
        {
            var value = lineParts[column];
            if (value.Contains("e"))
            {
                return Parse.Decimal(value, NumberStyles.Float);
            }

            return lineParts[column].ConvertInvariant<decimal>();
        }

        /// <summary>
        /// Simple class to add order direction to Tick
        /// used for aggregating Kaiko order book snapshots
        /// </summary>
        private class KaikoTick : Tick
        {
            public string OrderDirection { get; set; }
        }
    }
}