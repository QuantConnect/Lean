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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.TradierBrokerageModel
{
    /// <summary>
    /// Detects Tradier brokerage model rejections where the order quantity is not a whole number.
    /// </summary>
    public class IncorrectOrderQuantityAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to place an order with a non-integer quantity, which is not supported by Tradier.";

        public override int Weight { get; } = 68;

        protected override string[] ExpectedMessageText { get; } =
        [
            "Tradier order quantity must be a whole number, but received ",
        ];

        protected override List<string> Solutions(Language _) =>
        [
            "Round the order quantity to the nearest whole number before placing the order.",
        ];
    }
}
