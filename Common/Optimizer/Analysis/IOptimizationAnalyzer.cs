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

namespace QuantConnect.Optimizer.Analysis
{
    /// <summary>
    /// Builds an aggregate <see cref="OptimizationAnalysis"/> from a completed optimization's
    /// pre-extracted per-trial metrics. Implemented in Engine
    /// (<c>QuantConnect.Lean.Engine.Results.Analysis.Optimization.OptimizationAnalyzer</c>);
    /// the interface lives in Common so <see cref="LeanOptimizer"/> can hold a reference
    /// without taking a project dependency on Engine. The Engine implementation is wired in
    /// by <c>Optimizer.Launcher</c>.
    /// </summary>
    public interface IOptimizationAnalyzer
    {
        /// <summary>
        /// Runs the full optimization-analysis pipeline (Sharpe distribution, best trial,
        /// per-parameter slices, clusters, modes, zero-order failure breakdown).
        /// </summary>
        /// <param name="parameters">Completed trial metrics plus the parameter grid spec.</param>
        /// <returns>The populated <see cref="OptimizationAnalysis"/>, or <c>null</c> when no usable trials remain.</returns>
        OptimizationAnalysis Run(OptimizationAnalysisRunParameters parameters);
    }
}
