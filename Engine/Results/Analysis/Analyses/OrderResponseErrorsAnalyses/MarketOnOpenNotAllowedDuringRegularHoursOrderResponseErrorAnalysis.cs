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
        public override string Issue { get; } = "The algorithm tried to place a market on open order for an asset when it's during regular trading hours.";

        public override int Weight { get; } = 82;

        protected override string[] ExpectedMessageText { get; } =
        [
            "Cannot submit a MarketOnOpen order while the market is open.",
        ];

        protected override List<string> Solutions(Language language) =>
        [
            "Place the order when the market is closed.\n" +
            (language == Language.Python
                ? "```\nif not self.is_market_open(self._symbol):\n    self.market_on_open_order(self._symbol, quantity)\n```"
                : "```\nif (!IsMarketOpen(_symbol))\n{\n    MarketOnOpenOrder(_symbol, quantity);\n}\n```"),
        ];
    }
}
