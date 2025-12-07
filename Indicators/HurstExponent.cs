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
    /// Represents the Hurst Exponent indicator, which is used to measure the long-term memory of a time series.
    /// - H less than 0.5: Mean-reverting; high values followed by low ones, stronger as H approaches 0.
    /// - H equal to 0.5: Random walk (geometric).
    /// - H greater than 0.5: Trending; high values followed by higher ones, stronger as H approaches 1.
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
        private readonly List<int> _timeLags;

        /// <summary>
        /// Sum of the logarithms of the time lags, precomputed for efficiency.
        /// </summary>
        private readonly decimal _sumX;

        /// <summary>
        /// Sum of the squares of the logarithms of the time lags, precomputed for efficiency.
        /// </summary>
        private readonly decimal _sumX2;

        /// <summary>
        /// Initializes a new instance of the <see cref="HurstExponent"/> class.
        /// The default maxLag value of 20 is chosen for reliable and accurate results, but using a higher lag may reduce precision.
        /// </summary>
        /// <param name="name">The name of the indicator.</param>
        /// <param name="period">The period over which to calculate the Hurst Exponent.</param>
        /// <param name="maxLag">The maximum lag to consider for time series analysis.</param>
        public HurstExponent(string name, int period, int maxLag = 20) : base(name)
        {
            if (maxLag < 3)
            {
                throw new ArgumentException("The maxLag parameter must be greater than 2 to compute the Hurst Exponent.", nameof(maxLag));
            }
            _priceWindow = new RollingWindow<decimal>(period);
            _timeLags = new List<int>();

            // Precompute logarithms of time lags and their squares for regression calculations
            for (var i = 2; i <= maxLag; i++)
            {
                var logTimeLag = (decimal)Math.Log(i);
                _timeLags.Add(i);
                _sumX += logTimeLag;
                _sumX2 += logTimeLag * logTimeLag;
            }
            WarmUpPeriod = period;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HurstExponent"/> class with the specified period and maxLag.
        /// The default maxLag value of 20 is chosen for reliable and accurate results, but using a higher lag may reduce precision.
        /// </summary>
        /// <param name="period">The period over which to calculate the Hurst Exponent.</param>
        /// <param name="maxLag">The maximum lag to consider for time series analysis.</param>
        public HurstExponent(int period, int maxLag = 20)
            : this($"HE({period},{maxLag})", period, maxLag)
        {
        }

        /// <summary>
        /// Gets the period over which the indicator is calculated.
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

            // Sum of log(standard deviation) values
            var sumY = 0m;

            // Sum of log(lag) * log(standard deviation)
            var sumXY = 0m;

            foreach (var lag in _timeLags)
            {
                var mean = 0m;
                var sumOfSquares = 0m;
                var count = Math.Max(0, _priceWindow.Size - lag);
                // Calculate the differences between values separated by the given lag
                for (var i = 0; i < count; i++)
                {
                    var value = _priceWindow[i + lag] - _priceWindow[i];
                    sumOfSquares += value * value;
                    mean += value;
                }

                var standardDeviation = 0.0;
                // Avoid division by zero
                if (count > 0)
                {
                    mean = mean / count;
                    var variance = (sumOfSquares / count) - (mean * mean);
                    standardDeviation = Math.Sqrt((double)variance);
                }

                // Compute log(standard deviation) and log(lag) for the regression.
                var logTau = standardDeviation == 0.0 ? 0m : (decimal)Math.Log(standardDeviation);
                var logLag = (decimal)Math.Log(lag);

                // Accumulate sums for the regression equation.
                sumY += logTau;
                sumXY += logLag * logTau;
            }

            // Number of time lags used for the computation
            var n = _timeLags.Count;

            // Compute the Hurst Exponent using the slope of the log-log regression.
            var hurstExponent = (n * sumXY - _sumX * sumY) / (n * _sumX2 - _sumX * _sumX);
            return hurstExponent;
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