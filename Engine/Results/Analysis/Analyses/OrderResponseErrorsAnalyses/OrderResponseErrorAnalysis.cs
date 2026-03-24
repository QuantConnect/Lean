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
using QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Abstract base class for analyses that detect specific order-response errors
    /// by inspecting invalid order events for known message text fragments.
    /// </summary>
    public abstract class OrderResponseErrorAnalysis : MessageAnalysis
    {
        /// <summary>
        /// Filters <paramref name="orderEvents"/> to those with <see cref="OrderStatus.Invalid"/> status
        /// whose message contains all <see cref="MessageAnalysis.ExpectedMessageText"/> fragments.
        /// </summary>
        /// <param name="orderEvents">The order events to inspect.</param>
        /// <returns>An enumerable of matching message strings.</returns>
        protected IEnumerable<string> GetMatchingOrderEventsMessages(List<OrderEvent> orderEvents)
        {
            return orderEvents
                .Where(orderEvent => orderEvent.Status == OrderStatus.Invalid && orderEvent.Message != null && 
                    ExpectedMessageText.All(messagePart => orderEvent.Message.Contains(messagePart, StringComparison.InvariantCultureIgnoreCase)))
                .Select(x => x.Message);
        }

        /// <inheritdoc/>
        /// <remarks>Overrides the log-based default from <see cref="MessageAnalysis"/> to scan order events instead.</remarks>
        public override IReadOnlyList<QuantConnect.Analysis> Run(ResultsAnalysisRunParameters parameters)
            => Run([.. parameters.Result.OrderEvents], parameters.Language);

        /// <summary>
        /// Runs the analysis against a list of order events, extracting matching invalid-event messages
        /// and delegating to the message-based <see cref="MessageAnalysis.Run(IReadOnlyList{string}, Language)"/> overload.
        /// </summary>
        /// <param name="orderEvents">The order events from the backtest result.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <returns>Analysis results when any matching order response errors are found.</returns>
        public virtual IReadOnlyList<QuantConnect.Analysis> Run(List<OrderEvent> orderEvents, Language language)
        {
            return Run(GetMatchingOrderEventsMessages(orderEvents).ToList(), language);
        }
    }
}
