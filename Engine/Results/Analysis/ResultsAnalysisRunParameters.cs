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
using QuantConnect.Algorithm;
using System;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.Results.Analysis
{
    /// <summary>
    /// Bundles all dependencies that a <see cref="Analyses.BaseResultsAnalysis"/> may need,
    /// so every analysis shares a single <c>Run(ResultsAnalysisRunContext)</c> entry point.
    /// </summary>
    public class ResultsAnalysisRunParameters
    {
        /// <summary>
        /// The backtest result being analysed.
        /// </summary>
        public Result Result { get; }

        /// <summary>
        /// The algorithm instance used for history requests and API queries.
        /// </summary>
        public QCAlgorithm Algorithm { get; }

        /// <summary>
        /// The programming language the algorithm is written in.
        /// </summary>
        public Language Language { get; }

        /// <summary>
        /// The full list of log lines produced by the backtest.
        /// </summary>
        public IReadOnlyList<string> Logs { get; }

        /// <summary>
        /// Daily equity values for the strategy, keyed by date.
        /// </summary>
        public SortedList<DateTime, decimal> EquityCurve { get; }

        /// <summary>
        /// Daily equity values for the benchmark (SPY), keyed by date.
        /// </summary>
        public SortedList<DateTime, decimal> BenchmarkEquityCurve { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultsAnalysisRunParameters"/> class with the specified dependencies.
        /// </summary>
        public ResultsAnalysisRunParameters(
            Result result,
            QCAlgorithm algorithm,
            Language language,
            IReadOnlyList<string> logs,
            SortedList<DateTime, decimal> equityCurve,
            SortedList<DateTime, decimal> benchmarkEquityCurve)
        {
            Result = result;
            Algorithm = algorithm;
            Language = language;
            Logs = logs;
            EquityCurve = equityCurve;
            BenchmarkEquityCurve = benchmarkEquityCurve;
        }
    }
}
