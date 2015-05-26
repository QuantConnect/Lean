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
    /// Computes an Inverse Fisher Transform also known as The hyperbolic tangent activation function (TANH)
    /// 
    /// The transform takes a window of values and gets the minimum and maximum values for the window. The new
    /// input values is then located within a range of -1 to +1 according to the formula:
    /// 
    ///     f(x) = (e to the power of 2x -1) / (e to the power of 2x +1)
    /// 
    /// for the math function see http://www.heatonresearch.com/wiki/Hyperbolic_Tangent_Activation_Function
    ///  
    /// It is useful because it normalizes the current input, such as price, to fit within the range of the 
    /// High and Low of the data window.
    /// 
    /// For a discussion of how it is used in trading to clarify other indicators turning points
    /// see http://www.tradingsystemlab.com/files/The%20Inverse%20Fisher%20Transform.pdf
    /// 
    /// The formula comes from one of the activation functions of a neural network 
    /// 
    /// </summary>
    public class InverseFisherTransform : WindowIndicator<IndicatorDataPoint>
    {

        private readonly Minimum _minLow;
        private readonly Maximum _maxHigh;
        private RollingWindow<IndicatorDataPoint> value1;
        


        /// <summary>
        /// An Inverse Fisher Transform of Prices
        /// </summary>
        /// <param name="name">string - the name of the indicator</param>
        /// <param name="period">The number of periods for the indicator window</param>
        public InverseFisherTransform(string name, int period)
            : base(name, period)
        {
            // Initialize the local variables
            value1 = new RollingWindow<IndicatorDataPoint>(period);

            // add two minimum values to the value1 to get things started
            value1.Add(new IndicatorDataPoint(DateTime.MinValue, .0001m));
            value1.Add(new IndicatorDataPoint(DateTime.MinValue, .0001m));

            // Initialize the local variables
            _maxHigh = new Maximum("MaxHigh", period);
            _minLow = new Minimum("MinLow", period);
        }

        /// <summary>
        ///     Initializes a new instance of the FisherTransform class with the default name and period
        /// </summary>
        /// <param name="period">The period of the WMA</param>
        public InverseFisherTransform(int period)
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
            //value1.Add(input);
            //value2.Update(input);

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

                var ifish = (Math.Exp(2 * (double)value1[0].Value) - 1) / (Math.Exp(2 * (double)value1[0].Value) + 1);
                Current = new IndicatorDataPoint(input.Time, (decimal)ifish);
            }



            return this.Current;
        }
    }
}
