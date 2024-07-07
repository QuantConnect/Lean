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

using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Super trend indicator.
    /// Formula can be found here via the excel file:
    /// https://tradingtuitions.com/supertrend-indicator-excel-sheet-with-realtime-buy-sell-signals/
    /// </summary>
    public class SuperTrend : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly decimal _multiplier;
        private decimal _superTrend;
        private decimal _currentClose;
        private decimal _previousTrailingUpperBand;
        private decimal _previousTrailingLowerBand;
        private decimal _previousClose;
        private decimal _prevSuper;
        private readonly int _period;

        /// <summary>
        /// Average true range indicator used to calculate super trend's basic upper and lower bands
        /// </summary>
        private readonly AverageTrueRange _averageTrueRange;

        /// <summary>
        /// Basic Upper Band
        /// </summary>
        public decimal BasicUpperBand { get; private set; }

        /// <summary>
        /// Basic Lower band
        /// </summary>
        public decimal BasicLowerBand { get; private set; }

        /// <summary>
        /// Current Trailing Upper Band
        /// </summary>
        public decimal CurrentTrailingUpperBand { get; private set; }

        /// <summary>
        /// Current Trailing Lower Band
        /// </summary>
        public decimal CurrentTrailingLowerBand { get; private set; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _averageTrueRange.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Creates a new SuperTrend indicator using the specified name, period, multiplier and moving average type
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The smoothing period used by average true range</param>
        /// <param name="multiplier">The coefficient used in calculations of basic upper and lower bands</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the true range values</param>
        public SuperTrend(
            string name,
            int period,
            decimal multiplier,
            MovingAverageType movingAverageType = MovingAverageType.Wilders
        )
            : base(name)
        {
            _averageTrueRange = new AverageTrueRange(period, movingAverageType);
            _multiplier = multiplier;
            _period = period;
            _prevSuper = -1;
        }

        /// <summary>
        /// Creates a new SuperTrend indicator using the specified period, multiplier and moving average type
        /// </summary>
        /// <param name="period">The smoothing period used in average true range</param>
        /// <param name="multiplier">The coefficient used in calculations of basic upper and lower bands</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the true range values</param>
        public SuperTrend(
            int period,
            decimal multiplier,
            MovingAverageType movingAverageType = MovingAverageType.Wilders
        )
            : this($"SuperTrend({period},{multiplier})", period, multiplier, movingAverageType) { }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            if (!_averageTrueRange.Update(input))
            {
                _previousClose = input.Close;
                return 0m;
            }

            _currentClose = input.Close;
            BasicLowerBand =
                ((input.High + input.Low) / 2) - (_multiplier * _averageTrueRange.Current.Value);
            BasicUpperBand =
                ((input.High + input.Low) / 2) + (_multiplier * _averageTrueRange.Current.Value);

            CurrentTrailingLowerBand =
                (
                    (BasicLowerBand > _previousTrailingLowerBand)
                    || (_previousClose < _previousTrailingLowerBand)
                )
                    ? BasicLowerBand
                    : _previousTrailingLowerBand;
            CurrentTrailingUpperBand =
                (
                    (BasicUpperBand < _previousTrailingUpperBand)
                    || (_previousClose > _previousTrailingUpperBand)
                )
                    ? BasicUpperBand
                    : _previousTrailingUpperBand;

            if ((_prevSuper == -1) || (_prevSuper == _previousTrailingUpperBand))
            {
                _superTrend =
                    (_currentClose <= CurrentTrailingUpperBand)
                        ? CurrentTrailingUpperBand
                        : CurrentTrailingLowerBand;
            }
            else if (_prevSuper == _previousTrailingLowerBand)
            {
                _superTrend =
                    (_currentClose >= CurrentTrailingLowerBand)
                        ? CurrentTrailingLowerBand
                        : CurrentTrailingUpperBand;
            }

            // Save the values to be used in next iteration.
            _previousClose = _currentClose;
            _prevSuper = _superTrend;
            _previousTrailingLowerBand = CurrentTrailingLowerBand;
            _previousTrailingUpperBand = CurrentTrailingUpperBand;

            return _superTrend;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _averageTrueRange.Reset();
            _previousTrailingLowerBand = 0;
            _previousTrailingUpperBand = 0;
            _prevSuper = -1;
            base.Reset();
        }
    }
}
