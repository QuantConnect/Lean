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
    /// The Smoothed Force Index (SFX) is a composite volatility indicator.
    /// It combines the Average True Range (ATR), Standard Deviation of close prices,
    /// and a moving average of the Standard Deviation to provide a smoother volatility measure.
    /// SFX is designed to filter out noise and help detect changes in volatility regimes.
    /// </summary>
    public class SmoothedForceIndex : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly AverageTrueRange _atr;
        private readonly StandardDeviation _stdDev;
        private readonly IndicatorBase<IndicatorDataPoint> _maStdDev;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _atr.IsReady && _stdDev.IsReady && _maStdDev.IsReady; 

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates a new Smoothed Force Index (SFX) indicator.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="atrPeriod">The period used to calculate the Average True Range (ATR)</param>
        /// <param name="stdDevPeriod">The period used to calculate the Standard Deviation of close prices</param>
        /// <param name="stdDevSmoothingPeriod">The period used to smooth the Standard Deviation with a moving average</param>
        /// <param name="maType">The type of moving average used to smooth the Standard Deviation</param>
        public SmoothedForceIndex(string name, int atrPeriod, int stdDevPeriod, int stdDevSmoothingPeriod, MovingAverageType maType = MovingAverageType.Simple)
            : base(name)
        {
            _atr = new AverageTrueRange(atrPeriod, MovingAverageType.Wilders);
            _stdDev = new StandardDeviation(stdDevPeriod);
            _maStdDev = maType.AsIndicator($"{name}_{maType}", stdDevSmoothingPeriod);

            WarmUpPeriod = Math.Max(_atr.WarmUpPeriod, Math.Max(_stdDev.WarmUpPeriod, stdDevSmoothingPeriod));
        }

        /// <summary>
        /// Creates a new Smoothed Force Index (SFX) indicator with a default name.
        /// </summary>
        /// <param name="atrPeriod">The period used to calculate the Average True Range (ATR)</param>
        /// <param name="stdDevPeriod">The period used to calculate the Standard Deviation of close prices</param>
        /// <param name="stdDevSmoothingPeriod">The period used to smooth the Standard Deviation with a moving average</param>
        /// <param name="maType">The type of moving average used to smooth the Standard Deviation</param>
        public SmoothedForceIndex(int atrPeriod, int stdDevPeriod, int stdDevSmoothingPeriod, MovingAverageType maType = MovingAverageType.Simple)
            : this($"SFX({atrPeriod},{stdDevPeriod},{stdDevSmoothingPeriod})", atrPeriod, stdDevPeriod, stdDevSmoothingPeriod, maType)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given trade bar input.
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator, or 0 if not ready</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            _atr.Update(input);
            _stdDev.Update(new IndicatorDataPoint(input.EndTime, input.Close));
            _maStdDev.Update(new IndicatorDataPoint(input.EndTime, _stdDev.Current.Value));

            if (IsReady)
            {
                return (_atr.Current.Value + _stdDev.Current.Value + _maStdDev.Current.Value) / 3m;
            }

            return 0m;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _atr.Reset();
            _stdDev.Reset();
            _maStdDev.Reset();
            base.Reset();
        }
    }
}
