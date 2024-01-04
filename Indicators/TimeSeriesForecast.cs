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
    /// Represents an indicator capable of predicting new values given previous data from a window.
    /// Source: https://tulipindicators.org/tsf
    /// </summary>
    public class TimeSeriesForecast : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Creates a new TimeSeriesForecast indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period over which to look back</param>
        public TimeSeriesForecast(string name, int period)
            : base(name, period)
        {
            if (period < 2)
            {
                throw new ArgumentException(Messages.RollingWindow.InvalidSize, nameof(period));
            }
        }

        /// <summary>
        /// Creates a new TimeSeriesForecast indicator with the specified period
        /// </summary>
        /// <param name="period">The period over which to look back</param>
        public TimeSeriesForecast(int period)
            : this($"TSF{period})", period)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            if (!IsReady)
            {
                return 0;
            }
            
            // calculations are derived from https://tulipindicators.org/tsf
            decimal x1 = 0;
            decimal x2 = 0;
            decimal xy = 0;
            decimal y = 0;

            var i = Period - 1;
            for (; i > 0; i--)
            {
                x1 += i;
                x2 += i * i;
                xy += window[i].Value * (Period - i);
                y += window[i].Value;
            }

            x1 += Period;
            x2 += Period * Period;

            xy += window[0].Value * Period;
            y += window[0].Value;

            var bd = 1 / (Period * x2 - x1 * x1);
            var b = (Period * xy - x1 * y) * bd;
            var a = (y - b * x1) * (1m / Period);

            return a + b * (Period + 1);
        }
    }
}
