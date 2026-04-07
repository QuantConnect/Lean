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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.DefaultBrokerageModel
{
    /// <summary>
    /// Detects brokerage model rejections where an order update would cause the position to cross zero.
    /// </summary>
    public class UnsupportedCrossZeroOrderUpdateAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to update an order that would cause the position to cross zero, which is not supported.";

        public override int Weight { get; } = 0;

        protected override string[] ExpectedMessageText { get; } =
        [
            "Unable to update order with id ",
            " that would cross zero.",
        ];

        protected override List<string> Solutions(Language _) =>
        [
            "Cancel the existing order and submit a new one with the desired quantity instead.",
        ];
    }
}
