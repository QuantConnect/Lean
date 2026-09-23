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
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects algorithms terminated by an <see cref="Isolator"/> time limit, inspecting the error
    /// message for known text fragments. The runtime error is read from the result state, falling
    /// back to the "Runtime Error:" log line for results that carry no state. It covers a single
    /// time loop exceeding the per-loop limit ("Algorithm took longer than N minutes on a single
    /// time loop"), the whole run outliving the maximum allowed wall-clock time ("Execution
    /// Security Error: Operation timed out - N minutes max", "Failed to complete algorithm within
    /// N seconds"), and code still running once the shutdown grace period expires after a stop
    /// request ("Operation was canceled").
    /// </summary>
    public class SingleTimeLoopTimeoutRuntimeErrorAnalysis : BaseResultsAnalysis
    {
        /// <summary>
        /// The patterns identifying the timeout error messages. Each pattern is a set of text
        /// fragments that must all be present in the error message (case-insensitive); the error
        /// matches when any pattern does.
        /// </summary>
        private static readonly string[][] ErrorMessagePatterns =
        [
            ["took longer than", "single time loop"],
            ["Operation timed out", "minutes max"],
            ["Failed to complete algorithm within"],
            ["Operation was canceled"],
        ];

        /// <summary>
        /// A timeout runtime error terminates the backtest, so there is no in-progress run to analyze.
        /// </summary>
        public override bool RunsInRun { get; } = false;

        /// <summary>
        /// Gets the description of the timeout issue.
        /// </summary>
        public override string Issue { get; } = "The algorithm was terminated: a single time loop took longer than the maximum allowed, " +
            "the whole run exceeded the maximum runtime, or its code kept running after a stop request.";

        /// <summary>
        /// Gets the severity weight for this analysis. A timeout is a fatal error that terminated
        /// the run, so it ranks above every non-fatal finding.
        /// </summary>
        public override int Weight { get; } = 100;

        /// <summary>
        /// Runs the runtime error analysis against the provided backtest parameters.
        /// </summary>
        public override IReadOnlyList<QuantConnect.Analysis> Run(ResultsAnalysisRunParameters parameters)
            => Run(parameters.Result?.State, parameters.Logs, parameters.Language);

        /// <summary>
        /// Runs the runtime error analysis against the algorithm state and logs.
        /// </summary>
        /// <param name="state">The algorithm state of the result, holding the runtime error message if any.</param>
        /// <param name="logs">The full list of log lines produced by the backtest.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <returns>A single response with the matched error message and solutions, or without them when the error is not found.</returns>
        public IReadOnlyList<QuantConnect.Analysis> Run(IDictionary<string, string> state, IReadOnlyList<string> logs, Language language)
        {
            var sample = GetRuntimeErrorMessages(state, logs).FirstOrDefault(Matches);
            return SingleResponse(sample, sample != null ? Solutions(language) : []);
        }

        /// <summary>
        /// Determines whether the given runtime error message matches any of the
        /// <see cref="ErrorMessagePatterns"/>.
        /// </summary>
        private static bool Matches(string message)
        {
            return ErrorMessagePatterns.Any(
                fragments => fragments.All(fragment => message.Contains(fragment, StringComparison.InvariantCultureIgnoreCase)));
        }

        /// <summary>
        /// Gets the candidate runtime error messages: the result state's runtime error entry when
        /// present, plus any "Runtime Error:" lines from the logs.
        /// </summary>
        private static IEnumerable<string> GetRuntimeErrorMessages(IDictionary<string, string> state, IReadOnlyList<string> logs)
        {
            if (state != null && state.TryGetValue("RuntimeError", out var error) && !string.IsNullOrEmpty(error))
            {
                yield return error;
            }

            foreach (var log in logs ?? [])
            {
                if (log.Contains("Runtime Error", StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return log;
                }
            }
        }

        /// <summary>
        /// Gets the suggested solutions to keep the algorithm within the time limits.
        /// </summary>
        private static List<string> Solutions(Language language)
        {
            var solutions = new List<string>
            {
                $"Avoid heavy work in event handlers (`{FormatCode(nameof(QCAlgorithm.OnData), language)}`, scheduled events, universe selection): " +
                    "instead of recomputing values on every update, update them incrementally with rolling windows, consolidators or indicators.",

                "Reduce the universe size: select only the securities the strategy trades, " +
                    $"and narrow option and future chains with the `{FormatCode("SetFilter", language)}` strike and expiration filters.",

                "Reduce the number of subscribed securities or the data resolution.",

                "Avoid large history requests inside event handlers; request only the period needed or maintain the data incrementally.",

                $"Run machine learning training through the `{FormatCode(nameof(QCAlgorithm.Train), language)}` method, which allocates extra time for it.",

                "Check for infinite or recursive loops that may never exit: they keep the algorithm running and surface as this error.",

                "If the algorithm was stopped or deleted on purpose, an \"Operation was canceled\" error only means " +
                    "its code was still running when the shutdown grace period expired, and can be ignored.",
            };
            if (language == Language.Python)
            {
                solutions.Add("Vectorize heavy numeric loops with numpy or pandas.");
            }
            return solutions;
        }
    }
}
