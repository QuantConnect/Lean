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
    /// This indicator computes the Kaufman Efficiency Ratio or Efficiency Ratio (KEF).
    /// </summary>
    public class KaufmanEfficiencyRatio : Indicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly KaufmanAdaptiveMovingAverage _kaufmanAdaptiveMovingAverage;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _kaufmanAdaptiveMovingAverage.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _kaufmanAdaptiveMovingAverage.Period;

        /// <summary>
        /// Initializes a new instance of the <see cref="KaufmanEfficiencyRatio"/> class using the specified period.
        /// </summary>
        /// <param name="period">The period of the Kaufman Efficiency Ratio (KEF)</param>
        public KaufmanEfficiencyRatio(int period)
            : this($"KER({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KaufmanEfficiencyRatio"/> class using the specified name and period.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the Efficiency Ratio (ER)</param>
        public KaufmanEfficiencyRatio(string name, int period)
            : base(name)
        {
            _kaufmanAdaptiveMovingAverage = new KaufmanAdaptiveMovingAverage(period);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _kaufmanAdaptiveMovingAverage.Update(input);
            return _kaufmanAdaptiveMovingAverage.EfficiencyRatio.Current.Value;
        }
    }
}
