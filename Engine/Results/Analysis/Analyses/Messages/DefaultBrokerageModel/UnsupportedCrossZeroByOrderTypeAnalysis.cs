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
    /// Detects brokerage model rejections where the order type does not support crossing zero (flipping position direction in a single order).
    /// </summary>
    public class UnsupportedCrossZeroByOrderTypeAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to a position from long to short (or vice versa) in a single order, but the brokerage model does not support crossing zero.";

        public override int Weight { get; } = 58;

        protected override string[] ExpectedMessageText { get; } =
        [
            "Order Quantity must not cross zero. ",
            " does not support crossing zero",
        ];

        protected override List<string> Solutions(Language _) =>
        [
            "Submit two separate orders: one to close the existing position and one to open the new position.",
        ];
    }
}
