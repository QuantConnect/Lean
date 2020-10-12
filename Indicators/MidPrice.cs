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
    /// This indicator computes the MidPrice (MIDPRICE).
    /// The MidPrice is calculated using the following formula:
    /// MIDPRICE = (Highest High + Lowest Low) / 2
    /// </summary>
    public class MidPrice : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly Maximum _maximum;
        private readonly Minimum _minimum;

        /// <summary>
        /// Initializes a new instance of the <see cref="MidPrice"/> class using the specified name and period.
        /// </summary> 
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the MIDPRICE</param>
        public MidPrice(string name, int period) 
            : base(name)
        {
            _period = period;
            _maximum = new Maximum(period);
            _minimum = new Minimum(period);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MidPrice"/> class using the specified period.
        /// </summary> 
        /// <param name="period">The period of the MIDPRICE</param>
        public MidPrice(int period)
            : this($"MIDPRICE({period})", period)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= _period;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            _maximum.Update(new IndicatorDataPoint { Value = input.High });
            _minimum.Update(new IndicatorDataPoint { Value = input.Low });

            return (_maximum.Current.Value + _minimum.Current.Value) / 2;
        }
    }
}