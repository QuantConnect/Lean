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
    /// This indicator calculates the Accumulative Swing Index (ASI) as defined by Welles Wilder in his book:
    /// New Concepts in Technical Trading Systems
    /// </summary>
    public class WilderAccumulativeSwingIndex : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly WilderSwingIndex _si;

        /// <summary>
        /// Initializes a new instance of the <see cref="WilderAccumulativeSwingIndex"/> class using the specified name.
        /// </summary>
        /// <param name="absoluteLimitMove">The maximum change in price for the trading session as a fixed value</param>
        public WilderAccumulativeSwingIndex(decimal absoluteLimitMove)
            : base ("ASI")
        {
            _si = new WilderSwingIndex(absoluteLimitMove);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WilderAccumulativeSwingIndex"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="absoluteLimitMove">The maximum change in price for the trading session as a fixed value</param>
        public WilderAccumulativeSwingIndex(string name, decimal absoluteLimitMove)
            : base (name)
        {
            _si = new WilderSwingIndex(absoluteLimitMove); 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WilderAccumulativeSwingIndex"/> class using the specified name.
        /// </summary>
        /// <param name="relativeLimitMove">The maximum change in price for the trading session in basis points of the open price</param>
        public WilderAccumulativeSwingIndex(int relativeLimitMove)
            : base("ASI")
        {
            _si = new WilderSwingIndex(relativeLimitMove);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WilderAccumulativeSwingIndex"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="relativeLimitMove">The maximum change in price for the trading session in basis points of the open price</param>
        public WilderAccumulativeSwingIndex(string name, int relativeLimitMove)
            : base(name)
        {
            _si = new WilderSwingIndex(relativeLimitMove);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized.
        /// </summary>
        public override bool IsReady => Samples > 1;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => 3;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            _si.Update(input);

            if (_si.IsReady)
            {
                Current.Value += _si.Current.Value;
            }
            
            return IsReady ? Current.Value : 0m;
        }

        public override void Reset()
        {
            _si.Reset();
            base.Reset();
        }
    }
}