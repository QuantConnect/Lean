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
    /// This indicator computes the WaveTrend Oscillator (WTO), a momentum oscillator used to identify
    /// overbought and oversold market conditions as well as potential trend reversals through crossovers
    /// of its two output lines. The oscillator is derived from a double-smoothed deviation of the typical
    /// price (HLC/3) against an exponential moving average of that price.
    /// <para>
    /// Calculation:
    /// <code>
    /// HLC3 = (High + Low + Close) / 3
    /// ESA  = EMA(HLC3, channelPeriod)
    /// D    = EMA(|HLC3 - ESA|, channelPeriod)
    /// CI   = (HLC3 - ESA) / (0.015 * D)
    /// WT1  = EMA(CI, averagePeriod)      (this indicator's Current value)
    /// WT2  = SMA(WT1, signalPeriod)      (the signal line)
    /// </code>
    /// </para>
    /// Reference: https://www.tradingview.com/script/2KE8wTuF-Indicator-WaveTrend-Oscillator-WT/
    /// </summary>
    public class WaveTrendOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly ExponentialMovingAverage _esa;
        private readonly ExponentialMovingAverage _absoluteDeviation;
        private readonly ExponentialMovingAverage _wt1;

        /// <summary>
        /// The signal line (WT2), computed as the simple moving average of WT1 over <c>signalPeriod</c> bars.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Signal { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Signal.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveTrendOscillator"/> class using the specified parameters.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="channelPeriod">The period of the channel EMAs (ESA and absolute deviation), default is 10</param>
        /// <param name="averagePeriod">The period of the EMA used to smooth the channel index into WT1, default is 21</param>
        /// <param name="signalPeriod">The period of the SMA used to build the signal line WT2, default is 4</param>
        public WaveTrendOscillator(string name, int channelPeriod = 10, int averagePeriod = 21, int signalPeriod = 4)
            : base(name)
        {
            _esa = new ExponentialMovingAverage($"{name}_ESA", channelPeriod);
            _absoluteDeviation = new ExponentialMovingAverage($"{name}_D", channelPeriod);
            _wt1 = new ExponentialMovingAverage($"{name}_WT1", averagePeriod);
            Signal = new SimpleMovingAverage($"{name}_WT2", signalPeriod);

            // ESA warms up after channelPeriod bars. From that point the absolute deviation EMA is fed,
            // so it warms up channelPeriod bars later. WT1 then starts receiving the channel index and
            // warms up averagePeriod bars after that, and finally the signal SMA warms up signalPeriod
            // bars later. Each downstream indicator emits its first value on the same bar its upstream
            // becomes ready, so the total warm-up is the sum minus three overlapping bars.
            WarmUpPeriod = 2 * channelPeriod + averagePeriod + signalPeriod - 3;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveTrendOscillator"/> class using the specified parameters.
        /// </summary>
        /// <param name="channelPeriod">The period of the channel EMAs (ESA and absolute deviation), default is 10</param>
        /// <param name="averagePeriod">The period of the EMA used to smooth the channel index into WT1, default is 21</param>
        /// <param name="signalPeriod">The period of the SMA used to build the signal line WT2, default is 4</param>
        public WaveTrendOscillator(int channelPeriod = 10, int averagePeriod = 21, int signalPeriod = 4)
            : this($"WTO({channelPeriod},{averagePeriod},{signalPeriod})", channelPeriod, averagePeriod, signalPeriod)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
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

            _absoluteDeviation.Update(input.EndTime, Math.Abs(hlc3 - _esa.Current.Value));

            if (!_absoluteDeviation.IsReady)
            {
                return 0m;
            }

            var denominator = 0.015m * _absoluteDeviation.Current.Value;
            if (denominator == 0m)
            {
                return 0m;
            }

            var channelIndex = (hlc3 - _esa.Current.Value) / denominator;
            _wt1.Update(input.EndTime, channelIndex);

            if (!_wt1.IsReady)
            {
                return 0m;
            }

            Signal.Update(input.EndTime, _wt1.Current.Value);
            return _wt1.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _esa.Reset();
            _absoluteDeviation.Reset();
            _wt1.Reset();
            Signal.Reset();
            base.Reset();
        }
    }
}
