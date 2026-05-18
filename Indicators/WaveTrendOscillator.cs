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
    /// The WaveTrend Oscillator (WTO) is a momentum indicator that highlights overbought
    /// and oversold conditions by measuring how far the typical price has deviated from a
    /// smoothed moving average, normalized by an exponentially smoothed mean absolute
    /// deviation. The oscillator's main line (WT1) is an EMA of this normalized channel
    /// index, and the signal line (WT2) is an SMA of WT1; crossovers between the two
    /// lines are commonly used as entry and exit signals.
    ///
    /// Formula:
    ///     HLC3 = (High + Low + Close) / 3
    ///     ESA  = EMA(HLC3, channelPeriod)
    ///     D    = EMA(|HLC3 - ESA|, channelPeriod)
    ///     CI   = (HLC3 - ESA) / (0.015 * D)
    ///     WT1  = EMA(CI, averagePeriod)              (the indicator's Current.Value)
    ///     WT2  = SMA(WT1, signalPeriod)              (exposed via <see cref="Signal"/>)
    /// </summary>
    public class WaveTrendOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Scaling constant that keeps the channel index roughly within +/-100 most of the time,
        /// matching the original Lambert/CCI normalization convention.
        /// </summary>
        private const decimal NormalizationConstant = 0.015m;

        /// <summary>
        /// Gets the EMA of the typical price (ESA in the original WaveTrend formulation).
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> ChannelAverage { get; }

        /// <summary>
        /// Gets the EMA of the absolute deviation between the typical price and <see cref="ChannelAverage"/>.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> ChannelDeviation { get; }

        /// <summary>
        /// Gets the smoothed channel index (WT1): an EMA of the normalized channel index.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> ChannelIndexAverage { get; }

        /// <summary>
        /// Gets the signal line (WT2): a simple moving average of <see cref="ChannelIndexAverage"/>.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Signal { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized.
        /// </summary>
        public override bool IsReady => Signal.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveTrendOscillator"/> class.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="channelPeriod">The smoothing period for the typical-price EMA and the deviation EMA (n1)</param>
        /// <param name="averagePeriod">The EMA period applied to the channel index to produce WT1 (n2)</param>
        /// <param name="signalPeriod">The SMA period applied to WT1 to produce the WT2 signal line (n3)</param>
        public WaveTrendOscillator(string name, int channelPeriod, int averagePeriod, int signalPeriod)
            : base(name)
        {
            if (channelPeriod < 1 || averagePeriod < 1 || signalPeriod < 1)
            {
                throw new ArgumentException("WaveTrendOscillator: all periods must be greater than zero.");
            }

            ChannelAverage = new ExponentialMovingAverage(name + "_ChannelAverage", channelPeriod);
            ChannelDeviation = new ExponentialMovingAverage(name + "_ChannelDeviation", channelPeriod);
            ChannelIndexAverage = new ExponentialMovingAverage(name + "_ChannelIndexAverage", averagePeriod);
            Signal = new SimpleMovingAverage(name + "_Signal", signalPeriod);
            // The chain ESA -> D -> WT1 -> WT2 only advances each sub-indicator once the
            // upstream one is ready, so the total warm-up is the sum of the chained periods
            // minus three for the overlap on each transition.
            WarmUpPeriod = 2 * channelPeriod + averagePeriod + signalPeriod - 3;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveTrendOscillator"/> class with the default name.
        /// </summary>
        /// <param name="channelPeriod">The smoothing period for the typical-price EMA and the deviation EMA (n1)</param>
        /// <param name="averagePeriod">The EMA period applied to the channel index to produce WT1 (n2)</param>
        /// <param name="signalPeriod">The SMA period applied to WT1 to produce the WT2 signal line (n3)</param>
        public WaveTrendOscillator(int channelPeriod, int averagePeriod, int signalPeriod)
            : this($"WTO({channelPeriod},{averagePeriod},{signalPeriod})", channelPeriod, averagePeriod, signalPeriod)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given bar.
        /// </summary>
        /// <param name="input">The input bar</param>
        /// <returns>The next WT1 value (EMA of the channel index)</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var typicalPrice = (input.High + input.Low + input.Close) / 3m;

            if (!ChannelAverage.Update(input.EndTime, typicalPrice))
            {
                return 0m;
            }

            var deviation = Math.Abs(typicalPrice - ChannelAverage);
            if (!ChannelDeviation.Update(input.EndTime, deviation))
            {
                return 0m;
            }

            var weightedDeviation = NormalizationConstant * ChannelDeviation;
            if (weightedDeviation == 0m)
            {
                return Current.Value;
            }

            var channelIndex = (typicalPrice - ChannelAverage) / weightedDeviation;
            if (!ChannelIndexAverage.Update(input.EndTime, channelIndex))
            {
                return ChannelIndexAverage.Current.Value;
            }

            Signal.Update(input.EndTime, ChannelIndexAverage.Current.Value);
            return ChannelIndexAverage.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state.
        /// </summary>
        public override void Reset()
        {
            ChannelAverage.Reset();
            ChannelDeviation.Reset();
            ChannelIndexAverage.Reset();
            Signal.Reset();
            base.Reset();
        }
    }
}
