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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the TRIX (1-period ROC of a Triple EMA)
    /// The TRIX is calculated as explained here:
    /// http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:trix
    /// </summary>
    public class Trix : Indicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly ExponentialMovingAverage _ema1;
        private readonly ExponentialMovingAverage _ema2;
        private readonly ExponentialMovingAverage _ema3;
        private readonly RateOfChangePercent _roc;

        /// <summary>
        /// Initializes a new instance of the <see cref="Trix"/> class using the specified name and period.
        /// </summary> 
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the indicator</param>
        public Trix(string name, int period)
            : base(name)
        {
            _period = period;
            _ema1 = new ExponentialMovingAverage(name + "_1", period);
            _ema2 = new ExponentialMovingAverage(name + "_2", period);
            _ema3 = new ExponentialMovingAverage(name + "_3", period);
            _roc = new RateOfChangePercent(name + "_ROCP1", 1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Trix"/> class using the specified period.
        /// </summary> 
        /// <param name="period">The period of the indicator</param>
        public Trix(int period)
            : this($"TRIX({period})", period)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _roc.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => 3 * (_period - 1) + 1;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _ema1.Update(input);

            if (_ema1.IsReady)
                _ema2.Update(_ema1.Current);

            if (_ema2.IsReady)
                _ema3.Update(_ema2.Current);

            if (_ema3.IsReady)
                _roc.Update(_ema3.Current);

            return _roc;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _ema1.Reset();
            _ema2.Reset();
            _ema3.Reset();
            _roc.Reset();
            base.Reset();
        }
    }
}