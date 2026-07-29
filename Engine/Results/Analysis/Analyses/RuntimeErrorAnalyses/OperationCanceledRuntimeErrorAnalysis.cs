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
    /// Detects algorithms that were forcefully canceled, emitted by the <see cref="Isolator"/> when
    /// the algorithm is asked to stop but its code is still running once the shutdown grace period
    /// expires ("Operation was canceled").
    /// </summary>
    public class OperationCanceledRuntimeErrorAnalysis : RuntimeErrorAnalysis
    {
        /// <summary>
        /// Gets the description of the forced cancellation issue.
        /// </summary>
        public override string Issue { get; } = "The algorithm did not shut down within the grace period after a stop request and was forcefully canceled.";

        /// <summary>
        /// Gets the severity weight for this analysis. The cancellation is a fatal error that
        /// terminated the run, so it ranks above every non-fatal finding.
        /// </summary>
        public override int Weight { get; } = 100;

        /// <summary>
        /// Gets the patterns identifying the forced cancellation error message.
        /// </summary>
        protected override string[][] ErrorMessagePatterns { get; } =
        [
            ["Operation was canceled"],
        ];

        /// <summary>
        /// Gets the suggested solutions to let the algorithm respond to stop requests promptly.
        /// </summary>
        protected override List<string> Solutions(Language language) =>
        [
            "This error is raised when the algorithm is asked to stop, typically because the user stopped or deleted it, " +
                "but its code keeps running past the shutdown grace period. If you stopped it on purpose, the error is expected and can be ignored.",

            "If you did not stop the algorithm, reduce the work done per event handler so it can respond to stop requests promptly: " +
                "break up big loops, avoid long blocking operations, and avoid large history requests or model training on every data update.",

            "Reduce the universe size and the data resolution so each time loop processes less data and control returns to the engine sooner.",
        ];
    }
}
