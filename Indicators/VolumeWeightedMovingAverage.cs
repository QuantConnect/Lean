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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the Volume Weighted Moving Average (VWMA)
    /// It is a technical analysis indicator used by traders to determine the average price of an asset over a given period of time,
    /// taking into account both price and volume.
    /// </summary>
    public class VolumeWeightedMovingAverage : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {

        private readonly IndicatorBase<IndicatorDataPoint> _rollingSumPriceMultipliedByVolume;
        private readonly IndicatorBase<IndicatorDataPoint> _rollingSumVolume;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _rollingSumPriceMultipliedByVolume.IsReady && _rollingSumVolume.IsReady;
        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeWeightedMovingAverage"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the SMA</param>
        public VolumeWeightedMovingAverage(string name, int period)
            : base(name)
        {
            WarmUpPeriod = period;
            _rollingSumPriceMultipliedByVolume = new Sum(name + "_SumPxV", period);
            _rollingSumVolume = new Sum(name + "_SumVolume", period);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeWeightedMovingAverage"/> class using the specified name.
        /// </summary>
        /// <param name="period">The period of the SMA</param>
        public VolumeWeightedMovingAverage(int period)
            : this($"VWMA({period})", period)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            _rollingSumPriceMultipliedByVolume.Update(input.EndTime, input.Close * input.Volume);
            _rollingSumVolume.Update(input.EndTime, input.Volume);
            var sumVolume = _rollingSumVolume.Current.Value;
            if (sumVolume != 0)
            {
                return _rollingSumPriceMultipliedByVolume.Current.Value / sumVolume;
            }
            return input.Close;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _rollingSumPriceMultipliedByVolume.Reset();
            _rollingSumVolume.Reset();
            base.Reset();
        }

    }
}
