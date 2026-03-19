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
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects slow execution by parsing the last log line.
    /// Benchmark speeds: https://www.quantconnect.com/performance
    /// </summary>
    public class ExecutionSpeedAnalysis : BaseResultsAnalysis
    {
        /// <summary>
        /// Gets the description of the slow execution issue.
        /// </summary>
        public override string Issue { get; } = "The algorithm ran below 40k data points per second.";

        /// <summary>
        /// Gets the severity weight for the execution speed analysis.
        /// </summary>
        public override int Weight { get; } = 77;

        /// <summary>
        /// Runs the execution speed analysis against the provided backtest parameters.
        /// </summary>
        public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.Logs);

        private static readonly Regex DataPointsPerSecondRegex = new(
            @"Algorithm Id:\([^)]+\) completed in ([\d.]+) seconds at (\d+)k data points per second\. Processing total of [\d,]+ data points\.",
            RegexOptions.Compiled);

        /// <summary>
        /// Parses the backtest logs to determine execution speed and flags backtests that ran slowly.
        /// </summary>
        /// <param name="logs">The full list of log lines produced by the backtest.</param>
        /// <returns>Analysis results flagging slow execution when below 40k data points per second and runtime is at least 10 seconds.</returns>
        public IReadOnlyList<AnalysisResult> Run(IReadOnlyList<string> logs)
        {
            var result = TryGetDataPointsPerSecond(logs, out var timeInSeconds, out var dataPointsPerSecond) && timeInSeconds >= 10 && dataPointsPerSecond < 40
                ? $"The algorithm is slowly executing at only {dataPointsPerSecond}k data points per second"
                : null;

            var potentialSolutions = result is not null ? Solutions() : [];
            return SingleResponse(new ResultsAnalysisContext(result), potentialSolutions);
        }

        /// <summary>
        /// Searches <paramref name="logs"/> in reverse order for a completion line and extracts
        /// the execution time and data points per second (in thousands).
        /// Example match: "Algorithm Id:(Foo) completed in 25.68 seconds at 85k data points per second."
        /// returns seconds=25.68, dataPointsPerSecond=85.
        /// </summary>
        private static bool TryGetDataPointsPerSecond(IReadOnlyList<string> logs, out double? timeInSeconds, out int? dataPointsPerSecond)
        {
            for (var i = logs.Count - 1; i >= 0; i--)
            {
                var match = DataPointsPerSecondRegex.Match(logs[i]);
                if (match.Success)
                {
                    timeInSeconds = double.Parse(match.Groups[1].Value, NumberFormatInfo.InvariantInfo);
                    dataPointsPerSecond = int.Parse(match.Groups[2].Value, NumberFormatInfo.InvariantInfo);
                    return true;
                }
            }

            timeInSeconds = null;
            dataPointsPerSecond = null;
            return false;
        }

        /// <summary>
        /// Returns suggested solutions for improving execution speed.
        /// </summary>
        private static List<string> Solutions() =>
        [
            "Review the algorithm code for inefficiencies.",

            "If there is a universe, reduce its size.",

            "Reduce the data resolution.",

            "If the algorithm is training a model, reduce the amount of training data or reduce the number of epochs in the training process.",
        ];
    }
}
