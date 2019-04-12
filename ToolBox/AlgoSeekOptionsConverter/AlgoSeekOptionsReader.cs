/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    public class TickContainer
    {
        public string SecurityRawIdentifier { get; set; }
        public Tick Tick { get; set; }
    }

    /// <summary>
    ///     Enumerator for converting AlgoSeek option files into Ticks.
    /// </summary>
    public class AlgoSeekOptionsReader : IEnumerator<TickContainer>
    {
        private readonly int _columnExchange = -1;
        private readonly int _columnExpiration = -1;
        private readonly int _columnPremium = -1;
        private readonly int _columnPutCall = -1;
        private readonly int _columnQuantity = -1;
        private readonly int _columnsCount = -1;
        private readonly int _columnSide = -1;
        private readonly int _columnStrike = -1;
        private readonly int _columnTicker = -1;

        private readonly int _columnTimestamp = -1;
        private readonly int _columnType = -1;
        private readonly DateTime _date;
        private readonly string _file;

        private readonly StringBuilder _securityRawIdentifier = new StringBuilder();
        private readonly Stream _stream;
        private readonly StreamReader _streamReader;
        private HashSet<string> _symbolFilter;

        private Dictionary<string, Symbol> _underlyingCache;

        /// <summary>
        ///     Enumerate through the lines of the algoseek files.
        /// </summary>
        /// <param name="file">BZ File for algoseek</param>
        /// <param name="date">Reference date of the folder</param>
        public AlgoSeekOptionsReader(string file, DateTime date, HashSet<string> symbolFilter = null)
        {
            _file = file;
            _date = date;
            _symbolFilter = symbolFilter;

            _underlyingCache = new Dictionary<string, Symbol>();

            var streamProvider = StreamProvider.ForExtension(Path.GetExtension(file));
            _stream = streamProvider.Open(file).First();
            _streamReader = new StreamReader(_stream);

            // detecting column order in the file
            var headerLine = _streamReader.ReadLine();
            if (!string.IsNullOrEmpty(headerLine))
            {
                var header = headerLine.ToCsv();
                _columnTimestamp = header.FindIndex(x => x == "Timestamp");
                _columnTicker = header.FindIndex(x => x == "Ticker");
                _columnType = header.FindIndex(x => x == "Type");
                _columnSide = header.FindIndex(x => x == "Side");
                _columnPutCall = header.FindIndex(x => x == "PutCall");
                _columnExpiration = header.FindIndex(x => x == "Expiration");
                _columnStrike = header.FindIndex(x => x == "Strike");
                _columnQuantity = header.FindIndex(x => x == "Quantity");
                _columnPremium = header.FindIndex(x => x == "Premium");
                _columnExchange = header.FindIndex(x => x == "Exchange");

                _columnsCount = new[]
                {
                    _columnTimestamp, _columnTicker, _columnType, _columnSide,
                    _columnPutCall, _columnExpiration, _columnStrike, _columnQuantity, _columnPremium, _columnExchange
                }.Max();
            }

            //Prime the data pump, set the current.
            Current = null;
            MoveNext();
        }

        /// <summary>
        ///     Parse the next line of the algoseek option file.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            string line;
            TickContainer tick = null;
            while (tick == null && (line = _streamReader.ReadLine()) != null)
                // If line is invalid continue looping to find next valid line.
                tick = Parse(line);
            Current = tick;
            return Current != null;
        }

        /// <summary>
        ///     Current top of the tick file.
        /// </summary>
        public TickContainer Current { get; private set; }

        /// <summary>
        ///     Gets the current element in the collection.
        /// </summary>
        /// <returns>
        ///     The current element in the collection.
        /// </returns>
        object IEnumerator.Current => Current;

        /// <summary>
        ///     Reset the enumerator for the AlgoSeekOptionReader
        /// </summary>
        public void Reset()
        {
            throw new NotImplementedException("Reset not implemented for AlgoSeekOptionsReader.");
        }

        /// <summary>
        ///     Dispose of the underlying AlgoSeekOptionsReader
        /// </summary>
        public void Dispose()
        {
            _stream.Close();
            _stream.Dispose();
            _streamReader.Close();
            _streamReader.Dispose();
        }

        /// <summary>
        ///     Parse a string line into a option tick.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private TickContainer Parse(string line)
        {
            try
            {
                var column = -1;
                var last = 0;
                var tickBuilder = new Tick();
                var isAsk = false;
                var price = decimal.Zero;
                var quantity = 0;
                _securityRawIdentifier.Clear();
                var readOnlySpan = line.AsSpan();
                for (var i = 0; i < line.Length; i++)
                    if (line[i] == ',')
                    {
                        if (last != 0) last = last + 1;
                        var columnContent = readOnlySpan.Slice(last, i - last);
                        last = i;
                        column++;
                        if (columnContent.IsEmpty || columnContent.IsWhiteSpace()) continue;
                        if (column == _columnTimestamp) tickBuilder.Time = _date + TimeSpan.ParseExact(columnContent.ToString(), @"h\:mm\:ss\.fff", CultureInfo.InvariantCulture);
                        if (column == _columnType)
                            switch (columnContent.GetPinnableReference())
                            {
                                case 'O':
                                    tickBuilder.TickType = TickType.OpenInterest;
                                    break;
                                case 'T':
                                    tickBuilder.TickType = TickType.Trade;
                                    break;
                                case 'F':
                                    tickBuilder.TickType = TickType.Quote;
                                    break;
                            }
                        if (column == _columnSide)
                            switch (columnContent.GetPinnableReference())
                            {
                                case 'B':
                                    tickBuilder.TickType = TickType.Quote;
                                    break;
                                case 'O':
                                    tickBuilder.TickType = TickType.Quote;
                                    isAsk = true;
                                    break;
                            }

                        if (column == _columnTicker)
                        {
                            _securityRawIdentifier.Append(columnContent.ToArray());
                            _securityRawIdentifier.Append('-');
                        }

                        if (column == _columnPutCall)
                        {
                            _securityRawIdentifier.Append(columnContent.ToArray());
                            _securityRawIdentifier.Append('-');
                        }

                        if (column == _columnExpiration)
                        {
                            _securityRawIdentifier.Append(columnContent.ToArray());
                            _securityRawIdentifier.Append('-');
                        }

                        if (column == _columnStrike) _securityRawIdentifier.Append(columnContent.ToArray());

                        if (column == _columnPremium) price = columnContent.ToArray().ToInt32() / 10000m;
                        if (column == _columnQuantity) quantity = columnContent.ToArray().ToInt32();
                    }

                switch (tickBuilder.TickType)
                {
                    case TickType.Quote:
                        if (isAsk)
                        {
                            tickBuilder.AskPrice = price;
                            tickBuilder.AskSize = quantity;
                        }
                        else
                        {
                            tickBuilder.BidPrice = price;
                            tickBuilder.BidSize = quantity;
                        }

                        break;
                    case TickType.Trade:
                        tickBuilder.Exchange = Market.USA;
                        tickBuilder.Value = price;
                        tickBuilder.Quantity = quantity;
                        break;
                    case TickType.OpenInterest:
                        tickBuilder.Exchange = Market.USA;
                        tickBuilder.Value = quantity;
                        break;
                }

                return new TickContainer
                {
                    SecurityRawIdentifier = _securityRawIdentifier.ToString(),
                    Tick = tickBuilder
                };
            }
            catch (Exception err)
            {
                Log.Error(err);
                Log.Trace("Line: {0}, File: {1}", line, _file);
                return null;
            }
        }
    }
}