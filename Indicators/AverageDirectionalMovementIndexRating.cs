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

using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the Average Directional Movement Index Rating (ADXR). 
    /// The Average Directional Movement Index Rating is calculated with the following formula:
    /// ADXR[i] = (ADX[i] + ADX[i - period + 1]) / 2
    /// </summary>
    public class AverageDirectionalMovementIndexRating : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly AverageDirectionalIndex _adx;
        private readonly RollingWindow<decimal> _adxHistory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AverageDirectionalMovementIndexRating"/> class using the specified name and period.
        /// </summary> 
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the ADXR</param>
        public AverageDirectionalMovementIndexRating(string name, int period) 
            : base(name)
        {
            _period = period;
            _adx = new AverageDirectionalIndex(name + "_ADX", period);
            _adxHistory = new RollingWindow<decimal>(period);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AverageDirectionalMovementIndexRating"/> class using the specified period.
        /// </summary> 
        /// <param name="period">The period of the ADXR</param>
        public AverageDirectionalMovementIndexRating(int period)
            : this($"ADXR({period})", period)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _adxHistory.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period * 3 - 1;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            _adx.Update(input);

            if (_adx.IsReady)
            {
                _adxHistory.Add(_adx);
            }

            return IsReady ? (_adx + _adxHistory[_period - 1]) / 2 : 50m;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _adx.Reset();
            _adxHistory.Reset();
            base.Reset();
        }
    }
}