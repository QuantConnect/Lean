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
    /// <summary>Compares the full-period Sharpe ratio of the strategy to the benchmark.</summary>
    public class PerformanceRelativeToBenchmark : BaseBacktestAnalysis
    {
        public IReadOnlyList<BacktestAnalysisResult> Run(QCAlgorithm algorithm, SortedList<DateTime, decimal> backtestEquity,
            SortedList<DateTime, decimal> benchmarkEquity)
        {
            var (backtestSharpe, benchmarkSharpe) = CrisisEventsAnalysis.CalculateSharpeRatio(backtestEquity, benchmarkEquity, 
                algorithm.RiskFreeInterestRateModel);

            var result = backtestSharpe < benchmarkSharpe
                ? new { BacktestSharpe = backtestSharpe, BenchmarkSharpe = benchmarkSharpe }
                : null;

            var potentialSolutions = result is not null ? PotentialSolutions() : [];
            return SingleResponse(new BacktestAnalysysContext(result), potentialSolutions);
        }

        private static List<string> PotentialSolutions() =>
        [
            "The strategy has a lower Sharpe ratio than the benchmark. " +
            "Try adjusting the trading rules and/or the universe to get a strategy that outperforms the benchmark.",
        ];
    }
}
