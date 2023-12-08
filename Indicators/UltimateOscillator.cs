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
    /// This indicator computes the Ultimate Oscillator (ULTOSC)
    /// The Ultimate Oscillator is calculated as explained here:
    /// http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:ultimate_oscillator
    /// </summary>
    public class UltimateOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private IBaseDataBar _previousInput;
        private readonly TrueRange _trueRange;
        private readonly Sum _sumBuyingPressure1;
        private readonly Sum _sumBuyingPressure2;
        private readonly Sum _sumBuyingPressure3;
        private readonly Sum _sumTrueRange1;
        private readonly Sum _sumTrueRange2;
        private readonly Sum _sumTrueRange3;

        /// <summary>
        /// Initializes a new instance of the <see cref="UltimateOscillator"/> class using the specified parameters
        /// </summary>
        /// <param name="period1">The first period</param>
        /// <param name="period2">The second period</param>
        /// <param name="period3">The third period</param>
        public UltimateOscillator(int period1, int period2, int period3)
            : this($"ULTOSC({period1},{period2},{period3})", period1, period2, period3)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UltimateOscillator"/> class using the specified parameters
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period1">The first period</param>
        /// <param name="period2">The second period</param>
        /// <param name="period3">The third period</param>
        public UltimateOscillator(string name, int period1, int period2, int period3)
            : base(name)
        {
            _period = Math.Max(Math.Max(period1, period2), period3);
            _trueRange = new TrueRange(name + "_TR");
            _sumBuyingPressure1 = new Sum(name + "_BP1", period1);
            _sumBuyingPressure2 = new Sum(name + "_BP2", period2);
            _sumBuyingPressure3 = new Sum(name + "_BP3", period3);
            _sumTrueRange1 = new Sum(name + "_TR1", period1);
            _sumTrueRange2 = new Sum(name + "_TR2", period2);
            _sumTrueRange3 = new Sum(name + "_TR3", period3);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples > _period;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period + 1;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            _trueRange.Update(input);

            if (Samples == 1)
            {
                _previousInput = input;
                return 50m;
            }

            var buyingPressure = new IndicatorDataPoint { Value = input.Close - Math.Min(input.Low, _previousInput.Close) };

            _sumBuyingPressure1.Update(buyingPressure);
            _sumBuyingPressure2.Update(buyingPressure);
            _sumBuyingPressure3.Update(buyingPressure);

            _sumTrueRange1.Update(_trueRange.Current);
            _sumTrueRange2.Update(_trueRange.Current);
            _sumTrueRange3.Update(_trueRange.Current);

            _previousInput = input;

            if (!IsReady)
                return 50m;

            if (_sumTrueRange1.Current.Value == 0
                || _sumTrueRange2.Current.Value == 0
                || _sumTrueRange3.Current.Value == 0)
            {
                return Current.Value;
            }

            var average1 = _sumBuyingPressure1.Current.Value / _sumTrueRange1.Current.Value;
            var average2 = _sumBuyingPressure2.Current.Value / _sumTrueRange2.Current.Value;
            var average3 = _sumBuyingPressure3.Current.Value / _sumTrueRange3.Current.Value;

            return 100m * (4 * average1 + 2 * average2 + average3) / 7;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _previousInput = null;
            _trueRange.Reset();
            _sumBuyingPressure1.Reset();
            _sumBuyingPressure2.Reset();
            _sumBuyingPressure3.Reset();
            _sumTrueRange1.Reset();
            _sumTrueRange2.Reset();
            _sumTrueRange3.Reset();
            base.Reset();
        }
    }
}