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
    /// Detects MarketOnOpen orders submitted during regular trading hours.
    /// </summary>
    public class MarketOnOpenNotAllowedDuringRegularHoursOrderResponseErrorAnalysis : MessageAnalysis
    {
        /// <summary>
        /// Gets a description of the market-on-open during regular hours issue.
        /// </summary>
        public override string Issue { get; } = "The algorithm tried to place a market on open order for an asset when it's during regular trading hours.";

        /// <summary>
        /// Gets the priority weight for this analysis.
        /// </summary>
        public override int Weight { get; } = 82;

        /// <summary>
        /// Gets the message fragment that identifies a market-on-open during regular hours error.
        /// </summary>
        protected override string[] ExpectedMessageText { get; } =
        [
            "Cannot submit a MarketOnOpen order while the market is open.",
        ];

        /// <summary>
        /// Gets solutions for placing market-on-open orders outside regular hours.
        /// </summary>
        protected override List<string> Solutions(Language language) =>
        [
            "Place the order when the market is closed.\n" +
            (language == Language.Python
                ? "```\nif not self.is_market_open(self._symbol):\n    self.market_on_open_order(self._symbol, quantity)\n```"
                : "```\nif (!IsMarketOpen(_symbol))\n{\n    MarketOnOpenOrder(_symbol, quantity);\n}\n```"),
        ];
    }
}
