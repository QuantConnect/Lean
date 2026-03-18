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
using QuantConnect.Algorithm;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects zero-quantity order errors.
    /// </summary>
    public class OrderQuantityZeroOrderResponseErrorAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to place an order that has zero quantity or tried to update an order to have a zero quantity.";

        public override int Weight { get; } = 78;

        protected override string[] ExpectedMessageText { get; } =
        [
            "Unable to ",
            " order with id ",
            " that has zero quantity.",
        ];

        protected override List<string> Solutions(Language language) =>
        [
            $"This error commonly occurs if you use the `{FormatCode("SetHoldings", language)}` method but the portfolio weight you provide to the method is too small to translate into a non-zero order quantity.\n" +
            "Check if the quantity of the order is non-zero before you place the order. " +
            $"If you use the `{FormatCode(nameof(QCAlgorithm.SetHoldings), language)}` method, replace it with a combination of the `{FormatCode(nameof(QCAlgorithm.CalculateOrderQuantity), language)}` and `{FormatCode(nameof(QCAlgorithm.MarketOrder), language)}` methods.\n" +
            (language == Language.Python
                ? "```\nquantity = self.calculate_order_quantity(self._symbol, 0.05)\nif quantity:\n    self.market_order(self._symbol, quantity)\n```"
                : "```\nvar quantity = CalculateOrderQuantity(_symbol, 0.05);\nif (quantity != 0)\n{\n    MarketOrder(_symbol, quantity);\n}\n```"),

            "Increase the starting cash.",
        ];
    }
}
