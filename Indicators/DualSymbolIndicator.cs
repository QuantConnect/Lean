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
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using NodaTime;
using QuantConnect.Data;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Base class for indicators that work with two different symbols and calculate an indicator based on them.
    /// </summary>
    /// <typeparam name="T">Type of the data points stored in the rolling windows for each symbol (e.g., double, decimal, etc.)</typeparam>
    public abstract class DualSymbolIndicator<T> : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Time zone of the target symbol.
        /// </summary>
        private readonly DateTimeZone _targetTimeZone;

        /// <summary>
        /// Time zone of the reference symbol.
        /// </summary>
        private readonly DateTimeZone _referenceTimeZone;

        /// <summary>
        /// Stores the previous input data point.
        /// </summary>
        private IBaseDataBar _previousInput;

        /// <summary>
        /// The resolution of the data (e.g., daily, hourly, etc.).
        /// </summary>
        private Resolution _resolution;

        /// <summary>
        /// RollingWindow to store the data points of the target symbol
        /// </summary>
        protected RollingWindow<T> TargetDataPoints { get; }

        /// <summary>
        /// RollingWindow to store the data points of the reference symbol
        /// </summary>
        protected RollingWindow<T> ReferenceDataPoints { get; }

        /// <summary>
        /// Symbol of the reference used
        /// </summary>
        protected Symbol ReferenceSymbol { get; }

        /// <summary>
        /// Symbol of the target used
        /// </summary>
        protected Symbol TargetSymbol { get; }

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
        /// <param name="targetSymbol">The symbol of the target asset.</param>
        /// <param name="referenceSymbol">The symbol of the reference asset.</param>
        /// <param name="period">The period (number of data points) over which to calculate the indicator.</param>
        protected DualSymbolIndicator(string name, Symbol targetSymbol, Symbol referenceSymbol, int period) : base(name)
        {
            TargetDataPoints = new RollingWindow<T>(period);
            ReferenceDataPoints = new RollingWindow<T>(period);
            TargetSymbol = targetSymbol;
            ReferenceSymbol = referenceSymbol;

            var dataFolder = MarketHoursDatabase.FromDataFolder();
            _targetTimeZone = dataFolder.GetExchangeHours(TargetSymbol.ID.Market, TargetSymbol, TargetSymbol.ID.SecurityType).TimeZone;
            _referenceTimeZone = dataFolder.GetExchangeHours(ReferenceSymbol.ID.Market, ReferenceSymbol, ReferenceSymbol.ID.SecurityType).TimeZone;
            IsTimezoneDifferent = _targetTimeZone != _referenceTimeZone;
        }

        /// <summary>
        /// Checks and computes the indicator if the input data matches.
        /// This method ensures the input data points are from matching time periods and different symbols.
        /// </summary>
        /// <param name="input">The input data point (e.g., TradeBar for a symbol).</param>
        /// <returns>The most recently computed value of the indicator.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            if (_previousInput == null)
            {
                _previousInput = input;
                _resolution = GetResolution(input);
                return decimal.Zero;
            }

            var isMatchingTime = CompareEndTimes(input.EndTime, _previousInput.EndTime);

            if (input.Symbol != _previousInput.Symbol && isMatchingTime)
            {
                AddDataPoint(input);
                AddDataPoint(_previousInput);
                ComputeIndicator();
            }
            _previousInput = input;
            return IndicatorValue;
        }

        /// <summary>
        /// Performs the specific computation for the indicator.
        /// </summary>
        protected abstract void ComputeIndicator();

        /// <summary>
        /// Determines the resolution of the input data based on the time difference between its start and end times. 
        /// Returns <see cref="Resolution.Daily"/> if the difference exceeds 1 hour; otherwise, calculates a higher equivalent resolution.
        /// </summary>
        private Resolution GetResolution(IBaseData input)
        {
            var timeDifference = input.EndTime - input.Time;
            return timeDifference.TotalHours > 1 ? Resolution.Daily : timeDifference.ToHigherResolutionEquivalent(false);
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
                case Resolution.Second:
                    return date;
                default:
                    return date;
            }
        }

        /// <summary>
        /// Compares the end times of two data points to check if they are in the same time period.
        /// Adjusts for time zone differences if necessary.
        /// </summary>
        /// <param name="currentEndTime">The end time of the current data point.</param>
        /// <param name="previousEndTime">The end time of the previous data point.</param>
        /// <returns>True if the end times match after considering time zones and resolution.</returns>
        private bool CompareEndTimes(DateTime currentEndTime, DateTime previousEndTime)
        {
            var previousSymbolIsTarget = _previousInput.Symbol == TargetSymbol;
            if (IsTimezoneDifferent)
            {
                currentEndTime = currentEndTime.ConvertToUtc(previousSymbolIsTarget ? _referenceTimeZone : _targetTimeZone);
                previousEndTime = previousEndTime.ConvertToUtc(previousSymbolIsTarget ? _targetTimeZone : _referenceTimeZone);
            }
            return AdjustDateToResolution(currentEndTime) == AdjustDateToResolution(previousEndTime);
        }

        /// <summary>
        /// Adds the closing price to the corresponding symbol's data set (target or reference).
        /// This method stores the data points for each symbol and performs specific calculations 
        /// based on the symbol. For instance, it computes returns in the case of the Beta indicator.
        /// </summary>
        /// <param name="input">The input value for this symbol</param>
        /// <exception cref="ArgumentException">Thrown if the input symbol does not match either the target or reference symbol.</exception>
        protected abstract void AddDataPoint(IBaseDataBar input);

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _previousInput = null;
            IndicatorValue = 0;
            TargetDataPoints.Reset();
            ReferenceDataPoints.Reset();
            base.Reset();
        }
    }
}
