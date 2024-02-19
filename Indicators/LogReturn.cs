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
    /// Represents the LogReturn indicator (LOGR)
    /// - log returns are useful for identifying price convergence/divergence in a given period
    /// - logr = log (current price / last price in period)
    /// </summary>
    public class LogReturn : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => Period;

        /// <summary>
        /// Initializes a new instance of the LogReturn class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the LOGR</param>
        public LogReturn(string name, int period)
            : base(name, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the LogReturn class with the default name and period
        /// </summary>
        /// <param name="period">The period of the SMA</param>
        public LogReturn(int period)
            : base($"LOGR({period})", period)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// - logr = log (current price / last price in period)
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            var valuef = input;

            var value0 = window.Samples <= window.Size
                ? window[window.Count - 1]
                : window.MostRecentlyRemoved;
            var result = Math.Log((double)(valuef.Value.SafeDivision(value0.Value)));
            if (result == Double.NegativeInfinity || result == Double.PositiveInfinity)
            {
                return 0;
            }

            return result.SafeDecimalCast();
        }
    }
}