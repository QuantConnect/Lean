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
    public class TimeProfile: MarketProfile
    {
        /// <summary>
        /// Represents an Indicator of the Market Profile with Time Price Opporturnity (TPO) mode and its attributes
        /// </summary>
        /// <param name="period"></param>
        public TimeProfile(int period = 2)
            : this($"TP({period})", period)
        {
        }

        /// <summary>
        /// Creates a new MarkProfile indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        public TimeProfile(string name, int period, decimal roundoff = 0.05m)
            : base(name, period, roundoff)
        { }
        /// <summary>
        /// Add the new input value to the Close array and Volume dictionary in the TPO mode.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        public override void Add(TradeBar input)
        {
            decimal ClosePrice = Round(input.Close);
            if (!Close.Contains(ClosePrice))
            {
                Close.Add(ClosePrice);
                Volume[ClosePrice] = 1;
            }
            else
            {
                Volume[ClosePrice] += 1;
            }


            TotalVolume.Update(input.Time, 1);


            // Check if the indicator is in the given period
            try
            {
                TradeBar firstItem = DataPoints.MostRecentlyRemoved;
                ClosePrice = Round(firstItem.Close);
                Volume[ClosePrice]--;
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
