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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Represents the Connors Relative Strength Index (CRSI), a combination of 
    /// the standard Relative Strength Index (RSI), a Streak RSI (SRSI), and  
    /// Rate of Change (ROC).
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
        /// Stores recent price data for calculating the Rate of Change (ROC).
        /// </summary>
        private readonly RollingWindow<decimal> _recentPrices;

        /// <summary>
        /// Tracks the current trend streak (positive or negative) of price movements.
        /// </summary>
        private int _trendStreak;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnorsRelativeStrengthIndex"/> class.
        /// </summary>
        /// <param name="name">The name of the indicator instance.</param>
        /// <param name="rsiPeriod">The period for the RSI calculation.</param>
        /// <param name="rsiPeriodStreak">The period for the Streak RSI calculation.</param>
        /// <param name="rocPeriod">The period for the Rate of Change ROC calculation.</param>
        public ConnorsRelativeStrengthIndex(string name, int rsiPeriod, int rsiPeriodStreak, int rocPeriod) : base(name)
        {
            _rsi = new RelativeStrengthIndex(rsiPeriod);
            _srsi = new RelativeStrengthIndex(rsiPeriodStreak);
            _recentPrices = new RollingWindow<decimal>(rocPeriod + 1);
            _trendStreak = 0;
            WarmUpPeriod = Math.Max(rsiPeriod, Math.Max(rsiPeriodStreak, rocPeriod + 1));
        }

        /// <summary>
        /// Initializes a new instance of the ConnorsRelativeStrengthIndex with specified RSI, Streak RSI, 
        /// and ROC periods, using a default name format based on the provided parameters.
        /// </summary>
        public ConnorsRelativeStrengthIndex(int rsiPeriod, int rsiPeriodStreak, int rocPeriod)
            : this($"CRSI({rsiPeriod},{rsiPeriodStreak},{rocPeriod})", rsiPeriod, rsiPeriodStreak, rocPeriod)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the indicator is ready for use.
        /// The indicator is ready when all its components (RSI, SRSI, and ROC) are ready.
        /// </summary>
        public override bool IsReady => _rsi.IsReady && _srsi.IsReady && _recentPrices.IsReady;

        /// <summary>
        /// Gets the warm-up period required for the indicator to be ready.
        /// This is the maximum period of all components (RSI, SRSI, and ROC).
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Computes the next value for the Connors Relative Strength Index (CRSI) based on the latest input data point.
        /// The CRSI is calculated as the average of the traditional RSI, Streak RSI, and the Price Percentage Change.
        /// </summary>
        /// <param name="input">The current input data point (typically the price data for the current period).</param>
        /// <returns>The computed CRSI value, which combines the RSI, Streak RSI, and ROC into a single value. 
        /// Returns zero if the indicator is not yet ready.</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            //RSI
            _rsi.Update(input);

            //SRSI
            var previousValue = (_recentPrices.Count > 0) ? _recentPrices[0] : input.Value;
            var change = input.Value - previousValue;
            // If the price changes direction (up to down or down to up), reset the trend streak
            if ((_trendStreak > 0 && change < 0) || (_trendStreak < 0 && change > 0))
            {
                _trendStreak = 0;
            }
            // Increment or decrement the trend streak based on price change direction
            _trendStreak += change > 0 ? 1 : change < 0 ? -1 : 0;
            _srsi.Update(new IndicatorDataPoint(input.EndTime, _trendStreak));

            //ROC
            _recentPrices.Add(input.Value);
            // Get the initial price from the rolling window
            var initialPrice = _recentPrices[_recentPrices.Count - 1];
            if (initialPrice == 0)
            {
                return decimal.Zero;
            }
            var rateOfChange = (input.Value - initialPrice) / initialPrice;

            //CRSI
            if (IsReady)
            {
                return (_rsi.Current.Value + _srsi.Current.Value + rateOfChange) / 3;
            }
            return decimal.Zero;
        }

        /// <summary>
        /// Resets the indicator to its initial state. This clears all internal data and resets
        /// the RSI, Streak RSI, and ROC, as well as the trend streak counter.
        /// </summary>
        public override void Reset()
        {
            _rsi.Reset();
            _srsi.Reset();
            _recentPrices.Reset();
            _trendStreak = 0;
            base.Reset();
        }
    }
}
