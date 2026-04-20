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
    /// Represents the WaveTrend Oscillator (WTO) developed by LazyBear.
    ///
    /// The oscillator uses the typical price (HLC3) and is computed as follows:
    ///   ESA    = EMA(HLC3, channelPeriod)
    ///   D      = EMA(|HLC3 - ESA|, channelPeriod)
    ///   CI     = (HLC3 - ESA) / (0.015 * D)
    ///   WT1    = EMA(CI, averagePeriod)
    ///   WT2    = SMA(WT1, signalPeriod)
    ///
    /// The indicator's current value is WT1 (the main wave trend line).
    /// The <see cref="Signal"/> property exposes WT2, which is typically plotted as the signal line.
    /// </summary>
    public class WaveTrendOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Constant used to scale the channel index so most WTO values fall within +/- 100.
        /// </summary>
        private const decimal K = 0.015m;

        private readonly ExponentialMovingAverage _channelEma;
        private readonly ExponentialMovingAverage _meanDeviationEma;

        /// <summary>
        /// Gets the main wave trend line (WT1), computed as an EMA of the channel index.
        /// </summary>
        public ExponentialMovingAverage WaveTrend { get; }

        /// <summary>
        /// Gets the signal line (WT2), computed as an SMA of <see cref="WaveTrend"/>.
        /// </summary>
        public SimpleMovingAverage Signal { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveTrendOscillator"/> class.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="channelPeriod">The period used to smooth the typical price and its mean deviation (n1)</param>
        /// <param name="averagePeriod">The period used to smooth the channel index into the main wave trend line (n2)</param>
        /// <param name="signalPeriod">The period used to smooth the wave trend line into the signal line</param>
        public WaveTrendOscillator(string name, int channelPeriod, int averagePeriod, int signalPeriod)
            : base(name)
        {
            _channelEma = new ExponentialMovingAverage(name + "_ChannelEMA", channelPeriod);
            _meanDeviationEma = new ExponentialMovingAverage(name + "_MeanDeviationEMA", channelPeriod);
            WaveTrend = new ExponentialMovingAverage(name + "_WaveTrend", averagePeriod);
            Signal = new SimpleMovingAverage(name + "_Signal", signalPeriod);

            WarmUpPeriod = 2 * channelPeriod + averagePeriod + signalPeriod - 3;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveTrendOscillator"/> class using default parameters (10, 21, 4).
        /// </summary>
        /// <param name="channelPeriod">The period used to smooth the typical price and its mean deviation (n1)</param>
        /// <param name="averagePeriod">The period used to smooth the channel index into the main wave trend line (n2)</param>
        /// <param name="signalPeriod">The period used to smooth the wave trend line into the signal line</param>
        public WaveTrendOscillator(int channelPeriod = 10, int averagePeriod = 21, int signalPeriod = 4)
            : this($"WTO({channelPeriod},{averagePeriod},{signalPeriod})", channelPeriod, averagePeriod, signalPeriod)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized.
        /// </summary>
        public override bool IsReady => Signal.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Computes the next value of this indicator from the given state.
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The current value of the main wave trend line (WT1)</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var typicalPrice = (input.High + input.Low + input.Close) / 3.0m;

            _channelEma.Update(input.EndTime, typicalPrice);
            if (!_channelEma.IsReady)
            {
                return 0m;
            }

            var absDeviation = Math.Abs(typicalPrice - _channelEma.Current.Value);
            _meanDeviationEma.Update(input.EndTime, absDeviation);
            if (!_meanDeviationEma.IsReady)
            {
                return 0m;
            }

            var weightedMeanDeviation = K * _meanDeviationEma.Current.Value;
            if (weightedMeanDeviation == 0m)
            {
                return WaveTrend.Current.Value;
            }

            var channelIndex = (typicalPrice - _channelEma.Current.Value) / weightedMeanDeviation;
            WaveTrend.Update(input.EndTime, channelIndex);
            if (!WaveTrend.IsReady)
            {
                return WaveTrend.Current.Value;
            }

            Signal.Update(input.EndTime, WaveTrend.Current.Value);
            return WaveTrend.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state.
        /// </summary>
        public override void Reset()
        {
            _channelEma.Reset();
            _meanDeviationEma.Reset();
            WaveTrend.Reset();
            Signal.Reset();
            base.Reset();
        }
    }
}
