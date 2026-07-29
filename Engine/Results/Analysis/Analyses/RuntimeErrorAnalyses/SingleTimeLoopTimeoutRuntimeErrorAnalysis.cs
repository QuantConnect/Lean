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
    /// Detects algorithms terminated because a single time loop exceeded the per-loop time limit,
    /// emitted by the <see cref="Isolator"/> when the algorithm manager's time loop tracker trips
    /// ("Algorithm took longer than N minutes on a single time loop").
    /// </summary>
    public class SingleTimeLoopTimeoutRuntimeErrorAnalysis : RuntimeErrorAnalysis
    {
        /// <summary>
        /// Gets the description of the single time loop timeout issue.
        /// </summary>
        public override string Issue { get; } = "A single time loop took longer than the maximum allowed, so the algorithm was terminated.";

        /// <summary>
        /// Gets the severity weight for this analysis. A timeout is a fatal error that terminated
        /// the run, so it ranks above every non-fatal finding.
        /// </summary>
        public override int Weight { get; } = 100;

        /// <summary>
        /// Gets the patterns identifying the single time loop timeout error message.
        /// </summary>
        protected override string[][] ErrorMessagePatterns { get; } =
        [
            ["took longer than", "single time loop"],
        ];

        /// <summary>
        /// Gets the suggested solutions to keep each time loop within the time limit.
        /// </summary>
        protected override List<string> Solutions(Language language)
        {
            var solutions = new List<string>
            {
                $"Look for heavy work inside a single event handler (`{FormatCode(nameof(QCAlgorithm.OnData), language)}`, scheduled events, universe selection functions): " +
                    "large or nested loops over all securities, or values recomputed from scratch on every data update. " +
                    "Cache the values and update them incrementally with rolling windows, consolidators or indicators instead.",

                "If there is a universe, reduce its size and filter it: return from the selection functions only the securities the strategy actually trades, " +
                    $"and narrow option and future chain universes with the `{FormatCode("SetFilter", language)}` contract filters (strikes and expirations).",

                "Reduce the number of subscribed securities or the data resolution to lower the amount of data processed on each time loop.",

                "Avoid issuing large history requests inside event handlers; request only the period the strategy needs, " +
                    "or maintain the data incrementally instead of re-requesting it on every data update.",

                $"If the algorithm trains a machine learning model, run the training through the `{FormatCode(nameof(QCAlgorithm.Train), language)}` method, " +
                    "which allocates additional time for it to complete.",

                "Check for loops whose exit condition may never be met: an infinite loop in an event handler surfaces as this error.",
            };
            if (language == Language.Python)
            {
                solutions.Add("Vectorize heavy numeric Python loops with numpy or pandas operations, which run orders of magnitude faster.");
            }
            return solutions;
        }
    }
}
