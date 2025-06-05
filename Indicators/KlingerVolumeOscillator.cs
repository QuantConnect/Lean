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
        private readonly ExponentialMovingAverage _fastEma;
        private readonly ExponentialMovingAverage _slowEma;

        private readonly RollingWindow<decimal> _priceIndex;
        private readonly RollingWindow<decimal> _rangeWindow;
        private readonly RollingWindow<int> _trendWindow;
        private decimal _cumulativeMovement;

        // Minimum cumulative movement value to avoid division by zero and near zero in volume force calculation
        private const decimal MinCumulativeForDivision = 1e-8m;

        /// <summary>
        /// Gets the public signal line (EMA of KVO)
        /// </summary>
        public ExponentialMovingAverage Signal { get; }

        /// <summary>
        /// Gets the warm-up period required for the indicator to be ready.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Gets a value indicating whether the indicator is ready and has enough data.
        /// </summary>
        public override bool IsReady => _fastEma.IsReady && _slowEma.IsReady && Signal.IsReady;

        /// <summary>
        /// Initializes a new instance of the <see cref="KlingerVolumeOscillator"/> class with specified fast, slow periods.
        /// </summary>
        /// <param name="name">The name of the indicator.</param>
        /// <param name="fastPeriod">The fast EMA period.</param>
        /// <param name="slowPeriod">The slow EMA period.</param>
        /// <param name="signalPeriod">The signal line period.</param>
        public KlingerVolumeOscillator(string name, int fastPeriod, int slowPeriod, int signalPeriod)
            : base(name)
        {
            _fastEma = new ExponentialMovingAverage(name + "_FastEma", fastPeriod);
            _slowEma = new ExponentialMovingAverage(name + "_SlowEma", slowPeriod);
            Signal = new ExponentialMovingAverage(name + "_Signal", signalPeriod);

            _priceIndex = new RollingWindow<decimal>(2);
            _rangeWindow = new RollingWindow<decimal>(2);
            _trendWindow = new RollingWindow<int>(2);

            WarmUpPeriod = Math.Max(fastPeriod, Math.Max(slowPeriod, signalPeriod)) + 2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KlingerVolumeOscillator"/> class with specified fast, slow periods.
        /// </summary>
        /// <param name="fastPeriod">The fast EMA period.</param>
        /// <param name="slowPeriod">The slow EMA period.</param>
        /// <param name="signalPeriod">The signal line period (default is 13).</param>
        public KlingerVolumeOscillator(int fastPeriod, int slowPeriod, int signalPeriod = 13)
            : this($"KVO({fastPeriod},{slowPeriod},{signalPeriod})", fastPeriod, slowPeriod, signalPeriod)
        {
        }

        /// <summary>
        /// Computes the next value of the Klinger Volume Oscillator based on the input data point.
        /// </summary>
        protected override decimal ComputeNextValue(TradeBar bar)
        {
            // daily movement range
            var todaysMovement = bar.High - bar.Low;
            _rangeWindow.Add(todaysMovement);

            // price index value, used to compare current and previous price trends
            var hlc = bar.High + bar.Low + bar.Close;
            _priceIndex.Add(hlc);

            if (!_priceIndex.IsReady)
            {
                // Not enough data
                return 0m;
            }

            // determine if the price trend is going up or down, 1 for up, -1 for down
            var currentTrend = _priceIndex[0] > _priceIndex[1] ? 1 : -1;
            _trendWindow.Add(currentTrend);

            if (!_trendWindow.IsReady)
            {
                // Not enough data
                return 0m;
            }

            // Data is ready to calculate KVO
            var hasMovement = _cumulativeMovement != 0;
            var trendChanged = _trendWindow[0] != _trendWindow[1];
            var yesterdaysRange = _rangeWindow[1];

            if (!hasMovement || trendChanged)
            {
                // Start new flow accumulation with the previous daily movement
                _cumulativeMovement = todaysMovement + yesterdaysRange;
            }
            else
            {
                // Continue flow accumulation in the same trend direction
                _cumulativeMovement += todaysMovement;
            }

            // Volume force:  strength of volume flow in the direction of the trend.
            // There are various definitions of volume force, this is what we used in our implementation:
            // https://github.com/nardew/talipp/blob/70dc9a26889c9c9329e44321e1362c4db43dbcc3/talipp/indicators/KVO.py#L85
            // https://www.tradingview.com/support/solutions/43000589157-klinger-oscillator/
            // Protect from division by zero and near zero from blowing up the volume force calculation
            var denom = Math.Abs(_cumulativeMovement) < MinCumulativeForDivision ? MinCumulativeForDivision : _cumulativeMovement;
            var volumeForce = bar.Volume * Math.Abs(2m * (todaysMovement / denom - 1m)) * currentTrend * 100m;

            // update moving averages
            var dataPoint = new IndicatorDataPoint(bar.EndTime, volumeForce);
            _fastEma.Update(dataPoint);
            _slowEma.Update(dataPoint);

            if (!_fastEma.IsReady || !_slowEma.IsReady)
            {
                Signal.Update(new IndicatorDataPoint(bar.EndTime, 0m));
                return 0m;
            }

            // Calculate KVO value as the difference between fast and slow EMAs
            var kvo = _fastEma.Current.Value - _slowEma.Current.Value;
            Signal.Update(new IndicatorDataPoint(bar.EndTime, kvo));

            return kvo;
        }

        /// <summary>
        /// Resets the indicator to its initial state.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Signal.Reset();
            _fastEma.Reset();
            _slowEma.Reset();
            _priceIndex.Reset();
            _rangeWindow.Reset();
            _trendWindow.Reset();
            _cumulativeMovement = 0m;
        }
    }
}
