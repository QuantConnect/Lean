
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
using System.Collections.Generic;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Vortex Indicator (VI) is used to identify the start of a new trend or the continuation of an existing trend within financial markets.
    /// It consists of two oscillators: Positive VI (+VI) and Negative VI (-VI). These oscillators capture positive and negative trend movements.
    /// The indicators are calculated by comparing the current bar's high and low to the previous bar's high and low to create vectors.
    /// These vectors are then smoothed and normalized by the average true range (ATR) to provide a bound oscillator that fluctuates between 0 and 1.
    /// </summary>
    public class Vortex : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly AverageTrueRange _atr;
        private readonly List<decimal> _plusVM;
        private readonly List<decimal> _minusVM;
        private decimal _plusVMSum;
        private decimal _minusVMSum;
        private IBaseDataBar _previousInput;

        /// <summary>
        /// Gets the Positive Vortex Indicator (+VI).
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> PlusVI { get; private set; }

        /// <summary>
        /// Gets the Negative Vortex Indicator (-VI).
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> MinusVI { get; private set; }

        /// <summary>
        /// Indicates whether the indicator is ready to produce meaningful results after its initial warm-up period.
        /// </summary>
        public override bool IsReady => Samples >= _period;

        /// <summary>
        /// Gets the required period, in data points, for the indicator to be considered ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Initializes a new instance of the Vortex Indicator with a specified period.
        /// </summary>
        public  Vortex(int period)
            : this($"VTX({period})", period)
        {
        }

        public Vortex(string name,int period)
            : base(name)
        {
            _period = period;
            _atr = new AverageTrueRange("VTX_ATR_" + period, period, MovingAverageType.Simple);

            _plusVM = new List<decimal>();
            _minusVM = new List<decimal>();

            PlusVI = new FunctionalIndicator<IndicatorDataPoint>("PlusVI",
                input => CalculateVortexIndex(_plusVMSum, _atr.Current.Value * _period),
                isReady => _plusVM.Count >= _period && _atr.IsReady,
                () => { _plusVM.Clear(); _plusVMSum = 0m; });

            MinusVI = new FunctionalIndicator<IndicatorDataPoint>("MinusVI",
                input => CalculateVortexIndex(_minusVMSum, _atr.Current.Value * _period),
                isReady => _minusVM.Count >= _period && _atr.IsReady,
                () => { _minusVM.Clear(); _minusVMSum = 0m; });
        }

        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            if (_previousInput != null)
            {
                var plusVMValue = Math.Abs(input.High - _previousInput.Low);
                var minusVMValue = Math.Abs(input.Low - _previousInput.High);

                if (_plusVM.Count == _period)
                {
                    _plusVMSum -= _plusVM[0];
                    _plusVM.RemoveAt(0);
                }
                if (_minusVM.Count == _period)
                {
                    _minusVMSum -= _minusVM[0];
                    _minusVM.RemoveAt(0);
                }

                _plusVM.Add(plusVMValue);
                _minusVM.Add(minusVMValue);

                _plusVMSum += plusVMValue;
                _minusVMSum += minusVMValue;
            }

            _previousInput = input;
            _atr.Update(input);

            if (_plusVM.Count == _period && _minusVM.Count == _period && _atr.IsReady)
            {
                PlusVI.Update(input.EndTime, CalculateVortexIndex(_plusVMSum, _atr.Current.Value * _period));
                MinusVI.Update(input.EndTime, CalculateVortexIndex(_minusVMSum, _atr.Current.Value * _period));
            }

            return (PlusVI.Current.Value + MinusVI.Current.Value) / 2;
        }

        private decimal CalculateVortexIndex(decimal vmSum, decimal atrSum)
        {
            return atrSum != 0 ? vmSum / atrSum : 0m;
        }

        /// <summary>
        /// Resets this indicator to its initial state, including all intermediate computation values.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _plusVM.Clear();
            _minusVM.Clear();
            _plusVMSum = 0m;
            _minusVMSum = 0m;
            _atr.Reset();
            PlusVI.Reset();
            MinusVI.Reset();
            _previousInput = null;
        }
    }
}
