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
using System;
using System.Collections.Generic;
using QuantConnect.Algorithm;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Compares the full-period Sharpe ratio of the strategy to the benchmark.
    /// </summary>
    public class PerformanceRelativeToBenchmark : BaseResultsAnalysis
    {
        /// <summary>
        /// Gets the description of the underperformance relative to benchmark issue.
        /// </summary>
        public override string Issue { get; } = "The strategy has a lower Sharpe ratio than the benchmark.";

        /// <summary>
        /// Gets the severity weight for the benchmark comparison analysis.
        /// </summary>
        public override int Weight { get; } = 72;

        /// <summary>
        /// Runs the performance relative to benchmark analysis against the provided backtest parameters.
        /// </summary>
        public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.Algorithm, parameters.EquityCurve, parameters.BenchmarkEquityCurve);

        /// <summary>
        /// Calculates the Sharpe ratio of the strategy over the full backtest period and compares it to the benchmark.
        /// </summary>
        /// <param name="algorithm">The algorithm instance used to obtain the risk-free rate model.</param>
        /// <param name="backtestEquity">Daily equity values for the strategy, keyed by date.</param>
        /// <param name="benchmarkEquity">Daily equity values for the benchmark (SPY), keyed by date.</param>
        /// <returns>Analysis results when the strategy's Sharpe ratio is lower than the benchmark's.</returns>
        public IReadOnlyList<AnalysisResult> Run(QCAlgorithm algorithm, SortedList<DateTime, decimal> backtestEquity,
            SortedList<DateTime, decimal> benchmarkEquity)
        {
            var (backtestSharpe, benchmarkSharpe) = CrisisEventsAnalysis.CalculateSharpeRatio(backtestEquity, benchmarkEquity, 
                algorithm.RiskFreeInterestRateModel);

            var result = backtestSharpe < benchmarkSharpe
                ? new { BacktestSharpe = backtestSharpe, BenchmarkSharpe = benchmarkSharpe }
                : null;

            var potentialSolutions = result is not null ? Solutions() : [];
            return SingleResponse(new ResultsAnalysisContext(result), potentialSolutions);
        }

        /// <summary>
        /// Returns suggested solutions for improving performance relative to the benchmark.
        /// </summary>
        private static List<string> Solutions() =>
        [
            "Try adjusting the trading rules and/or the universe to get a strategy that outperforms the benchmark.",
        ];
    }
}
