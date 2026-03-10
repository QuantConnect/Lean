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
using Deedle;
using MathNet.Numerics.Distributions;
using QuantConnect.Lean.Engine.Results.Analysis.Utils;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// One-sample t-test: tests whether the strategy's excess daily returns
    /// (over benchmark) have a mean significantly greater than zero.
    /// Mirrors <c>tests/statistical_significance_of_daily_returns.py</c>.
    /// </summary>
    public class StatisticalSignificanceOfDailyReturnsAnalysis : BaseBacktestAnalysis
    {
        public IReadOnlyList<BacktestAnalysisResult> Run(
            Series<DateTime, double> backtestEquity,
            Series<DateTime, double> benchmarkEquity)
        {
            var backtestReturns  = backtestEquity.PctChange();
            var benchmarkReturns = benchmarkEquity.PctChange();

            // Excess daily returns (drop the first NaN row)
            var backtestKeys  = backtestReturns.Keys.ToArray();
            var backtestVals  = backtestReturns.Values.ToArray();
            var benchmarkDict = benchmarkReturns.Keys
                .Zip(benchmarkReturns.Values)
                .ToDictionary(t => t.First, t => t.Second);

            var excess = backtestKeys
                .Skip(1)   // mirrors pct_change()[1:]
                .Where(k => benchmarkDict.ContainsKey(k))
                .Select(k => benchmarkDict.TryGetValue(k, out var bv)
                    ? backtestVals[Array.IndexOf(backtestKeys, k)] - bv
                    : double.NaN)
                .Where(v => !double.IsNaN(v))
                .ToArray();

            double pValue = OneSampleTAnalysis(excess, 0.0);
            pValue /= 2; // one-tailed (positive direction)

            object? result = pValue > 0.05 ? new { pValue } : null;
            var potentialSolutions = result is not null ? PotentialSolutions() : [];
            return SingleResponse(result, potentialSolutions);
        }

        /// <summary>
        /// Returns the two-tailed p-value for a one-sample t-test against
        /// <paramref name="popmean"/> using the Student-t distribution.
        /// </summary>
        private static double OneSampleTAnalysis(double[] sample, double popmean)
        {
            if (sample.Length < 2) return 1.0;

            double mean   = sample.Average();
            double diff   = mean - popmean;
            double stdErr = SampleStdDev(sample) / Math.Sqrt(sample.Length);
            if (stdErr == 0) return 1.0;

            double t   = diff / stdErr;
            int    df  = sample.Length - 1;
            var    dist = new StudentT(0, 1, df);

            // Two-tailed p-value
            return 2.0 * dist.CumulativeDistribution(-Math.Abs(t));
        }

        private static double SampleStdDev(double[] data)
        {
            double mean = data.Average();
            double sum  = data.Sum(v => (v - mean) * (v - mean));
            return Math.Sqrt(sum / (data.Length - 1));
        }

        private static List<string> PotentialSolutions() =>
        [
            "The distribution of the strategy's daily returns in excess of the benchmark's daily returns has a p-value above 0.05. " +
            "Therefore, we fail to reject the null hypothesis that the mean of this distribution is above zero. " +
            "Try adjusting the trading rules and/or the universe to get a strategy that outperforms the benchmark.",
        ];
    }
}
