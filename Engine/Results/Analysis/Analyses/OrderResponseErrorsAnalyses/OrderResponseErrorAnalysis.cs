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
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    public abstract class MessageAnalysis : BaseBacktestAnalysis
    {
        protected abstract string[] ExpectedMessageText { get; }

        protected IEnumerable<string> Match(List<string> messages, string[] expectedMessages)
        {
            return messages
                .Where(message => expectedMessages.All(messagePart => message.Contains(messagePart, StringComparison.InvariantCultureIgnoreCase)));
        }

        protected virtual List<string> GetMatches(List<string> messages)
        {
            return Match(messages, ExpectedMessageText).ToList();
        }

        public virtual IReadOnlyList<BacktestAnalysisResult> Run(List<string> messages, Language language)
        {
            var foundMessages = GetMatches(messages);
            var potentialSolutions = foundMessages.Count > 0 ? PotentialSolutions(language) : [];
            return SingleResponse(foundMessages.Count > 0 ? (object)foundMessages : null, potentialSolutions);
        }

        protected abstract List<string> PotentialSolutions(Language language);
    }

    public abstract class OrderResponseErrorAnalysis : MessageAnalysis
    {
        protected IEnumerable<string> GetMatchingOrderEventsMessages(List<OrderEvent> orderEvents)
        {
            return orderEvents
                .Where(orderEvent => orderEvent.Status == OrderStatus.Invalid && ExpectedMessageText.All(messagePart => orderEvent.Message.Contains(messagePart, StringComparison.InvariantCultureIgnoreCase)))
                .Select(x => x.Message);
        }

        public virtual IReadOnlyList<BacktestAnalysisResult> Run(List<OrderEvent> orderEvents, Language language)
        {
            return Run(GetMatchingOrderEventsMessages(orderEvents).ToList(), language);
        }
    }
}
