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
using QuantConnect.Lean.Engine.Results.Analysis.Utils;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Block-bootstrap Monte Carlo test: flags strategies whose total return
    /// is in the top 10 % of simulated outcomes (potentially lucky).
    /// </summary>
    public class MonteCarloPercentile : BaseBacktestAnalysis
    {
        public IReadOnlyList<BacktestAnalysisResult> Run(Series<DateTime, double> backtestEquity)
        {
            var returns = backtestEquity.PctChange().Values.ToArray(); // skip first (NaN) handled by PctChange

            var simulatedTotalReturns = RunSimulation(returns, nSims: 5);

            var backtestVals = backtestEquity.Values.ToArray();
            double backtestTotalReturn = backtestVals[^1] / backtestVals[0] - 1.0;

            double percentile = simulatedTotalReturns.Count(r => r < backtestTotalReturn)
                                / (double)simulatedTotalReturns.Length * 100.0;

            object? result = percentile > 90 ? new { percentile } : null;
            var potentialSolutions = result is not null ? PotentialSolutions() : [];
            return SingleResponse(new BacktestAnalysysContext(result), potentialSolutions);
        }

        private static double[] RunSimulation(double[] returns, int nSims = 5000, int blockSize = 20)
        {
            var rng = new Random(42);
            int n = returns.Length;
            var simulatedTotalReturns = new double[nSims];

            for (int sim = 0; sim < nSims; sim++)
            {
                int nBlocks = n / blockSize + 1;
                var simReturns = new List<double>(nBlocks * blockSize);

                for (int b = 0; b < nBlocks; b++)
                {
                    int start = rng.Next(0, n - blockSize + 1);
                    for (int k = start; k < start + blockSize; k++)
                        simReturns.Add(returns[k]);
                }

                // Trim to original length then compute total return.
                double totalReturn = 1.0;
                for (int i = 0; i < n; i++)
                    totalReturn *= 1.0 + simReturns[i];
                simulatedTotalReturns[sim] = totalReturn - 1.0;
            }

            return simulatedTotalReturns;
        }

        private static List<string> PotentialSolutions() =>
        [
            "The equity curve is very optimistic. " +
            "It has a greater ending equity than more than 90% of the simulated equity curves, indicating the performance was unusually lucky due to a sequence of favorable days. " +
            "Try to scale position sizes based on recent volatility, so the algorithm has smaller position sizes during volatile periods.",
        ];
    }
}
