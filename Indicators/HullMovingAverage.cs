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
    /// Produces a Hull Moving Average as explained at http://www.alanhull.com/hull-moving-average/
    /// and derived from the instructions for the Excel VBA code at http://finance4traders.blogspot.com/2009/06/how-to-calculate-hull-moving-average.html
    /// </summary>
    public class HullMovingAverage : IndicatorBase<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        private readonly LinearWeightedMovingAverage _fastWma;
        private readonly LinearWeightedMovingAverage _slowWma;
        private readonly LinearWeightedMovingAverage _hullMa;

        /// <summary>
        /// A Hull Moving Average 
        /// </summary>
        /// <param name="name">string - a name for the indicator</param>
        /// <param name="period">int - the number of periods to calculate the HMA - the period of the slower LWMA</param>
        public HullMovingAverage(string name, int period)
            : base(name)
        {
            if (period < 2) throw new ArgumentException("The Hull Moving Average period should be greater or equal to 2", nameof(period));
            _slowWma = new LinearWeightedMovingAverage(period);
            _fastWma = new LinearWeightedMovingAverage((int)Math.Round(period * 1d / 2));
            var k = (int)Math.Round(Math.Sqrt(period));
            _hullMa = new LinearWeightedMovingAverage(k);
            WarmUpPeriod = period + k - 1;
        }
        /// <summary>
        /// A Hull Moving Average.
        /// </summary>
        /// <param name="period">int - the number of periods over which to calculate the HMA - the length of the slower LWMA</param>
        public HullMovingAverage(int period)
            : this($"HMA({period})", period)
        {
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _slowWma.Reset();
            _fastWma.Reset();
            _hullMa.Reset();
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _hullMa.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>
        /// A new value for this indicator
        /// </returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _fastWma.Update(input);
            _slowWma.Update(input);
            if (_fastWma.IsReady && _slowWma.IsReady)
            {
                _hullMa.Update(new IndicatorDataPoint(input.EndTime, 2 * _fastWma.Current.Value - _slowWma.Current.Value));
            }
            return _hullMa.Current.Value;
        }
    }
}
