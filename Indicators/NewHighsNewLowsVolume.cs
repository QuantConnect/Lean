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
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The New Highs - New Lows Volume Ratio is a Breadth indicator calculated as ratio of 
    /// summary volume of stocks reaching new high to summary volume of stocks reaching new
    /// low compared to high and low values in defined time period. 
    /// </summary>
    public class NewHighsNewLowsVolume : NewHighsNewLows<TradeBar>
    {
        /// <summary>
        /// Volume ratio between the number of assets reaching new highs and the number of assets
        /// reaching new lows in defined time period.
        /// </summary>
        public IndicatorBase<TradeBar> VolumeRatio { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NewHighsNewLowsVolume"/> class
        /// </summary>
        public NewHighsNewLowsVolume(string name, int period)
            : base(name, period)
        {
            VolumeRatio = new FunctionalIndicator<TradeBar>(
                $"{name}_VolumeRatio",
                (input) =>
                {
                    decimal newHighsVolume = NewHighs.Sum(x => x.Volume);
                    decimal newLowsVolume = NewLows.Sum(x => x.Volume);

                    return newLowsVolume == 0m ? newHighsVolume : newHighsVolume / newLowsVolume;
                },
                _ => IsReady);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            var nextValue = base.ComputeNextValue(input);

            VolumeRatio.Update(input);

            return VolumeRatio.Current.Value;
        }

        /// <summary>
        /// Resets tracked assets to its initial state
        /// </summary>
        public override void Reset()
        {
            VolumeRatio.Reset();

            base.Reset();
        }
    }
}
