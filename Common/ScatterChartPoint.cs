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
using Newtonsoft.Json;

namespace QuantConnect
{
    /// <summary>
    /// A chart point for a scatter series plot
    /// </summary>
    [JsonConverter(typeof(ScatterChartPointJsonConverter))]
    public class ScatterChartPoint : ChartPoint
    {
        /// <summary>
        /// A summary of this point for the tooltip
        /// </summary>
        [JsonProperty(PropertyName = "tooltip", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Tooltip { get; set; }

        /// <summary>
        /// Creates a new empty instance
        /// </summary>
        public ScatterChartPoint()
        {
        }

        /// <summary>
        /// Creates a new instance at the specified time and value
        /// </summary>
        public ScatterChartPoint(long time, decimal? value, string tooltip = null) : base(time, value)
        {
            Tooltip = tooltip;
        }

        /// <summary>
        /// Creates a new instance at the specified time and value
        /// </summary>
        public ScatterChartPoint(DateTime time, decimal? value, string tooltip = null) : base(time, value)
        {
            Tooltip = tooltip;
        }

        /// <summary>
        /// Clones this instance
        /// </summary>
        /// <returns>Clone of this instance</returns>
        public override ISeriesPoint Clone()
        {
            return new ScatterChartPoint { x = X, y = Y, Tooltip = Tooltip };
        }
    }
}
