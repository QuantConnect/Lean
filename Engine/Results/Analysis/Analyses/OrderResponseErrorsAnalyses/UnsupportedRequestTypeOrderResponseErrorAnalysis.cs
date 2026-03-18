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
using QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects unsupported option exercise requests.
    /// </summary>
    public class UnsupportedRequestTypeOrderResponseErrorAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "One of the following cases occurred:\n" +
            " - The algorithm tried to exercise an Option contract for which it holds a short position\n" +
            " - The algorithm tried to exercise more Option contracts than it holds\n\n";

        public override int Weight { get; } = 50;

        private static readonly string[] ShortMessageText =
        [
            "The security with symbol ",
            " has a short option position. Only long option positions are exercisable.",
        ];

        private static readonly string[] QuantityMessageText =
        [
            "Cannot exercise more contracts of ",
            " than is currently available in the portfolio.",
        ];

        protected override string[] ExpectedMessageText => [];

        /// <summary>
        /// Scans <paramref name="messages"/> for both short-option and excess-quantity exercise errors
        /// and returns combined results.
        /// </summary>
        /// <param name="messages">The log or order event messages to scan.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <returns>Analysis results when either unsupported request type pattern is detected.</returns>
        public virtual IReadOnlyList<AnalysisResult> Run(IReadOnlyList<string> messages, Language language)
        {
            var shortFoundMessages = Match(messages, ShortMessageText).ToList();
            var quantityFoundMessages = Match(messages, QuantityMessageText).ToList();
            var potentialSolutions = shortFoundMessages.Count > 0 || quantityFoundMessages.Count > 0 ? Solutions(language) : [];

            var contexts = new List<IResultsAnalysisContext>();
            if (shortFoundMessages.Count > 0)
            {
                contexts.Add(new ResultsAnalysisRepeatedContext(shortFoundMessages));
            }
            if (quantityFoundMessages.Count > 0)
            {
                contexts.Add(new ResultsAnalysisRepeatedContext(quantityFoundMessages));
            }

            return SingleResponse(new ResultsAnalysisAggregateContext(contexts), potentialSolutions);
        }

        protected override List<string> Solutions(Language language) =>
        [
            "Check the quantity of your holdings before you try to exercise an Option contract.\n" +
            (language == Language.Python
                ? "```\nholding_quantity = self.portfolio[self._contract_symbol].quantity\nif holding_quantity > 0:\n    self.exercise_option(self._contract_symbol, max(holding_quantity, exercise_quantity))\n```"
                : "```\nvar holdingQuantity = Portfolio[_contractSymbol].Quantity;\nif (holdingQuantity > 0)\n{\n    ExerciseOption(_contractSymbol, Math.Max(holdingQuantity, exerciseQuantity));\n}\n```"),
        ];
    }
}
