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
using System.ComponentModel;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Aggregate diagnostic produced by analyzing a completed optimization.
    /// </summary>
    public class OptimizationAnalysis
    {
        /// <summary>
        /// Natural-language interpretation of the analysis produced by a downstream AI consumer; empty until populated.
        /// </summary>
        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Interpretation { get; set; } = string.Empty;

        /// <summary>
        /// Total number of backtests observed, including failures.
        /// </summary>
        public int BacktestCountTotal { get; set; }

        /// <summary>
        /// Number of backtests used in the analysis after filtering failures.
        /// </summary>
        public int BacktestCountUsed { get; set; }

        /// <summary>
        /// Sharpe ratio statistics across all used backtests.
        /// </summary>
        public SharpeSummary OverallSharpe { get; set; }

        /// <summary>
        /// The best-performing backtest (argmax of Sharpe).
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public BacktestSummary Best { get; set; }

        /// <summary>
        /// Per-parameter sensitivity report; one entry per optimized parameter.
        /// </summary>
        public IReadOnlyList<ParameterReport> Parameters { get; set; }

        /// <summary>
        /// K-means clusters in standardized parameter space, ordered by mean Sharpe descending.
        /// </summary>
        public IReadOnlyList<Cluster> Clusters { get; set; }

        /// <summary>
        /// Local maxima of the Sharpe surface on the parameter grid, ordered by Sharpe descending.
        /// </summary>
        public IReadOnlyList<Mode> Modes { get; set; }

        /// <summary>
        /// Breakdown of zero-order backtests; null when none exist.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public FailedBacktestSummary FailedBacktests { get; set; }
    }
}
