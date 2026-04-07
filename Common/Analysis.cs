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

using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect
{
    /// <summary>
    /// Represents the outcome of a single backtest diagnostic analysis,
    /// containing the analysis name, diagnostic context, and a list of solutions.
    /// </summary>
    public class Analysis(string name, string issue, object sample, int? count, IReadOnlyList<string> solutions)
    {
        /// <summary>
        /// Gets or sets the name of the analysis that produced this result.
        /// </summary>
        public string Name { get; set; } = name;

        /// <summary>
        /// Gets or sets a short description of why the analysis was triggered.
        /// </summary>
        public string Issue { get; set; } = issue;

        /// <summary>
        /// Gets or sets a representative sample value of the issue detected by the analysis.
        /// It can be something like a log message, an order or an order event.
        /// </summary>
        public object Sample { get; set; } = sample;

        /// <summary>
        /// Gets or sets the total number of matching occurrences found by the analysis.
        /// If null, the analysis is reporting a single issue with the provided sample;
        /// if not null, the sample represents one of multiple occurrences of the same issue.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Count { get; set; } = count;

        /// <summary>
        /// Gets or sets human-readable suggestions for resolving the detected issue.
        /// </summary>
        public IReadOnlyList<string> Solutions { get; set; } = solutions;
    }
}
