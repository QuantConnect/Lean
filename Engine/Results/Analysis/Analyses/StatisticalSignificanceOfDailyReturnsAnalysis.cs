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
using System.Linq;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// One-sample t-test: tests whether the strategy's excess daily returns
    /// (over benchmark) have a mean significantly greater than zero.
    /// Mirrors <c>tests/statistical_significance_of_daily_returns.py</c>.
    /// </summary>
    public class StatisticalSignificanceOfDailyReturnsAnalysis : BaseResultsAnalysis
    {
        public override string Issue { get; } = "The distribution of the strategy's daily returns in excess of the benchmark's daily returns has a p-value above 0.05. " +
            "Therefore, we fail to reject the null hypothesis that the mean of this distribution is above zero.";

        public override int Weight { get; } = 28;
        public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.EquityCurve, parameters.BenchmarkEquityCurve);

        /// <summary>
        /// Computes excess daily returns (strategy minus benchmark) and applies a one-tailed
        /// one-sample t-test at the 5 % significance level.
        /// </summary>
        /// <param name="backtestEquity">Daily equity values for the strategy, keyed by date.</param>
        /// <param name="benchmarkEquity">Daily equity values for the benchmark (SPY), keyed by date.</param>
        /// <returns>Analysis results when the strategy's excess returns are not statistically significant.</returns>
        public IReadOnlyList<AnalysisResult> Run(SortedList<DateTime, decimal> backtestEquity,
            SortedList<DateTime, decimal> benchmarkEquity)
        {
            var backtestReturns = backtestEquity.PercentChange();
            var benchmarkReturns = benchmarkEquity.PercentChange();

            // Excess daily returns (drop the first NaN row)
            var excess = backtestReturns.Keys
                .Skip(1)   // mirrors PercentChange()[1:]
                .Where(benchmarkReturns.ContainsKey)
                .Select(k => benchmarkReturns.TryGetValue(k, out var bv)
                    ? (double)(backtestReturns[k] - bv)
                    : double.NaN)
                .Where(v => !double.IsNaN(v))
                .ToArray();

            var pValue = OneSampleTAnalysis(excess, 0.0);
            pValue /= 2.0; // one-tailed (positive direction)

            var result = pValue > 0.05 ? new { PValue = pValue } : null;
            var potentialSolutions = result is not null ? Solutions() : [];
            return SingleResponse(new ResultsAnalysisContext(result), potentialSolutions);
        }

        /// <summary>
        /// Returns the two-tailed p-value for a one-sample t-test against
        /// <paramref name="popmean"/> using the Student-t distribution.
        /// </summary>
        private static double OneSampleTAnalysis(double[] sample, double popmean)
        {
            if (sample.Length < 2)
            {
                return 1.0;
            }

            var mean = sample.Mean();
            var diff = mean - popmean;
            var stdErr = sample.StandardDeviation() / Math.Sqrt(sample.Length);
            if (stdErr == 0) return 1.0;

            var t = diff / stdErr;
            var df = sample.Length - 1;
            var dist = new StudentT(0, 1, df);

            // Two-tailed p-value
            return 2.0 * dist.CumulativeDistribution(-Math.Abs(t));
        }

        private static List<string> Solutions() =>
        [
            "Try adjusting the trading rules and/or the universe to get a strategy that outperforms the benchmark.",
        ];
    }
}
