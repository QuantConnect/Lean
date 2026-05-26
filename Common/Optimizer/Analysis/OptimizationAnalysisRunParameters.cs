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

using QuantConnect.Optimizer.Parameters;
using System.Collections.Generic;

namespace QuantConnect.Optimizer.Analysis
{
    /// <summary>
    /// Bundles the inputs needed by <see cref="OptimizationAnalyzer"/>: the per-trial metrics
    /// extracted as each backtest completed, and the parameter grid spec that drove the
    /// optimization. The optimization-side analogue of <c>ResultsAnalysisRunParameters</c>
    /// (which serves the backtest analyzer in Engine).
    /// </summary>
    public class OptimizationAnalysisRunParameters
    {
        /// <summary>
        /// All completed trials from the optimization (one per backtest), already reduced to
        /// the small extracted shape the analyzer reads. Heavy JSON payloads are not retained.
        /// </summary>
        public IReadOnlyList<OptimizationTrialMetrics> CompletedTrials { get; }

        /// <summary>
        /// The optimization parameter grid spec (used for searched-min/max/step bounds and to
        /// drive per-parameter slicing).
        /// </summary>
        public IReadOnlyCollection<OptimizationParameter> OptimizationParameters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizationAnalysisRunParameters"/> class.
        /// </summary>
        /// <param name="completedTrials">The completed trials, already extracted.</param>
        /// <param name="optimizationParameters">The parameter grid spec.</param>
        public OptimizationAnalysisRunParameters(
            IReadOnlyList<OptimizationTrialMetrics> completedTrials,
            IReadOnlyCollection<OptimizationParameter> optimizationParameters)
        {
            CompletedTrials = completedTrials;
            OptimizationParameters = optimizationParameters;
        }
    }
}
