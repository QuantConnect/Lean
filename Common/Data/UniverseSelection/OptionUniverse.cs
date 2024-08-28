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
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
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
        private static Dictionary<string, Symbol> _symbolsBySidStrCache = new();

        private bool _throwIfNotAnOption = true;
        private char[] _csvLine;

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
                return GetDecimalFromCsvLine(0);
            }
        }

        /// <summary>
        /// High price of the option/underlying
        /// </summary>
        public decimal High
        {
            get
            {
                return GetDecimalFromCsvLine(1);
            }
        }

        /// <summary>
        /// Low price of the option/underlying
        /// </summary>
        public decimal Low
        {
            get
            {
                return GetDecimalFromCsvLine(2);
            }
        }

        /// <summary>
        /// Close price of the option/underlying
        /// </summary>
        public decimal Close
        {
            get
            {
                return GetDecimalFromCsvLine(3);
            }
        }

        /// <summary>
        /// Volume of the option/underlying
        /// </summary>
        public decimal Volume
        {
            get
            {
                return GetDecimalFromCsvLine(4);
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
                return GetDecimalFromCsvLine(5);
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
                return GetDecimalFromCsvLine(6);
            }
        }

        /// <summary>
        /// Greeks values of the option
        /// </summary>
        public BaseGreeks Greeks
        {
            get
            {
                ThrowIfNotAnOption(nameof(Greeks));

                return new BaseGreeks(
                    GetDecimalFromCsvLine(7),
                    GetDecimalFromCsvLine(8),
                    GetDecimalFromCsvLine(9),
                    GetDecimalFromCsvLine(10),
                    GetDecimalFromCsvLine(11),
                    decimal.Zero);
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
            : base(date, symbol)
        {
            _csvLine = csv.ToCharArray();
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
        /// <param name="line">Line of the source document</param>
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

            Symbol symbol;
            lock (_symbolsBySidStrCache)
            {
                if (!_symbolsBySidStrCache.TryGetValue(key, out symbol))
                {
                    var sid = SecurityIdentifier.Parse(sidStr);

                    if (sid.HasUnderlying)
                    {
                        SymbolRepresentation.TryDecomposeOptionTickerOSI(symbolValue, out var underlyingValue, out var _, out var _, out var _);
                        var underlyingKey = $"{sid.Underlying}:{underlyingValue}";

                        if (!_symbolsBySidStrCache.TryGetValue(underlyingKey, out var underlyingSymbol))
                        {
                            underlyingSymbol = new Symbol(sid.Underlying, underlyingValue);
                            _symbolsBySidStrCache[underlyingKey] = underlyingSymbol;
                        }

                        symbol = new Symbol(sid, symbolValue, underlyingSymbol);
                    }
                    else
                    {
                        symbol = new Symbol(sid, symbolValue);
                    }

                    _symbolsBySidStrCache[key] = symbol;
                }
            }

            var result = new OptionUniverse(date, symbol, remainingLine);
            // The data list will not be used, might as well save some memory
            result.Data = null;

            return result;
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
        /// Gets the CSV string representation of this universe entry
        /// </summary>
        public string ToCsv()
        {
            _throwIfNotAnOption = false;
            // Single access to avoid parsing the csv multiple times
            var greeks = Greeks;
            var csv = $"{Symbol.ID},{Symbol.Value},{Open},{High},{Low},{Close},{Volume}," +
                $"{OpenInterest},{ImpliedVolatility},{greeks.Delta},{greeks.Gamma},{greeks.Vega},{greeks.Theta},{greeks.Rho}";
            _throwIfNotAnOption = true;
            return csv;
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
        private decimal GetDecimalFromCsvLine(int index)
        {
            if (_csvLine.IsNullOrEmpty())
            {
                return decimal.Zero;
            }

            var span = _csvLine.AsSpan();
            for (int i = 0; i < index; i++)
            {
                var commaIndex = span.IndexOf(',');
                if (commaIndex == -1)
                {
                    return decimal.Zero;
                }
                span = span.Slice(commaIndex + 1);
            }

            var nextCommaIndex = span.IndexOf(',');
            if (nextCommaIndex == -1)
            {
                nextCommaIndex = span.Length;
            }

            return span.Slice(0, nextCommaIndex).ToString().ToDecimal();
        }

        private void ThrowIfNotAnOption(string propertyName)
        {
            if (_throwIfNotAnOption && !Symbol.SecurityType.IsOption())
            {
                throw new InvalidOperationException($"{propertyName} is only available for options.");
            }
        }
    }
}
