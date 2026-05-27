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
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Optimizer.Analysis
{
    /// <summary>
    /// Builds an aggregate <see cref="OptimizationAnalysis"/> from a completed optimization's per-backtest metrics; optimization-side analogue of the Engine ResultsAnalyzer.
    /// </summary>
    public class OptimizationAnalyzer
    {
        /// <summary>
        /// Runs the full optimization-analysis pipeline.
        /// </summary>
        /// <param name="parameters">Completed backtest metrics plus the parameter grid spec.</param>
        /// <returns>The populated <see cref="OptimizationAnalysis"/>, or null when no usable backtests remain.</returns>
        public OptimizationAnalysis Run(OptimizationAnalysisRunParameters parameters)
        {
            var allBacktests = parameters?.CompletedBacktests ?? new List<OptimizationBacktestMetrics>();
            var backtests = allBacktests.Where(b => b?.TotalPerformance?.PortfolioStatistics != null).ToList();
            if (backtests.Count == 0)
            {
                Log.Trace("OptimizationAnalyzer.Run(): no completed backtests with parsable Sharpe ratios; skipping analysis");
                return null;
            }

            var sharpes = backtests.Select(b => b.SharpeRatio).ToList();
            var overall = new SharpeSummary
            {
                Mean = sharpes.Average(),
                StdDev = StdDev(sharpes),
                Min = sharpes.Min(),
                Max = sharpes.Max(),
                Median = Median(sharpes)
            };

            // Sharpe is the universal yardstick regardless of the optimization's Criterion.
            var best = backtests.OrderByDescending(b => b.SharpeRatio).First();
            var bestSummary = new BacktestSummary
            {
                BacktestId = best.BacktestId,
                Parameters = new Dictionary<string, decimal>(best.Parameters),
                SharpeRatio = best.SharpeRatio
            };

            var paramReports = parameters.OptimizationParameters
                .Select(p => OptimizationSlicing.AnalyzeParameter(p, backtests, best))
                .ToList();

            var clusters = OptimizationClustering.Build(backtests, parameters.OptimizationParameters);
            var modes = OptimizationModes.Find(backtests, parameters.OptimizationParameters);
            var failed = OptimizationFailedBacktests.Build(allBacktests);

            return new OptimizationAnalysis
            {
                BacktestCountTotal = allBacktests.Count,
                BacktestCountUsed = backtests.Count,
                OverallSharpe = overall,
                Best = bestSummary,
                Parameters = paramReports,
                Clusters = clusters,
                Modes = modes,
                FailedBacktests = failed
            };
        }

        // ── Aggregate helpers ────────────────────────────────────────────────────

        private static decimal StdDev(IReadOnlyCollection<decimal> values)
        {
            if (values.Count < 2) return 0m;
            var mean = values.Average();
            var s = values.Sum(v => (v - mean) * (v - mean));
            // System.Math has no decimal Sqrt; cross into double for the root and back.
            return (decimal)System.Math.Sqrt((double)(s / (values.Count - 1)));
        }

        private static decimal Median(IEnumerable<decimal> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0) return 0m;
            return sorted.Count % 2 == 1
                ? sorted[sorted.Count / 2]
                : 0.5m * (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]);
        }
    }
}
