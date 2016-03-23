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
    /// This indicator computes the Percentage Price Oscillator (PPO)
    /// The Percentage Price Oscillator is calculated using the following formula:
    /// PPO[i] = 100 * (FastMA[i] - SlowMA[i]) / SlowMA[i]
    /// </summary>
    public class PercentagePriceOscillator : IndicatorBase<IndicatorDataPoint>
    {
        private readonly IndicatorBase<IndicatorDataPoint> _fast;
        private readonly IndicatorBase<IndicatorDataPoint> _slow;

        /// <summary>
        /// Initializes a new instance of the <see cref="PercentagePriceOscillator"/> class using the specified name and parameters.
        /// </summary> 
        /// <param name="name">The name of this indicator</param>
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        /// <param name="movingAverageType">The type of moving average to use</param>
        public PercentagePriceOscillator(string name, int fastPeriod, int slowPeriod, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : base(name)
        {
            _fast = movingAverageType.AsIndicator(fastPeriod);
            _slow = movingAverageType.AsIndicator(slowPeriod);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PercentagePriceOscillator"/> class using the specified parameters.
        /// </summary> 
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        /// <param name="movingAverageType">The type of moving average to use</param>
        public PercentagePriceOscillator(int fastPeriod, int slowPeriod, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : this(string.Format("PPO({0},{1})", fastPeriod, slowPeriod), fastPeriod, slowPeriod, movingAverageType)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return _fast.IsReady && _slow.IsReady; }
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _fast.Update(input);
            _slow.Update(input);

            return (IsReady && _slow != 0m) ? 100 * (_fast - _slow) / _slow : 0m;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _fast.Reset();
            _slow.Reset();
            base.Reset();
        }
    }
}
