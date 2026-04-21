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
    /// The WaveTrend Oscillator, popularized by LazyBear on TradingView, is a momentum
    /// oscillator derived from the distance between the typical price (HLC3) and its
    /// exponential moving average, normalized by the average of that distance and
    /// then smoothed. The indicator's main output (<see cref="IndicatorBase.Current"/>)
    /// is the WT1 line; the signal line WT2 is exposed via <see cref="Signal"/>.
    /// <para>
    /// HLC3 = (High + Low + Close) / 3
    /// ESA  = EMA(HLC3, channelPeriod)
    /// D    = EMA(|HLC3 - ESA|, channelPeriod)
    /// CI   = (HLC3 - ESA) / (0.015 * D)
    /// WT1  = EMA(CI, averagePeriod)
    /// WT2  = SMA(WT1, signalPeriod)
    /// </para>
    /// </summary>
    public class WaveTrendOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly ExponentialMovingAverage _esa;
        private readonly ExponentialMovingAverage _d;
        private readonly ExponentialMovingAverage _wt1;

        /// <summary>
        /// Gets the signal line (WT2), the simple moving average of WT1.
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
        /// Creates a new WaveTrend Oscillator with the specified name and periods.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="channelPeriod">The period of the channel EMAs (ESA and D)</param>
        /// <param name="averagePeriod">The period of the WT1 EMA</param>
        /// <param name="signalPeriod">The period of the WT2 SMA (signal line)</param>
        public WaveTrendOscillator(string name, int channelPeriod, int averagePeriod, int signalPeriod)
            : base(name)
        {
            _esa = new ExponentialMovingAverage(name + "_ESA", channelPeriod);
            _d = new ExponentialMovingAverage(name + "_D", channelPeriod);
            _wt1 = new ExponentialMovingAverage(name + "_WT1", averagePeriod);
            Signal = new SimpleMovingAverage(name + "_WT2", signalPeriod);
            WarmUpPeriod = 2 * channelPeriod + averagePeriod + signalPeriod - 3;
        }

        /// <summary>
        /// Creates a new WaveTrend Oscillator with the specified periods.
        /// </summary>
        /// <param name="channelPeriod">The period of the channel EMAs (ESA and D)</param>
        /// <param name="averagePeriod">The period of the WT1 EMA</param>
        /// <param name="signalPeriod">The period of the WT2 SMA (signal line)</param>
        public WaveTrendOscillator(int channelPeriod, int averagePeriod, int signalPeriod)
            : this($"WTO({channelPeriod},{averagePeriod},{signalPeriod})", channelPeriod, averagePeriod, signalPeriod)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state.
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The WT1 value</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var hlc3 = (input.High + input.Low + input.Close) / 3m;

            if (!_esa.Update(input.EndTime, hlc3))
            {
                return 0m;
            }

            if (!_d.Update(input.EndTime, Math.Abs(hlc3 - _esa)))
            {
                return 0m;
            }

            var denominator = 0.015m * _d;
            var ci = denominator == 0m ? 0m : (hlc3 - _esa) / denominator;

            if (_wt1.Update(input.EndTime, ci))
            {
                Signal.Update(input.EndTime, _wt1);
            }

            return _wt1;
        }

        /// <summary>
        /// Resets this indicator to its initial state.
        /// </summary>
        public override void Reset()
        {
            _esa.Reset();
            _d.Reset();
            _wt1.Reset();
            Signal.Reset();
            base.Reset();
        }
    }
}
