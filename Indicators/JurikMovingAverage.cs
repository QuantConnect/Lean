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
    /// Represents the Jurik Moving Average (JMA) indicator.
    /// JMA is a three-stage adaptive filter that produces smoother output with less lag
    /// than the traditional EMA by combining an adaptive EMA, Kalman-style velocity
    /// estimation, and error correction.
    /// Note: The original JMA algorithm is proprietary (Jurik Research). This implementation
    /// follows the community-standard reverse-engineered formula used by pandas_ta,
    /// TradingView, and other open-source libraries.
    /// </summary>
    public class JurikMovingAverage : Indicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly decimal _phaseRatio;
        private readonly decimal _alpha;
        private readonly decimal _beta;

        private decimal _e0;
        private decimal _e1;
        private decimal _e2;
        private decimal _jma;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= _period;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Initializes a new instance of the <see cref="JurikMovingAverage"/> class using the specified name and period.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the JMA</param>
        /// <param name="phase">The phase parameter (-100 to 100), controls the tradeoff between lag and overshoot</param>
        /// <param name="power">The power parameter, controls smoothing aggressiveness</param>
        public JurikMovingAverage(string name, int period, decimal phase = 0, decimal power = 2)
            : base(name)
        {
            _period = period;

            // Compute phase ratio: clamp phase to [-100, 100] range
            if (phase < -100m)
            {
                _phaseRatio = 0.5m;
            }
            else if (phase > 100m)
            {
                _phaseRatio = 2.5m;
            }
            else
            {
                _phaseRatio = phase / 100m + 1.5m;
            }

            // Compute smoothing constants
            _beta = 0.45m * (_period - 1) / (0.45m * (_period - 1) + 2m);
            _alpha = (decimal)Math.Pow((double)_beta, (double)power);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JurikMovingAverage"/> class using the specified period.
        /// </summary>
        /// <param name="period">The period of the JMA</param>
        /// <param name="phase">The phase parameter (-100 to 100), controls the tradeoff between lag and overshoot</param>
        /// <param name="power">The power parameter, controls smoothing aggressiveness</param>
        public JurikMovingAverage(int period, decimal phase = 0, decimal power = 2)
            : this($"JMA({period},{phase},{power})", period, phase, power)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            if (!IsReady)
            {
                return 0;
            }

            if (Samples == _period)
            {
                // Seed the filter with the first price
                _e0 = input.Value;
                _e1 = 0;
                _e2 = 0;
                _jma = input.Value;
                return input.Value;
            }

            // Stage 1: Adaptive EMA
            _e0 = (1 - _alpha) * input.Value + _alpha * _e0;

            // Stage 2: Kalman-style velocity estimation
            _e1 = (input.Value - _e0) * (1 - _beta) + _beta * _e1;

            // Stage 3: Error correction with phase adjustment
            var oneMinusAlpha = 1 - _alpha;
            _e2 = (_e0 + _phaseRatio * _e1 - _jma) * (oneMinusAlpha * oneMinusAlpha) + _alpha * _alpha * _e2;

            // Final JMA value
            _jma = _jma + _e2;

            return _jma;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _e0 = 0;
            _e1 = 0;
            _e2 = 0;
            _jma = 0;
            base.Reset();
        }
    }
}
