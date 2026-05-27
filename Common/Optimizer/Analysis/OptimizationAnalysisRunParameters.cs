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
    /// Bundles the inputs to the optimization analyzer: per-backtest metrics and the parameter grid spec.
    /// </summary>
    public class OptimizationAnalysisRunParameters
    {
        /// <summary>
        /// Completed backtests from the optimization, already reduced to the metrics the analyzer reads.
        /// </summary>
        public IReadOnlyList<OptimizationBacktestMetrics> CompletedBacktests { get; }

        /// <summary>
        /// The optimization parameter grid spec.
        /// </summary>
        public IReadOnlyCollection<OptimizationParameter> OptimizationParameters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizationAnalysisRunParameters"/> class.
        /// </summary>
        /// <param name="completedBacktests">The completed backtest metrics.</param>
        /// <param name="optimizationParameters">The parameter grid spec.</param>
        public OptimizationAnalysisRunParameters(
            IReadOnlyList<OptimizationBacktestMetrics> completedBacktests,
            IReadOnlyCollection<OptimizationParameter> optimizationParameters)
        {
            CompletedBacktests = completedBacktests;
            OptimizationParameters = optimizationParameters;
        }
    }
}
