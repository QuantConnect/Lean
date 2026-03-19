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
    /// Detects margin-call events in the backtest logs.
    /// </summary>
    public class MarginCallsAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm encountered a margin call.";

        public override int Weight { get; } = 78;

        protected override string[] ExpectedMessageText { get; } =
        [
            "Executed MarginCallOrder",
        ];

        protected override List<string> Solutions(Language language) =>
        [
            "Adjust the ordering and rebalancing logic to reduce margin usage.",

            $"Add an `{FormatCode(nameof(QCAlgorithm.OnMarginCallWarning), language)}` method to reduce margin used before margin calls occur. " +
            "The `DefaultMarginCallModel` issues margin call warnings when the margin remaining in your portfolio is less than or equal to 5% of the total portfolio value. " +
            "When your total margin used exceeds 10% of the total portfolio value, this model creates margin call orders.",

            "Check if it's a data issue that's causing the margin call.",

            "Disable margin calls.\n" +
            (language == Language.Python
                ? "```\ndef initialize(self) -> None:\n    self.portfolio.margin_call_model = MarginCallModel.NULL\n```"
                : "```\npublic override void Initialize()\n{\n    Portfolio.MarginCallModel = MarginCallModel.Null;\n}\n```"),
        ];
    }
}
