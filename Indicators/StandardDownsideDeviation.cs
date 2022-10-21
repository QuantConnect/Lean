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
    /// This indicator computes the n-period downside population standard deviation.
    /// </summary>
    public class StandardDownsideDeviation : DownsideVariance
    {
        /// <summary>
        /// Initializes a new instance of the StandardDownsideDeviation class with the specified period.
        ///
        /// Evaluates the standard downside deviation of samples in the look-back period. 
        /// On a data set of size N will use an N normalizer and would thus be biased if applied to a subset.
        /// </summary>
        /// <param name="period">The sample size of the standard downside deviation</param>
        public StandardDownsideDeviation(int period)
            : this($"STDD({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the StandardDownsideDeviation class with the specified name and period.
        /// 
        /// Evaluates the standard downside deviation of samples in the look-back period.
        /// On a data set of size N will use an N normalizer and would thus be biased if applied to a subset.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The sample size of the standard downside deviation</param>
        public StandardDownsideDeviation(string name, int period)
            : base(name, period)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="window">The window for the input history</param>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            return Math.Sqrt((double)base.ComputeNextValue(window, input)).SafeDecimalCast();
        }
    }
}
