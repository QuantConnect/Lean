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
using System.Linq;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Set of helper methods for factor files and price scaling operations
    /// </summary>
    public static class PriceScalingExtensions
    {
        /// <summary>
        /// Resolves the price scale for a date given a factor file and required settings
        /// </summary>
        /// <param name="factorFile">The factor file to use</param>
        /// <param name="dateTime">The date for the price scale lookup</param>
        /// <param name="normalizationMode">The price normalization mode requested</param>
        /// <param name="contractOffset">The contract offset, useful for continuous contracts</param>
        /// <param name="dataMappingMode">The data mapping mode used, useful for continuous contracts</param>
        /// <returns>The price scale to use</returns>
        public static decimal GetPriceScale(
            this FactorFile factorFile,
            DateTime dateTime,
            DataNormalizationMode normalizationMode,
            uint contractOffset = 0,
            DataMappingMode? dataMappingMode = null
            )
        {
            if (factorFile == null)
            {
                if (normalizationMode is DataNormalizationMode.BackwardsPanamaCanal or DataNormalizationMode.ForwardPanamaCanal)
                {
                    return 0;
                }
                return 1;
            }

            factorFile.DataNormalizationMode = normalizationMode;
            factorFile.DataMappingMode = dataMappingMode;

            return factorFile.GetPriceScaleFactor(dateTime, contractOffset);
        }

        /// <summary>
        /// Determines the symbol to use to fetch it's factor file
        /// </summary>
        /// <remarks>This is useful for futures where the symbol to use is the canonical</remarks>
        public static Symbol GetFactorFileSymbol(this Symbol symbol)
        {
            return symbol.SecurityType == SecurityType.Future ? symbol.Canonical : symbol;
        }

        /// <summary>
        /// Helper method to return an empty factor file
        /// </summary>
        public static FactorFile GetEmptyFactorFile(this Symbol symbol)
        {
            return new FactorFile(symbol.ID.Symbol, Enumerable.Empty<FactorFileRow>());
        }
    }
}
