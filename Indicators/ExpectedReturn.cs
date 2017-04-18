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
    /// This indicator computes the n-period expected return.
    /// </summary>
    public class ExpectedReturn : WindowIndicator<IndicatorDataPoint>
    {   
        private decimal _rollingSum;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpectedReturn"/> class using the specified period.
        /// </summary> 
        /// <param name="period">The period of the indicator</param>
        public ExpectedReturn(string name, int period) 
            :base(name, period)
        {       
        }       

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpectedReturn"/> class using the specified name and period.
        /// </summary> 
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the indicator</param>
        public ExpectedReturn(int period) 
            : base("EXPRET" + period, period)
        {       
        }       

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
            public override bool IsReady
            {   
                    get { return Samples >= Period; }
            }   

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <param name="window">The window for the input history</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {       
            _rollingSum += input.Value;
            if(Samples < Period)
                return 0m;      
            var meanValue = _rollingSum / Period;
            var remvedValue = window[Period -1];
            _rollingSum -= removedValue;
            return meanValue;
        }       
    }   
}
