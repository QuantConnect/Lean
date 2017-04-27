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
using System.Collections.Generic;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Produces a Hull Moving Average as explained at http://www.alanhull.com/hull-moving-average/
    /// and derived from the instructions for the Excel VBA code at http://finance4traders.blogspot.com/2009/06/how-to-calculate-hull-moving-average.html
    /// </summary>
    public class HullMovingAverage : WindowIndicator<IndicatorDataPoint>
    {
        private readonly LinearWeightedMovingAverage _longWma;
        private readonly LinearWeightedMovingAverage _shortWma;
        private readonly RollingWindow<IndicatorDataPoint> _smooth;
        private readonly LinearWeightedMovingAverage _result;
        // The length of the smoothed window
        // square root of period rounded to the nearest whole number 

        /// <summary>
        /// A Hull Moving Average 
        /// </summary>
        /// <param name="name">string - a name for the indicator</param>
        /// <param name="period">int - the number of periods over which to calculate the HMA - the length of the longWMA</param>
        public HullMovingAverage(string name, int period)
            : base(name, period)
        {
            // Creates the long LWMA for the number of periods specified in the constuctor
            _longWma = new LinearWeightedMovingAverage("Long", period);

            // Creates the short LWMA for half the period rounded to the nearest whole number
            _shortWma = new LinearWeightedMovingAverage("Short", period / 2);

            // Creates the smoother data set to which the resulting wma is applied
            _smooth = new RollingWindow<IndicatorDataPoint>(period);

            // number of historical periods to look at in the resulting WMA
            int k = System.Convert.ToInt32(Math.Sqrt(period));

            // Creates the LWMA for the output. This step probably could have been skipped
            
            _result = new LinearWeightedMovingAverage("Result", k);

        }
        /// <summary>
        /// A Hull Moving Average with the default name
        /// </summary>
        /// <param name="period">int - the number of periods over which to calculate the HMA - the length of the longWMA</param>
        public HullMovingAverage(int period)
            : this("HMA" + period, period)
        {
        }

        /// <summary>
        ///     Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return _smooth.IsReady; }
        }

        /// <summary>
        ///     Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>        /// <returns></returns>
        /// <remarks>
        /// The Hull moving average is a series of nested weighted moving averages. 
        /// Using the LWMA custom function for calculating weighted moving averages, 
        /// the Hull moving average can be calculated following the steps. 
        ///
        ///1.Calculate the n periodweighted moving average of a series "=WMA(price for n periods)"
        ///2.Calculate the n/2 period weighted moving average of a series"=WMA(price for n/2 periods)". Round n/2 to the nearest whole number
        ///3.Create a time series with 2*WMA from Step 2 - WMA from Step 1
        ///4.The HMA is the WMA of the series in Step 3. "=WMA(Step 3 outputs fo k period)"
        /// </remarks>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            _longWma.Update(input);
            _shortWma.Update(input);
            _smooth.Add(new IndicatorDataPoint(input.Time, 2 * _shortWma.Current.Value - _longWma.Current.Value));
            _result.Update(new IndicatorDataPoint(input.Time, _smooth[0].Value));
            return _result.Current.Value;
        }
    }
}
