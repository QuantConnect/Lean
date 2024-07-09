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
        private readonly RollingWindow<float> _highs;
        private readonly RollingWindow<float> _lows;
        private IndicatorBase<IBaseDataBar> _trueRange;
        private readonly RollingWindow<float> _trueRangeHistory;

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
        /// <param name="period">The smoothing period used to smooth the true range values</param>
        public ChoppinessIndex(string name, int period)
            : base(name)
        {
            _period = period;
            WarmUpPeriod = period;

            _trueRange = new TrueRange();
            _trueRangeHistory = new RollingWindow<float>(period);

            _highs = new RollingWindow<float>(period);
            _lows = new RollingWindow<float>(period);
        }

        /// <summary>
        /// Creates a new ChoppinessIndex indicator using the specified period
        /// </summary>
        /// <param name="period">The smoothing period used to smooth the true range values</param>
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
            _highs.Add((float)input.High);
            _lows.Add((float)input.Low);

            // store true range in rolling window
            if (_trueRange.IsReady) {
                _trueRangeHistory.Add((float)_trueRange.Current.Value);
            }
            else
            {
                _trueRangeHistory.Add((float)(input.High - input.Low));
            }           
            if (!IsReady)
            {
                return 0;
            }

            // calculate max high and min low
            var max_high = _highs.Max();
            var min_low = _lows.Min();

            if (IsReady)
            {
                if (max_high != min_low)
                {
                    // return CHOP index
                    return (decimal)(100.0 * Math.Log10(_trueRangeHistory.Sum() / (max_high - min_low)) / Math.Log10(_period));
                }
                else
                {
                    // return a sentinel value when max_high == min_low
                    return decimal.MinValue;
                }
            }
            else
            {
                // return 0 when indicator is not ready
                return 0;
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
