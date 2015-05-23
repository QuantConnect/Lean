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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Ehlers Relative Vigor Index to be used when the symbol is in Cycle mode.  
    /// The oscillator is basiclly in phase with the cyclic component fo the market prices.
    /// 
    /// It is also possible to generate a leading function if the summation window
    /// is less than half wavelength of the dominant cycle.
    /// 
    /// If the dominant cycle period is not available, you can sum the RVI components
    /// over a fixed default period.  A period of 8 or 10 is suggested because most 
    /// cycles of interest are 16 or 20 bars long.
    /// 
    /// A crossing of the indicator with the indicator with a lag of 1 bar is a good way 
    /// to create an unambiguous signal.
    /// </summary>
    public class RelativeVigorIndex : TradeBarIndicator
    {
        private RollingWindow<IndicatorDataPoint> value1;
        private RollingWindow<IndicatorDataPoint> value2;
        /// <summary>
        /// 
        /// </summary>
        public RollingWindow<IndicatorDataPoint> RviWindow { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public RollingWindow<TradeBar> Bars { get; set; }

        /// <summary>
        /// Creates the indicator
        /// </summary>
        /// <param name="name">string - the name of the indicator</param>
        /// <param name="period">int - half the number of periods in the dominant market cycle. </param>
        public RelativeVigorIndex(string name, int period)
            : base(name)
        {

            value1 = new RollingWindow<IndicatorDataPoint>(period);
            value2 = new RollingWindow<IndicatorDataPoint>(period);
            RviWindow = new RollingWindow<IndicatorDataPoint>(period);
            Bars = new RollingWindow<TradeBar>(period);
        }


        /// <summary>
        /// Automatically names the indicator
        /// </summary>
        /// <param name="period">int - the length of computation cycle</param>
        public RelativeVigorIndex(int period)
            : this("RVI_" + period, period)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return value1.IsReady && value2.IsReady && RviWindow.IsReady; }
        }
        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            // Save the bar for summing
            Bars.Add(input);

            // check if it is not ready add a 0 to the RviWindow
            if (!Bars.IsReady)
            {
                RviWindow.Add(new IndicatorDataPoint(input.EndTime, 0.0m));
            }
            else
            {
                // Bars have been added, so Bars[0] is the current value of the input


                // Filter the Close - Open with a four-bar symetical FIR filter before the terms are summed.
                var v1 = ((Bars[0].Close - Bars[0].Open) + 2 * (Bars[1].Close - Bars[1].Open) +
                          2 * (Bars[2].Close - Bars[3].Open) + (Bars[3].Close - Bars[3].Open) / 6);
                value1.Add(new IndicatorDataPoint(input.EndTime, v1));

                // Filter the High - Low with a four-bar symetical FIR filter before the terms are summed.
                var v2 = ((Bars[0].High - Bars[0].Low) + 2 * (Bars[1].High - Bars[1].Low) +
                          2 * (Bars[2].High - Bars[3].Low) + (Bars[3].High - Bars[3].Low) / 6);
                value2.Add(new IndicatorDataPoint(input.EndTime, v2));

                // The numerator and denominator are summed independently
                decimal num = value1.Sum(point => point.Value);
                decimal denom = value2.Sum(point => point.Value);

                try
                {
                    // The RVI is the ratio of the numerator to the denominator.  Since
                    //  they are lagged by the same amount, due to the filtering,
                    //  the lag is removed by taking the ratio.
                    RviWindow.Add(new IndicatorDataPoint(input.EndTime, num / denom));
                }
                catch (DivideByZeroException zex)
                {
                    throw new Exception(zex.Message + zex.StackTrace);
                }
            }
            return RviWindow[0].Value;
        }
    }
}
