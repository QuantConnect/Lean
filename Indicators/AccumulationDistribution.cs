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

using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the Accumulation/Distribution (AD)
    /// The Accumulation/Distribution is calculated using the following formula:
    /// AD = AD + ((Close - Low) - (High - Close)) / (High - Low) * Volume
    /// </summary>
    public class AccumulationDistribution : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccumulationDistribution"/> class using the specified name.
        /// </summary>
        public AccumulationDistribution()
            : this("AD")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccumulationDistribution"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public AccumulationDistribution(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples > 0;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => 1;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            var range = input.High - input.Low;
            return Current.Value + (range > 0 ? ((input.Close - input.Low) - (input.High - input.Close)) / range * input.Volume : 0m);
        }
    }
}