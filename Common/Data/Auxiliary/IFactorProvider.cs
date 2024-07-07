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
*
*/

using System;
using System.Collections.Generic;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Providers price scaling factors for a permanent tick
    /// </summary>
    public interface IFactorProvider : IEnumerable<IFactorRow>
    {
        /// <summary>
        /// Gets the symbol this factor file represents
        /// </summary>
        public string Permtick { get; }

        /// <summary>
        /// The minimum tradeable date for the symbol
        /// </summary>
        /// <remarks>
        /// Some factor files have INF split values, indicating that the stock has so many splits
        /// that prices can't be calculated with correct numerical precision.
        /// To allow backtesting these symbols, we need to move the starting date
        /// forward when reading the data.
        /// Known symbols: GBSN, JUNI, NEWL
        /// </remarks>
        public DateTime? FactorFileMinimumDate { get; set; }

        /// <summary>
        /// Gets the price factor for the specified search date
        /// </summary>
        decimal GetPriceFactor(
            DateTime searchDate,
            DataNormalizationMode dataNormalizationMode,
            DataMappingMode? dataMappingMode = null,
            uint contractOffset = 0
        );
    }
}
