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
    /// The McClellan Summation Index (MSI) is a market breadth indicator that is based on the rolling average of difference
    /// between the number of advancing and declining issues on a stock exchange. It is generally considered as is
    /// a long-term version of the <see cref="McClellanOscillator"/>
    /// </summary>
    public class McClellanSummationIndex : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// The McClellan Summation Index value
        /// </summary>
        /// <remarks>Protected for testing</remarks>
        protected IndicatorDataPoint Summation { get; }

        /// <summary>
        /// The McClellan Oscillator is a market breadth indicator which was developed by Sherman and Marian McClellan. It is based on the difference between the number of advancing and declining periods.
        /// </summary>
        public McClellanOscillator McClellanOscillator { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => McClellanOscillator.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => McClellanOscillator.WarmUpPeriod;

        /// <summary>
        /// Initializes a new instance of the <see cref="McClellanSummationIndex"/> class
        /// <param name="name">The name of the indicator</param>
        /// <param name="fastPeriod">The fast period of EMA of advance decline difference</param>
        /// <param name="slowPeriod">The slow period of EMA of advance decline difference</param>
        /// </summary>
        public McClellanSummationIndex(string name, int fastPeriod = 19, int slowPeriod = 39)
            : base(name)
        {
            Summation = new();
            McClellanOscillator = new McClellanOscillator(fastPeriod, slowPeriod);
            McClellanOscillator.Updated += (_, updated) =>
            {
                // Update only when new indicator data point was consolidated
                if (updated.EndTime != Summation.Time)
                {
                    Summation.Time = updated.EndTime;
                    Summation.Value += updated.Value;
                }
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="McClellanSummationIndex"/> class
        /// <param name="fastPeriod">The fast period of EMA of advance decline difference</param>
        /// <param name="slowPeriod">The slow period of EMA of advance decline difference</param>
        /// </summary>
        public McClellanSummationIndex(int fastPeriod = 19, int slowPeriod = 39)
            : this("McClellanSummationIndex", fastPeriod, slowPeriod) { }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            McClellanOscillator.Update(input);

            return Summation + McClellanOscillator.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            McClellanOscillator.Reset();
            base.Reset();
        }

        /// <summary>
        /// Add Tracking asset issue
        /// </summary>
        /// <param name="asset">the tracking asset issue</param>
        public void Add(Symbol asset)
        {
            McClellanOscillator.Add(asset);
        }

        /// <summary>
        /// Remove Tracking asset issue
        /// </summary>
        /// <param name="asset">the tracking asset issue</param>
        public void Remove(Symbol asset)
        {
            McClellanOscillator.Remove(asset);
        }
    }
}
