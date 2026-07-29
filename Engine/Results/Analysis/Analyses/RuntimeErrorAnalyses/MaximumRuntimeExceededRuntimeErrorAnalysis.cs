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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects algorithms terminated because the whole run outlived the maximum allowed wall-clock
    /// time, emitted by the <see cref="Isolator"/> ("Execution Security Error: Operation timed out -
    /// N minutes max") or by the engine when the isolator reports an incomplete run ("Failed to
    /// complete algorithm within N seconds").
    /// </summary>
    public class MaximumRuntimeExceededRuntimeErrorAnalysis : RuntimeErrorAnalysis
    {
        /// <summary>
        /// Gets the description of the maximum runtime exceeded issue.
        /// </summary>
        public override string Issue { get; } = "The algorithm exceeded the maximum allowed total runtime and was terminated.";

        /// <summary>
        /// Gets the severity weight for this analysis. The timeout is a fatal error that terminated
        /// the run, so it ranks above every non-fatal finding.
        /// </summary>
        public override int Weight { get; } = 100;

        /// <summary>
        /// Gets the patterns identifying the maximum runtime exceeded error message.
        /// </summary>
        protected override string[][] ErrorMessagePatterns { get; } =
        [
            ["Operation timed out", "minutes max"],
            ["Failed to complete algorithm within"],
        ];

        /// <summary>
        /// Gets the suggested solutions to complete the run within the maximum allowed runtime.
        /// </summary>
        protected override List<string> Solutions(Language language) =>
        [
            "Reduce the backtest period so the run completes within the allowed runtime.",

            "Reduce the universe size and the data resolution to lower the total amount of data processed.",

            "Review the algorithm code for inefficiencies: avoid recomputing values from scratch on every data update, " +
                "and avoid history requests whose range grows as the backtest progresses.",

            "Check for infinite or recursive loops that keep the algorithm running without making progress.",
        ];
    }
}
