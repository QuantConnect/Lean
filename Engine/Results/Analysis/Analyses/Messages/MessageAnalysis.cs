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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages
{
    /// <summary>
    /// Abstract base class for analyses that detect issues by scanning log or order event messages
    /// for one or more expected text fragments.
    /// </summary>
    public abstract class MessageAnalysis : BaseBacktestAnalysis
    {
        protected abstract string[] ExpectedMessageText { get; }

        /// <summary>
        /// Returns messages from <paramref name="messages"/> that contain all strings in <paramref name="expectedMessages"/>
        /// (case-insensitive).
        /// </summary>
        /// <param name="messages">The candidate messages to search.</param>
        /// <param name="expectedMessages">All substrings that must be present in a message for it to match.</param>
        /// <returns>An enumerable of matching messages.</returns>
        protected IEnumerable<string> Match(IReadOnlyList<string> messages, string[] expectedMessages)
        {
            return messages
                .Where(message => expectedMessages.All(messagePart => message.Contains(messagePart, StringComparison.InvariantCultureIgnoreCase)));
        }

        /// <summary>
        /// Runs the analysis by scanning <paramref name="messages"/> for the expected text fragments
        /// and returns results with solutions when matches are found.
        /// </summary>
        /// <param name="messages">The log or message strings to scan.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <returns>Analysis results containing potential solutions when any matching messages are found.</returns>
        public virtual IReadOnlyList<BacktestAnalysisResult> Run(IReadOnlyList<string> messages, Language language)
        {
            var foundMessages = Match(messages, ExpectedMessageText).ToList();
            var potentialSolutions = foundMessages.Count > 0 ? PotentialSolutions(language) : [];
            return SingleResponse(new BacktestAnalysysRepeatedContext(foundMessages), potentialSolutions);
        }

        protected abstract List<string> PotentialSolutions(Language language);
    }
}
