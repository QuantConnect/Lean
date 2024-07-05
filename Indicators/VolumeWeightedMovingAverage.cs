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
using MathNet.Numerics;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the Volume Weighted Moving Average (VWMA)
    /// </summary>
    public class VolumeWeightedMovingAverage : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {

        private IndicatorBase<IndicatorDataPoint> RollingSumS { get; }
        private IndicatorBase<IndicatorDataPoint> RollingSumV { get; }


        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeWeightedMovingAverage"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the SMA</param>
        public VolumeWeightedMovingAverage(string name, int period)
            : base(name)
        {
            RollingSumS = new Sum(name + "_SumS", period);
            RollingSumV = new Sum(name + "_SumV", period);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => RollingSumS.IsReady && RollingSumV.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        //public int WarmUpPeriod => 1;
        public int WarmUpPeriod => RollingSumS.Window.Size;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            RollingSumS.Update(input.Time, input.Close * input.Volume);
            RollingSumV.Update(input.Time, input.Volume);
            return RollingSumS.Current.Value / RollingSumV.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            RollingSumS.Reset();
            RollingSumV.Reset();
            base.Reset();
        }

    }
}
