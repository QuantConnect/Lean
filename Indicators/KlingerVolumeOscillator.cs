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

        private readonly RollingWindow<decimal> _price;
        private readonly RollingWindow<decimal> _dailyMovement;
        private readonly RollingWindow<decimal> _comulativeMovement;
        private readonly RollingWindow<int> _trendDirection;


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
        public override bool IsReady => _fastEma.IsReady && _slowEma.IsReady;

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

            _price = new RollingWindow<decimal>(2);
            _dailyMovement = new RollingWindow<decimal>(2);
            _comulativeMovement = new RollingWindow<decimal>(2);
            _trendDirection = new RollingWindow<int>(2);

            WarmUpPeriod = Math.Max(fastPeriod, slowPeriod) + 2;
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
            // daily movement
            var todaysMovement = bar.High - bar.Low;

            // price index value
            var hlc3 = (bar.High + bar.Low + bar.Close) / 3m;

            _price.Add(hlc3);
            _dailyMovement.Add(todaysMovement);

            if (_price.Count < 2)
            {
                // Not enough data
                _comulativeMovement.Add(0m);
                return 0m;
            }

            var currentTrend = _price[0] > _price[1] ? 1 : -1;
            _trendDirection.Add(currentTrend);

            if (_trendDirection.Count < 2)
            {
                // Not enough data
                _comulativeMovement.Add(0);
                return 0m;
            }

            // Data is ready to calculate KVO
            var hasMovement = _comulativeMovement[0] != 0;
            var trendChanged = _trendDirection[0] != _trendDirection[1];
            var trendMovement = todaysMovement;

            if (!hasMovement || trendChanged)
            {
                // Start new flow accumulation with the previous daily movement
                trendMovement += _dailyMovement[1];
            }
            else
            {
                // Continue flow accumulation in the same trend direction
                trendMovement += _comulativeMovement[0];
            }
            _comulativeMovement.Add(trendMovement);

            // Volume force:  strength of volume flow in the direction of the trend.
            // There are various definitions of volume force, this is what we used in our implementation:
            // https://github.com/nardew/talipp/blob/70dc9a26889c9c9329e44321e1362c4db43dbcc3/talipp/indicators/KVO.py#L85
            // https://www.tradingview.com/support/solutions/43000589157-klinger-oscillator/
            var volumeForce = trendMovement == 0m ? 0m : bar.Volume * Math.Abs(2m * (todaysMovement / _comulativeMovement[0] - 1m)) * currentTrend * 100m;

            // update moving averages
            _fastEma.Update(new IndicatorDataPoint(bar.EndTime, volumeForce));
            _slowEma.Update(new IndicatorDataPoint(bar.EndTime, volumeForce));

            // Calculate KVO value as the difference between fast and slow EMAs
            var kvo = _fastEma.IsReady && _slowEma.IsReady
                ? _fastEma.Current.Value - _slowEma.Current.Value
                : 0m;
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
            _price.Reset();
            _dailyMovement.Reset();
            _comulativeMovement.Reset();
            _trendDirection.Reset();
        }

    }
}
