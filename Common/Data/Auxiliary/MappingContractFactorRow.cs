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
using System.Linq;
using Newtonsoft.Json;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Collection of factors for continuous contracts and their back months contracts for a specific mapping mode <see cref="DataMappingMode"/> and date
    /// </summary>
    public class MappingContractFactorRow : IFactorRow
    {
        /// <summary>
        /// Gets the date associated with this data
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Backwards ratio price scaling factors for the front month [index 0] and it's 'i' back months [index 0 + i]
        /// <see cref="DataNormalizationMode.BackwardsRatio"/>
        /// </summary>
        public IReadOnlyList<decimal> BackwardsRatioScale { get; set; } = new List<decimal>();

        /// <summary>
        /// Backwards Panama Canal price scaling factors for the front month [index 0] and it's 'i' back months [index 0 + i]
        /// <see cref="DataNormalizationMode.BackwardsPanamaCanal"/>
        /// </summary>
        public IReadOnlyList<decimal> BackwardsPanamaCanalScale { get; set; } = new List<decimal>();

        /// <summary>
        /// Forward Panama Canal price scaling factors for the front month [index 0] and it's 'i' back months [index 0 + i]
        /// <see cref="DataNormalizationMode.ForwardPanamaCanal"/>
        /// </summary>
        public IReadOnlyList<decimal> ForwardPanamaCanalScale { get; set; } = new List<decimal>();

        /// <summary>
        /// Allows the consumer to specify a desired mapping mode
        /// </summary>
        public DataMappingMode? DataMappingMode { get; set; }

        /// <summary>
        /// Empty constructor for json converter
        /// </summary>
        public MappingContractFactorRow() { }

        /// <summary>
        /// Writes factor file row into it's file format
        /// </summary>
        /// <remarks>Json formatted</remarks>
        public string GetFileFormat(string source = null)
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// Parses the lines as factor files rows while properly handling inf entries
        /// </summary>
        /// <param name="lines">The lines from the factor file to be parsed</param>
        /// <param name="factorFileMinimumDate">The minimum date from the factor file</param>
        /// <returns>An enumerable of factor file rows</returns>
        public static List<MappingContractFactorRow> Parse(
            IEnumerable<string> lines,
            out DateTime? factorFileMinimumDate
        )
        {
            factorFileMinimumDate = null;

            var rows = new List<MappingContractFactorRow>();

            // parse factor file lines
            foreach (var line in lines)
            {
                var row = JsonConvert.DeserializeObject<MappingContractFactorRow>(line);
                if (
                    !row.DataMappingMode.HasValue
                    || Enum.IsDefined(typeof(DataMappingMode), row.DataMappingMode.Value)
                )
                {
                    rows.Add(row);
                }
            }

            if (rows.Count > 0)
            {
                factorFileMinimumDate = rows.Min(ffr => ffr.Date).AddDays(-1);
            }

            return rows;
        }
    }
}
