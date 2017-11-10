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
using System.Linq;
using System.IO;
using QuantConnect.Data.Market;
using QuantConnect.Util;
using QuantConnect.Logging;
using System.Globalization;
using QuantConnect.Data;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    /// <summary>
    /// Enumerator for converting AlgoSeek option files into Ticks.
    /// </summary>
    public class AlgoSeekOptionsReader : IEnumerator<Tick>
    {
        private DateTime _date;
        private Stream _stream;
        private StreamReader _streamReader;
        private HashSet<string> _symbolFilter;

        private Dictionary<string, Symbol> _underlyingCache;

        private readonly int _columnTimestamp = -1;
        private readonly int _columnTicker = -1;
        private readonly int _columnType = -1;
        private readonly int _columnSide = -1;
        private readonly int _columnPutCall = -1;
        private readonly int _columnExpiration = -1;
        private readonly int _columnStrike = -1;
        private readonly int _columnQuantity = -1;
        private readonly int _columnPremium = -1;
        private readonly int _columnExchange = -1;
        private readonly int _columnsCount = -1;
        private string _file;

        /// <summary>
        /// Enumerate through the lines of the algoseek files.
        /// </summary>
        /// <param name="file">BZ File for algoseek</param>
        /// <param name="date">Reference date of the folder</param>
        public AlgoSeekOptionsReader(string file, DateTime date, HashSet<string> symbolFilter = null)
        {
            _date = date;
            _underlyingCache = new Dictionary<string, Symbol>();

            var streamProvider = StreamProvider.ForExtension(Path.GetExtension(file));
            _file = file;
            _stream = streamProvider.Open(file).First();
            _streamReader = new StreamReader(_stream);
            _symbolFilter = symbolFilter;

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

                _columnsCount = Enumerable.Max(new[] { _columnTimestamp, _columnTicker, _columnType, _columnSide,
                    _columnPutCall, _columnExpiration, _columnStrike, _columnQuantity, _columnPremium, _columnExchange });
            }
            //Prime the data pump, set the current.
            Current = null;
            MoveNext();
        }

        /// <summary>
        /// Parse the next line of the algoseek option file.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            string line;
            Tick tick = null;
            while (tick == null && (line = _streamReader.ReadLine()) != null)
                {
                // If line is invalid continue looping to find next valid line.
                tick = Parse(line);
            }
            Current = tick;
            return Current != null;
        }

        /// <summary>
        /// Current top of the tick file.
        /// </summary>
        public Tick Current
        {
            get; private set;

        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        object IEnumerator.Current
        {
            get { return Current; }
        }

        /// <summary>
        /// Reset the enumerator for the AlgoSeekOptionReader
        /// </summary>
        public void Reset()
        {
            throw new NotImplementedException("Reset not implemented for AlgoSeekOptionsReader.");
        }

        /// <summary>
        /// Dispose of the underlying AlgoSeekOptionsReader
        /// </summary>
        public void Dispose()
        {
            _stream.Close();
            _stream.Dispose();
            _streamReader.Close();
            _streamReader.Dispose();
        }

        /// <summary>
        /// Parse a string line into a option tick.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private Tick Parse(string line)
        {
            try
            {
                // parse csv check column count
                var csv = line.ToCsv();
                if (csv.Count - 1 < _columnsCount)
                {
                    return null;
                }

                TickType tickType;
                bool isAsk = false;

                switch (csv[_columnType])
                {
                    case "O":
                        tickType = TickType.OpenInterest;
                        break;
                    case "T":
                        tickType = TickType.Trade;
                        break;
                    case "F":
                        switch (csv[_columnSide])
                        {
                            case "B":
                                tickType = TickType.Quote;
                                isAsk = false;
                                break;
                            case "O":
                                tickType = TickType.Quote;
                                isAsk = true;
                                break;
                            default:
                                return null;
                        }
                        break;
                    default:
                        return null;
                }

                var underlying = csv[_columnTicker];

                if (_symbolFilter != null && !_symbolFilter.Contains(underlying))
                    return null;

                if (string.IsNullOrEmpty(underlying))
                {
                    return null;
                }

                // ignoring time zones completely -- this is all in the 'data-time-zone'
                var timeString = csv[_columnTimestamp];
                var hours = timeString.Substring(0, 2).ToInt32();
                var minutes = timeString.Substring(3, 2).ToInt32();
                var seconds = timeString.Substring(6, 2).ToInt32();
                var millis = timeString.Substring(9, 3).ToInt32();
                var time = _date.Add(new TimeSpan(0, hours, minutes, seconds, millis));

                var optionRight = csv[_columnPutCall][0] == 'P' ? OptionRight.Put : OptionRight.Call;

                var expiry = DateTime.MinValue;
                if (!DateTime.TryParseExact(csv[_columnExpiration], "yyyyMMdd", null, DateTimeStyles.None, out expiry))
                {
                    // sometimes we see the corrupted data with yyyyMMdd, where dd is equal to zeros
                    DateTime.TryParseExact(csv[_columnExpiration], "yyyyMM", null, DateTimeStyles.None, out expiry);
                }

                var strike = csv[_columnStrike].ToDecimal() / 10000m;
                var optionStyle = OptionStyle.American; // couldn't see this specified in the file, maybe need a reference file

                Symbol symbol;

                if (!_underlyingCache.ContainsKey(underlying))
                {
                    symbol = Symbol.CreateOption(underlying, Market.USA, optionStyle, optionRight, strike, expiry, null, false);
                    _underlyingCache[underlying] = symbol.Underlying;
                }
                else
                {
                    symbol = Symbol.CreateOption(_underlyingCache[underlying], Market.USA, optionStyle, optionRight, strike, expiry);
                }

                var price = csv[_columnPremium].ToDecimal() / 10000m;
                var quantity = csv[_columnQuantity].ToInt32();

                switch (tickType)
                {
                    case TickType.Quote:

                        var tick = new Tick
                        {
                            Symbol = symbol,
                            Time = time,
                            TickType = tickType,
                            Exchange = Market.USA,
                            Value = price
                        };

                        if (isAsk)
                        {
                            tick.AskPrice = price;
                            tick.AskSize = quantity;
                        }
                        else
                        {
                            tick.BidPrice = price;
                            tick.BidSize = quantity;
                        }

                        return tick;

                    case TickType.Trade:

                        tick = new Tick
                        {
                            Symbol = symbol,
                            Time = time,
                            TickType = tickType,
                            Exchange = Market.USA,
                            Value = price,
                            Quantity = quantity
                        };

                        return tick;

                    case TickType.OpenInterest:

                        tick = new Tick
                        {
                            Symbol = symbol,
                            Time = time,
                            TickType = tickType,
                            Exchange = Market.USA,
                            Value = quantity
                        };

                        return tick;
                }

                return null;
            }
            catch(Exception err)
            {
                Log.Error(err);
                Log.Trace("Line: {0}, File: {1}", line, _file);
                return null;
            }
        }
    }
}
