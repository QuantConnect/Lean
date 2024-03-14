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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the n-period adaptive weighted moving average indicator.
    /// VIDYAi = Pricei x F x ABS(CMOi) + VIDYAi-1 x (1 - F x ABS(CMOi))
    /// where:
    /// VIDYAi - is the value of the current period.
    /// Pricei - is the source price of the period being calculated.
    /// F = 2/(Period_EMA+1) - is a smoothing factor.
    /// ABS(CMOi) - is the absolute current value of CMO.
    /// VIDYAi-1 - is the value of the period immediately preceding the period being calculated.
    /// </summary>
    public class VariableIndexDynamicAverage : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        private decimal _vidya;
        private ChandeMomentumOscillator _CMO;
        private readonly decimal _smoothingFactor;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableIndexDynamicAverage"/> class using the specified period.
        /// </summary> 
        /// <param name="period">The period of the indicator</param>
        public VariableIndexDynamicAverage(int period)
            : this($"VIDYA({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableIndexDynamicAverage"/> class using the specified name and period.
        /// </summary> 
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the indicator</param>
        public VariableIndexDynamicAverage(string name, int period)
            : base(name, period)
        {
            _CMO = new ChandeMomentumOscillator(period);
            _smoothingFactor = 2m / (period + 1);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples > Period;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public override int WarmUpPeriod => Period + 1;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <param name="window">The window for the input history</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            _CMO.Update(input);
            if (!IsReady)
            {
                _vidya = input.Value;
                return 0m;
            }
            var absCMO = Math.Abs(_CMO.Current.Value / 100);
            _vidya = (input.Value * _smoothingFactor * absCMO) + (_vidya * (1 - _smoothingFactor * absCMO));

            return _vidya;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _vidya = 0;
            _CMO.Reset();
            base.Reset();
        }
    }
}
