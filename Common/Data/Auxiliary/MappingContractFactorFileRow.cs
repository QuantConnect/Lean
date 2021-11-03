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
using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Collection of factors for continuous contracts and their back months contracts for a specific mapping mode <see cref="DataMappingMode"/> and date
    /// </summary>
    public class MappingContractFactorFileRow : FactorFileRow
    {
        /// <summary>
        /// Backwards ratio price scaling factors for the front month [index 0] and it's 'i' back months [index 0 + i]
        /// <see cref="DataNormalizationMode.BackwardsRatio"/>
        /// </summary>
        public IReadOnlyList<decimal> BackwardsRatioScale { get; set;  } = new List<decimal>();

        /// <summary>
        /// Backwards Panama Canal price scaling factors for the front month [index 0] and it's 'i' back months [index 0 + i]
        /// <see cref="DataNormalizationMode.BackwardsPanamaCanal"/>
        /// </summary>
        public IReadOnlyList<decimal> BackwardsPanamaCanalScale { get; set;  } = new List<decimal>();

        /// <summary>
        /// Forward Panama Canal price scaling factors for the front month [index 0] and it's 'i' back months [index 0 + i]
        /// <see cref="DataNormalizationMode.ForwardPanamaCanal"/>
        /// </summary>
        public IReadOnlyList<decimal> ForwardPanamaCanalScale { get; set;  } = new List<decimal>();

        /// <summary>
        /// Allows the consumer to specify a desired mapping mode
        /// </summary>
        public DataMappingMode? DataMappingMode { get; set; }

        /// <summary>
        /// Empty constructor for json converter
        /// </summary>
        public MappingContractFactorFileRow()
            : this(DateTime.MinValue)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="date"></param>
        public MappingContractFactorFileRow(DateTime date)
            : base(date, 0, 0)
        {
        }

        /// <summary>
        /// Writes factor file row into it's file format
        /// </summary>
        /// <remarks>Json formatted</remarks>
        public override string GetFileFormat(string source = null)
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
