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
    public abstract class MessageAnalysis : BaseResultsAnalysis
    {
        protected abstract string[] ExpectedMessageText { get; }

        /// <summary>
        /// Returns messages from <paramref name="messages"/> that contain all strings in <paramref name="expectedMessages"/>
        /// (case-insensitive).
        /// </summary>
        protected IEnumerable<string> Match(IReadOnlyList<string> messages, string[] expectedMessages)
        {
            return messages
                .Where(message => expectedMessages.All(messagePart => message.Contains(messagePart, StringComparison.InvariantCultureIgnoreCase)));
        }

        /// <inheritdoc/>
        public override IReadOnlyList<QuantConnect.Analysis> Run(ResultsAnalysisRunParameters parameters)
            => Run(parameters.Logs, parameters.Language);

        /// <summary>
        /// Runs the analysis by scanning <paramref name="messages"/> for the expected text fragments
        /// and returns results with solutions when matches are found.
        /// </summary>
        public virtual IReadOnlyList<QuantConnect.Analysis> Run(IReadOnlyList<string> messages, Language language)
        {
            var foundMessages = Match(messages, ExpectedMessageText).ToList();
            var solutions = foundMessages.Count > 0 ? Solutions(language) : [];
            return SingleResponse(foundMessages.Count > 0 ? foundMessages[0] : null, foundMessages.Count > 1 ? foundMessages.Count : null, solutions);
        }

        protected abstract List<string> Solutions(Language language);
    }
}
