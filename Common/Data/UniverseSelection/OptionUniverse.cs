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
using QuantConnect.Util;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Represents a universe of options data
    /// </summary>
    public class OptionUniverse : BaseDataCollection
    {
        private string[] _csvLine;

        private decimal? _open;
        private decimal? _high;
        private decimal? _low;
        private decimal? _close;
        private decimal? _volume;
        private decimal? _openInterest;
        private decimal? _impliedVolatility;
        private Greeks _greeks;

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
                if (!_open.HasValue)
                {
                    _open = GetDecimalFromCsvLine(2);
                }

                return _open.Value;
            }
        }

        /// <summary>
        /// High price of the option/underlying
        /// </summary>
        public decimal High
        {
            get
            {
                if (!_high.HasValue)
                {
                    _high = GetDecimalFromCsvLine(3);
                }

                return _high.Value;
            }
        }

        /// <summary>
        /// Low price of the option/underlying
        /// </summary>
        public decimal Low
        {
            get
            {
                if (!_low.HasValue)
                {
                    _low = GetDecimalFromCsvLine(4);
                }

                return _low.Value;
            }
        }

        /// <summary>
        /// Close price of the option/underlying
        /// </summary>
        public decimal Close
        {
            get
            {
                if (!_close.HasValue)
                {
                    _close = GetDecimalFromCsvLine(5);
                }

                return _close.Value;
            }
        }

        /// <summary>
        /// Volume of the option/underlying
        /// </summary>
        public decimal Volume
        {
            get
            {
                if (!_volume.HasValue)
                {
                    _volume = GetDecimalFromCsvLine(6);
                }

                return _volume.Value;
            }
        }

        /// <summary>
        /// Open interest value of the option
        /// </summary>
        public decimal? OpenInterest
        {
            get
            {
                ThrowIfNotAnOption(nameof(OpenInterest));

                if (!_openInterest.HasValue)
                {
                    _openInterest = GetDecimalFromCsvLine(7);
                }

                return _openInterest.Value;
            }
        }

        /// <summary>
        /// Implied volatility value of the option
        /// </summary>
        public decimal? ImpliedVolatility
        {
            get
            {
                ThrowIfNotAnOption(nameof(ImpliedVolatility));

                if (!_impliedVolatility.HasValue)
                {
                    _impliedVolatility = GetDecimalFromCsvLine(8);
                }

                return _impliedVolatility.Value;
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

                if (_greeks == null)
                {
                    _greeks = new Greeks(GetDecimalFromCsvLine(9),
                        GetDecimalFromCsvLine(10),
                        GetDecimalFromCsvLine(11),
                        GetDecimalFromCsvLine(12),
                        GetDecimalFromCsvLine(13),
                        0m);
                }

                return _greeks;
            }
            private set
            {
                _greeks = value;
            }
        }

        /// <summary>
        /// Period of the data
        /// </summary>
        public TimeSpan Period { get; } = TimeSpan.FromDays(1);

        /// <summary>
        /// Time that the data became available to use
        /// </summary>
        public override DateTime EndTime
        {
            // TODO: Should EndTime be midnight of next tradable date and Time = EndTime - Period?
            get { return Time + Period; }
            set { Time = value - Period; }
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
        public OptionUniverse(DateTime date, Symbol symbol, string[] csv)
            : base(date, symbol)
        {
            _csvLine = csv;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OptionUniverse"/> class
        /// </summary>
        public OptionUniverse(DateTime date, Symbol symbol, decimal open, decimal high, decimal low, decimal close, decimal volume,
            decimal? openInterest, decimal? impliedVolatility, Greeks greeks)
            : base(date, symbol)
        {
            Initialize(open, high, low, close, volume, openInterest, impliedVolatility, greeks);
        }

        private void Initialize(decimal? open, decimal? high, decimal? low, decimal? close, decimal? volume, decimal? openInterest,
            decimal? impliedVolatility, Greeks greeks)
        {
            _open = open;
            _high = high;
            _low = low;
            _close = close;
            _volume = volume;
            _openInterest = openInterest;
            _impliedVolatility = impliedVolatility;
            _greeks = greeks;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OptionUniverse"/> class as a copy of the given instance
        /// </summary>
        public OptionUniverse(OptionUniverse other)
            : base(other)
        {
            _csvLine = other._csvLine;
            Initialize(other._open, other._high, other._low, other._close, other._volume, other._openInterest, other._impliedVolatility, other._greeks);
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
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            if (string.IsNullOrEmpty(line))
            {
                return null;
            }

            var csv = line.Split(',');
            var sid = SecurityIdentifier.Parse(csv[0]);
            var symbolValue = csv[1];

            Symbol symbol;
            if (sid.HasUnderlying)
            {
                symbol = Symbol.CreateOption(sid, symbolValue);
            }
            else
            {
                symbol = new Symbol(sid, symbolValue);
            }

            return new OptionUniverse(date, symbol, csv);
        }

        /// <summary>
        /// Adds a new data point to this collection.
        /// If the data point is for the underlying, it will be stored in the <see cref="BaseDataCollection.Underlying"/> property.
        /// </summary>
        /// <param name="newDataPoint">The new data point to add</param>
        public override void Add(BaseData newDataPoint)
        {
            if (newDataPoint.Symbol.HasUnderlying)
            {
                base.Add(newDataPoint);
            }
            else
            {
                Underlying = newDataPoint;
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
            return $"{Symbol.ID},{Symbol.Value},{Open},{High},{Low},{Close},{Volume}," +
                $"{_openInterest},{_impliedVolatility},{_greeks?.Delta},{_greeks?.Gamma},{_greeks?.Vega},{_greeks?.Theta},{_greeks?.Rho}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private decimal GetDecimalFromCsvLine(int index)
        {
            return !_csvLine.IsNullOrEmpty() ? _csvLine[index].ToDecimal() : decimal.Zero;
        }

        private void ThrowIfNotAnOption(string propertyName)
        {
            if (!Symbol.SecurityType.IsOption())
            {
                throw new InvalidOperationException($"{propertyName} is only available for options.");
            }
        }
    }
}
