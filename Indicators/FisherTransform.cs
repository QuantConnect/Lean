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
 *
*/
using System;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Fisher transform is a mathematical process which is used to convert any data set to a modified data set
    /// whose Probabilty Distrbution Function is approximately Gaussian.  Once the Fisher transform is computed,
    /// the transformed data can then be analyzed in terms of it's deviation from the mean.
    /// 
    /// The equation is y = .05 * ln [ 1 + x / 1 -x ] where
    /// x is the input
    /// y is the output
    /// ln is the natural logarithm
    /// 
    /// The Fisher transform has much sharper turning points than other indicators such as MACD
    /// 
    /// For more info, read chapter 1 of Cybernetic Analysis for Stocks and Futures by John F. Ehlers
    /// </summary>
    public class FisherTransform : WindowIndicator<IndicatorDataPoint>
    {

        private readonly Minimum _minLow;
        private readonly Maximum _maxHigh;
        private RollingWindow<IndicatorDataPoint> value1;

        /// <summary>
        /// A Fisher Transform of Prices
        /// </summary>
        /// <param name="name">string - the name of the indicator</param>
        /// <param name="period">The number of periods for the indicator</param>
        public FisherTransform(string name, int period)
            : base(name, period)
        {
            // Initialize the local variables
            _maxHigh = new Maximum("MaxHigh", period);
            _minLow = new Minimum("MinLow", period);
            value1 = new RollingWindow<IndicatorDataPoint>(period);

            // add two minimum values to the value1 to get things started
            value1.Add(new IndicatorDataPoint(DateTime.MinValue, .0001m));
            value1.Add(new IndicatorDataPoint(DateTime.MinValue, .0001m));

        }

        /// <summary>
        ///     Initializes a new instance of the FisherTransform class with the default name and period
        /// </summary>
        /// <param name="period">The period of the WMA</param>
        public FisherTransform(int period)
            : this("Fish_" + period, period)
        {
        }
        /// <summary>
        /// Computes the next value in the transform. 
        /// value1 is a function used to normalize price withing the last _period day range.
        /// value1 is centered on its midpoint and then doubled so that value1 wil swing between -1 and +1.  
        /// value1 is also smoothed with an exponential moving average whose alpha is 0.33.  
        /// 
        /// Since the smoothing may allow value1 to exceed the _period day price range, limits are introduced to 
        /// preclude the transform from blowing up by having an input larger than unity.
        /// </summary>
        /// <param name="window">The IReadOnlyWindow of Indicator Data Points for the history of this indicator</param>
        /// <param name="input">IndicatorDataPoint - the time and value of the next price</param>
        /// <returns></returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            _maxHigh.Update(input);
            _minLow.Update(input);
            if (IsReady)
            {
                // get some local variables
                var price = input.Value;
                var minL = _minLow.Current.Value;
                var maxH = _maxHigh.Current.Value;

                // get the value1 from the last time this function was called
                var v1 = value1[0].Value;

                // compute the EMA of the price and the last value1
                value1.Add(new IndicatorDataPoint(input.Time, .33m * 2m * ((price - minL) / (maxH - minL) - .5m) + .67m * v1));

                // limit the new value1 so that it falls within positive or negative unity
                if (value1[0].Value > .9999m)
                    value1[0].Value = .9999m;
                if (value1[0].Value < -.9999m)
                    value1[0].Value = -.9999m;
                var current = Current;

                // calcuate the Fisher transform according the the formula above and from Ehlers
                //  Math.Log takes and produces doubles, so the result is converted back to Decimal
                // The calculation uses the Current.Value from the last time the function was called,
                //  so an intermediate variable is introduced so that it can be used in the 
                //  calculation before the result is assigned to the Current.Value after the calculation is made.
                var fishx = Convert.ToDecimal(.5 * Math.Log((1.0 + (double)value1[0].Value) / (1.0 - (double)value1[0].Value)));
                Current.Value = fishx;
            }
            return this.Current;
        }
    }
}
