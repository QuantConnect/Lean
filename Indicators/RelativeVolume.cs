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
    /// Represents the relative volume indicator (RVOL)
    /// </summary>
    public class RelativeVolume : WindowIndicator<TradeBar>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// A rolling sum for computing the average for the given period
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> RollingSum { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => RollingSum.IsReady;

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            RollingSum.Reset();
            base.Reset();
        }

        /// <summary>
        /// Initializes a new instance of the RelativeVolume class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the RVOL</param>
        public RelativeVolume(string name, int period)
            : base(name, period)
        {
            RollingSum = new Sum(name + "_Sum", period);
        }

        /// <summary>
        /// Initializes a new instance of the RelativeVolume class with the default name
        /// </summary>
        /// <param name="period">The period of the RVOL</param>
        public RelativeVolume(int period = 50)
            : this($"RVOL({period})", period)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<TradeBar> window, TradeBar input)
        {
            RollingSum.Update(input.Time, input.Volume);
            return input.Volume / (RollingSum.Current.Value / window.Count);
        }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => Period;
    }
}
