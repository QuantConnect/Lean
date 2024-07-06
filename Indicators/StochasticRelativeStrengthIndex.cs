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
        /// <summary>
        /// Gets the type of moving average
        /// </summary>
        public MovingAverageType MovingAverageType { get; }

        /// <summary>
        /// Gets the %K output
        /// </summary>
        public readonly IndicatorBase<IndicatorDataPoint> k;

        /// <summary>
        /// Gets the %D output
        /// </summary>
        public readonly IndicatorBase<IndicatorDataPoint> d;

        private RelativeStrengthIndex _rsi;
        private readonly RollingWindow<decimal> _recentRSIValues;
        private readonly int _stochPeriod;


        /// <summary>
        /// Initializes a new instance of the StochasticRelativeStrengthIndex class
        /// </summary>
        /// <param name="rsiPeriod">The period of the relative strength index</param>
        /// <param name="stochPeriod">The period of the stochastic indicator</param>
        /// <param name="kSmoothingPeriod">The smoothing period of k output (aka %K)</param>
        /// <param name="dSmoothingPeriod">The smoothing period of d output (aka %D)</param>
        /// <param name="movingAverageType">The type of moving average to be used</param>
        public StochasticRelativeStrengthIndex(int rsiPeriod, int stochPeriod, int kSmoothingPeriod, int dSmoothingPeriod, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : this($"StochRSI({rsiPeriod},{stochPeriod},{kSmoothingPeriod},{dSmoothingPeriod},{movingAverageType})", rsiPeriod, stochPeriod, kSmoothingPeriod, dSmoothingPeriod, movingAverageType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the StochasticRelativeStrengthIndex class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="rsiPeriod">The period of the relative strength index</param>
        /// <param name="stochPeriod">The period of the stochastic indicator</param>
        /// <param name="kSmoothingPeriod">The smoothing period of k output</param>
        /// <param name="dSmoothingPeriod">The smoothing period of d output</param>
        /// <param name="movingAverageType">The type of moving average to be used</param>
        public StochasticRelativeStrengthIndex(string name, int rsiPeriod, int stochPeriod, int kSmoothingPeriod, int dSmoothingPeriod, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : base(name)
        {
            _stochPeriod = stochPeriod;
            _rsi = new RelativeStrengthIndex(rsiPeriod);
            _recentRSIValues = new RollingWindow<decimal>(stochPeriod);

            k = movingAverageType.AsIndicator($"{name}_k_{movingAverageType}", kSmoothingPeriod);
            d = movingAverageType.AsIndicator($"{name}_d_{movingAverageType}", dSmoothingPeriod);

            WarmUpPeriod = stochPeriod;
            MovingAverageType = movingAverageType;
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => true; // MiddleBand.IsReady && UpperBand.IsReady && LowerBand.IsReady && BandWidth.IsReady && PercentB.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Computes the next value of the following sub-indicators from the given state:
        /// StandardDeviation, MiddleBand, UpperBand, LowerBand, BandWidth, %B
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _rsi.Update(input);

            if (Samples < _stochPeriod)
            {
                Console.WriteLine(Samples);
                return 0;
            }

            _recentRSIValues.Add(_rsi.Current.Value);

            var max_high = _recentRSIValues.Max();
            var min_low = _recentRSIValues.Min();

            decimal _k = 100;
            if (max_high != min_low)
                _k = 100 * (_rsi.Current.Value - min_low) / (max_high - min_low);

            k.Update(input.Time, _k);
            d.Update(input.Time, k.Current.Value);

            Console.WriteLine(k.Current.Value);
            return k.Current.Value;
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators
        /// </summary>
        public override void Reset()
        {
            _rsi.Reset();
            _recentRSIValues.Reset();
            k.Reset();
            d.Reset();
            base.Reset();
        }
    }
}
