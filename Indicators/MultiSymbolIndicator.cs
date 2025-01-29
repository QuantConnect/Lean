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
using QuantConnect.Securities;
using NodaTime;
using QuantConnect.Data;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Base class for indicators that work with multiple different symbols.
    /// </summary>
    /// <typeparam name="TInput">Indicator input data type</typeparam>
    /// <typeparam name="TData">Type of the data points stored in the rolling windows for each symbol (e.g., double, decimal, etc.)</typeparam>
    public abstract class MultiSymbolIndicator<TInput, TData> : IndicatorBase<TInput>, IIndicatorWarmUpPeriodProvider
        where TInput : IBaseData
    {
        /// <summary>
        /// The resolution of the data (e.g., daily, hourly, etc.).
        /// </summary>
        private Resolution _resolution;

        /// <summary>
        /// </summary>
        protected Dictionary<Symbol, SymbolData> DataBySymbol { get; }

        /// <summary>
        /// Indicates if the time zone for the target and reference are different.
        /// </summary>
        protected bool IsTimezoneDifferent { get; }

        /// <summary>
        /// The most recently computed value of the indicator.
        /// </summary>
        protected decimal IndicatorValue { get; set; }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; set; }

        /// <summary>
        /// Initializes the dual symbol indicator.
        /// <para>
        /// The constructor accepts a target symbol and a reference symbol. It also initializes
        /// the time zones for both symbols and checks if they are different.
        /// </para>
        /// </summary>
        /// <param name="name">The name of the indicator.</param>
        /// <param name="symbols">The symbols the indicator works on .</param>
        /// <param name="period">The period (number of data points) over which to calculate the indicator.</param>
        protected MultiSymbolIndicator(string name, IEnumerable<Symbol> symbols, int period)
            : base(name)
        {
            DataBySymbol = symbols.ToDictionary(symbol => symbol, symbol => new SymbolData(symbol, period));
            IsTimezoneDifferent = DataBySymbol.Values.Select(data => data.ExchangeTimeZone).Distinct().Count() > 1;
        }

        /// <summary>
        /// Checks and computes the indicator if the input data matches.
        /// This method ensures the input data points are from matching time periods and different symbols.
        /// </summary>
        /// <param name="input">The input data point (e.g., TradeBar for a symbol).</param>
        /// <returns>The most recently computed value of the indicator.</returns>
        protected override decimal ComputeNextValue(TInput input)
        {
            if (!DataBySymbol.TryGetValue(input.Symbol, out var symbolData))
            {
                throw new ArgumentException($"Input symbol {input.Symbol} does not correspond to any " +
                    $"of the symbols this indicator works on ({string.Join(", ", DataBySymbol.Keys)})");
            }

            symbolData.CurrentInput = input;

            if (Samples == 1)
            {
                _resolution = GetResolution(input);
                return decimal.Zero;
            }

            if (IsReadyToCalculate())
            {
                IndicatorValue = ComputeIndicator(DataBySymbol.Values.Select(data => data.CurrentInput).ToList());
            }

            return IndicatorValue;
        }

        /// <summary>
        /// Determines if the indicator is ready to compute a new value.
        /// The indicator is ready when data for all symbols at a given time is available.
        /// </summary>
        private bool IsReadyToCalculate()
        {
            var referenceTime = AdjustDateToResolution(DataBySymbol.Values.First().CurrentInputEndTimeUtc);
            return DataBySymbol.Values.All(data => AdjustDateToResolution(data.CurrentInputEndTimeUtc) == referenceTime);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state.
        /// This will be called only when the indicator is ready, that is,
        /// when data for all symbols at a given time is available.
        /// </summary>
        protected abstract decimal ComputeIndicator(IEnumerable<TInput> inputs);

        /// <summary>
        /// Truncates the given DateTime based on the specified resolution (Daily, Hourly, Minute, or Second).
        /// </summary>
        /// <param name="date">The DateTime to truncate.</param>
        /// <returns>A DateTime truncated to the specified resolution.</returns>
        private DateTime AdjustDateToResolution(DateTime date)
        {
            switch (_resolution)
            {
                case Resolution.Daily:
                    return date.Date;
                case Resolution.Hour:
                    return date.Date.AddHours(date.Hour);
                case Resolution.Minute:
                    return date.Date.AddHours(date.Hour).AddMinutes(date.Minute);
                default:
                    return date;
            }
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            IndicatorValue = 0;
            foreach (var data in DataBySymbol.Values)
            {
                data.DataPoints.Reset();
                data.CurrentInput = default;
            }
            base.Reset();
        }

        /// <summary>
        /// Determines the resolution of the input data based on the time difference between its start and end times.
        /// Returns <see cref="Resolution.Daily"/> if the difference exceeds 1 hour; otherwise, calculates a higher equivalent resolution.
        /// </summary>
        private static Resolution GetResolution(IBaseData input)
        {
            var timeDifference = input.EndTime - input.Time;
            return timeDifference.TotalHours > 1 ? Resolution.Daily : timeDifference.ToHigherResolutionEquivalent(false);
        }

        /// <summary>
        /// Contains the data points, the current input and other relevant indicator data for a symbol.
        /// </summary>
        protected class SymbolData
        {
            private static MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

            private TInput _currentInput;

            /// <summary>
            /// The exchange time zone for the security represented by this symbol.
            /// </summary>
            public DateTimeZone ExchangeTimeZone { get; }

            /// <summary>
            /// Data points for the symbol.
            /// </summary>
            public RollingWindow<TData> DataPoints { get; }

            /// <summary>
            /// The last input data point for the symbol.
            /// </summary>
            public TInput CurrentInput
            {
                get => _currentInput;
                set
                {
                    _currentInput = value;
                    CurrentInputEndTimeUtc = _currentInput != null ? _currentInput.EndTime.ConvertToUtc(ExchangeTimeZone) : default;
                }
            }

            /// <summary>
            /// The end time of the last input data point for the symbol in UTC.
            /// </summary>
            public DateTime CurrentInputEndTimeUtc { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="SymbolData"/> class.
            /// </summary>
            public SymbolData(Symbol symbol, int period)
            {
                ExchangeTimeZone = _marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.ID.SecurityType).TimeZone;
                DataPoints = new(period);
            }
        }
    }

    /// <summary>
    /// Base class for indicators that work with multiple different symbols.
    /// </summary>
    /// <typeparam name="TInput">Indicator input data type</typeparam>
    public abstract class MultiSymbolIndicator<TInput> : MultiSymbolIndicator<TInput, decimal>
        where TInput : IBaseData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiSymbolIndicator{TInput}"/> class.
        /// </summary>
        protected MultiSymbolIndicator(string name, IEnumerable<Symbol> symbols, int period)
            : base(name, symbols, period)
        {
        }
    }
}
