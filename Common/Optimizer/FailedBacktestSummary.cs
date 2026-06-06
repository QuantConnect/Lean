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

using System.Collections.Generic;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Breakdown of backtests in an optimization that produced zero orders.
    /// </summary>
    public class FailedBacktestSummary
    {
        /// <summary>
        /// Total number of backtests that produced zero orders.
        /// </summary>
        public int ZeroOrderCount { get; set; }

        /// <summary>
        /// Number of zero-order backtests inspected for analysis tags; may be smaller than <see cref="ZeroOrderCount"/>.
        /// </summary>
        public int InspectedCount { get; set; }

        /// <summary>
        /// Map of analysis-tag name to the number of inspected backtests carrying that tag.
        /// </summary>
        public IReadOnlyDictionary<string, int> AnalysisNameCounts { get; set; }
    }
}
