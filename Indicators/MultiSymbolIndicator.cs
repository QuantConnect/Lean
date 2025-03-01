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
    public abstract class MultiSymbolIndicator<TInput> : IndicatorBase<TInput>, IIndicatorWarmUpPeriodProvider
        where TInput : IBaseData
    {
        /// <summary>
        /// Relevant data for each symbol the indicator works on, including all inputs
        /// and actual data points used for calculation.
        /// </summary>
        protected Dictionary<Symbol, SymbolData> DataBySymbol { get; }

        /// <summary>
        /// The most recently computed value of the indicator.
        /// </summary>
        protected decimal IndicatorValue { get; set; }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; set; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => DataBySymbol.Values.All(data => data.DataPoints.IsReady);

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
            var isTimezoneDifferent = DataBySymbol.Values.Select(data => data.ExchangeTimeZone).Distinct().Count() > 1;
            WarmUpPeriod = period + (isTimezoneDifferent ? 1 : 0);
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

            if (Samples == 1)
            {
                SetResolution(input);
                return decimal.Zero;
            }

            symbolData.CurrentInput = input;

            // Ready to calculate when all symbols get data for the same time
            if (DataBySymbol.Values.Select(data => data.CurrentInputEndTimeUtc).Distinct().Count() == 1)
            {
                // Add the actual inputs that should be used to the rolling windows
                foreach (var data in DataBySymbol.Values)
                {
                    data.DataPoints.Add(data.CurrentInput);
                }
                IndicatorValue = ComputeIndicator();
            }

            return IndicatorValue;
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state.
        /// This will be called only when the indicator is ready, that is,
        /// when data for all symbols at a given time is available.
        /// </summary>
        protected abstract decimal ComputeIndicator();

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            IndicatorValue = 0;
            foreach (var data in DataBySymbol.Values)
            {
                data.Reset();
            }
            base.Reset();
        }

        /// <summary>
        /// Determines the resolution of the input data based on the time difference between its start and end times.
        /// Resolution will <see cref="Resolution.Daily"/> if the difference exceeds 1 hour; otherwise, calculates a higher equivalent resolution.
        /// Then it sets the resolution to the symbols data so that the time alignment is performed correctly.
        /// </summary>
        private void SetResolution(TInput input)
        {
            var timeDifference = input.EndTime - input.Time;
            var resolution = timeDifference.TotalHours > 1 ? Resolution.Daily : timeDifference.ToHigherResolutionEquivalent(false);
            foreach (var (symbol, data) in DataBySymbol)
            {
                data.SetResolution(resolution);
                if (symbol == input.Symbol)
                {
                    data.CurrentInput = input;
                }
            }
        }

        /// <summary>
        /// Contains the data points, the current input and other relevant indicator data for a symbol.
        /// </summary>
        protected class SymbolData
        {
            private static MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

            private TInput _currentInput;
            private Resolution _resolution;

            /// <summary>
            /// The exchange time zone for the security represented by this symbol.
            /// </summary>
            public DateTimeZone ExchangeTimeZone { get; }

            /// <summary>
            /// Data points for the symbol.
            /// This only hold the data points that have been used to calculate the indicator,
            /// which are those that had matching end times for every symbol.
            /// </summary>
            public RollingWindow<TInput> DataPoints { get; }

            /// <summary>
            /// The last input data point for the symbol.
            /// </summary>
            public TInput CurrentInput
            {
                get => _currentInput;
                set
                {
                    _currentInput = value;
                    if (_currentInput != null)
                    {
                        CurrentInputEndTimeUtc = AdjustDateToResolution(_currentInput.EndTime.ConvertToUtc(ExchangeTimeZone));
                        NewInput?.Invoke(this, _currentInput);
                    }
                    else
                    {
                        CurrentInputEndTimeUtc = default;
                    }
                }
            }

            /// <summary>
            /// Event that fires when a new input data point is set for the symbol.
            /// </summary>
            public event EventHandler<TInput> NewInput;

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

            /// <summary>
            /// Resets this symbol data to its initial state
            /// </summary>
            public void Reset()
            {
                DataPoints.Reset();
                CurrentInput = default;
            }

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
            /// Sets the resolution for this symbol data, to be used for time alignment.
            /// </summary>
            public void SetResolution(Resolution resolution)
            {
                _resolution = resolution;
            }
        }
    }
}
