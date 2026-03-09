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
    /// This indicator computes the Accumulation/Distribution Oscillator (ADOSC)
    /// The Accumulation/Distribution Oscillator is calculated using the following formula:
    /// ADOSC = EMA(fast,AD) - EMA(slow,AD)
    /// </summary>
    public class AccumulationDistributionOscillator : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly AccumulationDistribution _ad;
        private readonly ExponentialMovingAverage _emaFast;
        private readonly ExponentialMovingAverage _emaSlow;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccumulationDistributionOscillator"/> class using the specified parameters
        /// </summary> 
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        public AccumulationDistributionOscillator(int fastPeriod, int slowPeriod)
            : this($"ADOSC({fastPeriod},{slowPeriod})", fastPeriod, slowPeriod)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccumulationDistributionOscillator"/> class using the specified parameters
        /// </summary> 
        /// <param name="name">The name of this indicator</param>
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        public AccumulationDistributionOscillator(string name, int fastPeriod, int slowPeriod)
            : base(name)
        {
            _period = Math.Max(fastPeriod, slowPeriod);
            _ad = new AccumulationDistribution(name + "_AD");
            _emaFast = new ExponentialMovingAverage(name + "_Fast", fastPeriod);
            _emaSlow = new ExponentialMovingAverage(name + "_Slow", slowPeriod);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= _period;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            _ad.Update(input);
            _emaFast.Update(_ad.Current);
            _emaSlow.Update(_ad.Current);

            return IsReady ? _emaFast.Current.Value - _emaSlow.Current.Value : 0m;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _ad.Reset();
            _emaFast.Reset();
            _emaSlow.Reset();
            base.Reset();
        }
    }
}