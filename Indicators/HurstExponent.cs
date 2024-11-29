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
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Represents the Hurst Exponent indicator, which is used to measure the long-term memory of a time series.
    /// </summary>
    public class HurstExponent : Indicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// A rolling window that holds the most recent price values.
        /// </summary>
        private readonly RollingWindow<decimal> _priceWindow;

        /// <summary>
        /// The list of time lags used to calculate tau values.
        /// </summary>
        private List<int> timeLags;

        /// <summary>
        /// The list of the logarithms of the time lags, used for the regression line calculation.
        /// </summary>
        private List<decimal> logTimeLags;

        /// <summary>
        /// Initializes a new instance of the <see cref="HurstExponent"/> class.
        /// The default maxLag value of 20 is chosen for reliable and accurate results, but using a higher lag may reduce precision.
        /// </summary>
        /// <param name="name">The name of the indicator.</param>
        /// <param name="lookbackPeriod">The lookbackPeriod over which to calculate the Hurst Exponent.</param>
        /// <param name="maxLag">The maximum lag to consider for time series analysis.</param>
        public HurstExponent(string name, int lookbackPeriod, int maxLag = 20) : base(name)
        {
            _priceWindow = new RollingWindow<decimal>(lookbackPeriod);
            timeLags = Enumerable.Range(2, maxLag - 2).ToList();
            logTimeLags = timeLags.Select(x => (decimal)Math.Log(x)).ToList();
            WarmUpPeriod = lookbackPeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HurstExponent"/> class with the specified lookbackPeriod and maxLag.
        /// The default maxLag value of 20 is chosen for reliable and accurate results, but using a higher lag may reduce precision.
        /// </summary>
        /// <param name="lookbackPeriod">The lookbackPeriod over which to calculate the Hurst Exponent.</param>
        /// <param name="maxLag">The maximum lag to consider for time series analysis.</param>
        public HurstExponent(int lookbackPeriod, int maxLag = 20)
            : this($"HE({lookbackPeriod},{maxLag})", lookbackPeriod, maxLag)
        {
        }

        /// <summary>
        /// Gets the lookbackPeriod over which the indicator is calculated.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Indicates whether the indicator has enough data to produce a valid result.
        /// </summary>
        public override bool IsReady => _priceWindow.IsReady;

        /// <summary>
        /// Computes the next value of the Hurst Exponent indicator.
        /// </summary>
        /// <param name="input">The input data point to use for the next value computation.</param>
        /// <returns>The computed Hurst Exponent value, or zero if insufficient data is available.</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _priceWindow.Add(input.Value);
            if (!_priceWindow.IsReady)
            {
                return decimal.Zero;
            }

            // List to store the log of tau values for each time lag
            var logTauValues = new List<decimal>();
            foreach (var lag in timeLags)
            {
                var sub = new List<decimal>();
                // Calculate the differences between values separated by the given lag
                for (int i = _priceWindow.Size - 1 - lag; i >= 0; i--)
                {
                    var value = _priceWindow[i] - _priceWindow[i + lag];
                    sub.Add(value);
                }
                var standardDeviation = 0.0;
                // Ensure sub is not empty to avoid division by zero.
                if (sub.Count > 0)
                {
                    standardDeviation = ComputeStandardDeviation(sub);
                }
                logTauValues.Add(standardDeviation == 0.0 ? 0m : (decimal)Math.Log(standardDeviation));
            }

            // Calculate the Hurst Exponent as the slope of the log-log plot
            var hurstExponent = ComputeSlope(logTimeLags, logTauValues);
            if (IsReady)
            {
                return hurstExponent;
            }
            return decimal.Zero;
        }

        /// <summary>
        /// Calculates the standard deviation of a list of decimal values.
        /// </summary>
        /// <param name="values">The list of values to calculate the standard deviation for.</param>
        /// <returns>The standard deviation of the given values.</returns>
        private double ComputeStandardDeviation(IEnumerable<decimal> values)
        {
            var avg = values.Average();
            var variance = values.Sum(x => (x - avg) * (x - avg)) / values.Count();
            return Math.Sqrt((double)variance);
        }

        /// <summary>
        /// Calculates the slope of the regression line through a set of data points.
        /// </summary>
        /// <param name="x">The x-coordinates of the data points.</param>
        /// <param name="y">The y-coordinates of the data points.</param>
        /// <returns>The slope of the regression line.</returns>
        public decimal ComputeSlope(IEnumerable<decimal> x, IEnumerable<decimal> y)
        {
            int n = x.Count();
            var sumX = x.Sum();
            var sumY = y.Sum();
            var sumXY = x.Zip(y, (xi, yi) => xi * yi).Sum();
            var sumX2 = x.Select(xi => xi * xi).Sum();
            var m = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return m;
        }

        /// <summary>
        /// Resets the indicator to its initial state. This clears all internal data and resets
        /// </summary>
        public override void Reset()
        {
            _priceWindow.Reset();
            base.Reset();
        }
    }
}