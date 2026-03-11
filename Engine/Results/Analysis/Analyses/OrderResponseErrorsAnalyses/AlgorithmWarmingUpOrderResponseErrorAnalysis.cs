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
using QuantConnect.Lean.Engine.Results.Analysis.Utils;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects orders placed during the algorithm warm-up period.
    /// Error code: OrderResponseErrorCode.ALGORITHM_WARMING_UP (-24)
    /// </summary>
    public class AlgorithmWarmingUpOrderResponseErrorAnalysis : MessageAnalysis
    {
        protected override string[] ExpectedMessageText { get; } =
        [
            "This operation is not allowed in Initialize or during warm up: OrderRequest.",
            ". Please move this code to the OnWarmupFinished() method.",
        ];

        protected override List<string> PotentialSolutions(Language language) =>
        [
            "This error occurs in the following situations:\n" +
            " - When you try to place, update, or cancel an order during the warm-up period\n" +
            " - When the Option assignment simulator assigns you to an Option during the warm-up period\n\n" +
            $"To avoid the error, move the invalid operation to `{CodeByLanguage.OnWarmupFinished[language]}` " +
            $"or protect them with an `{CodeByLanguage.IsWarmingUp[language]}` guard.",
        ];
    }
}
