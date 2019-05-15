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
    /// Williams %R, or just %R, is the current closing price in relation to the high and low of
    /// the past N days (for a given N). The value of this indicator fluctuates between -100 and 0.
    /// The symbol is said to be oversold when the oscillator is below -80%,
    /// and overbought when the oscillator is above -20%. 
    /// </summary>
    public class WilliamsPercentR : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Gets the Maximum indicator
        /// </summary>
        public Maximum Maximum { get; }

        /// <summary>
        /// Gets the Minimum indicator
        /// </summary>
        public Minimum Minimum { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Maximum.IsReady && Minimum.IsReady; 
        
        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates a new Williams %R.
        /// </summary>
        /// <param name="period">The look-back period to determine the Williams %R</param>
        public WilliamsPercentR(int period)
            : this($"WILR({period})", period)
        {
        }

        /// <summary>
        /// Creates a new Williams %R.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The look-back period to determine the Williams %R</param>
        public WilliamsPercentR(string name, int period)
            : base(name)
        {
            Maximum = new Maximum(name + "_Max", period);
            Minimum = new Minimum(name + "_Min", period);
            WarmUpPeriod = period;
        }

        /// <summary>
        /// Resets this indicator and both sub-indicators (Max and Min)
        /// </summary>
        public override void Reset()
        {
            Maximum.Reset();
            Minimum.Reset();
            base.Reset();
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            Minimum.Update(input.Time, input.Low);
            Maximum.Update(input.Time, input.High);

            if (!IsReady) return 0;

            var range = Maximum.Current.Value - Minimum.Current.Value;

            return range == 0 ? 0 : -100m * (Maximum.Current.Value - input.Close) / range;
        }
    }
}