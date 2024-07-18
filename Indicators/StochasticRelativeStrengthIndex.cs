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
    /// Stochastic RSI, or simply StochRSI, is a technical analysis indicator used to determine whether 
    /// an asset is overbought or oversold, as well as to identify current market trends.
    /// As the name suggests, the StochRSI is a derivative of the standard Relative Strength Index (RSI) and, 
    /// as such, is considered an indicator of an indicator.
    /// It is a type of oscillator, meaning that it fluctuates above and below a center line.
    /// </summary>
    public class StochasticRelativeStrengthIndex : Indicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly RelativeStrengthIndex _rsi;
        private readonly RollingWindow<decimal> _recentRSIValues;

        /// <summary>
        /// Gets the %K output
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> K { get; }

        /// <summary>
        /// Gets the %D output
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> D { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= WarmUpPeriod;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Initializes a new instance of the StochasticRelativeStrengthIndex class
        /// </summary>
        /// <param name="rsiPeriod">The period of the relative strength index</param>
        /// <param name="stochPeriod">The period of the stochastic indicator</param>
        /// <param name="kSmoothingPeriod">The smoothing period of K output (aka %K)</param>
        /// <param name="dSmoothingPeriod">The smoothing period of D output (aka %D)</param>
        /// <param name="movingAverageType">The type of moving average to be used for k and d</param>
        public StochasticRelativeStrengthIndex(int rsiPeriod, int stochPeriod, int kSmoothingPeriod, int dSmoothingPeriod, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : this($"SRSI({rsiPeriod},{stochPeriod},{kSmoothingPeriod},{dSmoothingPeriod},{movingAverageType})", rsiPeriod, stochPeriod, kSmoothingPeriod, dSmoothingPeriod, movingAverageType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the StochasticRelativeStrengthIndex class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="rsiPeriod">The period of the relative strength index</param>
        /// <param name="stochPeriod">The period of the stochastic indicator</param>
        /// <param name="kSmoothingPeriod">The smoothing period of K output</param>
        /// <param name="dSmoothingPeriod">The smoothing period of D output</param>
        /// <param name="movingAverageType">The type of moving average to be used</param>
        public StochasticRelativeStrengthIndex(string name, int rsiPeriod, int stochPeriod, int kSmoothingPeriod, int dSmoothingPeriod, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : base(name)
        {
            _rsi = new RelativeStrengthIndex(rsiPeriod);
            _recentRSIValues = new RollingWindow<decimal>(stochPeriod);

            K = movingAverageType.AsIndicator($"{name}_K_{movingAverageType}", kSmoothingPeriod);
            D = movingAverageType.AsIndicator($"{name}_D_{movingAverageType}", dSmoothingPeriod);

            WarmUpPeriod = rsiPeriod + stochPeriod + Math.Max(kSmoothingPeriod, dSmoothingPeriod);
        }

        /// <summary>
        /// Computes the next value of the following sub-indicators from the given state:
        /// K (%K) and D (%D)
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _rsi.Update(input);
            _recentRSIValues.Add(_rsi.Current.Value);

            if (!_recentRSIValues.IsReady)
            {
                return 0m;
            }

            var maxHigh = _recentRSIValues.Max();
            var minLow = _recentRSIValues.Min();

            decimal k = 100;
            if (maxHigh != minLow) {
                k = 100 * (_rsi.Current.Value - minLow) / (maxHigh - minLow);
            }

            K.Update(input.EndTime, k);
            D.Update(input.EndTime, K.Current.Value);

            return input.Value;
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators
        /// </summary>
        public override void Reset()
        {
            _rsi.Reset();
            _recentRSIValues.Reset();
            K.Reset();
            D.Reset();
            base.Reset();
        }
    }
}
