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
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the n-period downside population variance.
    /// </summary>
    public class DownsideVariance : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Initializes a new instance of the DownsideVariance class with the specified period.
        ///
        /// Evaluates the downside variance of samples in the look-back period. 
        /// On a data set of size N will use an N normalizer and would thus be biased if applied to a subset.
        /// </summary>
        /// <param name="period">The sample size of the downside variance</param>
        public DownsideVariance(int period)
            : this($"VARD({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DownsideVariance class with the specified name and period.
        /// 
        /// Evaluates the downside variance of samples in the look-back period.
        /// On a data set of size N will use an N normalizer and would thus be biased if applied to a subset.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The sample size of the downside variance</param>
        public DownsideVariance(string name, int period)
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
            var negativeValues = window.Where(x => x < 0).Select(x => x.Value).ToList();
            if (!negativeValues.Any())
            {
                return 0m;
            }

            var average = negativeValues.Average();
            var sum = negativeValues.Sum(ret => Math.Pow((double)(ret - average), 2));
            return (sum / negativeValues.Count).SafeDecimalCast();
        }
    }
}
