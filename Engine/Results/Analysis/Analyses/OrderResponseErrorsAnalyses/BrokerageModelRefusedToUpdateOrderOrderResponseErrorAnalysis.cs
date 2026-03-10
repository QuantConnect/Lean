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
using System.Linq;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages;
using QuantConnect.Orders;

using Default = QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.DefaultBrokerageModel;
using IB = QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.InteractiveBrokersBrokerageModel;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects brokerage-model-refused-to-update-order errors.
    /// Error code: OrderResponseErrorCode.BROKERAGE_MODEL_REFUSED_TO_UPDATE_ORDER (-25)
    /// </summary>
    public class BrokerageModelRefusedToUpdateOrderOrderResponseErrorAnalysis : OrderResponseErrorAnalysis
    {
        private static readonly MessageAnalysis[] Analyses =
        [
            // DefaultBrokerageModel
            new Default.InvalidOrderQuantityAnalysis(),
            new Default.OrderUpdateNotSupportedAnalysis(),
            new Default.UnsupportedCrossZeroOrderUpdateAnalysis(),
            new Default.UnsupportedOrderTypeAnalysis(),
            new Default.UnsupportedSecurityTypeAnalysis(),
            new Default.UnsupportedUpdateQuantityOrderAnalysis(),
            // InteractiveBrokersBrokerageModel
            new IB.InvalidForexOrderSizeAnalysis(),
        ];

        protected override string[] ExpectedMessageText { get; } =
        [
            "BrokerageModel declared unable to update order: ",
        ];

        public override IReadOnlyList<BacktestAnalysisResult> Run(List<OrderEvent> orderEvents, Language language)
        {
            var matchedMessages = GetMatchingOrderEventsMessages(orderEvents).ToList();
            if (matchedMessages.Count == 0)
            {
                return [];
            }

            return CreateAggregatedResponse(Analyses.SelectMany(analysis => analysis.Run(matchedMessages, language)));
        }

        protected override List<string> PotentialSolutions(Language language) => [];
    }
}
