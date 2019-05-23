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
using ikvm.extensions;
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
        public CoinApiEntryData GetCoinApiEntryData(FileInfo file, DateTime processingDate, string market)
        {
            // crypto/<market>/<date>/<ticktype>-563-BITFINEX_SPOT_BTC_USD.csv.gz

            var tickType = file.FullName.Contains("trades") ? TickType.Trade : TickType.Quote;

            var filenameParts = Path.GetFileNameWithoutExtension(file.Name).Split('_');
            var pairs = filenameParts.Skip(filenameParts.Length - 2).ToArray();

            var ticker = pairs[0] + pairs[1];
            var symbol = Symbol.Create(ticker, SecurityType.Crypto, market);

            return new CoinApiEntryData
            {
                Name = file.Name,
                Symbol = symbol,
                TickType = tickType,
                Date = processingDate
            };
        }

        /// <summary>
        /// Gets an enumerable of ticks for the given CoinAPI tar entry
        /// </summary>
        /// <param name="tar">The tar input stream</param>
        /// <param name="entryData">The entry data</param>
        /// <returns>An <see cref="IEnumerable{Tick}"/> for the ticks read from the entry</returns>
        public IEnumerable<Tick> ProcessCoinApiEntry(CoinApiEntryData entryData, FileInfo file)
        {
            Log.Trace("CoinApiDataReader.ProcessTarEntry(): Processing " +
                      $"{entryData.Symbol.ID.Market}-{entryData.Symbol.Value}-{entryData.TickType} " +
                      $"for {entryData.Date:yyyy-MM-dd}");

            var innerStream = StreamProvider.ForExtension(file.Extension).Open(file.FullName).First();

            using (innerStream)
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
