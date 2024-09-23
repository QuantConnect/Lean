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
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The SmoothedOnBalanceVolume indicator is smoothed version of OnBalanceVolume
    /// This indicator computes the OnBalanceVolume and then
    /// smoothes it over a given period.
    public class SmoothedOnBalanceVolume : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>This indicator is used to smooth the OnBalanceVolume computation</summary>
        /// <remarks>This is not exposed publicly since it is the same value as this indicator, meaning
        /// that this '_smoother' computers the OnBalanceVolume directly, so exposing it publicly would be duplication</remarks>
        private readonly IndicatorBase<IndicatorDataPoint> _smoother;

        /// <summary>
        /// Gets the OnBalanceVolume which is the more volatile calculation to be smoothed by this indicator
        /// </summary>
        public OnBalanceVolume OnBalanceVolume { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _smoother.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates a new SmoothedOnBalanceVolume indicator using the specified period and moving average type
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The smoothing period used to smooth the OnBalanceVolume values</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the OnBalanceVolume values</param>
        public SmoothedOnBalanceVolume(string name, int period, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : base(name)
        {
            WarmUpPeriod = period;

            OnBalanceVolume = new OnBalanceVolume();
            _smoother = movingAverageType.AsIndicator($"{name}_{movingAverageType}", period);

        }

        /// <summary>
        /// Creates a new SmoothedOnBalanceVolume indicator using the specified period and moving average type
        /// </summary>
        /// <param name="period">The smoothing period used to smooth the OnBalanceVolume values</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the OnBalanceVolume values</param>
        public SmoothedOnBalanceVolume(int period, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : this($"SOBV({period})", period, movingAverageType)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            // compute the OnBalanceVolume
            OnBalanceVolume.Update(input);
            
            if (_smoother.Update(input.EndTime, OnBalanceVolume.Current.Value))  // Send true range to our smoother and test if it's ready
            {
                return _smoother.Current.Value;
            }
            else
            {
                return 0m;
            }
            
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _smoother.Reset();
            OnBalanceVolume.Reset();
            base.Reset();
        }
    }
}
