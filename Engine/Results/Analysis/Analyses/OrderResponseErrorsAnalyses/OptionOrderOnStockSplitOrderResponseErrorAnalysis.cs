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
    /// Detects Option orders placed when the underlying stock had a split.
    /// </summary>
    public class OptionOrderOnStockSplitOrderResponseErrorAnalysis : MessageAnalysis
    {
        /// <summary>
        /// Gets a description of the option order during stock split issue.
        /// </summary>
        public override string Issue { get; } = "The algorithm tried to submit an order for an Equity Option contract when the current time slice contained a split for the underlying Equity.";

        /// <summary>
        /// Gets the priority weight for this analysis.
        /// </summary>
        public override int Weight { get; } = 83;

        /// <summary>
        /// Gets the message fragment that identifies an option-order-on-stock-split error.
        /// </summary>
        protected override string[] ExpectedMessageText { get; } =
        [
            "Options orders are not allowed when a split occurred for its underlying stock",
        ];

        /// <summary>
        /// Gets solutions for checking underlying split events before placing option orders.
        /// </summary>
        protected override List<string> Solutions(Language language) =>
        [
            "Check if the time slice has a split event for the underlying Equity of the contract before you place an order for the contract.\n" +
            (language == Language.Python
                ? "```\nif self.contract_symbol.underlying not in slice.splits:\n    self.market_order(self.contract_symbol, quantity)\n```"
                : "```\nif (!slice.Splits.ContainsKey(_contractSymbol.Underlying))\n{\n    MarketOrder(_contractSymbol, quantity);\n}\n```"),
        ];
    }
}
