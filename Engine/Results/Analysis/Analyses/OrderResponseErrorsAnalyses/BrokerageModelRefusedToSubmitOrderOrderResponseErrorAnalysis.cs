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

using Alpaca = QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.AlpacaBrokerageModel;
using Binance = QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.BinanceBrokerageModel;
using Coinbase = QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.CoinbaseBrokerageModel;
using DefaultBrokerage = QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.DefaultBrokerageModel;
using IB = QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.InteractiveBrokersBrokerageModel;
using Rbi = QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.RbiBrokerageModel;
using Tradier = QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.TradierBrokerageModel;
using TradingTech = QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.TradingTechnologiesBrokerageModel;
using Wolverine = QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.WolverineBrokerageModel;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects brokerage-model-refused-to-submit-order errors and dispatches to
    /// per-message sub-tests to surface specific solutions.
    /// </summary>
    public class BrokerageModelRefusedToSubmitOrderOrderResponseErrorAnalysis : OrderResponseErrorAnalysis
    {
        public override string Issue { get; } = "Brokerage model refused to submit orders";

        public override int Weight { get; } = 95;

        private static readonly MessageAnalysis[] Analyses =
        [
            // AlpacaBrokerageModel
            new Alpaca.TradingOutsideRegularHoursNotSupported(),
            // BinanceBrokerageModel
            new Binance.UnsupportedOrderTypeWithLinkToSupportedTypesAnalysis(),
            // CoinbaseBrokerageModel
            new Coinbase.StopMarketOrdersNoLongerSupportedAnalysis(),
            // DefaultBrokerageModel
            new DefaultBrokerage.InvalidOrderQuantityAnalysis(),
            new DefaultBrokerage.InvalidOrderSizeAnalysis(),
            new DefaultBrokerage.NoDataForSymbolAnalysis(),
            new DefaultBrokerage.OrderUpdateNotSupportedAnalysis(),
            new DefaultBrokerage.UnsupportedCrossZeroByOrderTypeAnalysis(),
            new DefaultBrokerage.UnsupportedMarketOnOpenOrderTimeAnalysis(),
            new DefaultBrokerage.UnsupportedMarketOnOpenOrdersForFutureAndFutureOptionsAnalysis(),
            new DefaultBrokerage.UnsupportedOrderTypeAnalysis(),
            new DefaultBrokerage.UnsupportedSecurityTypeAnalysis(),
            new DefaultBrokerage.UnsupportedTimeInForceAnalysis(),
            // InteractiveBrokersBrokerageModel
            new IB.InvalidForexOrderSizeAnalysis(),
            new IB.UnsupportedExerciseForIndexAndCashSettledOptionsAnalysis(),
            // RBIBrokerageModel
            new Rbi.RbiUnsupportedOrderTypeAnalysis(),
            // TradierBrokerageModel
            new Tradier.ExtendedMarketHoursTradingNotSupportedOutsideExtendedSessionAnalysis(),
            new Tradier.IncorrectOrderQuantityAnalysis(),
            new Tradier.SellShortOrderLastPriceBelow5Analysis(),
            new Tradier.ShortOrderIsGtcAnalysis(),
            new Tradier.TradierUnsupportedSecurityTypeAnalysis(),
            new Tradier.UnsupportedTimeInForceTypeAnalysis(),
            // TradingTechnologiesBrokerageModel
            new TradingTech.InvalidStopLimitOrderLimitPriceAnalysis(),
            new TradingTech.InvalidStopLimitOrderPriceAnalysis(),
            new TradingTech.InvalidStopMarketOrderPriceAnalysis(),
            // WolverineBrokerageModel
            new Wolverine.WolverineUnsupportedOrderTypeAnalysis(),
        ];

        protected override string[] ExpectedMessageText { get; } =
        [
            "BrokerageModel declared unable to submit order: ",
        ];

        /// <summary>
        /// Filters order events for brokerage-refused-to-submit errors and dispatches the matched
        /// messages to each per-brokerage sub-analysis to surface specific solutions.
        /// </summary>
        /// <param name="orderEvents">The order events from the backtest result.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <returns>Aggregated analysis results from all sub-analyses that detected a matching message.</returns>
        public override IReadOnlyList<AnalysisResult> Run(List<OrderEvent> orderEvents, Language language)
        {
            var matchedMessages = GetMatchingOrderEventsMessages(orderEvents).ToList();
            if (matchedMessages.Count == 0)
            {
                return [];
            }

            return CreateAggregatedResponse(Analyses.SelectMany(analysis => analysis.Run(matchedMessages, language)));
        }

        protected override List<string> Solutions(Language language) => [];
    }
}
