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
    /// Abstract base class for analyses that detect a specific runtime error that terminated the
    /// algorithm by inspecting the error message for known text fragments. The runtime error is
    /// read from the result state, falling back to the "Runtime Error:" log line for results
    /// that carry no state.
    /// </summary>
    public abstract class RuntimeErrorAnalysis : BaseResultsAnalysis
    {
        /// <summary>
        /// Gets the patterns identifying the runtime error. Each pattern is a set of text fragments
        /// that must all be present in the error message (case-insensitive); the error matches
        /// when any pattern does.
        /// </summary>
        protected abstract string[][] ErrorMessagePatterns { get; }

        /// <summary>
        /// Gets the suggested solutions for the detected runtime error.
        /// </summary>
        protected abstract List<string> Solutions(Language language);

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
        private bool Matches(string message)
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
    }
}
