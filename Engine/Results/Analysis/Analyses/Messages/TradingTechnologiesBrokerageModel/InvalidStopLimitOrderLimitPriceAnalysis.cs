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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.TradingTechnologiesBrokerageModel
{
    /// <summary>
    /// Detects Trading Technologies brokerage model rejections where a stop-limit order's limit price
    /// is on the wrong side of the stop price.
    /// </summary>
    public class InvalidStopLimitOrderLimitPriceAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to place a stop-limit order where the limit price is on the wrong side of the stop price.";

        public override int Weight { get; } = 62;

        protected override string[] ExpectedMessageText { get; } =
        [
            "Trading Technologies does not support a",
            "stop-limit order with a limit price",
        ];

        protected override List<string> Solutions(Language _) =>
        [
            "Trading Technologies requires the limit price of a stop-limit order to be worse than the stop price (further from the market). " +
            "For a buy order the limit price must be above the stop price; for a sell order it must be below.",
        ];
    }
}
