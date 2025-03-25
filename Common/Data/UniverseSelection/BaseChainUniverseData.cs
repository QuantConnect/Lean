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
using System.IO;
using QuantConnect.Python;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Represents a chain universe.
    /// Intended as a base for options and futures universe data.
    /// </summary>
    public abstract class BaseChainUniverseData : BaseDataCollection, IChainUniverseData
    {
        /// <summary>
        /// Csv line to get the values from
        /// </summary>
        /// <remarks>We keep the properties as they are in the csv file to reduce memory usage (strings vs decimals)</remarks>
        protected string CsvLine { get; }

        /// <summary>
        /// The security identifier of the option symbol
        /// </summary>
        [PandasIgnore]
        public SecurityIdentifier ID => Symbol.ID;

        /// <summary>
        /// Price of the security
        /// </summary>
        [PandasIgnore]
        public override decimal Value => Close;

        /// <summary>
        /// Open price of the security
        /// </summary>
        public decimal Open
        {
            get
            {
                // Parse the values every time to avoid keeping them in memory
                return CsvLine.GetDecimalFromCsv(0);
            }
        }

        /// <summary>
        /// High price of the security
        /// </summary>
        public decimal High
        {
            get
            {
                return CsvLine.GetDecimalFromCsv(1);
            }
        }

        /// <summary>
        /// Low price of the security
        /// </summary>
        public decimal Low
        {
            get
            {
                return CsvLine.GetDecimalFromCsv(2);
            }
        }

        /// <summary>
        /// Close price of the security
        /// </summary>
        public decimal Close
        {
            get
            {
                return CsvLine.GetDecimalFromCsv(3);
            }
        }

        /// <summary>
        /// Volume value of the security
        /// </summary>
        public decimal Volume
        {
            get
            {
                return CsvLine.GetDecimalFromCsv(4);
            }
        }

        /// <summary>
        /// Open interest value
        /// </summary>
        public virtual decimal OpenInterest
        {
            get
            {
                return CsvLine.GetDecimalFromCsv(5);
            }
        }

        /// <summary>
        /// Time that the data became available to use
        /// </summary>
        public override DateTime EndTime
        {
            get { return Time + QuantConnect.Time.OneDay; }
            set { Time = value - QuantConnect.Time.OneDay; }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BaseChainUniverseData"/> class
        /// </summary>
        protected BaseChainUniverseData()
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BaseChainUniverseData"/> class
        /// </summary>
        protected BaseChainUniverseData(DateTime date, Symbol symbol, string csv)
            : base(date, date, symbol, null, null)
        {
            CsvLine = csv;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BaseChainUniverseData"/> class as a copy of the given instance
        /// </summary>
        protected BaseChainUniverseData(BaseChainUniverseData other)
            : base(other)
        {
            CsvLine = other.CsvLine;
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String URL of source file.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var path = GetUniverseFullFilePath(config.Symbol, date);
            return new SubscriptionDataSource(path, SubscriptionTransportMedium.LocalFile, FileFormat.FoldingCollection);
        }

        /// <summary>
        /// Generates the file path for a universe data file based on the given symbol and date.
        /// Optionally, creates the directory if it does not exist.
        /// </summary>
        /// <param name="symbol">The financial symbol for which the universe file is generated.</param>
        /// <param name="date">The date associated with the universe file.</param>
        /// <returns>The full file path to the universe data file.</returns>
        public static string GetUniverseFullFilePath(Symbol symbol, DateTime date)
        {
            return Path.Combine(LeanData.GenerateUniversesDirectory(Globals.DataFolder, symbol), $"{date:yyyyMMdd}.csv");
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object
        /// each time it is called.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="stream">Stream reader of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="symbol">The symbol read and parsed from the current line in the stream</param>
        /// <param name="remainingLine">The remaining string after reading the symbol from the current line in the stream</param>
        /// <returns>Whether a valid line starting with a symbol was read</returns>
        protected static bool TryRead(SubscriptionDataConfig config, StreamReader stream, DateTime date, out Symbol symbol, out string remainingLine)
        {
            symbol = null;
            remainingLine = null;

            if (stream == null || stream.EndOfStream)
            {
                return false;
            }

            var sidStr = stream.GetString();

            if (sidStr.StartsWith("#", StringComparison.InvariantCulture))
            {
                stream.ReadLine();
                return false;
            }

            var symbolValue = stream.GetString();
            remainingLine = stream.ReadLine();

            var key = $"{sidStr}:{symbolValue}";

            if (!TryGetCachedSymbol(key, out symbol))
            {
                var sid = SecurityIdentifier.Parse(sidStr);

                if (sid.HasUnderlying)
                {
                    // Let's try to get the underlying symbol from the cache
                    SymbolRepresentation.TryDecomposeOptionTickerOSI(symbolValue, sid.SecurityType,
                        out var _, out var underlyingValue, out var _, out var _, out var _);
                    var underlyingKey = $"{sid.Underlying}:{underlyingValue}";
                    var underlyingWasCached = TryGetCachedSymbol(underlyingKey, out var underlyingSymbol);

                    symbol = Symbol.CreateOption(sid, symbolValue, underlyingSymbol);

                    if (!underlyingWasCached)
                    {
                        CacheSymbol(underlyingKey, symbol.Underlying);
                    }
                }
                else
                {
                    symbol = new Symbol(sid, symbolValue);
                }

                CacheSymbol(key, symbol);
            }

            return true;
        }

        /// <summary>
        /// Gets the default resolution for this data and security type
        /// </summary>
        /// <remarks>This is a method and not a property so that python
        /// custom data types can override it</remarks>
        public override Resolution DefaultResolution()
        {
            return Resolution.Daily;
        }

        /// <summary>
        /// Gets the symbol of the option
        /// </summary>
        public Symbol ToSymbol()
        {
            return Symbol;
        }
    }
}
