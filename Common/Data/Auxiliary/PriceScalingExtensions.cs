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
using System.Collections.Generic;

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
        /// <param name="endDateTime">The reference end date for scaling prices. Default is today (latest factor entry)</param>
        /// <returns>The price scale to use</returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="normalizationMode"/> is <see cref="DataNormalizationMode.ScaledRaw"/> and <paramref name="endDateTime"/> is null
        /// </exception>
        public static decimal GetPriceScale(
            this IFactorProvider factorFile,
            DateTime dateTime,
            DataNormalizationMode normalizationMode,
            uint contractOffset = 0,
            DataMappingMode? dataMappingMode = null,
            DateTime? endDateTime = null
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

            var endDateTimeFactor = 1m;
            if (normalizationMode == DataNormalizationMode.ScaledRaw)
            {
                if (endDateTime == null)
                {
                    throw new ArgumentException(
                        $"{nameof(DataNormalizationMode.ScaledRaw)} normalization mode requires an end date for price scaling.");
                }

                // For ScaledRaw, we need to get the price scale at the end date to adjust prices to that date instead of "today"
                endDateTimeFactor = factorFile.GetPriceFactor(endDateTime.Value, normalizationMode, dataMappingMode, contractOffset);
            }

            return factorFile.GetPriceFactor(dateTime, normalizationMode, dataMappingMode, contractOffset) / endDateTimeFactor;
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
        public static IFactorProvider GetEmptyFactorFile(this Symbol symbol)
        {
            if (symbol.SecurityType == SecurityType.Future)
            {
                return new MappingContractFactorProvider(symbol.ID.Symbol, Enumerable.Empty<MappingContractFactorRow>());
            }
            return new CorporateFactorProvider(symbol.ID.Symbol, Enumerable.Empty<CorporateFactorRow>());
        }

        /// <summary>
        /// Parses the contents as a FactorFile, if error returns a new empty factor file
        /// </summary>
        public static IFactorProvider SafeRead(string permtick, IEnumerable<string> contents, SecurityType securityType)
        {
            try
            {
                DateTime? minimumDate;

                contents = contents.Distinct();

                if (securityType == SecurityType.Future)
                {
                    return new MappingContractFactorProvider(permtick, MappingContractFactorRow.Parse(contents, out minimumDate), minimumDate);
                }
                // FactorFileRow.Parse handles entries with 'inf' and exponential notation and provides the associated minimum tradeable date for these cases
                // previously these cases were not handled causing an exception and returning an empty factor file
                return new CorporateFactorProvider(permtick, CorporateFactorRow.Parse(contents, out minimumDate), minimumDate);
            }
            catch (Exception e)
            {
                if (securityType == SecurityType.Future)
                {
                    return new MappingContractFactorProvider(permtick, Enumerable.Empty<MappingContractFactorRow>());
                }
                return new CorporateFactorProvider(permtick, Enumerable.Empty<CorporateFactorRow>());
            }
        }
    }
}
