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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Represents the traditional exponential moving average indicator (EMA)
    /// </summary>
    public class ExponentialMovingAverage : Indicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly decimal _k;
        private readonly int _period;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Initializes a new instance of the ExponentialMovingAverage class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the EMA</param>
        public ExponentialMovingAverage(string name, int period)
            : base(name)
        {
            _period = period;
            _k = SmoothingFactorDefault(period);
        }

        /// <summary>
        /// Initializes a new instance of the ExponentialMovingAverage class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the EMA</param>
        /// <param name="smoothingFactor">The percentage of data from the previous value to be carried into the next value</param>
        public ExponentialMovingAverage(string name, int period, decimal smoothingFactor)
            : base(name)
        {
            _period = period;
            _k = smoothingFactor;
        }

        /// <summary>
        /// Initializes a new instance of the ExponentialMovingAverage class with the default name and period
        /// </summary>
        /// <param name="period">The period of the EMA</param>
        public ExponentialMovingAverage(int period)
            : this($"EMA({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ExponentialMovingAverage class with the default name and period
        /// </summary>
        /// <param name="period">The period of the EMA</param>
        /// <param name="smoothingFactor">The percentage of data from the previous value to be carried into the next value</param>
        public ExponentialMovingAverage(int period, decimal smoothingFactor)
            : this($"EMA({period},{smoothingFactor})", period, smoothingFactor)
        {
        }

        /// <summary>
        /// Calculates the default smoothing factor for an ExponentialMovingAverage indicator
        /// </summary>
        /// <param name="period">The period of the EMA</param>
        /// <returns>The default smoothing factor</returns>
        public static decimal SmoothingFactorDefault(int period) => 2.0m / (1 + period);

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= _period;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            // our first data point just return identity
            if (Samples == 1)
            {
                return input.Value;
            }
            return input.Value * _k + Current.Value * (1 - _k);
        }
    }
}