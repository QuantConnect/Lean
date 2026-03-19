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
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects orders placed during the algorithm warm-up period.
    /// Error code: OrderResponseErrorCode.ALGORITHM_WARMING_UP (-24)
    /// </summary>
    public class AlgorithmWarmingUpOrderResponseErrorAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "One of the following cases occurred:\n" +
            " - The algorithm tried to place, update, or cancel an order during the warm-up period\n" +
            " - The Option assignment simulator assigned you to an Option during the warm-up period";

        public override int Weight { get; } = 96;

        protected override string[] ExpectedMessageText { get; } =
        [
            "This operation is not allowed in Initialize or during warm up: OrderRequest.",
            ". Please move this code to the OnWarmupFinished() method.",
        ];

        protected override List<string> Solutions(Language language) =>
        [
            $"Move the invalid operation to `{FormatCode(nameof(IAlgorithm.OnWarmupFinished), language)}` " +
            $"or protect them with an `{FormatCode(nameof(IAlgorithm.IsWarmingUp), language)}` guard.",
        ];
    }
}
