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
using System.IO.Compression;
using System.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using CompressionMode = System.IO.Compression.CompressionMode;

namespace QuantConnect.ToolBox.CoinApiDataConverter
{
    /// <summary>
    /// Reader class for CoinAPI crypto raw data.
    /// </summary>
    public class CoinApiDataReader
    {
        private readonly ISymbolMapper _symbolMapper;

        /// <summary>
        /// Creates a new instance of the <see cref="CoinApiDataReader"/> class
        /// </summary>
        /// <param name="symbolMapper">The symbol mapper</param>
        public CoinApiDataReader(ISymbolMapper symbolMapper)
        {
            _symbolMapper = symbolMapper;
        }

        /// <summary>
        /// Gets the coin API entry data.
        /// </summary>
        /// <param name="file">The source file.</param>
        /// <param name="processingDate">The processing date.</param>
        /// <param name="market">The market/exchange.</param>
        /// <returns></returns>
        public CoinApiEntryData GetCoinApiEntryData(FileInfo file, DateTime processingDate, string market)
        {
            // crypto/<market>/<date>/<ticktype>-563-BITFINEX_SPOT_BTC_USD.csv.gz
            var tickType = file.FullName.Contains("trade") ? TickType.Trade : TickType.Quote;

            var symbolId = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file.Name)).Split('-').Last();

            var symbol = _symbolMapper.GetLeanSymbol(symbolId, SecurityType.Crypto, market);

            return new CoinApiEntryData
            {
                Name = file.Name,
                Symbol = symbol,
                TickType = tickType,
                Date = processingDate
            };
        }

        /// <summary>
        /// Gets an list of ticks for a given CoinAPI source file.
        /// </summary>
        /// <param name="entryData">The entry data.</param>
        /// <param name="file">The file.</param>
        /// <returns></returns>
        /// <exception cref="Exception">CoinApiDataReader.ProcessCoinApiEntry(): CSV header not found for entry name: {entryData.Name}</exception>
        public IEnumerable<Tick> ProcessCoinApiEntry(CoinApiEntryData entryData, FileInfo file)
        {
            Log.Trace("CoinApiDataReader.ProcessTarEntry(): Processing " +
                      $"{entryData.Symbol.ID.Market}-{entryData.Symbol.Value}-{entryData.TickType} " +
                      $"for {entryData.Date:yyyy-MM-dd}");


            using (var stream = new GZipStream(file.OpenRead(), CompressionMode.Decompress))
            using (var reader = new StreamReader(stream))
            {
                var headerLine = reader.ReadLine();
                if (headerLine == null)
                {
                    throw new Exception($"CoinApiDataReader.ProcessCoinApiEntry(): CSV header not found for entry name: {entryData.Name}");
                }

                var headerParts = headerLine.Split(';').ToList();

                var tickList = entryData.TickType == TickType.Trade
                    ? ParseTradeData(entryData.Symbol, reader, headerParts)
                    : ParseQuoteData(entryData.Symbol, reader, headerParts);

                foreach (var tick in tickList)
                {
                    yield return tick;
                }
            }
        }

        /// <summary>
        /// Parses CoinAPI trade data.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="headerParts">The header parts.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Parses CoinAPI quote data.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="headerParts">The header parts.</param>
        /// <returns></returns>
        private IEnumerable<Tick> ParseQuoteData(Symbol symbol, StreamReader reader, List<string> headerParts)
        {
            var columnTime = headerParts.FindIndex(x => x == "time_exchange");
            var columnAskPrice = headerParts.FindIndex(x => x == "ask_px");
            var columnAskSize = headerParts.FindIndex(x => x == "ask_sx");
            var columnBidPrice = headerParts.FindIndex(x => x == "bid_px");
            var columnBidSize = headerParts.FindIndex(x => x == "bid_sx");

            var previousAskPrice = 0m;
            var previousBidPrice = 0m;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var lineParts = line.Split(';');

                var time = DateTime.Parse(lineParts[columnTime], CultureInfo.InvariantCulture);
                var askPrice = lineParts[columnAskPrice].ToDecimal();
                var askSize = lineParts[columnAskSize].ToDecimal();
                var bidPrice = lineParts[columnBidPrice].ToDecimal();
                var bidSize = lineParts[columnBidSize].ToDecimal();

                if (askPrice == previousAskPrice && bidPrice == previousBidPrice)
                {
                    // only save quote if bid price or ask price changed
                    continue;
                }

                previousAskPrice = askPrice;
                previousBidPrice = bidPrice;

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
