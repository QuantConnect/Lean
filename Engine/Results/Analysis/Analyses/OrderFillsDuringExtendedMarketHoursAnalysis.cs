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
using QuantConnect.Orders;
using QuantConnect.Algorithm;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>Detects order fills that occurred outside regular market hours.</summary>
    public class OrderFillsDuringExtendedMarketHoursAnalysis : BaseBacktestAnalysis
    {
        public IReadOnlyList<BacktestAnalysisResult> Run(QCAlgorithm algorithm, IReadOnlyList<OrderEvent> orderEvents, Language language)
        {
            var result = new List<OrderEvent>();

            foreach (var orderEvent in orderEvents)
            {
                if (orderEvent.Status != OrderStatus.Filled || algorithm.IsMarketOpen(orderEvent.Symbol))
                {
                    continue;
                }

                result.Add(orderEvent);
            }

            var potentialSolutions = result.Count > 0 ? PotentialSolutions(language) : [];
            return SingleResponse(new BacktestAnalysysRepeatedContext(orderEvents), potentialSolutions);
        }

        private static List<string> PotentialSolutions(Language language) =>
        [
            "Filling orders during extended market hours can cause a lot of slippage since there is less liquidity than during regular trading hours. " +
            "If you don't intend to trading during extended market hours, add a guard before you place orders.\n" +
            (language == Language.Python
                ? "```\nif self.is_market_open(self._symbol):\n    self.market_order(self._symbol, quantity)\n```"
                : "```\nif (IsMarketOpen(_symbol))\n{\n    MarketOrder(symbol, quantity);\n}\n```"),
        ];
    }
}
