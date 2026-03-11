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
using QuantConnect.Lean.Engine.Results.Analysis.Utils;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects zero-quantity order errors.
    /// Error code: OrderResponseErrorCode.ORDER_QUANTITY_ZERO (-11)
    /// </summary>
    public class OrderQuantityZeroOrderResponseErrorAnalysis : BaseBacktestAnalysis
    {
        private static readonly string[] MessageText =
        [
            "Unable to ",
            " order with id ",
            " that has zero quantity.",
        ];

        public IReadOnlyList<BacktestAnalysisResult> Run(List<string> logs, Language language)
        {
            var result = logs.Where(l => MessageText.All(t => l.Contains(t))).ToList();
            var potentialSolutions = result.Count > 0 ? PotentialSolutions(language) : [];
            return SingleResponse(new BacktestAnalysysRepeatedContext(result), potentialSolutions);
        }

        private static List<string> PotentialSolutions(Language language) =>
        [
            "This error occurs when you place an order that has zero quantity or when you update an order to have a zero quantity. " +
            $"This error commonly occurs if you use the `{CodeByLanguage.SetHoldings[language]}` method but the portfolio weight you provide to the method is too small to translate into a non-zero order quantity.\n" +
            "To avoid this order response error, check if the quantity of the order is non-zero before you place the order. " +
            $"If you use the `{CodeByLanguage.SetHoldings[language]}` method, replace it with a combination of the `{CodeByLanguage.CalculateOrderQuantity[language]}` and `{CodeByLanguage.MarketOrder[language]}` methods.\n" +
            (language == Language.Python
                ? "```\nquantity = self.calculate_order_quantity(self._symbol, 0.05)\nif quantity:\n    self.market_order(self._symbol, quantity)\n```"
                : "```\nvar quantity = CalculateOrderQuantity(_symbol, 0.05);\nif (quantity != 0)\n{\n    MarketOrder(_symbol, quantity);\n}\n```"),

            "Increase the starting cash.",
        ];
    }
}
