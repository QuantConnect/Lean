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
    /// This indicator computes the Kaufman Adaptive Moving Average (KAMA).
    /// The Kaufman Adaptive Moving Average is calculated as explained here:
    /// http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:kaufman_s_adaptive_moving_average
    /// </summary>
    public class KaufmanAdaptiveMovingAverage : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        private readonly decimal _slowSmoothingFactor;
        private readonly decimal _diffSmoothingFactor;

        private decimal _sumRoc1;
        private decimal _periodRoc;
        private decimal _prevKama;
        private decimal _trailingValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="KaufmanAdaptiveMovingAverage"/> class using the specified name and period.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the Efficiency Ratio (ER)</param>
        /// <param name="fastEmaPeriod">The period of the fast EMA used to calculate the Smoothing Constant (SC)</param>
        /// <param name="slowEmaPeriod">The period of the slow EMA used to calculate the Smoothing Constant (SC)</param>
        public KaufmanAdaptiveMovingAverage(string name, int period, int fastEmaPeriod = 2, int slowEmaPeriod = 30)
            : base(name, period + 1)
        {
            // Smoothing factor of the slow EMA
            _slowSmoothingFactor = 2m / (slowEmaPeriod + 1m);
            // Difference between the smoothing factor of the fast and slow EMA
            _diffSmoothingFactor = 2m / (fastEmaPeriod + 1m) - _slowSmoothingFactor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KaufmanAdaptiveMovingAverage"/> class using the specified period.
        /// </summary>
        /// <param name="period">The period of the Efficiency Ratio (ER)</param>
        /// <param name="fastEmaPeriod">The period of the fast EMA used to calculate the Smoothing Constant (SC)</param>
        /// <param name="slowEmaPeriod">The period of the slow EMA used to calculate the Smoothing Constant (SC)</param>
        public KaufmanAdaptiveMovingAverage(int period, int fastEmaPeriod = 2, int slowEmaPeriod = 30)
            : this($"KAMA({period},{fastEmaPeriod},{slowEmaPeriod})", period, fastEmaPeriod, slowEmaPeriod)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= Period;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => Period;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <param name="window">The window for the input history</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            if (Samples < Period)
            {
                if (Samples > 1)
                {
                    _sumRoc1 += Math.Abs(input.Value - window[1].Value);
                }

                return input.Value;
            }

            if (Samples == Period)
            {
                _sumRoc1 += Math.Abs(input.Value - window[1].Value);

                // At this point sumROC1 represents the 
                // summation of the 1-day price difference
                // over the (optInTimePeriod-1)

                // Calculate the first KAMA
                // The yesterday price is used here as the previous KAMA.
                _prevKama = window[1].Value;
            }

            var newTrailingValue = window[Period - 1];
            _periodRoc = input.Value - newTrailingValue.Value;

            if (Samples > Period)
            {
                // Adjust sumROC1:
                // - Remove trailing ROC1 
                // - Add new ROC1
                _sumRoc1 -= Math.Abs(_trailingValue - newTrailingValue.Value);
                _sumRoc1 += Math.Abs(input.Value - window[1].Value);
            }

            // Save the trailing value.
            _trailingValue = newTrailingValue.Value;

            // Calculate the efficiency ratio
            var efficiencyRatio = (_sumRoc1 <= _periodRoc) || _sumRoc1 == 0 ? 1m : Math.Abs(_periodRoc / _sumRoc1);

            // Calculate the smoothing constant
            var smoothingConstant = efficiencyRatio * _diffSmoothingFactor + _slowSmoothingFactor;
            smoothingConstant *= smoothingConstant;

            // Calculate the KAMA like an EMA, using the
            // smoothing constant as the adaptive factor.
            _prevKama = (input.Value - _prevKama) * smoothingConstant + _prevKama;

            return _prevKama;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _sumRoc1 = 0;
            _periodRoc = 0;
            _prevKama = 0;
            _trailingValue = 0;
            base.Reset();
        }
    }
}