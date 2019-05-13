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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the Double Exponential Moving Average (DEMA).
    /// The Double Exponential Moving Average is calculated with the following formula:
    /// EMA2 = EMA(EMA(t,period),period)
    /// DEMA = 2 * EMA(t,period) - EMA2
    /// The Generalized DEMA (GD) is calculated with the following formula:
    /// GD = (volumeFactor+1) * EMA(t,period) - volumeFactor * EMA2
    /// </summary>
    public class DoubleExponentialMovingAverage : IndicatorBase<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly decimal _volumeFactor;
        private readonly ExponentialMovingAverage _ema1;
        private readonly ExponentialMovingAverage _ema2;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleExponentialMovingAverage"/> class using the specified name and period.
        /// </summary> 
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the DEMA</param>
        /// <param name="volumeFactor">The volume factor of the DEMA (value must be in the [0,1] range, set to 1 for standard DEMA)</param>
        public DoubleExponentialMovingAverage(string name, int period, decimal volumeFactor = 1m)
            : base(name)
        {
            _period = period;
            _volumeFactor = volumeFactor;
            _ema1 = new ExponentialMovingAverage(name + "_1", period);
            _ema2 = new ExponentialMovingAverage(name + "_2", period);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleExponentialMovingAverage"/> class using the specified period.
        /// </summary> 
        /// <param name="period">The period of the DEMA</param>
        /// <param name="volumeFactor">The volume factor of the DEMA (value must be in the [0,1] range, set to 1 for standard DEMA)</param>
        public DoubleExponentialMovingAverage(int period, decimal volumeFactor = 1m)
            : this($"DEMA({period})", period, volumeFactor)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples > 2 * (_period - 1);

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => 1 + 2 * (_period - 1);

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _ema1.Update(input);

            if (!_ema1.IsReady)
                return _ema1;

            _ema2.Update(_ema1.Current);

            return (_volumeFactor + 1) * _ema1 - _volumeFactor * _ema2;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _ema1.Reset();
            _ema2.Reset();
            base.Reset();
        }
    }
}