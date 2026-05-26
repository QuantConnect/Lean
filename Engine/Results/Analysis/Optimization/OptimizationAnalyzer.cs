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

using QuantConnect.Logging;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Analysis;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis.Optimization
{
    /// <summary>
    /// Builds an aggregate diagnostic (<see cref="OptimizationAnalysis"/>) from a completed
    /// optimization's pre-extracted per-trial metrics. Computes the Sharpe distribution, the
    /// best trial, per-parameter sensitivity slices, k-means clusters in parameter space,
    /// local maxima, and a zero-order failure breakdown. The optimization-side analogue of
    /// <see cref="ResultsAnalyzer"/> (which runs per-backtest); both live under
    /// <c>Engine/Results/Analysis</c> so the diagnostic surface stays in one place.
    /// </summary>
    /// <remarks>
    /// Stateless and safe to share — call <see cref="Run"/> once per optimization. The
    /// instance is constructed in <c>Optimizer.Launcher</c> and assigned to
    /// <c>LeanOptimizer.Analyzer</c> via property injection so the Optimizer assembly does
    /// not need to reference Engine.
    /// </remarks>
    public class OptimizationAnalyzer : IOptimizationAnalyzer
    {
        /// <summary>
        /// Runs the full optimization-analysis pipeline.
        /// </summary>
        /// <param name="parameters">Completed trial metrics plus the parameter grid spec.</param>
        /// <returns>The populated <see cref="OptimizationAnalysis"/>, or <c>null</c> when no usable trials remain.</returns>
        public OptimizationAnalysis Run(OptimizationAnalysisRunParameters parameters)
        {
            var allTrials = parameters?.CompletedTrials ?? new List<OptimizationTrialMetrics>();
            var trials = allTrials.Where(t => t != null && t.HasSharpe).ToList();
            if (trials.Count == 0)
            {
                Log.Trace("OptimizationAnalyzer.Run(): no completed backtests with parsable Sharpe ratios; skipping analysis");
                return null;
            }

            var sharpes = trials.Select(t => t.Sharpe).ToList();
            var overall = new SharpeSummary
            {
                Mean = sharpes.Average(),
                StdDev = StdDev(sharpes),
                Min = sharpes.Min(),
                Max = sharpes.Max(),
                Median = Median(sharpes)
            };

            // Always maximize Sharpe. The optimization's chosen Criterion may be something else
            // but the analyzer uses Sharpe as the universal yardstick for the analysis surface.
            var best = trials.OrderByDescending(t => t.Sharpe).First();
            var bestSummary = new BestTrialSummary
            {
                BacktestId = best.BacktestId,
                Parameters = new Dictionary<string, double>(best.Parameters),
                SharpeRatio = best.Sharpe
            };

            var paramReports = parameters.OptimizationParameters
                .Select(p => OptimizationSlicing.AnalyzeParameter(p, trials, best))
                .ToList();

            var clusters = OptimizationClustering.Build(trials, parameters.OptimizationParameters);
            var modes = OptimizationModes.Find(trials, parameters.OptimizationParameters);
            var failed = OptimizationFailedBacktests.Build(allTrials);

            return new OptimizationAnalysis
            {
                TrialCountTotal = allTrials.Count,
                TrialCountUsed = trials.Count,
                OverallSharpe = overall,
                Best = bestSummary,
                Parameters = paramReports,
                Clusters = clusters,
                Modes = modes,
                FailedBacktests = failed
            };
        }

        // ── Aggregate helpers ────────────────────────────────────────────────────

        private static double StdDev(IReadOnlyCollection<double> values)
        {
            if (values.Count < 2) return 0;
            var mean = values.Average();
            var s = values.Sum(v => (v - mean) * (v - mean));
            return System.Math.Sqrt(s / (values.Count - 1));
        }

        private static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0) return 0;
            return sorted.Count % 2 == 1
                ? sorted[sorted.Count / 2]
                : 0.5 * (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]);
        }
    }
}
