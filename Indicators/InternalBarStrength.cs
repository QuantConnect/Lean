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
    /// The InternalBarStrenght indicator is a measure of the relative position of a period's closing price 
    /// to the same period's high and low.
    /// The IBS can be interpreted to predict a bullish signal when displaying a low value and a bearish signal when presenting a high value.
    /// </summary>
    public class InternalBarStrength : BarIndicator, IIndicatorWarmUpPeriodProvider
    {

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples > 0;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => 1;

        /// <summary>
        /// Creates a new InternalBarStrenght indicator using the specified period and moving average type
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public InternalBarStrength(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Creates a new InternalBarStrenght indicator using the specified period and moving average type
        /// </summary>
        public InternalBarStrength()
            : this($"IBS()")
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            if (input.High == input.Low)
            {
                return 1m;
            }
            else
            {
                return (input.Close - input.Low) / (input.High - input.Low);
            }
        }
    }
}
