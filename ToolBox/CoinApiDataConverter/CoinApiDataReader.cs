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
using System.Globalization;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Tar;
using Ionic.Zlib;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.CoinApiDataConverter
{
    /// <summary>
    /// Reader class for CoinAPI crypto raw data.
    /// </summary>
    public class CoinApiDataReader
    {
        /// <summary>
        /// Gets the data for the given CoinAPI tar entry
        /// </summary>
        /// <param name="tar">The tar input stream</param>
        /// <param name="entry">The tar entry</param>
        /// <returns>A new instance of type <see cref="CoinApiEntryData"/></returns>
        public CoinApiEntryData GetCoinApiEntryData(TarInputStream tar, TarEntry entry)
        {
            var gzipFileName = entry.Name;
            Log.Trace($"CoinApiDataReader.ProcessTarEntry(): Processing entry: {gzipFileName}");

            // datatype-exchange-date-symbol/trades/COINBASE/2019/05/07/27781-COINBASE_SPOT_LTC_BTC.csv.gz
            var parts = gzipFileName.Split('/');
            if (parts.Length != 7)
            {
                throw new Exception($"CoinApiDataReader.ProcessTarEntry(): Unexpected entry path in tar file: {gzipFileName}");
            }

            var tickType = parts[1] == "trades" ? TickType.Trade : TickType.Quote;
            var market = parts[2] == "COINBASE" ? Market.GDAX : parts[2].ToLower();
            var year = Convert.ToInt32(parts[3]);
            var month = Convert.ToInt32(parts[4]);
            var day = Convert.ToInt32(parts[5]);
            var date = new DateTime(year, month, day);

            var nameParts = Path.GetFileNameWithoutExtension(parts[6].Substring(0, parts[6].IndexOf('.'))).Split('_');
            if (nameParts.Length != 4)
            {
                throw new Exception($"CoinApiDataReader.ProcessTarEntry(): Unexpected entry name in tar file: {gzipFileName}");
            }
            var ticker = nameParts[2] + nameParts[3];
            var symbol = Symbol.Create(ticker, SecurityType.Crypto, market);

            return new CoinApiEntryData
            {
                Name = gzipFileName,
                Symbol = symbol,
                TickType = tickType,
                Date = date
            };
        }

        /// <summary>
        /// Gets an enumerable of ticks for the given CoinAPI tar entry
        /// </summary>
        /// <param name="tar">The tar input stream</param>
        /// <param name="entryData">The entry data</param>
        /// <returns>An <see cref="IEnumerable{Tick}"/> for the ticks read from the entry</returns>
        public IEnumerable<Tick> ProcessCoinApiEntry(TarInputStream tar, CoinApiEntryData entryData)
        {
            Log.Trace("CoinApiDataReader.ProcessTarEntry(): Processing " +
                      $"{entryData.Symbol.ID.Market}-{entryData.Symbol.Value}-{entryData.TickType} " +
                      $"for {entryData.Date:yyyy-MM-dd}");

            using (var gzipStream = new MemoryStream())
            {
                tar.CopyEntryContents(gzipStream);
                gzipStream.Seek(0, SeekOrigin.Begin);

                using (var innerStream = new GZipStream(gzipStream, CompressionMode.Decompress))
                {
                    using (var reader = new StreamReader(innerStream))
                    {
                        var headerLine = reader.ReadLine();
                        if (headerLine == null)
                        {
                            throw new Exception($"CoinApiDataReader.ProcessTarEntry(): CSV header not found for entry name: {entryData.Name}");
                        }

                        var headerParts = headerLine.Split(';').ToList();

                        var ticks = entryData.TickType == TickType.Trade
                            ? ParseTradeData(entryData.Symbol, reader, headerParts)
                            : ParseQuoteData(entryData.Symbol, reader, headerParts);

                        foreach (var tick in ticks)
                        {
                            yield return tick;
                        }
                    }
                }
            }
        }

        private IEnumerable<Tick> ParseTradeData(Symbol symbol, StreamReader reader, List<string> headerParts)
        {
            var columnTime = headerParts.FindIndex(x => x == "time_exchange");
            var columnPrice = headerParts.FindIndex(x => x == "price");
            var columnQuantity = headerParts.FindIndex(x => x == "base_amount");

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var lineParts = line.Split(';');

                var time = DateTime.Parse(lineParts[columnTime], CultureInfo.InvariantCulture);
                var price = lineParts[columnPrice].ToDecimal();
                var quantity = lineParts[columnQuantity].ToDecimal();

                yield return new Tick
                {
                    Symbol = symbol,
                    Time = time,
                    Value = price,
                    Quantity = quantity,
                    TickType = TickType.Trade
                };
            }
        }

        private IEnumerable<Tick> ParseQuoteData(Symbol symbol, StreamReader reader, List<string> headerParts)
        {
            var columnTime = headerParts.FindIndex(x => x == "time_exchange");
            var columnAskPrice = headerParts.FindIndex(x => x == "ask_px");
            var columnAskSize = headerParts.FindIndex(x => x == "ask_sx");
            var columnBidPrice = headerParts.FindIndex(x => x == "bid_px");
            var columnBidSize = headerParts.FindIndex(x => x == "bid_sx");

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var lineParts = line.Split(';');

                var time = DateTime.Parse(lineParts[columnTime], CultureInfo.InvariantCulture);
                var askPrice = lineParts[columnAskPrice].ToDecimal();
                var askSize = lineParts[columnAskSize].ToDecimal();
                var bidPrice = lineParts[columnBidPrice].ToDecimal();
                var bidSize = lineParts[columnBidSize].ToDecimal();

                yield return new Tick
                {
                    Symbol = symbol,
                    Time = time,
                    AskPrice = askPrice,
                    AskSize = askSize,
                    BidPrice = bidPrice,
                    BidSize = bidSize,
                    TickType = TickType.Quote
                };
            }
        }
    }
}
