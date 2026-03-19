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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Block-bootstrap Monte Carlo test: flags strategies whose total return
    /// is in the top 10 % of simulated outcomes (potentially lucky).
    /// </summary>
    public class MonteCarloPercentile : BaseResultsAnalysis
    {
        public override string Issue { get; } =  "The equity curve is very optimistic. " +
            "It has a greater ending equity than more than 90% of the simulated equity curves, indicating the performance was unusually lucky due to a sequence of favorable days.";

        public override int Weight { get; } = 69;
        public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.EquityCurve);

        /// <summary>
        /// Runs the Monte Carlo percentile test against the given equity curve.
        /// </summary>
        /// <param name="backtestEquity">Daily equity values from the backtest, keyed by date.</param>
        /// <returns>Analysis results indicating whether the strategy's return is suspiciously high.</returns>
        public IReadOnlyList<AnalysisResult> Run(SortedList<DateTime, decimal> backtestEquity)
        {
            if (backtestEquity.Count == 0)
            {
                return SingleResponse(new ResultsAnalysisContext(null));
            }

            var returns = backtestEquity.PercentChange().Values.ToArray();

            if (returns.Length == 0)
            {
                return SingleResponse(new ResultsAnalysisContext(null));
            }

            var simulatedTotalReturns = RunSimulation(returns, nSims: 5);

            var backtestVals = backtestEquity.Values.ToArray();
            var backtestTotalReturn = backtestVals[^1] / backtestVals[0] - 1m;

            var percentile = simulatedTotalReturns.Count(r => r < backtestTotalReturn) / simulatedTotalReturns.Length * 100m;

            var result = percentile > 90m ? new { Percentile = percentile } : null;
            var potentialSolutions = result is not null ? Solutions() : [];
            return SingleResponse(new ResultsAnalysisContext(result), potentialSolutions);
        }

        private static decimal[] RunSimulation(decimal[] returns, int nSims = 5000, int blockSize = 20)
        {
            blockSize = Math.Min(blockSize, returns.Length);
            var rng = new Random(42);
            var n = returns.Length;
            var nBlocks = n / blockSize + 1;
            var simulatedTotalReturns = new decimal[nSims];

            for (var sim = 0; sim < nSims; sim++)
            {
                var simReturns = new decimal[n];
                var filled = 0;

                for (var b = 0; b < nBlocks && filled < n; b++)
                {
                    var start = rng.Next(0, n - blockSize + 1);
                    var toCopy = Math.Min(blockSize, n - filled);
                    Array.Copy(returns, start, simReturns, filled, toCopy);
                    filled += toCopy;
                }

                var totalReturn = 1m;
                for (var i = 0; i < n; i++)
                {
                    totalReturn *= (1m + simReturns[i]);
                }
                simulatedTotalReturns[sim] = totalReturn - 1m;
            }

            return simulatedTotalReturns;
        }

        private static List<string> Solutions() =>
        [
            "Try to scale position sizes based on recent volatility, so the algorithm has smaller position sizes during volatile periods.",
        ];
    }
}
