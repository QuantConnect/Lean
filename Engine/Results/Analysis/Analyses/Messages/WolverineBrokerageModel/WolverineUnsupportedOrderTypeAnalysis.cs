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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.WolverineBrokerageModel
{
    /// <summary>
    /// Detects Wolverine brokerage model rejections due to an unsupported order type (only Market orders are supported).
    /// </summary>
    public class WolverineUnsupportedOrderTypeAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to place an order type that is not supported by the Wolverine brokerage model.";

        public override int Weight { get; } = 0;

        protected override string[] ExpectedMessageText { get; } =
        [
            "Wolverine does not support ",
            " order type.",
        ];

        protected override List<string> Solutions(Language _) =>
        [
            "The Wolverine brokerage model only supports Market orders. " +
            "Change your order type to Market.",
        ];
    }
}
