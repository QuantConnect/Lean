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
    /// Detects orders placed for non-tradable securities.
    /// </summary>
    public class NonTradableSecurityOrderResponseErrorAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to place an order for a security that's not tradable.";

        public override int Weight { get; } = 93;

        protected override string[] ExpectedMessageText { get; } =
        [
            "The security with symbol ",
            " is marked as non-tradable.",
        ];

        protected override List<string> Solutions(Language language) =>
        [
            "Check if a security is tradable before you trade it.\n" +
            (language == Language.Python
                ? "```\nif self.securities[self._symbol].is_tradable:\n    self.market_order(self._symbol, quantity)\n```"
                : "```\nif (Securities[_symbol].IsTradable)\n{\n    MarketOrder(_symbol, quantity);\n}\n```") + "\n" +
            "Indices, canonical Option securities, and continuous Futures contracts are not tradable. " +
            "In live mode, custom data objects are also not tradable, even if the custom data represents a tradable asset.",
        ];
    }
}
