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
    /// JMA is a volatility-adaptive filter that produces smoother output with less lag
    /// than the traditional EMA. It uses volatility bands to dynamically adjust its
    /// smoothing factor, combined with a three-stage adaptive pipeline: adaptive EMA,
    /// Kalman-style velocity estimation, and Jurik error correction.
    /// The period parameter controls both the base smoothing constants and the volatility
    /// band adaptation rate. Higher periods produce smoother, more lagged output.
    /// Note: The original JMA algorithm is proprietary (Jurik Research). This implementation
    /// follows the community-standard reverse-engineered formula used by pandas_ta,
    /// TradingView, and other open-source libraries.
    /// </summary>
    public class JurikMovingAverage : Indicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly decimal _phaseRatio;
        private readonly decimal _beta;
        private readonly decimal _power;

        // Volatility band constants derived from period
        private readonly double _length1;
        private readonly double _pow1;
        private readonly double _bet;

        // Volatility tracking
        private const int VolatilitySumLength = 10;
        private const int VolatilityAvgLength = 65;
        private readonly RollingWindow<decimal> _voltyWindow;
        private readonly RollingWindow<decimal> _vSumWindow;
        private decimal _vSum;

        // Adaptive band state
        private decimal _upperBand;
        private decimal _lowerBand;

        // Three-stage filter state
        private decimal _ma1;
        private decimal _det0;
        private decimal _det1;
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
        /// <param name="period">The period of the JMA, controls the smoothing window and volatility adaptation</param>
        /// <param name="phase">The phase parameter (-100 to 100), controls the tradeoff between lag and overshoot</param>
        /// <param name="power">The power parameter, controls smoothing aggressiveness (default 2)</param>
        public JurikMovingAverage(string name, int period, decimal phase = 0, decimal power = 2)
            : base(name)
        {
            _period = period;
            _power = power;

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

            // Base smoothing constant from period
            _beta = 0.45m * (_period - 1) / (0.45m * (_period - 1) + 2m);

            // Volatility band constants derived from period
            var length = 0.5 * (_period - 1);
            _length1 = Math.Max(Math.Log(Math.Sqrt(length)) / Math.Log(2.0) + 2.0, 0);
            _pow1 = Math.Max(_length1 - 2.0, 0.5);
            var length2 = _length1 * Math.Sqrt(length);
            _bet = length2 / (length2 + 1);

            // Rolling windows for volatility tracking
            _voltyWindow = new RollingWindow<decimal>(VolatilitySumLength + 1);
            _vSumWindow = new RollingWindow<decimal>(VolatilityAvgLength);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JurikMovingAverage"/> class using the specified period.
        /// </summary>
        /// <param name="period">The period of the JMA, controls the smoothing window and volatility adaptation</param>
        /// <param name="phase">The phase parameter (-100 to 100), controls the tradeoff between lag and overshoot</param>
        /// <param name="power">The power parameter, controls smoothing aggressiveness (default 2)</param>
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
            var price = input.Value;

            if (Samples == 1)
            {
                // Seed all state from first price
                _ma1 = price;
                _upperBand = price;
                _lowerBand = price;
                _jma = price;
                _det0 = 0;
                _det1 = 0;
                _vSum = 0;
                _voltyWindow.Add(0);
                _vSumWindow.Add(0);
                return 0m;
            }

            // Compute volatility relative to adaptive bands
            var del1 = price - _upperBand;
            var del2 = price - _lowerBand;
            var volty = Math.Abs(del1) != Math.Abs(del2)
                ? Math.Max(Math.Abs(del1), Math.Abs(del2))
                : 0m;

            // Update rolling volatility sum (running average over VolatilitySumLength bars)
            _voltyWindow.Add(volty);
            var oldest = _voltyWindow.Count > VolatilitySumLength
                ? _voltyWindow[VolatilitySumLength]
                : _voltyWindow[_voltyWindow.Count - 1];
            _vSum = _vSum + (volty - oldest) / VolatilitySumLength;
            _vSumWindow.Add(_vSum);

            // Average volatility: mean of v_sum values over available history (up to 65 bars)
            decimal avgVolty = 0;
            var count = (int)_vSumWindow.Count;
            if (count > 0)
            {
                decimal sum = 0;
                for (var i = 0; i < count; i++)
                {
                    sum += _vSumWindow[i];
                }
                avgVolty = sum / count;
            }

            // Relative volatility factor, clamped to [1, length1^(1/pow1)]
            var dVolty = avgVolty == 0 ? 0m : volty / avgVolty;
            var maxRVolty = (decimal)Math.Pow(_length1, 1.0 / _pow1);
            var rVolty = Math.Max(1.0m, Math.Min(maxRVolty, dVolty));

            // Update Jurik volatility bands using adaptive coefficient
            var pow2 = Math.Pow((double)rVolty, _pow1);
            var kv = (decimal)Math.Pow(_bet, Math.Sqrt(pow2));
            _upperBand = del1 > 0 ? price : price - kv * del1;
            _lowerBand = del2 < 0 ? price : price - kv * del2;

            // Adaptive alpha: beta^(rVolty^pow1) — varies with market volatility
            var alpha = (decimal)Math.Pow((double)_beta, pow2);

            // Stage 1: Adaptive EMA
            _ma1 = (1 - alpha) * price + alpha * _ma1;

            // Stage 2: Kalman-style velocity estimation
            _det0 = (price - _ma1) * (1 - _beta) + _beta * _det0;
            var ma2 = _ma1 + _phaseRatio * _det0;

            // Stage 3: Jurik adaptive error correction
            _det1 = (ma2 - _jma) * (1 - alpha) * (1 - alpha) + alpha * alpha * _det1;
            _jma = _jma + _det1;

            if (!IsReady)
            {
                return 0m;
            }

            return _jma;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _ma1 = 0;
            _det0 = 0;
            _det1 = 0;
            _jma = 0;
            _upperBand = 0;
            _lowerBand = 0;
            _vSum = 0;
            _voltyWindow.Reset();
            _vSumWindow.Reset();
            base.Reset();
        }
    }
}
