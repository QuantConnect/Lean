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
    /// </summary>
    public class TrueStrengthIndex : Indicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly decimal _period;

        private decimal _prevClose;

        private readonly ExponentialMovingAverage _pcEma;

        private readonly ExponentialMovingAverage _pcEmaEma;

        private readonly ExponentialMovingAverage _apcEma;

        private readonly ExponentialMovingAverage _apcEmaEma;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrueStrengthIndex"/> class using the specified short and long term smoothing periods.
        /// </summary>
        /// <param name="shortTermPeriod">Period used for the first price change smoothing</param>
        /// <param name="longTermPeriod">Period used for the second (double) price change smoothing</param>
        public TrueStrengthIndex(int longTermPeriod = 25, int shortTermPeriod = 13)
            : this($"TSI({longTermPeriod},{shortTermPeriod})", longTermPeriod, shortTermPeriod)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrueStrengthIndex"/> class using the specified name and short and long term smoothing periods.
        /// </summary>
        /// <param name="name">The name of the indicator</param>
        /// <param name="shortTermPeriod">Period used for the first price change smoothing</param>
        /// <param name="longTermPeriod">Period used for the second (double) price change smoothing</param>
        public TrueStrengthIndex(string name, int longTermPeriod = 25, int shortTermPeriod = 13)
            : base(name)
        {
            _pcEma = new ExponentialMovingAverage(name + "_PC_EMA", longTermPeriod);
            _pcEmaEma = new ExponentialMovingAverage(name + "_PC_EMA_EMA", shortTermPeriod);
            _apcEma = new ExponentialMovingAverage(name + "_APC_EMA", longTermPeriod);
            _apcEmaEma = new ExponentialMovingAverage(name + "_APC_EMA_EMA", shortTermPeriod);
            _period = WarmUpPeriod = longTermPeriod + shortTermPeriod;
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= _period;

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

            decimal pc = input.Price - _prevClose;
            _prevClose = input.Price;

            _pcEma.Update(input.Time, pc);
            _apcEma.Update(input.Time, Math.Abs(pc));

            if (_pcEma.IsReady && _apcEma.IsReady) {
                _pcEmaEma.Update(_pcEma.Current);
                _apcEmaEma.Update(_apcEma.Current);
            }

            return !IsReady ||_apcEmaEma.Current.Value == 0m
                ? Current.Value
                : (_pcEmaEma.Current.Value / _apcEmaEma.Current.Value) * 100m;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _prevClose = 0;
            _pcEma.Reset();
            _pcEmaEma.Reset();
            _apcEma.Reset();
            _apcEmaEma.Reset();
            base.Reset();
        }
    }
}
