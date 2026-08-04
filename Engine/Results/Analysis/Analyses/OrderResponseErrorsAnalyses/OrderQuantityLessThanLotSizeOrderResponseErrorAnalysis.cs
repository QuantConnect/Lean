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
using QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects orders with quantity below the security's lot size.
    /// </summary>
    public class OrderQuantityLessThanLotSizeOrderResponseErrorAnalysis : MessageAnalysis
    {
        /// <summary>
        /// Gets a description of the order quantity below lot size issue.
        /// </summary>
        public override string Issue { get; } = "The algorithm tried to place an order with a quantity that's less than the lot size of the security.";

        /// <summary>
        /// Gets the priority weight for this analysis.
        /// </summary>
        public override int Weight { get; } = 85;

        /// <summary>
        /// Gets the message fragments that identify a quantity-less-than-lot-size error.
        /// </summary>
        protected override string[] ExpectedMessageText { get; } =
        [
            "Unable to ",
            " order with id ",
            " which quantity (",
            ") is less than lot size (",
        ];

        /// <summary>
        /// Gets solutions for validating order quantity against the lot size.
        /// </summary>
        protected override List<string> Solutions(Language language) =>
        [
            "Check if the order quantity is greater than or equal to the security lot size before you place an order.\n" +
            (language == Language.Python
                ? "```\nlot_size = self.securities[self._symbol].symbol_properties.lot_size\nif quantity >= lot_size:\n    self.market_order(self._symbol, quantity)\n```"
                : "```\nvar lotSize = Securities[_symbol].SymbolProperties.LotSize;\nif (quantity >= lotSize)\n{\n    MarketOrder(_symbol, quantity);\n}\n```"),
        ];
    }
}
