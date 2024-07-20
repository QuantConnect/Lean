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
using System.Linq;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The ChoppinessIndex indicator is an indicator designed to determine if the market is choppy (trading sideways)
    /// or not choppy (trading within a trend in either direction)
    /// </summary>
    public class ChoppinessIndex : BarIndicator, IIndicatorWarmUpPeriodProvider
    {

        private readonly int _period;
        private readonly RollingWindow<decimal> _highs;
        private readonly RollingWindow<decimal> _lows;
        private readonly IndicatorBase<IBaseDataBar> _trueRange;
        private readonly RollingWindow<decimal> _trueRangeHistory;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= WarmUpPeriod;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates a new ChoppinessIndex indicator using the specified period and moving average type
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period used for rolling windows for highs and lows</param>
        public ChoppinessIndex(string name, int period)
            : base(name)
        {
            _period = period;
            WarmUpPeriod = period;

            _trueRange = new TrueRange();
            _trueRangeHistory = new RollingWindow<decimal>(period);

            _highs = new RollingWindow<decimal>(period);
            _lows = new RollingWindow<decimal>(period);
        }

        /// <summary>
        /// Creates a new ChoppinessIndex indicator using the specified period
        /// </summary>
        /// <param name="period">The period used for rolling windows for highs and lows</param>
        public ChoppinessIndex(int period)
            : this($"CHOP({period})", period)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            // compute the true range
            _trueRange.Update(input);

            // store candle high and low
            _highs.Add(input.High);
            _lows.Add(input.Low);

            // store true range in rolling window
            if (_trueRange.IsReady)
            {
                _trueRangeHistory.Add(_trueRange.Current.Value);
            }
            else
            {
                _trueRangeHistory.Add(input.High - input.Low);
            }           
            if (!IsReady)
            {
                return 0m;
            }

            // calculate max high and min low
            var maxHigh = _highs.Max();
            var minLow = _lows.Min();

            if (IsReady)
            {
                if (maxHigh != minLow)
                {
                    // return CHOP index
                    return (decimal)(100.0 * Math.Log10(((double) _trueRangeHistory.Sum()) / ((double) (maxHigh - minLow))) / Math.Log10(_period));
                }
                else
                {
                    // situation of maxHigh = minLow represents a totally "choppy" or stagnant market,
                    // with no price movement at all.
                    // It's the extreme case of consolidation, hence the maximum value of 100 for the index
                    return 100m;
                }
            }
            else
            {
                // return 0 when indicator is not ready
                return 0m;
            }
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _trueRange.Reset();
            _trueRangeHistory.Reset();
            _highs.Reset();
            _lows.Reset();
            base.Reset();
        }
    }
}
