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

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Aggregate diagnostic produced by analyzing a completed optimization. Captures the
    /// per-trial Sharpe distribution, the best trial, per-parameter sensitivity slices,
    /// k-means clusters in parameter space, local maxima ("modes"), and any zero-order
    /// failure breakdown.
    /// </summary>
    public class OptimizationAnalysis
    {
        /// <summary>
        /// Total number of trial backtests observed during the optimization (including failures).
        /// </summary>
        public int TrialCountTotal { get; set; }

        /// <summary>
        /// Number of trial backtests successfully used in the analysis after filtering failed runs.
        /// </summary>
        public int TrialCountUsed { get; set; }

        /// <summary>
        /// Univariate Sharpe ratio statistics across all used trials.
        /// </summary>
        public SharpeSummary OverallSharpe { get; set; }

        /// <summary>
        /// The best-performing trial (argmax of Sharpe).
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public BestTrialSummary Best { get; set; }

        /// <summary>
        /// Per-parameter sensitivity report. One entry per optimized parameter.
        /// </summary>
        public IReadOnlyList<ParameterReport> Parameters { get; set; }

        /// <summary>
        /// K-means clusters in standardized parameter space, ordered by mean Sharpe descending.
        /// Empty when there are too few trials to cluster meaningfully.
        /// </summary>
        public IReadOnlyList<Cluster> Clusters { get; set; }

        /// <summary>
        /// Local maxima of the Sharpe surface on the parameter grid (face-neighbor sense),
        /// ordered by Sharpe descending.
        /// </summary>
        public IReadOnlyList<Mode> Modes { get; set; }

        /// <summary>
        /// Summary of backtests that produced zero orders, with their analysis-tag counts.
        /// Null/omitted when no zero-order trials exist.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public FailedBacktestSummary FailedBacktests { get; set; }
    }
}
