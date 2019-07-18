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
    /// This indicator computes the T3 Moving Average (T3).
    /// The T3 Moving Average is calculated with the following formula:
    /// EMA1(x, Period) = EMA(x, Period)
    /// EMA2(x, Period) = EMA(EMA1(x, Period),Period)
    /// GD(x, Period, volumeFactor) = (EMA1(x, Period)*(1+volumeFactor)) - (EMA2(x, Period)* volumeFactor)
    /// T3 = GD(GD(GD(t, Period, volumeFactor), Period, volumeFactor), Period, volumeFactor);
    /// </summary>
    public class T3MovingAverage : IndicatorBase<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly DoubleExponentialMovingAverage _gd1;
        private readonly DoubleExponentialMovingAverage _gd2;
        private readonly DoubleExponentialMovingAverage _gd3;

        /// <summary>
        /// Initializes a new instance of the <see cref="T3MovingAverage"/> class using the specified name and period.
        /// </summary> 
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the T3MovingAverage</param>
        /// <param name="volumeFactor">The volume factor of the T3MovingAverage (value must be in the [0,1] range, defaults to 0.7)</param>
        public T3MovingAverage(string name, int period, decimal volumeFactor = 0.7m) 
            : base(name)
        {
            _period = period;
            _gd1 = new DoubleExponentialMovingAverage(name + "_1", period, volumeFactor);
            _gd2 = new DoubleExponentialMovingAverage(name + "_2", period, volumeFactor);
            _gd3 = new DoubleExponentialMovingAverage(name + "_3", period, volumeFactor);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T3MovingAverage"/> class using the specified period.
        /// </summary> 
        /// <param name="period">The period of the T3MovingAverage</param>
        /// <param name="volumeFactor">The volume factor of the T3MovingAverage (value must be in the [0,1] range, defaults to 0.7)</param>
        public T3MovingAverage(int period, decimal volumeFactor = 0.7m)
            : this($"T3({period},{volumeFactor})", period, volumeFactor)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples > 6 * (_period - 1);

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => 1 + 6 * (_period - 1);

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _gd1.Update(input);

            if (!_gd1.IsReady)
                return _gd1;

            _gd2.Update(_gd1.Current);

            if (!_gd2.IsReady)
                return _gd2;

            _gd3.Update(_gd2.Current);

            return _gd3;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _gd1.Reset();
            _gd2.Reset();
            _gd3.Reset();
            base.Reset();
        }
    }
}