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
    /// The McClellan Oscillator is a market breadth indicator which was
    /// developed by Sherman and Marian McClellan. It is based on the
    /// difference between the number of advancing and declining periods.
    /// </summary>
    public class McClellanOscillator : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly IndicatorBase<IndicatorDataPoint> _averageDelta;

        /// <summary>
        /// Fast period EMA of advance decline difference
        /// </summary>
        public ExponentialMovingAverage EMAFast { get; }

        /// <summary>
        /// Slow period EMA of advance decline difference
        /// </summary>
        public ExponentialMovingAverage EMASlow { get; }

        /// <summary>
        /// The number of advance assets minus the number of decline assets
        /// </summary>
        public AdvanceDeclineDifference ADDifference { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => EMASlow.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => EMASlow.WarmUpPeriod + ADDifference.WarmUpPeriod;

        /// <summary>
        /// Initializes a new instance of the <see cref="McClellanOscillator"/> class
        /// <param name="name">The name of the indicator</param>
        /// <param name="fastPeriod">The fast period of EMA of advance decline difference</param>
        /// <param name="slowPeriod">The slow period of EMA of advance decline difference</param>
        /// </summary>
        public McClellanOscillator(string name, int fastPeriod = 19, int slowPeriod = 39) : base(name)
        {
            if (fastPeriod > slowPeriod)
            {
                throw new ArgumentException("fastPeriod must be less than slowPeriod.");
            }

            ADDifference = new AdvanceDeclineDifference("ADD");
            EMAFast = ADDifference.EMA(fastPeriod);
            EMASlow = ADDifference.EMA(slowPeriod);
            _averageDelta = EMAFast.Minus(EMASlow);
        }

        public McClellanOscillator(int fastPeriod = 19, int slowPeriod = 39)
            : this("McClellanOscillator", fastPeriod, slowPeriod) { }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            ADDifference.Update(input);

            return _averageDelta.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            ADDifference.Reset();
            EMAFast.Reset();
            EMASlow.Reset();
            _averageDelta.Reset();

            base.Reset();
        }

        /// <summary>
        /// Add Tracking asset issue
        /// </summary>
        /// <param name="asset">the tracking asset issue</param>
        public void Add(Symbol asset)
        {
            ADDifference.Add(asset);
        }

        /// <summary>
        /// Remove Tracking asset issue
        /// </summary>
        /// <param name="asset">the tracking asset issue</param>
        public void Remove(Symbol asset)
        {
            ADDifference.Remove(asset);
        }
    }
}
