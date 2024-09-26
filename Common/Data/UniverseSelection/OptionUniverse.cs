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
using System.Runtime.CompilerServices;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Represents a universe of options data
    /// </summary>
    public class OptionUniverse : BaseDataCollection, ISymbol
    {
        private const int StartingGreeksCsvIndex = 7;

        // We keep the properties as they are in the csv file to reduce memory usage (strings vs decimals)
        private readonly string _csvLine;

        /// <summary>
        /// The security identifier of the option symbol
        /// </summary>
        public SecurityIdentifier ID => Symbol.ID;

        /// <summary>
        /// Price of the option/underlying
        /// </summary>
        public override decimal Value => Close;

        /// <summary>
        /// Open price of the option/underlying
        /// </summary>
        public decimal Open
        {
            get
            {
                // Parse the values every time to avoid keeping them in memory
                return _csvLine.GetDecimalFromCsv(0);
            }
        }

        /// <summary>
        /// High price of the option/underlying
        /// </summary>
        public decimal High
        {
            get
            {
                return _csvLine.GetDecimalFromCsv(1);
            }
        }

        /// <summary>
        /// Low price of the option/underlying
        /// </summary>
        public decimal Low
        {
            get
            {
                return _csvLine.GetDecimalFromCsv(2);
            }
        }

        /// <summary>
        /// Close price of the option/underlying
        /// </summary>
        public decimal Close
        {
            get
            {
                return _csvLine.GetDecimalFromCsv(3);
            }
        }

        /// <summary>
        /// Volume of the option/underlying
        /// </summary>
        public decimal Volume
        {
            get
            {
                return _csvLine.GetDecimalFromCsv(4);
            }
        }

        /// <summary>
        /// Open interest value of the option
        /// </summary>
        public decimal OpenInterest
        {
            get
            {
                ThrowIfNotAnOption(nameof(OpenInterest));
                return _csvLine.GetDecimalFromCsv(5);
            }
        }

        /// <summary>
        /// Implied volatility value of the option
        /// </summary>
        public decimal ImpliedVolatility
        {
            get
            {
                ThrowIfNotAnOption(nameof(ImpliedVolatility));
                return _csvLine.GetDecimalFromCsv(6);
            }
        }

        /// <summary>
        /// Greeks values of the option
        /// </summary>
        public Greeks Greeks
        {
            get
            {
                ThrowIfNotAnOption(nameof(Greeks));
                return new PreCalculatedGreeks(_csvLine);
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
        /// Creates a new instance of the <see cref="OptionUniverse"/> class
        /// </summary>
        public OptionUniverse()
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OptionUniverse"/> class
        /// </summary>
        public OptionUniverse(DateTime date, Symbol symbol, string csv)
            : base(date, date, symbol, null, null)
        {
            _csvLine = csv;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OptionUniverse"/> class as a copy of the given instance
        /// </summary>
        public OptionUniverse(OptionUniverse other)
            : base(other)
        {
            _csvLine = other._csvLine;
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
            var path = LeanData.GenerateUniversesDirectory(Globals.DataFolder, config.Symbol);
            path = Path.Combine(path, $"{date:yyyyMMdd}.csv");

            return new SubscriptionDataSource(path, SubscriptionTransportMedium.LocalFile, FileFormat.FoldingCollection);
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object
        /// each time it is called.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="stream">Stream reader of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>Instance of the T:BaseData object generated by this line of the CSV</returns>
        public override BaseData Reader(SubscriptionDataConfig config, StreamReader stream, DateTime date, bool isLiveMode)
        {
            if (stream == null || stream.EndOfStream)
            {
                return null;
            }

            var sidStr = stream.GetString();

            if (sidStr.StartsWith("#", StringComparison.InvariantCulture))
            {
                stream.ReadLine();
                return null;
            }

            var symbolValue = stream.GetString();
            var remainingLine = stream.ReadLine();

            var key = $"{sidStr}:{symbolValue}";

            if (!TryGetCachedSymbol(key, out var symbol))
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

            return new OptionUniverse(date, symbol, remainingLine);
        }

        /// <summary>
        /// Adds a new data point to this collection.
        /// If the data point is for the underlying, it will be stored in the <see cref="BaseDataCollection.Underlying"/> property.
        /// </summary>
        /// <param name="newDataPoint">The new data point to add</param>
        public override void Add(BaseData newDataPoint)
        {
            if (newDataPoint is OptionUniverse optionUniverseDataPoint)
            {
                if (optionUniverseDataPoint.Symbol.HasUnderlying)
                {
                    optionUniverseDataPoint.Underlying = Underlying;
                    base.Add(optionUniverseDataPoint);
                }
                else
                {
                    Underlying = optionUniverseDataPoint;
                    foreach (OptionUniverse data in Data)
                    {
                        data.Underlying = optionUniverseDataPoint;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a copy of the instance
        /// </summary>
        /// <returns>Clone of the instance</returns>
        public override BaseData Clone()
        {
            return new OptionUniverse(this);
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
        /// Gets the CSV string representation of this universe entry
        /// </summary>
        public static string ToCsv(Symbol symbol, decimal open, decimal high, decimal low, decimal close, decimal volume, decimal? openInterest,
            decimal? impliedVolatility, Greeks greeks)
        {
            return $"{symbol.ID},{symbol.Value},{open},{high},{low},{close},{volume},"
                + $"{openInterest},{impliedVolatility},{greeks?.Delta},{greeks?.Gamma},{greeks?.Vega},{greeks?.Theta},{greeks?.Rho}";
        }

        /// <summary>
        /// Implicit conversion into <see cref="Symbol"/>
        /// </summary>
        /// <param name="data">The option universe data to be converted</param>
        public static implicit operator Symbol(OptionUniverse data)
        {
            return data.Symbol;
        }

        /// <summary>
        /// Gets the symbol of the option
        /// </summary>
        public Symbol ToSymbol()
        {
            return (Symbol)this;
        }

        /// <summary>
        /// Gets the CSV header string for this universe entry
        /// </summary>
        public static string CsvHeader => "symbol_id,symbol_value,open,high,low,close,volume,open_interest,implied_volatility,delta,gamma,vega,theta,rho";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfNotAnOption(string propertyName)
        {
            if (!Symbol.SecurityType.IsOption())
            {
                throw new InvalidOperationException($"{propertyName} is only available for options.");
            }
        }

        /// <summary>
        /// Pre-calculated greeks lazily parsed from csv line.
        /// It parses the greeks values from the csv line only when they are requested to avoid holding decimals in memory.
        /// </summary>
        private class PreCalculatedGreeks : Greeks
        {
            private readonly string _csvLine;

            /// <inheritdoc />
            public override decimal Delta => _csvLine.GetDecimalFromCsv(StartingGreeksCsvIndex);

            /// <inheritdoc />
            public override decimal Gamma => _csvLine.GetDecimalFromCsv(StartingGreeksCsvIndex + 1);

            /// <inheritdoc />
            public override decimal Vega => _csvLine.GetDecimalFromCsv(StartingGreeksCsvIndex + 2);

            /// <inheritdoc />
            public override decimal Theta => _csvLine.GetDecimalFromCsv(StartingGreeksCsvIndex + 3);

            /// <inheritdoc />
            public override decimal Rho => _csvLine.GetDecimalFromCsv(StartingGreeksCsvIndex + 4);

            /// <inheritdoc />
            public override decimal Lambda => decimal.Zero;

            /// <summary>
            /// Initializes a new default instance of the <see cref="PreCalculatedGreeks"/> class
            /// </summary>
            public PreCalculatedGreeks(string csvLine)
            {
                _csvLine = csvLine;
            }

            /// <summary>
            /// Gets a string representation of the greeks values
            /// </summary>
            public override string ToString()
            {
                return $"D: {Delta}, G: {Gamma}, V: {Vega}, T: {Theta}, R: {Rho}";
            }
        }
    }
}
