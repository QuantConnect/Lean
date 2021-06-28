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
    /// The Augen Price Spike indicator is an indicator that measures price 
    /// changes in terms of standard deviations. In the book, The 
    /// Volatility Edge in Options Trading, Jeff Augen describes a 
    /// method for tracking absolute price changes in terms of recent 
    /// volatility, using the standard deviation.
    /// 
    /// length = x
    /// sd = StandardDeviation(x)
    /// m = sd.Current.Value * previousClose
    /// spike = (currentPrice - previousClose) / m
    /// </summary>
    public class AugenPriceSpike : Indicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly StandardDeviation _sd;
        private decimal _previousValue;

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _sd.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _sd.WarmUpPeriod;

        /// <summary>
        /// Initializeds a new instance of the AugenPriceSpike class using the specified period
        /// </summary>
        /// <param name="period">The period over which to perform to computation</param>
        public AugenPriceSpike(int period = 2)
            : this($"APS({period})", period)
        {
        }
        /// <summary>
        /// Creates a new AugenPriceSpike indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        public AugenPriceSpike(string name, int period)
            : base(name)
        {
            _sd = new StandardDeviation(period);
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A a value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            if (_previousValue == 0) _previousValue = input.Value;

            _sd.Update(input.Time, input.Value);
            if (!_sd.IsReady)
            {
                _previousValue = input.Value;
                return 0;
            }

            var m = _sd.Current.Value * _previousValue;
            var val = input.Value;
            if (m == 0)
            {
                _previousValue = input.Value;
                return 0;
            }

            var spikeValue = (input.Value - _previousValue) / m;
            _previousValue = input.Value;
            return spikeValue;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _sd.Reset();
            _previousValue = 0.0m;
            base.Reset();
        }
    }
}
