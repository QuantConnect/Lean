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
    /// WaveTrend Oscillator (WTO) as popularized by LazyBear on TradingView.
    /// It produces a main oscillator line (WT1) and a smoothed signal line (WT2).
    /// The formula applies two nested exponential moving averages to a Commodity
    /// Channel Index-like transformation of the typical price, then smooths the
    /// result with a simple moving average to derive the signal line:
    ///   hlc3 = (High + Low + Close) / 3
    ///   esa  = EMA(hlc3, channelPeriod)
    ///   d    = EMA(|esa - hlc3|, channelPeriod)
    ///   ci   = (hlc3 - esa) / (0.015 * d)
    ///   WT1  = EMA(ci, averagePeriod)
    ///   WT2  = SMA(WT1, signalPeriod)
    /// </summary>
    public class WaveTrendOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly ExponentialMovingAverage _esa;
        private readonly ExponentialMovingAverage _d;
        private readonly ExponentialMovingAverage _tci;

        /// <summary>
        /// Gets the signal line (WT2): the simple moving average of WT1.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Signal { get; }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized.
        /// </summary>
        public override bool IsReady => Signal.IsReady;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveTrendOscillator"/> class.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="channelPeriod">The channel length used by the EMA that smooths the typical price</param>
        /// <param name="averagePeriod">The average length used by the EMA applied to the CCI-like series (produces WT1)</param>
        /// <param name="signalPeriod">The period of the simple moving average applied to WT1 to produce WT2</param>
        public WaveTrendOscillator(string name, int channelPeriod, int averagePeriod, int signalPeriod)
            : base(name)
        {
            _esa = new ExponentialMovingAverage(name + "_ESA", channelPeriod);
            _d = new ExponentialMovingAverage(name + "_D", channelPeriod);
            _tci = new ExponentialMovingAverage(name + "_TCI", averagePeriod);
            Signal = new SimpleMovingAverage(name + "_Signal", signalPeriod);
            WarmUpPeriod = 2 * channelPeriod + averagePeriod + signalPeriod - 3;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveTrendOscillator"/> class with the default name format.
        /// </summary>
        /// <param name="channelPeriod">The channel length used by the EMA that smooths the typical price</param>
        /// <param name="averagePeriod">The average length used by the EMA applied to the CCI-like series (produces WT1)</param>
        /// <param name="signalPeriod">The period of the simple moving average applied to WT1 to produce WT2</param>
        public WaveTrendOscillator(int channelPeriod, int averagePeriod, int signalPeriod)
            : this($"WTO({channelPeriod},{averagePeriod},{signalPeriod})", channelPeriod, averagePeriod, signalPeriod)
        {
        }

        /// <summary>
        /// Computes the next value (WT1) of this indicator from the given bar.
        /// </summary>
        /// <param name="input">The input bar providing High, Low and Close</param>
        /// <returns>The current value of WT1</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var hlc3 = (input.High + input.Low + input.Close) / 3m;

            _esa.Update(input.EndTime, hlc3);
            if (!_esa.IsReady)
            {
                return 0m;
            }

            _d.Update(input.EndTime, Math.Abs(_esa.Current.Value - hlc3));
            var divisor = 0.015m * _d.Current.Value;
            if (!_d.IsReady || divisor == 0m)
            {
                return 0m;
            }

            var ci = (hlc3 - _esa.Current.Value) / divisor;
            _tci.Update(input.EndTime, ci);
            if (!_tci.IsReady)
            {
                return 0m;
            }

            Signal.Update(input.EndTime, _tci.Current.Value);
            return _tci.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state.
        /// </summary>
        public override void Reset()
        {
            _esa.Reset();
            _d.Reset();
            _tci.Reset();
            Signal.Reset();
            base.Reset();
        }
    }
}
