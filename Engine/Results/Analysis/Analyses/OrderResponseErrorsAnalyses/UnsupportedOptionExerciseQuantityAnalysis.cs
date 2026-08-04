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
    /// Detects attempts to exercise more Option contracts than are currently held in the portfolio.
    /// </summary>
    public class UnsupportedOptionExerciseQuantityAnalysis : MessageAnalysis
    {
        /// <summary>
        /// Gets a description of the excess-quantity option exercise issue.
        /// </summary>
        public override string Issue { get; } = "The algorithm tried to exercise more Option contracts than it holds.";

        /// <summary>
        /// Gets the priority weight for this analysis.
        /// </summary>
        public override int Weight { get; } = 89;

        /// <summary>
        /// Gets the message fragments that identify an excess-quantity option exercise error.
        /// </summary>
        protected override string[] ExpectedMessageText { get; } =
        [
            "Cannot exercise more contracts of ",
            " than is currently available in the portfolio.",
        ];

        /// <summary>
        /// Gets solutions for capping the exercise quantity to what is actually held.
        /// </summary>
        protected override List<string> Solutions(Language language) =>
        [
            "Check the quantity of your holdings before you try to exercise an Option contract.\n" +
            (language == Language.Python
                ? "```\nholding_quantity = self.portfolio[self._contract_symbol].quantity\nif holding_quantity > 0:\n    self.exercise_option(self._contract_symbol, min(holding_quantity, exercise_quantity))\n```"
                : "```\nvar holdingQuantity = Portfolio[_contractSymbol].Quantity;\nif (holdingQuantity > 0)\n{\n    ExerciseOption(_contractSymbol, Math.Min(holdingQuantity, exerciseQuantity));\n}\n```"),
        ];
    }
}
