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
    /// <summary>
    /// Detects order fills that occurred outside regular market hours.
    /// </summary>
    public class OrderFillsDuringExtendedMarketHoursAnalysis : BaseResultsAnalysis
    {
        public override string Issue { get; } = "The algorithm filled orders during extended market hours." + 
            "Filling orders during extended market hours can cause a lot of slippage since there is less liquidity than during regular trading hours.";

        public override int Weight { get; } = 75;
        public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.Algorithm, parameters.Result.OrderEvents, parameters.Language);

        /// <summary>
        /// Iterates filled order events and flags those that occurred when the exchange was not open.
        /// </summary>
        /// <param name="algorithm">The algorithm instance used to check market-open status at the fill time.</param>
        /// <param name="orderEvents">The list of order events from the backtest result.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <returns>Analysis results when fills outside regular hours are detected.</returns>
        public IReadOnlyList<AnalysisResult> Run(QCAlgorithm algorithm, IReadOnlyList<OrderEvent> orderEvents, Language language)
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

            var potentialSolutions = result.Count > 0 ? Solutions(language) : [];
            return SingleResponse(new ResultsAnalysisRepeatedContext(orderEvents), potentialSolutions);
        }

        private static List<string> Solutions(Language language) =>
        [
            "If you don't intend to trading during extended market hours, add a guard before you place orders.\n" +
            (language == Language.Python
                ? "```\nif self.is_market_open(self._symbol):\n    self.market_order(self._symbol, quantity)\n```"
                : "```\nif (IsMarketOpen(_symbol))\n{\n    MarketOrder(symbol, quantity);\n}\n```"),
        ];
    }
}
