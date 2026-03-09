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
    /// Mapping related factor provider. Factors based on price differences on mapping dates
    /// </summary>
    public class MappingContractFactorProvider : FactorFile<MappingContractFactorRow>
    {
        /// <summary>
        ///Creates a new instance
        /// </summary>
        public MappingContractFactorProvider(string permtick, IEnumerable<MappingContractFactorRow> data, DateTime? factorFileMinimumDate = null)
            : base(permtick, data, factorFileMinimumDate)
        {
        }

        /// <summary>
        /// Gets the price scale factor for the specified search date
        /// </summary>
        public override decimal GetPriceFactor(DateTime searchDate, DataNormalizationMode dataNormalizationMode, DataMappingMode? dataMappingMode = null, uint contractOffset = 0)
        {
            if (dataNormalizationMode == DataNormalizationMode.Raw)
            {
                return 0;
            }

            var factor = 1m;
            if (dataNormalizationMode is DataNormalizationMode.BackwardsPanamaCanal or DataNormalizationMode.ForwardPanamaCanal)
            {
                // default value depends on the data mode
                factor = 0;
            }

            for (var i = 0; i < ReversedFactorFileDates.Count; i++)
            {
                var factorDate = ReversedFactorFileDates[i];
                if (factorDate.Date < searchDate.Date)
                {
                    break;
                }

                var factorFileRow = SortedFactorFileData[factorDate];
                switch (dataNormalizationMode)
                {
                    case DataNormalizationMode.BackwardsRatio:
                    {
                        var row = factorFileRow.FirstOrDefault(row => row.DataMappingMode == dataMappingMode);
                        if (row != null && row.BackwardsRatioScale.Count > contractOffset)
                        {
                            factor = row.BackwardsRatioScale[(int)contractOffset];
                        }
                        break;
                    }
                    case DataNormalizationMode.BackwardsPanamaCanal:
                    {
                        var row = factorFileRow.FirstOrDefault(row => row.DataMappingMode == dataMappingMode);
                        if (row != null && row.BackwardsPanamaCanalScale.Count > contractOffset)
                        {
                            factor = row.BackwardsPanamaCanalScale[(int)contractOffset];
                        }
                        break;
                    }
                    case DataNormalizationMode.ForwardPanamaCanal:
                    {
                        var row = factorFileRow.FirstOrDefault(row => row.DataMappingMode == dataMappingMode);
                        if (row != null && row.ForwardPanamaCanalScale.Count > contractOffset)
                        {
                            factor = row.ForwardPanamaCanalScale[(int)contractOffset];
                        }
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(dataNormalizationMode));
                }
            }

            return factor;
        }
    }
}
