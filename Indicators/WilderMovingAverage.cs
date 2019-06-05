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
    /// Represents the moving average indicator defined by Welles Wilder in his book:
    /// New Concepts in Technical Trading Systems.
    /// </summary>
    public class WilderMovingAverage : Indicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly decimal _k;
        private readonly int _period;
        private readonly IndicatorBase<IndicatorDataPoint> _sma;

        /// <summary>
        /// Initializes a new instance of the WilderMovingAverage class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the Wilder Moving Average</param>
        public WilderMovingAverage(string name, int period)
            : base(name)
        {
            _period = period;
            _k = 1m / period;
            _sma = new SimpleMovingAverage(name + "_SMA", period);
        }

        /// <summary>
        /// Initializes a new instance of the WilderMovingAverage class with the default name and period
        /// </summary>
        /// <param name="period">The period of the Wilder Moving Average</param>
        public WilderMovingAverage(int period)
            : this("WWMA" + period, period)
        {
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
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _sma.Reset();
            base.Reset();
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            if (!IsReady)
            {
                _sma.Update(input);
                return _sma;
            }
            return input * _k + Current * (1 - _k);
        }
    }
}