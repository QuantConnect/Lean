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
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Represents the Connors Relative Strength Index (CRSI), a combination of 
    /// the traditional Relative Strength Index (RSI), a Streak RSI (SRSI), and  
    /// Percent Rank.
    /// This index is designed to provide a more robust measure of market strength 
    /// by combining momentum, streak behavior, and price change.
    /// </summary>
    public class ConnorsRelativeStrengthIndex : Indicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Computes the traditional Relative Strength Index (RSI).
        /// </summary>
        private readonly RelativeStrengthIndex _rsi;

        /// <summary>
        /// Computes the RSI based on consecutive price streaks (SRSI).
        /// </summary>
        private readonly RelativeStrengthIndex _srsi;

        /// <summary>
        /// Stores recent price change ratios for calculating the Percent Rank.
        /// </summary>
        private readonly RollingWindow<decimal> _priceChangeRatios;

        /// <summary>
        /// Tracks the current trend streak (positive or negative) of price movements.
        /// </summary>
        private int _trendStreak;

        /// <summary>
        /// Stores the previous input data point.
        /// </summary>
        private IndicatorDataPoint _previousInput;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnorsRelativeStrengthIndex"/> class.
        /// </summary>
        /// <param name="name">The name of the indicator instance.</param>
        /// <param name="rsiPeriod">The period for the RSI calculation.</param>
        /// <param name="rsiPeriodStreak">The period for the Streak RSI calculation.</param>
        /// <param name="lookBackPeriod">The period for calculating the Percent Rank.</param>
        public ConnorsRelativeStrengthIndex(string name, int rsiPeriod, int rsiPeriodStreak, int lookBackPeriod) : base(name)
        {
            _rsi = new RelativeStrengthIndex(rsiPeriod);
            _srsi = new RelativeStrengthIndex(rsiPeriodStreak);
            _priceChangeRatios = new RollingWindow<decimal>(lookBackPeriod);
            _trendStreak = 0;
            WarmUpPeriod = Math.Max(lookBackPeriod, Math.Max(_rsi.WarmUpPeriod, _srsi.WarmUpPeriod));
        }

        /// <summary>
        /// Initializes a new instance of the ConnorsRelativeStrengthIndex with specified RSI, Streak RSI, 
        /// and lookBack periods, using a default name format based on the provided parameters.
        /// </summary>
        public ConnorsRelativeStrengthIndex(int rsiPeriod, int rsiPeriodStreak, int rocPeriod)
            : this($"CRSI({rsiPeriod},{rsiPeriodStreak},{rocPeriod})", rsiPeriod, rsiPeriodStreak, rocPeriod)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the indicator is ready for use.
        /// The indicator is ready when all its components (RSI, SRSI, and PriceChangeRatios) are ready.
        /// </summary>
        public override bool IsReady => _rsi.IsReady && _srsi.IsReady && _priceChangeRatios.IsReady;

        /// <summary>
        /// Gets the warm-up period required for the indicator to be ready.
        /// This is the maximum period of all components (RSI, SRSI, and PriceChangeRatios).
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Computes the next value for the Connors Relative Strength Index (CRSI) based on the latest input data point.
        /// The CRSI is calculated as the average of the traditional RSI, Streak RSI, and Percent Rank.
        /// </summary>
        /// <param name="input">The current input data point (typically the price data for the current period).</param>
        /// <returns>The computed CRSI value, which combines the RSI, Streak RSI, and Percent Rank into a single value. 
        /// Returns zero if the indicator is not yet ready.</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            // RSI
            _rsi.Update(input);

            ComputeTrendStreak(input);
            _srsi.Update(new IndicatorDataPoint(input.EndTime, _trendStreak));

            if (_previousInput == null || _previousInput.Value == 0)
            {
                _previousInput = input;
                _priceChangeRatios.Add(0m);
                return decimal.Zero;
            }

            // PercentRank
            var relativeMagnitude = 0m;
            var priceChangeRatio = (input.Value - _previousInput.Value) / _previousInput.Value;

            // Calculate PercentRank using only the previous values (exclude the current priceChangeRatio)
            if (_priceChangeRatios.IsReady)
            {
                relativeMagnitude = 100m * _priceChangeRatios.Count(x => x < priceChangeRatio) / _priceChangeRatios.Count;
            }

            // Add the current priceChangeRatio to the rolling window for future calculations
            _priceChangeRatios.Add(priceChangeRatio);

            _previousInput = input;

            // CRSI
            if (IsReady)
            {
                // Calculate the CRSI only if all components are ready
                return (_rsi.Current.Value + _srsi.Current.Value + relativeMagnitude) / 3;
            }

            // If not ready, return 0
            return decimal.Zero;
        }

        /// <summary>
        /// Updates the trend streak based on the price change direction between the current and previous input.
        /// Resets the streak if the direction changes, otherwise increments or decrements it.
        /// </summary>
        /// <param name="input">The current input data point with price information.</param>
        private void ComputeTrendStreak(IndicatorDataPoint input)
        {
            if (_previousInput == null)
            {
                return;
            }
            var change = input.Value - _previousInput.Value;
            // If the price changes direction (up to down or down to up), reset the trend streak
            if ((_trendStreak > 0 && change < 0) || (_trendStreak < 0 && change > 0))
            {
                _trendStreak = 0;
            }
            // Increment or decrement the trend streak based on price change direction
            if (change > 0)
            {
                _trendStreak++;
            }
            else if (change < 0)
            {
                _trendStreak--;
            }
        }

        /// <summary>
        /// Resets the indicator to its initial state. This clears all internal data and resets
        /// the RSI, Streak RSI, and PriceChangeRatios, as well as the trend streak counter.
        /// </summary>
        public override void Reset()
        {
            _rsi.Reset();
            _srsi.Reset();
            _priceChangeRatios.Reset();
            _trendStreak = 0;
            base.Reset();
        }
    }
}
