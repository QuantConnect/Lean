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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects attempts to exercise an Option contract while holding a short position.
    /// </summary>
    public class UnsupportedOptionShortPositionExerciseAnalysis : MessageAnalysis
    {
        /// <summary>
        /// Gets a description of the short-position option exercise issue.
        /// </summary>
        public override string Issue { get; } = "The algorithm tried to exercise an Option contract for which it holds a short position.";

        /// <summary>
        /// Gets the priority weight for this analysis.
        /// </summary>
        public override int Weight { get; } = 90;

        /// <summary>
        /// Gets the message fragments that identify a short-position option exercise error.
        /// </summary>
        protected override string[] ExpectedMessageText { get; } =
        [
            "The security with symbol ",
            " has a short option position. Only long option positions are exercisable.",
        ];

        /// <summary>
        /// Gets solutions for verifying the position direction before exercising an Option contract.
        /// </summary>
        protected override List<string> Solutions(Language language) =>
        [
            "Check that you hold a long position before you exercise an Option contract.\n" +
            (language == Language.Python
                ? "```\nholding_quantity = self.portfolio[self._contract_symbol].quantity\nif holding_quantity > 0:\n    self.exercise_option(self._contract_symbol, exercise_quantity)\n```"
                : "```\nvar holdingQuantity = Portfolio[_contractSymbol].Quantity;\nif (holdingQuantity > 0)\n{\n    ExerciseOption(_contractSymbol, exerciseQuantity);\n}\n```"),
        ];
    }
}
