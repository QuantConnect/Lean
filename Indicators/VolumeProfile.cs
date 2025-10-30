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
    /// Represents an Indicator of the Market Profile with Volume Profile mode and its attributes
    /// </summary>
    public class VolumeProfile: MarketProfile
    {
        /// <summary>
        /// Creates a new VolumeProfile indicator with the specified period
        /// </summary>
        /// <param name="period">The period of the indicator</param>
        /// <param name="valueAreaVolumePercentage">The percentage of volume contained in the value area</param>
        /// <param name="priceRangeRoundOff">How many digits you want to round and the precision.
        public VolumeProfile(int period = 2, decimal valueAreaVolumePercentage = 0.70m, decimal priceRangeRoundOff = 0.05m)
            : this($"VP({period},{valueAreaVolumePercentage},{priceRangeRoundOff})", period, valueAreaVolumePercentage, priceRangeRoundOff)
        {
        }

        /// <summary>
        /// Creates a new VolumeProfile indicator with the specified name, period and priceRangeRoundOff
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="valueAreaVolumePercentage">The percentage of volume contained in the value area</param>
        /// <param name="priceRangeRoundOff">How many digits you want to round and the precision.
        /// i.e 0.01 round to two digits exactly.</param>
        public VolumeProfile(string name, int period = 2, decimal valueAreaVolumePercentage = 0.70m, decimal priceRangeRoundOff = 0.05m)
            : base(name, period, valueAreaVolumePercentage, priceRangeRoundOff)
        { }

        /// <summary>
        /// Define the Volume for the Volume Profile mode
        /// </summary>
        /// <param name="input"></param>
        /// <returns>The volume of the input Data Point</returns>
        protected override decimal GetVolume(TradeBar input)
        {
            return input.Volume;
        }
    }
}
