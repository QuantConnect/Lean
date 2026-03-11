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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects orders with quantity below the security's lot size.
    /// Error code: OrderResponseErrorCode.ORDER_QUANTITY_LESS_THAN_LOT_SIZE (-30)
    /// </summary>
    public class OrderQuantityLessThanLotSizeOrderResponseErrorAnalysis : BaseBacktestAnalysis
    {
        private static readonly string[] MessageText =
        [
            "Unable to ",
            " order with id ",
            " which quantity (",
            ") is less than lot size (",
        ];

        public IReadOnlyList<BacktestAnalysisResult> Run(List<string> logs, Language language)
        {
            var result = logs.Where(l => MessageText.All(t => l.Contains(t))).ToList();
            var potentialSolutions = result.Count > 0 ? PotentialSolutions(language) : [];
            return SingleResponse(new BacktestAnalysysRepeatedContext(result), potentialSolutions);
        }

        private static List<string> PotentialSolutions(Language language) =>
        [
            "This error occurs when you place an order with a quantity that's less than the lot size of the security. " +
            "To avoid this order response error, check if the order quantity is greater than or equal to the security lot size before you place an order.\n" +
            (language == Language.Python
                ? "```\nlot_size = self.securities[self._symbol].symbol_properties.lot_size\nif quantity >= lot_size:\n    self.market_order(self._symbol, quantity)\n```"
                : "```\nvar lotSize = Securities[_symbol].SymbolProperties.LotSize;\nif (quantity >= lotSize)\n{\n    MarketOrder(_symbol, quantity);\n}\n```"),
        ];
    }
}
