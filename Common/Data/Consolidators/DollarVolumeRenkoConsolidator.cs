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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data;

namespace Common.Data.Consolidators
{
    /// <summary>
    /// This consolidator transforms a stream of <see cref="BaseData"/> instances into a stream of <see cref="RenkoBar"/>
    /// with a constant dollar volume for each bar.
    /// </summary>
    public class DollarVolumeRenkoConsolidator : VolumeRenkoConsolidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DollarVolumeRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// </summary>
        /// <param name="barSize">The constant dollar volume size of each bar</param>
        public DollarVolumeRenkoConsolidator(decimal barSize)
            : base(barSize)
        {
        }

        /// <summary>
        /// Converts raw volume into dollar volume by multiplying it with the trade price.
        /// </summary>
        /// <param name="volume">The raw trade volume</param>
        /// <param name="price">The trade price</param>
        /// <returns>The dollar volume</returns>
        protected override decimal AdjustVolume(decimal volume, decimal price)
        {
            return volume * price;
        }
    }
}