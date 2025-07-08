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
    /// This indicator computes the True Strength Index (TSI).
    /// The True Strength Index is calculated as explained here:
    /// https://school.stockcharts.com/doku.php?id=technical_indicators:true_strength_index
    ///
    /// Briefly, the calculation has three steps:
    ///   1. Smooth the momentum and the absolute momentum by getting an EMA of them (typically of period 25)
    ///   2. Double smooth the momentum and the absolute momentum by getting an EMA of their EMA (typically of period 13)
    ///   3. The TSI formula itself: divide the double-smoothed momentum over the double-smoothed absolute momentum and multiply by 100
    ///
    /// The signal is typically a 7-to-12-EMA of the TSI.
    /// </summary>
    public class TrueStrengthIndex : Indicator, IIndicatorWarmUpPeriodProvider
    {
        private decimal _prevClose;

        private readonly ExponentialMovingAverage _priceChangeEma;

        private readonly ExponentialMovingAverage _priceChangeEmaEma;

        private readonly ExponentialMovingAverage _absPriceChangeEma;

        private readonly ExponentialMovingAverage _absPriceChangeEmaEma;

        private readonly IndicatorBase<IndicatorDataPoint> _tsi;

        /// <summary>
        /// Gets the signal line for the TSI indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Signal { get; }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= WarmUpPeriod;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrueStrengthIndex"/> class using the specified short and long term smoothing periods, and the signal period and type.
        /// </summary>
        /// <param name="shortTermPeriod">Period used for the first price change smoothing</param>
        /// <param name="longTermPeriod">Period used for the second (double) price change smoothing</param>
        /// <param name="signalPeriod">The signal period</param>
        /// <param name="signalType">The type of moving average to use for the signal</param>
        public TrueStrengthIndex(int longTermPeriod = 25, int shortTermPeriod = 13, int signalPeriod = 7, MovingAverageType signalType = MovingAverageType.Exponential)
            : this($"TSI({longTermPeriod},{shortTermPeriod},{signalPeriod})", longTermPeriod, shortTermPeriod, signalPeriod, signalType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrueStrengthIndex"/> class using the specified name, the short and long term smoothing periods, and the signal period and type.
        /// </summary>
        /// <param name="name">The name of the indicator</param>
        /// <param name="shortTermPeriod">Period used for the first price change smoothing</param>
        /// <param name="longTermPeriod">Period used for the second (double) price change smoothing</param>
        /// <param name="signalPeriod">The signal period</param>
        /// <param name="signalType">The type of moving average to use for the signal</param>
        public TrueStrengthIndex(string name, int longTermPeriod = 25, int shortTermPeriod = 13, int signalPeriod = 7, MovingAverageType signalType = MovingAverageType.Exponential)
            : base(name)
        {
            _priceChangeEma = new ExponentialMovingAverage(name + "_PC_EMA", longTermPeriod);
            _absPriceChangeEma = new ExponentialMovingAverage(name + "_APC_EMA", longTermPeriod);
            _priceChangeEmaEma = new ExponentialMovingAverage(name + "_PC_EMA_EMA", shortTermPeriod).Of(_priceChangeEma, true);
            _absPriceChangeEmaEma = new ExponentialMovingAverage(name + "_APC_EMA_EMA", shortTermPeriod).Of(_absPriceChangeEma, true);
            _tsi = _priceChangeEmaEma.Over(_absPriceChangeEmaEma).Times(100m);
            Signal = signalType.AsIndicator(name + "_Signal", signalPeriod).Of(_tsi, true);
            WarmUpPeriod = longTermPeriod + shortTermPeriod;
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            if (Samples == 1)
            {
                _prevClose = input.Price;
                return 0m;
            }

            var priceChange = input.Price - _prevClose;
            _prevClose = input.Price;
            _priceChangeEma.Update(input.EndTime, priceChange);
            _absPriceChangeEma.Update(input.EndTime, Math.Abs(priceChange));

            return _tsi.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _prevClose = 0;
            _priceChangeEma.Reset();
            _priceChangeEmaEma.Reset();
            _absPriceChangeEma.Reset();
            _absPriceChangeEmaEma.Reset();
            _tsi.Reset();
            Signal.Reset();
            base.Reset();
        }
    }
}
