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
    /// Detects attempts to exercise a European option before expiry.
    /// Error code: OrderResponseErrorCode.EUROPEAN_OPTION_NOT_EXPIRED_ON_EXERCISE (-33)
    /// </summary>
    public class EuropeanOptionNotExpiredOnExerciseOrderResponseErrorAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to exercise a European Option before its expiration date.";

        public override int Weight { get; } = 48;

        protected override string[] ExpectedMessageText { get; } =
        [
            "Cannot exercise European style option with symbol ",
            " before its expiration date.",
        ];

        protected override List<string> Solutions(Language language) =>
        [
            "Check the type and expiry date of the contract before you exercise it.\n" +
            (language == Language.Python
                ? "```\nif self.contract_symbol.id.option_style == OptionStyle.EUROPEAN && self.contract_symbol.id.date == self.time.date:\n    self.exercise_option(self.contract_symbol, quantity)\n```"
                : "```\nif (_contractSymbol.ID.OptionStyle == OptionStyle.European && _contractSymbol.ID.Date == Time.Date)\n{\n    ExerciseOption(_contractSymbol, quantity);\n}\n```"),
        ];
    }
}
