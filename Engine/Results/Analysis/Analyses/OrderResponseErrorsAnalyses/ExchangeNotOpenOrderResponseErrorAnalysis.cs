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
using QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    // ── Sub-test: exercise while exchange not open ────────────────────────────────

    internal class ExerciseOptionWhileExchangeNotOpenAnalysis : MessageAnalysis
    {
        protected override string[] ExpectedMessageText { get; } =
        [
            " order and exchange not open.",
        ];

        protected override List<string> PotentialSolutions(Language language) =>
        [
            "This error occurs when you try to exercise an Option while the exchange is not open. " +
            "To avoid the order response error in this case, check if the exchange is open before you exercise an Option contract.\n" +
            (language == Language.Python
                ? "```\nif self.is_market_open(self.contract_symbol):\n    self.exercise_option(self.contract_symbol, quantity)\n```"
                : "```\nif (IsMarketOpen(_contractSymbol))\n{\n    ExerciseOption(_contractSymbol, quantity);\n}\n```"),
        ];
    }

    // ── Sub-test: MOC order for Future / FOP ─────────────────────────────────────

    internal class MOCOrderForFutureOrFOPAnalysis : MessageAnalysis
    {
        protected override string[] ExpectedMessageText { get; } =
        [
            " orders not supported for ",
        ];

        protected override List<string> PotentialSolutions(Language language) =>
        [
            "This error occurs when you try to place a market on open order for a Futures contract or a Future Option contract. " +
            "To avoid the order response error in this case, check if the exchange is open before you place the order.",
        ];
    }

    // ── Orchestrator ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects "exchange not open" order response errors (error code -15).
    /// Returns the first sub-test that fires.
    /// </summary>
    public class ExchangeNotOpenOrderResponseErrorAnalysis : BaseBacktestAnalysis
    {
        private static readonly MessageAnalysis[] SubTests =
        [
            new ExerciseOptionWhileExchangeNotOpenAnalysis(),
            new MOCOrderForFutureOrFOPAnalysis(),
        ];

        public IReadOnlyList<BacktestAnalysisResult> Run(IReadOnlyList<string> logs, Language language)
        {
            foreach (var subTest in SubTests)
            {
                var results = subTest.Run(logs, language);
                if (results.Any(r => r.Context is not null))
                    return results;
            }

            return SingleResponse(null);
        }
    }
}
