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

using MathNet.Numerics.Statistics;
using MathNet.Numerics.Distributions;
using System;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes 1-day VaR for a specified confidence level and lookback period
    /// </summary>
    public class ValueAtRisk : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Confidence level for VaR calculation
        /// </summary>
        private readonly double _confidenceLevel;

        /// <summary>
        /// Rolling window to store the input data points
        /// </summary>
        private readonly RollingWindow<decimal> _dataPoints;

        /// <summary>
        /// Rolling window to store the returns of the input data
        /// </summary>
        private readonly RollingWindow<double> _returns;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public override int WarmUpPeriod { get; }

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _dataPoints.Samples >= WarmUpPeriod;

        /// <summary>
        /// Creates a new ValueAtRisk indicator with a specified period and confidence level
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">Historical lookback period in days</param>
        /// <param name="confidenceLevel">Confidence level for VaR calculation</param>
        public ValueAtRisk(string name, int period, double confidenceLevel) 
            : base(name, period)
        {
            if (period < 3)
            {
                throw new ArgumentException($"Period parameter for ValueAtRisk indicator must be greater than 2 but was {period}");
            }

            WarmUpPeriod = 3;
            _confidenceLevel = confidenceLevel;

            _dataPoints = new RollingWindow<decimal>(2);
            _returns = new RollingWindow<double>(period);
        }

        /// <summary>
        /// Creates a new ValueAtRisk indicator with a specified period and confidence level
        /// </summary>
        /// <param name="period">Historical lookback period in days</param>
        /// <param name="confidenceLevel">Confidence level for VaR calculation</param>
        public ValueAtRisk(int period, double confidenceLevel)
            : this($"VaR({period}, {confidenceLevel})", period, confidenceLevel)
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
            _dataPoints.Add(input.Value);

            if (_dataPoints.Samples > 1)
            {
                _returns.Add(GetNewReturn(_dataPoints));
            }
            
            if (_returns.Count < 2)
            {
                return 0m;
            }

            var mean = _returns.Mean();
            var standardDeviation = _returns.StandardDeviation();
            return (decimal)Normal.InvCDF(mean, standardDeviation, 1 - _confidenceLevel);
        }

        /// <summary>
        /// Computes the returns by comparing the new data point and the previous data point
        /// </summary>
        /// <param name="rollingWindow">The collection of data points</param>
        /// <returns>The return value</returns>
        private static double GetNewReturn(RollingWindow<decimal> rollingWindow)
        {
            if (rollingWindow[1] != 0)
            {
                return (double)((rollingWindow[0] / rollingWindow[1]) - 1);
            }
            return 0;
        }

        /// <summary>
        ///     Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _dataPoints.Reset();
            _returns.Reset();
            base.Reset();
        }
    }
}

