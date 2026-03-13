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
using QuantConnect.Orders;
using System;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects orders rejected because they exceed the available shortable quantity.
    /// </summary>
    public class ExceedsShortableQuantityOrderResponseErrorAnalysis : BaseBacktestAnalysis
    {
        private static readonly string[] MessageText =
        [
            "Order exceeds shortable quantity ",
            " for Symbol ",
            " requested ",
        ];

        /// <summary>
        /// Searches order events for exceeds-shortable-quantity rejection messages.
        /// </summary>
        /// <param name="orderEvents">The order events from the backtest result.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <returns>Analysis results when shortable quantity violations are detected.</returns>
        public IReadOnlyList<BacktestAnalysisResult> Run(IReadOnlyList<OrderEvent> orderEvents, Language language)
        {
            var result = orderEvents
                .Where(e => e.Message != null && MessageText.All(t => e.Message.Contains(t, StringComparison.InvariantCultureIgnoreCase)))
                //.Select(OrdersReader.ParseOrderEvent)
                .ToList();

            var potentialSolutions = result.Count > 0 ? PotentialSolutions(language) : [];
            return SingleResponse(new BacktestAnalysysRepeatedContext(result), potentialSolutions);
        }

        private static List<string> PotentialSolutions(Language language) =>
        [
            "This error occurs when you place an order to short a security but the shortable provider of the brokerage model states there isn't enough shares to borrow. " +
            "To avoid this order response error, check if there are enough shares available before you place an order to short a security.\n" +
            (language == Language.Python
                ? "```\nif quantity_to_borrow <= self.shortable_quantity(self._symbol):\n    self.market_order(self._symbol, -quantity_to_borrow)\n```"
                : "```\nif (quantityToBorrow <= ShortableQuantity(_symbol))\n{\n    MarketOrder(_symbol, -quantityToBorrow);\n}\n```"),
        ];
    }
}
