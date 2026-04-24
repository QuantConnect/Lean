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
    /// This indicator computes the WaveTrend Oscillator (WTO), a momentum oscillator
    /// that measures how far a smoothed typical price has deviated from its own moving
    /// average, normalized by the average absolute deviation. The main line (TCI) is a
    /// further exponential smoothing of that normalized channel index, and the Signal
    /// line (WT2) is a short simple moving average of the main line.
    /// <para/>
    /// Formula:
    ///   hlc3 = (High + Low + Close) / 3
    ///   esa  = EMA(hlc3, channelPeriod)
    ///   d    = EMA(|esa - hlc3|, channelPeriod)
    ///   ci   = (hlc3 - esa) / (0.015 * d)
    ///   tci  = EMA(ci, averagePeriod)  (main line)
    ///   wt2  = SMA(tci, signalPeriod)  (signal line)
    /// </summary>
    /// <remarks>
    /// Reference: https://www.tradingview.com/script/2KE8wTuF-Indicator-WaveTrend-Oscillator-WT/
    /// </remarks>
    public class WaveTrendOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly ExponentialMovingAverage _esa;
        private readonly ExponentialMovingAverage _d;
        private readonly ExponentialMovingAverage _tci;

        /// <summary>
        /// Gets the signal line (WT2), a simple moving average of the main line (TCI).
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Signal { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= WarmUpPeriod;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveTrendOscillator"/> class using the specified name and periods.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="channelPeriod">The period used to compute the Average Smoothed Average (ESA) and its mean absolute deviation</param>
        /// <param name="averagePeriod">The period used to smooth the normalized channel index into the main line (TCI)</param>
        /// <param name="signalPeriod">The period used to compute the signal line as a simple moving average of the main line</param>
        public WaveTrendOscillator(string name, int channelPeriod, int averagePeriod, int signalPeriod)
            : base(name)
        {
            _esa = new ExponentialMovingAverage(name + "_ESA", channelPeriod);
            _d = new ExponentialMovingAverage(name + "_D", channelPeriod);
            _tci = new ExponentialMovingAverage(name + "_TCI", averagePeriod);
            Signal = new SimpleMovingAverage(name + "_Signal", signalPeriod).Of(_tci, true);
            WarmUpPeriod = 2 * channelPeriod + averagePeriod - 2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveTrendOscillator"/> class using the specified periods.
        /// </summary>
        /// <param name="channelPeriod">The period used to compute the Average Smoothed Average (ESA) and its mean absolute deviation</param>
        /// <param name="averagePeriod">The period used to smooth the normalized channel index into the main line (TCI)</param>
        /// <param name="signalPeriod">The period used to compute the signal line as a simple moving average of the main line</param>
        public WaveTrendOscillator(int channelPeriod = 10, int averagePeriod = 21, int signalPeriod = 4)
            : this($"WTO({channelPeriod},{averagePeriod},{signalPeriod})", channelPeriod, averagePeriod, signalPeriod)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The main line (TCI) value for this bar</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var hlc3 = (input.High + input.Low + input.Close) / 3m;
            _esa.Update(input.EndTime, hlc3);

            if (!_esa.IsReady)
            {
                return 0m;
            }

            _d.Update(input.EndTime, Math.Abs(_esa - hlc3));

            if (!_d.IsReady)
            {
                return _tci;
            }

            // 0.015 * EMA can underflow to zero in decimal when bars are nearly
            // constant (e.g. consolidated VolumeRenko), so guard the denominator
            // rather than the EMA itself.
            var denominator = 0.015m * _d.Current.Value;
            if (denominator == 0m)
            {
                return _tci;
            }

            var ci = (hlc3 - _esa) / denominator;
            _tci.Update(input.EndTime, ci);

            return _tci;
        }

        /// <summary>
        /// Resets this indicator to its initial state
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
