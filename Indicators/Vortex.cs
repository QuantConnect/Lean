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
    /// Represents the Vortex Indicator, which identifies the start and continuation of market trends.
    /// It includes components that capture positive (upward) and negative (downward) trend movements.
    /// This indicator compares the ranges within the current period to previous periods to calculate
    /// upward and downward movement trends.
    /// </summary>
    public class Vortex : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly AverageTrueRange _atr;
        private readonly Sum _atrSum;
        private readonly Sum _plusVMSum;
        private readonly Sum _minusVMSum;
        private IBaseDataBar _previousInput;

        /// <summary>
        /// Gets the Positive Vortex Indicator, which reflects positive trend movements.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> PlusVortex { get; private set; }

        /// <summary>
        /// Gets the Negative Vortex Indicator, which reflects negative trend movements.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> MinusVortex { get; private set; }

        /// <summary>
        /// Indicates whether this indicator is fully ready and all buffers have been filled.
        /// </summary>
        public override bool IsReady => Samples >= _period;

        /// <summary>
        /// The minimum number of samples needed for the indicator to be ready and provide reliable values.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Initializes a new instance of the <see cref="Vortex"/> class using the specified period.
        /// </summary>
        /// <param name="period">The number of periods used to construct the Vortex Indicator.</param>
        public Vortex(int period)
            : this($"VTX({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vortex"/> class with a custom name and period.
        /// </summary>
        /// <param name="name">The custom name for this instance of the Vortex Indicator.</param>
        /// <param name="period">The number of periods used to construct the Vortex Indicator.</param>
        public Vortex(string name, int period)
            : base(name)
        {
            _period = period;
            _atr = new AverageTrueRange($"{Name}_ATR", 1, MovingAverageType.Simple);
            _atrSum = new Sum("ATR_Sum", period).Of(_atr);
            _plusVMSum = new Sum("PlusVM_Sum", period);
            _minusVMSum = new Sum("MinusVM_Sum", period);

            PlusVortex = _plusVMSum.Over(_atrSum);
            MinusVortex = _minusVMSum.Over(_atrSum);
        }

        /// <summary>
        /// Computes the next value of the Vortex Indicator based on the provided input.
        /// </summary>
        /// <param name="input">The input data used to compute the indicator value.</param>
        /// <returns>The computed value of the indicator.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            _atr.Update(input);

            if (_previousInput != null)
            {
                var plusVMValue = Math.Abs(input.High - _previousInput.Low);
                var minusVMValue = Math.Abs(input.Low - _previousInput.High);

                _plusVMSum.Update(input.EndTime, plusVMValue);
                _minusVMSum.Update(input.EndTime, minusVMValue);
            }

            _previousInput = input;

            if (!IsReady)
            {
                return 0;
            }

            return (PlusVortex.Current.Value + MinusVortex.Current.Value) / 2;
        }

        /// <summary>
        /// Resets all indicators and internal state.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _atr.Reset();
            _atrSum.Reset();
            _plusVMSum.Reset();
            _minusVMSum.Reset();
            PlusVortex.Reset();
            MinusVortex.Reset();
            _previousInput = null;
        }
    }
}
