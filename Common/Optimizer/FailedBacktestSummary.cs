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
    /// Summary of backtests in an optimization that produced zero orders. Aggregates how many
    /// of the inspected backtests carry each backtest-level analysis tag (e.g.
    /// "FlatEquityCurveAnalysis"), giving a quick view of common failure modes that aggregate
    /// optimization statistics can mask.
    /// </summary>
    public class FailedBacktestSummary
    {
        /// <summary>
        /// Total number of backtests in the optimization that produced zero orders.
        /// </summary>
        public int ZeroOrderCount { get; set; }

        /// <summary>
        /// Number of zero-order backtests actually inspected for analysis tags (capped to
        /// keep the summary bounded; <see cref="ZeroOrderCount"/> may be larger).
        /// </summary>
        public int InspectedCount { get; set; }

        /// <summary>
        /// Membership counts: analysis-tag name -> number of inspected backtests that carry
        /// the tag. Each name appears at most once per backtest in the count.
        /// </summary>
        public IReadOnlyDictionary<string, int> AnalysisNameCounts { get; set; }
    }
}
