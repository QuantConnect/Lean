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
        public override decimal GetPriceScaleFactor(DateTime searchDate, DataNormalizationMode dataNormalizationMode, DataMappingMode? dataMappingMode = null, uint contractOffset = 0)
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

            for (var i = 0; i < _reversedFactorFileDates.Count; i++)
            {
                var factorDate = _reversedFactorFileDates[i];
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
                        throw new ArgumentOutOfRangeException();
                }
            }

            return factor;
        }
    }
}
