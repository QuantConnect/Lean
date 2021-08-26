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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Represents an Indicator of the Market Profile with Volume Profile mode and its attributes
    /// </summary>
    public class VolumeProfile: MarketProfile
    {
        public VolumeProfile(int period = 2)
            : this($"VP({period})", period)
        {
        }

        /// <summary>
        /// Creates a new MarkProfile indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        public VolumeProfile(string name, int period, decimal roundoff = 0.05m)
            : base(name, period, roundoff)
        { }

        /// <summary>
        /// Add the new input value to the Close array and Volume dictionary in the VOL mode.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        public override void Add(TradeBar input)
        {
            decimal ClosePrice = Round(input.Close);// Roundoff the close price
            if (!Close.Contains(ClosePrice))
            {
                Close.Add(ClosePrice);
                Volume[ClosePrice] = input.Volume;
            }
            else
            {
                Volume[ClosePrice] += input.Volume;
            }
            TotalVolume.Update(input.Time, input.Volume);

            // Check if the indicator is in the given period

            try
            {
                TradeBar firstItem = DataPoints.MostRecentlyRemoved;
                ClosePrice = Round(firstItem.Close); // Roundoff the close price
                Volume[ClosePrice] -= firstItem.Volume;
                if (Volume[ClosePrice] == 0)
                {
                    Volume.Remove(ClosePrice);
                    Close.Remove(ClosePrice);
                }
            }
            catch { }

        }
    }
}
