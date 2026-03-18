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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.InteractiveBrokersBrokerageModel
{
    /// <summary>
    /// Detects Interactive Brokers brokerage model rejections where a Forex order is below the minimum required size.
    /// </summary>
    public class InvalidForexOrderSizeAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to place a Forex order with a size below the minimum required by Interactive Brokers.";

        public override int Weight { get; } = 68;

        protected override string[] ExpectedMessageText { get; } =
        [
            "The minimum order size for IBKR Forex orders is ",
        ];

        protected override List<string> Solutions(Language _) =>
        [
            "Increase the order size to meet the minimum requirement.",
        ];
    }
}
