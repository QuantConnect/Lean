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
    /// Represents the WaveTrend Oscillator, a momentum indicator popularized by LazyBear
    /// on TradingView. It is built from the distance between the bar's typical price and
    /// an exponential moving average of that price, normalized by the average distance,
    /// and then smoothed twice.
    /// <para>
    /// HLC3 = (High + Low + Close) / 3
    /// ESA  = EMA(HLC3, channelPeriod)
    /// D    = EMA(|HLC3 - ESA|, channelPeriod)
    /// CI   = (HLC3 - ESA) / (0.015 * D)
    /// WT1  = EMA(CI, averagePeriod)
    /// WT2  = SMA(WT1, signalPeriod)
    /// </para>
    /// <see cref="Current"/> exposes the WaveTrend line (WT1) and <see cref="Signal"/>
    /// exposes the signal line (WT2). Crossovers between the two are the typical entry
    /// and exit triggers for this indicator.
    /// </summary>
    public class WaveTrendOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly ExponentialMovingAverage _esa;
        private readonly ExponentialMovingAverage _d;

        /// <summary>
        /// Gets the WaveTrend line (WT1), the exponential moving average of the
        /// normalized channel index.
        /// </summary>
        public ExponentialMovingAverage WT1 { get; }

        /// <summary>
        /// Gets the signal line (WT2), the simple moving average of <see cref="WT1"/>.
        /// </summary>
        public SimpleMovingAverage Signal { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized.
        /// </summary>
        public override bool IsReady => Signal.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveTrendOscillator"/> class with
        /// the specified name and periods.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="channelPeriod">The smoothing period used for the channel EMAs (ESA and D)</param>
        /// <param name="averagePeriod">The smoothing period used for WT1</param>
        /// <param name="signalPeriod">The period of the signal line (simple moving average of WT1)</param>
        public WaveTrendOscillator(string name, int channelPeriod, int averagePeriod, int signalPeriod)
            : base(name)
        {
            _esa = new ExponentialMovingAverage($"{name}_ESA", channelPeriod);
            _d = new ExponentialMovingAverage($"{name}_D", channelPeriod);
            WT1 = new ExponentialMovingAverage($"{name}_WT1", averagePeriod);
            Signal = new SimpleMovingAverage($"{name}_WT2", signalPeriod);
            WarmUpPeriod = 2 * channelPeriod + averagePeriod + signalPeriod - 3;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveTrendOscillator"/> class with
        /// the specified periods.
        /// </summary>
        /// <param name="channelPeriod">The smoothing period used for the channel EMAs (ESA and D)</param>
        /// <param name="averagePeriod">The smoothing period used for WT1</param>
        /// <param name="signalPeriod">The period of the signal line (simple moving average of WT1)</param>
        public WaveTrendOscillator(int channelPeriod, int averagePeriod, int signalPeriod)
            : this($"WTO({channelPeriod},{averagePeriod},{signalPeriod})", channelPeriod, averagePeriod, signalPeriod)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state.
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var hlc3 = (input.High + input.Low + input.Close) / 3m;

            _esa.Update(input.EndTime, hlc3);

            if (!_esa.IsReady)
            {
                return 0m;
            }

            _d.Update(input.EndTime, Math.Abs(hlc3 - _esa.Current.Value));

            if (!_d.IsReady)
            {
                return 0m;
            }

            var denominator = 0.015m * _d.Current.Value;
            if (denominator == 0m)
            {
                return 0m;
            }

            var ci = (hlc3 - _esa.Current.Value) / denominator;
            WT1.Update(input.EndTime, ci);

            if (!WT1.IsReady)
            {
                return WT1.Current.Value;
            }

            Signal.Update(input.EndTime, WT1.Current.Value);

            return WT1.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state.
        /// </summary>
        public override void Reset()
        {
            _esa.Reset();
            _d.Reset();
            WT1.Reset();
            Signal.Reset();
            base.Reset();
        }
    }
}
