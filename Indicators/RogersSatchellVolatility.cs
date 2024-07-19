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
    /// This indicator computes the Rogers-Satchell Volatility
    /// </summary>
    public class RogersSatchellVolatility : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly RollingWindow<TradeBar> _inputWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="RogersSatchellVolatility"/> class using the specified parameters
        /// </summary> 
        /// <param name="period">The period of moving window</param>
        public RogersSatchellVolatility(int period)
            : this($"RSVolat({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RogersSatchellVolatility"/> class using the specified parameters
        /// </summary> 
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of moving window</param>
        public RogersSatchellVolatility(string name, int period)
            : base(name)
        {
            _period = period;
            _inputWindow = new RollingWindow<TradeBar>(_period);
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
            _inputWindow.Add(input);

            if (!IsReady)
            {
                return 0m;
            }

            var s = 0.0;
            foreach (var bar in _inputWindow)
            {
                if ((bar.Open == 0) || (bar.High == 0) || (bar.Low == 0) || (bar.Close == 0))
                {
                    // return a sentinel value
                    return decimal.MinValue;
                }

                s += Math.Log((double)bar.High / (double)bar.Close) * Math.Log((double)bar.High / (double)bar.Open) +
                     Math.Log((double)bar.Low / (double)bar.Close) * Math.Log((double)bar.Low / (double)bar.Open);
            }
            return (decimal) Math.Sqrt(s / _period);
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _inputWindow.Reset();
            base.Reset();
        }
    }
}
