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
using QLNet;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    ///The Force Index is calculated by comparing the current market price with the previous market price 
    ///and multiplying its difference with the traded volume during a specific time period.
    ///
    /// </summary>
    public class ForceIndex : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private TradeBar _previousInput;

        /// <summary>This indicator is used to smooth the ForceIndex computation</summary>
        /// <remarks>This is not exposed publicly since it is the same value as this indicator, meaning
        /// that this '_smoother' computers the ForceIndex directly, so exposing it publicly would be duplication</remarks>
        private readonly IndicatorBase<IndicatorDataPoint> _smoother;


        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _smoother.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates a new ForceIndex indicator using the specified period and moving average type
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The smoothing period used to smooth the true range values</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the true range values</param>
        public ForceIndex(string name, int period, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : base(name)
        {
            WarmUpPeriod = period;

            _smoother = movingAverageType.AsIndicator($"{name}_{movingAverageType}", period);

        }

        /// <summary>
        /// Creates a new ForceIndex indicator using the specified period and moving average type
        /// </summary>
        /// <param name="period">The smoothing period used to smooth the instantenous force index values</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the instantenous force index values</param>
        public ForceIndex(int period, MovingAverageType movingAverageType = MovingAverageType.Exponential)
            : this($"FI({period})", period, movingAverageType)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            if (Samples < 2)
            {
                return 0;
            }
            // compute the instantaneous force index and then send it to our smoother
            
            _smoother.Update(input.Time, (input.Close - _previousInput.Close) * input.Volume);

            _previousInput = input;

            return _smoother.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _previousInput = null;
            _smoother.Reset();
            base.Reset();
        }
    }
}
