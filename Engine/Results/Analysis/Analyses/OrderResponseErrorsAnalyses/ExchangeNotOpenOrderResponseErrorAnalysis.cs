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
    /// <summary>
    /// Detects "exchange not open" order response errors.
    /// Returns the first sub-test that fires.
    /// </summary>
    public class ExchangeNotOpenOrderResponseErrorAnalysis : BaseResultsAnalysis
    {
        public override string Issue { get; } = "One of the following cases occurred:\n" +
            " - The algorithm tried to exercise an Option while the exchange was closed.\n" +
            " - The algorithm tried to place a market on open order for a Futures contract or a Future Option contract";

        public override int Weight { get; } = 89;
        public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.Logs, parameters.Language);

        private static readonly MessageAnalysis[] SubAnalyses =
        [
            new ExerciseOptionWhileExchangeNotOpenAnalysis(),
            new MOCOrderForFutureOrFOPAnalysis(),
        ];

        /// <summary>
        /// Runs the first sub-analysis that produces a match, covering exercise-while-closed
        /// and MOC-on-Futures scenarios.
        /// </summary>
        /// <param name="logs">The log lines produced by the backtest.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <returns>The results of the first matching sub-analysis, or a single empty response when none match.</returns>
        public IReadOnlyList<AnalysisResult> Run(IReadOnlyList<string> logs, Language language)
        {
            foreach (var subTest in SubAnalyses)
            {
                var results = subTest.Run(logs, language);
                if (results.Any(r => r.Context is not null))
                {
                    return results;
                }
            }

            return SingleResponse(null);
        }

        // ── Sub-analysis: exercise while exchange not open ────────────────────────────────

        private class ExerciseOptionWhileExchangeNotOpenAnalysis : MessageAnalysis
        {
            public override string Issue { get; } = "The algorithm tried to exercise an Option while the exchange was closed.";

            public override int Weight { get; } = 60;

            protected override string[] ExpectedMessageText { get; } =
            [
                " order and exchange not open.",
            ];

            protected override List<string> Solutions(Language language) =>
            [
                "Check if the exchange is open before you exercise an Option contract.\n" +
                (language == Language.Python
                    ? "```\nif self.is_market_open(self.contract_symbol):\n    self.exercise_option(self.contract_symbol, quantity)\n```"
                    : "```\nif (IsMarketOpen(_contractSymbol))\n{\n    ExerciseOption(_contractSymbol, quantity);\n}\n```"),
            ];
        }

        // ── Sub-analysis: MOC order for Future / FOP ─────────────────────────────────────

        private class MOCOrderForFutureOrFOPAnalysis : MessageAnalysis
        {
            public override string Issue { get; } = "The algorithm tried to place a market on open order for a Futures contract or a Future Option contract.";

            public override int Weight { get; } = 60;

            protected override string[] ExpectedMessageText { get; } =
            [
                " orders not supported for ",
            ];

            protected override List<string> Solutions(Language language) =>
            [
                "Check if the exchange is open before you place the order.",
            ];
        }
    }
}
