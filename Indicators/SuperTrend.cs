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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Super trend indicator. 
    /// Formula can be found for example here:
    /// https://www.tradingfuel.com/supertrend-indicator-formula-and-calculation/
    /// </summary>
    public class SuperTrend : BarIndicator
    {
        private readonly decimal _multiplier;
        private decimal _superTrend;
        private decimal _currentClose;
        private decimal _currentBasicUpperBand;
        private decimal _currentBasicLowerBand;
        private decimal _currentTrailingUpperBand;
        private decimal _currentTrailingLowerBand;
        private decimal _currentTrend;
        private decimal _previousTrend;
        private decimal _previousTrailingUpperBand;
        private decimal _previousTrailingLowerBand;
        private decimal _previousClose;

        // Average true range indicator used to calculate super trend's basic upper and lower bands
        private readonly AverageTrueRange _averageTrueRange;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _superTrend != 0m;

        /// <summary>
        /// Creates a new SuperTrend indicator using the specified name, period, multiplier and moving average type
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The smoothing period used by average true range</param>
        /// <param name="multiplier">The coefficient used in calculations of basic upper and lower bands</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the true range values</param>
        public SuperTrend(string name, int period, decimal multiplier, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : base(name)
        {
            _averageTrueRange = new AverageTrueRange(period, movingAverageType);
            _multiplier = multiplier;
        }

        /// <summary>
        /// Creates a new SuperTrend indicator using the specified period, multiplier and moving average type
        /// </summary>
        /// <param name="period">The smoothing period used in average true range</param>
        /// <param name="multiplier">The coefficient used in calculations of basic upper and lower bands</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the true range values</param>
        public SuperTrend(int period, decimal multiplier, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : this($"SuperTrend({period},{multiplier})", period, multiplier, movingAverageType)
        {

        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            _averageTrueRange.Update(input);
            if (!_averageTrueRange.IsReady)
            {
                return 0m;
            }

            _currentClose = input.Close;
            _currentBasicLowerBand = (input.Low + input.High) / 2 - _multiplier * _averageTrueRange.Current.Value;
            _currentBasicUpperBand = (input.Low + input.High) / 2 + _multiplier * _averageTrueRange.Current.Value;

            _currentTrailingLowerBand = _previousClose > _previousTrailingLowerBand ? 
                Math.Max(_currentBasicLowerBand, _previousTrailingLowerBand) : _currentBasicLowerBand;

            _currentTrailingUpperBand = _previousClose < _previousTrailingUpperBand ? 
                Math.Min(_currentBasicUpperBand, _previousTrailingUpperBand) : _currentBasicUpperBand;

            // Is 0m (zero) when first time updated
            _currentTrend = _currentClose > _currentTrailingUpperBand ? 1 :
                _currentClose < _currentTrailingLowerBand ? -1 : _previousTrend;

            // Calculate super trend. Has a 0m value (zero) only at first update .
            _superTrend = _currentTrend == 1 ? _currentTrailingLowerBand :
                _currentTrend == -1 ? _currentTrailingUpperBand : 0m;

            // Save the values to be used in next iteration.
            _previousClose = _currentClose;
            _previousTrailingLowerBand = _currentTrailingLowerBand;
            _previousTrailingUpperBand = _currentTrailingUpperBand;
            _previousTrend = _currentTrend;

            return _superTrend;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _averageTrueRange.Reset();
            base.Reset();
        }
    }
}