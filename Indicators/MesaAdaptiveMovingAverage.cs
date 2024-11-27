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
    /// Implements the Mesa Adaptive Moving Average (MAMA) indicator along with the following FAMA (Following Adaptive Moving Average) as a secondary indicator.
    /// The MAMA adjusts its smoothing factor based on the market's volatility, making it more adaptive than a simple moving average.
    /// </summary>
    public class MesaAdaptiveMovingAverage : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// The fast limit value used in the adaptive calculation.
        /// </summary>
        private readonly decimal _fastLimit;

        /// <summary>
        /// The slow limit value used in the adaptive calculation.
        /// </summary>
        private readonly decimal _slowLimit;

        /// <summary>
        /// Conversion factor for converting radians to degrees.
        /// </summary>
        private readonly decimal _rad2Deg = 180m / (4m * (decimal)Math.Atan(1.0));

        /// <summary>
        /// Rolling windows to store historical data for calculation purposes.
        /// </summary>
        private readonly RollingWindow<decimal> _priceHistory;
        private readonly RollingWindow<decimal> _smoothHistory;
        private readonly RollingWindow<decimal> _detrendHistory;
        private readonly RollingWindow<decimal> _inPhaseHistory;
        private readonly RollingWindow<decimal> _quadratureHistory;

        /// <summary>
        /// Variables holding previous calculation values for use in subsequent iterations.
        /// </summary>
        private decimal _prevPeriod;
        private decimal _prevI2;
        private decimal _prevQ2;
        private decimal _prevRe;
        private decimal _prevIm;
        private decimal _prevSmoothPeriod;
        private decimal _prevPhase;
        private decimal _prevMama;

        /// <summary>
        /// Gets the FAMA (Following Adaptive Moving Average) indicator value.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Fama { get; }

        /// <summary>
        /// Initializes a new instance of the MesaAdaptiveMovingAverage class.
        /// </summary>
        /// <param name="name">The name of the indicator.</param>
        /// <param name="fastLimit">The fast limit for the adaptive moving average (default is 0.5).</param>
        /// <param name="slowLimit">The slow limit for the adaptive moving average (default is 0.05).</param>
        public MesaAdaptiveMovingAverage(string name, decimal fastLimit = 0.5m, decimal slowLimit = 0.05m)
            : base(name)
        {
            _fastLimit = fastLimit;
            _slowLimit = slowLimit;
            _priceHistory = new RollingWindow<decimal>(13);
            _smoothHistory = new RollingWindow<decimal>(6);
            _detrendHistory = new RollingWindow<decimal>(6);
            _inPhaseHistory = new RollingWindow<decimal>(6);
            _quadratureHistory = new RollingWindow<decimal>(6);
            _prevPeriod = 0m;
            _prevI2 = 0m;
            _prevQ2 = 0m;
            _prevRe = 0m;
            _prevIm = 0m;
            _prevSmoothPeriod = 0m;
            _prevPhase = 0m;
            _prevMama = 0m;
            Fama = new Identity(name + "_Fama");
        }

        /// <summary>
        /// Initializes a new instance of the MesaAdaptiveMovingAverage class with default name ("MAMA") 
        /// and the specified fast and slow limits for the adaptive moving average calculation.
        /// </summary>
        /// <param name="fastLimit">The fast limit for the adaptive moving average (default is 0.5).</param>
        /// <param name="slowLimit">The slow limit for the adaptive moving average (default is 0.05).</param>
        public MesaAdaptiveMovingAverage(decimal fastLimit = 0.5m, decimal slowLimit = 0.05m)
            : this($"MAMA", fastLimit, slowLimit)
        {
        }


        /// <summary>
        /// Returns whether the indicator has enough data to be used (ready to calculate values).
        /// </summary>
        public override bool IsReady => Samples >= WarmUpPeriod;

        /// <summary>
        /// Gets the number of periods required for warming up the indicator.
        /// 33 periods are sufficient for the MAMA to provide stable and accurate results,
        /// </summary>
        public int WarmUpPeriod => 33;

        /// <summary>
        /// Computes the next value for the Mesa Adaptive Moving Average (MAMA).
        /// It calculates the MAMA by applying a series of steps including smoothing, detrending, and phase adjustments.
        /// </summary>
        /// <param name="input">The input bar (price data).</param>
        /// <returns>The calculated MAMA value.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var price = (input.High + input.Low) / 2;
            _priceHistory.Add(price);

            if (!_priceHistory.IsReady)
            {
                return decimal.Zero;
            }

            //Calculate the MAMA and FAMA
            var (mama, fama) = ComputeMamaAndFama();

            // Update previous values
            _prevMama = mama;
            Fama.Update(input.EndTime, fama);

            if (!IsReady)
            {
                return decimal.Zero;
            }
            return mama;
        }

        private (decimal, decimal) ComputeMamaAndFama()
        {
            // Small Coefficient
            const decimal sC = 0.0962m;
            // Large Coefficient
            const decimal lC = 0.5769m;

            var adjustedPeriod = 0.075m * _prevPeriod + 0.54m;

            // Compute the smoothed price value using a weighted average of the most recent prices.
            var smooth = (4 * _priceHistory[0] + 3 * _priceHistory[1] + 2 * _priceHistory[2] + _priceHistory[3]) / 10;

            // Detrend the smoothed price to remove market noise, applying coefficients and adjusted period.
            var detrender = (sC * smooth + lC * _smoothHistory[1] - lC * _smoothHistory[3] - sC * _smoothHistory[5]) * adjustedPeriod;

            // Compute the InPhase (I1) and Quadrature (Q1) components for the adaptive moving average.
            var q1 = (sC * detrender + lC * _detrendHistory[1] - lC * _detrendHistory[3] - sC * _detrendHistory[5]) * adjustedPeriod;
            var i1 = _detrendHistory[2];

            // Advance the phase of I1 and Q1 by 90 degrees
            var ji = (sC * i1 + lC * _inPhaseHistory[1] - lC * _inPhaseHistory[3] - sC * _inPhaseHistory[5]) * adjustedPeriod;
            var jq = (sC * q1 + lC * _quadratureHistory[1] - lC * _quadratureHistory[3] - sC * _quadratureHistory[5]) * adjustedPeriod;
            var i2 = i1 - jq;
            var q2 = q1 + ji;

            // Smooth the I2 and Q2 components before applying the discriminator
            i2 = 0.2m * i2 + 0.8m * _prevI2;
            q2 = 0.2m * q2 + 0.8m * _prevQ2;

            // Get alpha
            var alpha = ComputeAlpha(i1, q1, i2, q2);

            // Calculate the MAMA and FAMA
            var mama = alpha * _priceHistory[0] + (1m - alpha) * _prevMama;
            var fama = 0.5m * alpha * mama + (1m - 0.5m * alpha) * Fama.Current.Value;

            // Update rolling history
            _smoothHistory.Add(smooth);
            _detrendHistory.Add(detrender);
            _inPhaseHistory.Add(i1);
            _quadratureHistory.Add(q1);

            return (mama, fama);
        }

        private decimal ComputeAlpha(decimal i1, decimal q1, decimal i2, decimal q2)
        {
            var re = i2 * _prevI2 + q2 * _prevQ2;
            var im = i2 * _prevQ2 - q2 * _prevI2;
            re = 0.2m * re + 0.8m * _prevRe;
            im = 0.2m * im + 0.8m * _prevIm;

            // Calculate the period 
            var period = 0m;
            if (im != 0 && re != 0)
            {
                var angleInDegrees = (decimal)Math.Atan((double)(im / re)) * _rad2Deg;
                period = (angleInDegrees > 0) ? 360m / angleInDegrees : 0m;
            }

            // Limit the period to certain thresholds
            if (period > 1.5m * _prevPeriod)
            {
                period = 1.5m * _prevPeriod;
            }
            if (period < 0.67m * _prevPeriod)
            {
                period = 0.67m * _prevPeriod;
            }
            if (period < 6)
            {
                period = 6;
            }
            if (period > 50)
            {
                period = 50;
            }

            // Smooth the period and calculate the phase
            period = 0.2m * period + 0.8m * _prevPeriod;
            var smoothPeriod = 0.33m * period + 0.67m * _prevSmoothPeriod;

            // Calculate the phase
            var phase = 0m;
            if (i1 != 0)
            {
                phase = (decimal)Math.Atan((double)(q1 / i1)) * _rad2Deg;
            }

            // Calculate the delta phase
            var deltaPhase = _prevPhase - phase;
            if (deltaPhase < 1m)
            {
                deltaPhase = 1m;
            }

            // Calculate alpha
            var alpha = _fastLimit / deltaPhase;
            if (alpha < _slowLimit)
            {
                alpha = _slowLimit;
            }

            // Update previous values
            _prevI2 = i2;
            _prevQ2 = q2;
            _prevRe = re;
            _prevIm = im;
            _prevPeriod = period;
            _prevSmoothPeriod = smoothPeriod;
            _prevPhase = phase;

            return alpha;
        }

        /// <summary>
        /// Resets the indicator's state, clearing history and resetting internal values.
        /// </summary>
        public override void Reset()
        {
            _priceHistory.Reset();
            _smoothHistory.Reset();
            _detrendHistory.Reset();
            _inPhaseHistory.Reset();
            _quadratureHistory.Reset();
            _prevPeriod = 0m;
            _prevI2 = 0m;
            _prevQ2 = 0m;
            _prevRe = 0m;
            _prevIm = 0m;
            _prevSmoothPeriod = 0m;
            _prevPhase = 0m;
            _prevMama = 0m;
            Fama.Reset();
            base.Reset();
        }
    }
}
