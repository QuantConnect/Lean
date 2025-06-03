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

using QuantConnect.Data.Market;
using System;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Klinger Volume Oscillator (KVO) is a technical indicator that analyzes the relationship between
    /// price movement and trading volume to assess the strength of market trends and identify potential
    /// trend reversals. As a volume-based oscillator, it measures the force behind price movements by
    /// incorporating volume data adjusted for price trends and specific conditions. Traders use the KVO
    /// to analyze its behavior relative to price action, looking for patterns such as divergences or
    /// crossovers that can provide insights into market trends and potential turning points.
    /// </summary>
    public class KlingerVolumeOscillator : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _fastPeriod;
        private readonly int _slowPeriod;

        private readonly ExponentialMovingAverage _fastEma;
        private readonly ExponentialMovingAverage _slowEma;

        private bool _hasPrevBar;
        private decimal _previousHlc3;
        private decimal _previousDm;
        private decimal _previousCm;
        private int _previousTrend;

        /// <summary>
        /// Gets the warm-up period required for the indicator to be ready.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Gets a value indicating whether the indicator is ready and has enough data.
        /// </summary>
        public override bool IsReady => _fastEma.IsReady && _slowEma.IsReady;

        /// <summary>
        /// Initializes a new instance of the <see cref="KlingerVolumeOscillator"/> class with specified fast, slow periods.
        /// </summary>
        /// <param name="name">The name of the indicator.</param>
        /// <param name="fastPeriod">The fast EMA period.</param>
        /// <param name="slowPeriod">The slow EMA period.</param>
        public KlingerVolumeOscillator(string name, int fastPeriod, int slowPeriod)
            : base(name)
        {
            _fastPeriod = fastPeriod;
            _slowPeriod = slowPeriod;

            _fastEma = new ExponentialMovingAverage(name + "_FastEma", fastPeriod);
            _slowEma = new ExponentialMovingAverage(name + "_SlowEma", slowPeriod);

            _hasPrevBar = false;
            _previousHlc3 = 0m;
            _previousDm = 0m;
            _previousCm = 0m;
            _previousTrend = 0;

            WarmUpPeriod = Math.Max(fastPeriod, slowPeriod) + 2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KlingerVolumeOscillator"/> class with specified fast, slow periods.
        /// </summary>
        /// <param name="fastPeriod">The fast EMA period.</param>
        /// <param name="slowPeriod">The slow EMA period.</param>
        public KlingerVolumeOscillator(int fastPeriod, int slowPeriod)
            : this($"KVO({fastPeriod},{slowPeriod})", fastPeriod, slowPeriod)
        {
        }

        /// <summary>
        /// Computes the next value of the Klinger Volume Oscillator based on the input data point.
        /// </summary>
        protected override decimal ComputeNextValue(TradeBar bar)
        {
            // daily movement
            var dm = bar.High - bar.Low;
            var hlc3 = (bar.High + bar.Low + bar.Close) / 3m;

            if (!_hasPrevBar)
            {
                // First bar
                _previousDm = dm;
                _previousHlc3 = hlc3;
                _previousTrend = 0;
                _hasPrevBar = true;
                return 0m;
            }

            // trend direction
            var currentTrend = hlc3 > _previousHlc3 ? 1 : -1;

            if (_previousTrend == 0)
            {
                // Second bar
                _previousTrend = currentTrend;
                _previousDm = dm;
                _previousHlc3 = hlc3;
                return 0m;
            }

            // Cumulative measurement (cm): Accumulates daily price range (High - Low) based on trend continuity
            var cm = 0m;
            var cum = 0m;
            if (_previousCm == 0m || currentTrend != _previousTrend)
            {
                // Start new flow accumulation with the previous daily movement (dm)
                cum = _previousDm;
            }
            else
            {
                // Continue flow accumulation in the same trend direction
                cum = _previousCm;
            }
            cm = cum + dm;

            // Volume force:  strength of volume flow in the direction of the trend.
            // This implementation follows Talipp's formula.
            // Formula: volumeForce = volume * |2 * (dm / cm - 1)| * trend * 100
            // Special case: If cm is zero, volumeForce is set to 0 to prevent division by zero.
            // Reference: https://github.com/nardew/talipp/blob/70dc9a26889c9c9329e44321e1362c4db43dbcc3/talipp/indicators/KVO.py#L85
            var volumeForce = cm == 0m ? 0m : bar.Volume * Math.Abs(2m * (dm / cm - 1m)) * currentTrend * 100m;

            // update moving averages
            _fastEma.Update(new IndicatorDataPoint(bar.Time, volumeForce));
            _slowEma.Update(new IndicatorDataPoint(bar.Time, volumeForce));

            // prepare for next iteration
            _previousDm = dm;
            _previousCm = cm;
            _previousHlc3 = hlc3;
            _previousTrend = currentTrend;

            // Return KVO (fast EMA - slow EMA) if both EMAs are ready
            return _fastEma.IsReady && _slowEma.IsReady ? _fastEma.Current.Value - _slowEma.Current.Value : 0m;
        }

        public override void Reset()
        {
            base.Reset();
            _fastEma.Reset();
            _slowEma.Reset();
            _hasPrevBar = false;
            _previousDm = 0m;
            _previousCm = 0m;
            _previousHlc3 = 0m;
            _previousTrend = 0;
        }
    }
}
