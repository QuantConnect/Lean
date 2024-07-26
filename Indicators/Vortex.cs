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
using QuantConnect.Indicators;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Computes the Vortex Indicator (VI), which is designed to identify the start of a new trend or the continuation of an existing trend within financial markets.
    /// The Vortex Indicator involves calculations of upward and downward movements (VM+ and VM-), normalized by the True Range.
    /// </summary>
    public class VortexIndicator : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Indicates whether this indicator is ready and fully initialized.
        /// </summary>
        public override bool IsReady => _window.IsReady && _tr.IsReady;

        private readonly RollingWindow<TradeBar> _window;
        private readonly int _period;
        private readonly TrueRange _tr; // TrueRange indicator

        /// <summary>
        /// Indicator representing the positive trend movement (+VI).
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> PlusVI { get; private set; }

        /// <summary>
        /// Indicator representing the negative trend movement (-VI).
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> MinusVI { get; private set; }

        /// <summary>
        /// The period over which the indicator is calculated.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Initializes a new instance of the VortexIndicator class with the specified name and period.
        /// Constructs a VortexIndicator with an internal rolling window of trade bars used to calculate the +VI and -VI based on the given period.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period over which to calculate the indicator</param>
        public VortexIndicator(string name, int period) : base(name)
        {
            _period = period;
            PlusVI = new Identity($"{name}_PlusVI");
            MinusVI = new Identity($"{name}_MinusVI");
            _window = new RollingWindow<TradeBar>(period);
            _tr = new TrueRange(); // Instantiate TrueRange
        }

        /// <summary>
        /// Computes the next value of the Vortex Indicator using the specified TradeBar input.
        /// This method is responsible for updating the PlusVI and MinusVI values based on the current and historical trade bar data.
        /// </summary>
        /// <param name="input">The input TradeBar data used to calculate the indicator</param>
        /// <returns>The difference between the PlusVI and MinusVI values</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            _window.Add(input);
            _tr.Update(input);

            if (_window.IsReady)
            {
                decimal sumPlusVM = 0m, sumMinusVM = 0m, sumTR = _tr.Current.Value;
                for (int i = 1; i < _window.Count; i++)
                {
                    var currentBar = _window[i];
                    var previousBar = _window[i - 1];
                    sumPlusVM += Math.Abs(currentBar.High - previousBar.Low);
                    sumMinusVM += Math.Abs(currentBar.Low - previousBar.High);
                }

                if (sumTR != 0m) // Avoid division by zero
                {
                    PlusVI.Update(input.Time, sumPlusVM / sumTR);
                    MinusVI.Update(input.Time, sumMinusVM / sumTR);
                }
            }
            return (PlusVI.Current.Value - MinusVI.Current.Value);
        }

        /// <summary>
        /// Resets this indicator to its initial state, clearing any internal state and calculations.
        /// This method is crucial for ensuring that the indicator can be reused without residual data from previous calculations.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            PlusVI.Reset();
            MinusVI.Reset();
            _tr.Reset();
            _window.Reset();
        }
    }
}
