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
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Parabolic SAR Indicator 
    /// Based on TA-Lib implementation
    /// </summary>
    public class ParabolicStopAndReverse : ParabolicStopAndReverseExtended, IIndicatorWarmUpPeriodProvider
    {

        /// Create new Parabolic SAR
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="afStart">Acceleration factor start value</param>
        /// <param name="afIncrement">Acceleration factor increment value</param>
        /// <param name="afMax">Acceleration factor max value</param>
        public ParabolicStopAndReverse(string name, decimal afStart = 0.02m, decimal afIncrement = 0.02m, decimal afMax = 0.2m)
            : base(name, 0.0m, 0.0m, afStart, afIncrement, afMax, afStart, afIncrement, afMax)
        {
        }

        /// <summary>
        /// Create new Parabolic SAR
        /// </summary>
        /// <param name="afStart">Acceleration factor start value</param>
        /// <param name="afIncrement">Acceleration factor increment value</param>
        /// <param name="afMax">Acceleration factor max value</param>
        public ParabolicStopAndReverse(decimal afStart = 0.02m, decimal afIncrement = 0.02m, decimal afMax = 0.2m)
            : this($"PSAR({afStart},{afIncrement},{afMax})", afStart, afIncrement, afMax)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The trade bar input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            // PSAR is special case of SAR except it always positive
            return Math.Abs(base.ComputeNextValue(input));

        }
    }
}