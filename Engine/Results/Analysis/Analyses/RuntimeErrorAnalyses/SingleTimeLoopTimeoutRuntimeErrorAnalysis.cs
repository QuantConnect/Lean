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
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects algorithms terminated because their code kept running past an engine time limit,
    /// emitted by the <see cref="Isolator"/> when a single time loop exceeds the per-loop limit
    /// ("Algorithm took longer than N minutes on a single time loop") or when the algorithm is
    /// asked to stop but its code is still running once the shutdown grace period expires
    /// ("Operation was canceled").
    /// </summary>
    public class SingleTimeLoopTimeoutRuntimeErrorAnalysis : RuntimeErrorAnalysis
    {
        /// <summary>
        /// Gets the description of the time loop timeout issue.
        /// </summary>
        public override string Issue { get; } = "The algorithm was terminated: a single time loop took longer than the maximum allowed, " +
            "or its code kept running after a stop request.";

        /// <summary>
        /// Gets the severity weight for this analysis. A timeout is a fatal error that terminated
        /// the run, so it ranks above every non-fatal finding.
        /// </summary>
        public override int Weight { get; } = 100;

        /// <summary>
        /// Gets the patterns identifying the time loop timeout and forced cancellation error messages.
        /// </summary>
        protected override string[][] ErrorMessagePatterns { get; } =
        [
            ["took longer than", "single time loop"],
            ["Operation was canceled"],
        ];

        /// <summary>
        /// Gets the suggested solutions to keep each time loop within the time limit.
        /// </summary>
        protected override List<string> Solutions(Language language)
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

                "Check for loops that may never exit: an infinite loop in an event handler surfaces as this error.",

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
