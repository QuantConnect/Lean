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
using System.Linq;
using Python.Runtime;
using QuantConnect.Indicators;
using System.Collections.Generic;

namespace QuantConnect.Data
{
    /// <summary>
    /// Provides historical values of an indicator
    /// </summary>
    public class IndicatorHistory : DataHistory<IndicatorDataPoints>
    {
        private readonly Dictionary<string, List<IndicatorDataPoint>> _pointsPerName;

        /// <summary>
        /// The indicators historical values
        /// </summary>
        public List<IndicatorDataPoint> Current => this["current"];

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="indicatorsDataPointsByTime">Indicators data points by time</param>
        /// <param name="indicatorsDataPointPerProperty">Indicators data points by property name</param>
        /// <param name="dataframe">The lazy data frame constructor</param>
        public IndicatorHistory(List<IndicatorDataPoints> indicatorsDataPointsByTime, List<InternalIndicatorValues> indicatorsDataPointPerProperty, Lazy<PyObject> dataframe)
            : base(indicatorsDataPointsByTime, dataframe)
        {
            // for the index accessor we enforce uniqueness by name
            _pointsPerName = indicatorsDataPointPerProperty.DistinctBy(x => x.Name.ToLowerInvariant()).ToDictionary(x => x.Name.ToSnakeCase(), x => x.Values);
        }

        /// <summary>
        /// Access the historical indicator values per indicator property name
        /// </summary>
        public List<IndicatorDataPoint> this[string name]
        {
            get
            {
                if (_pointsPerName.TryGetValue(name.ToSnakeCase().ToLowerInvariant(), out var result))
                {
                    return result;
                }
                return null;
            }
        }
    }
}
