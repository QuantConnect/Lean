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
    /// Detects brokerage model rejections where a MarketOnOpen order was placed for a Futures or Future Options contract.
    /// </summary>
    public class UnsupportedMarketOnOpenOrdersForFutureAndFutureOptionsAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to place a market on open order for a Future or Future Option contract, which isn't supported by the brokerage model.";

        public override int Weight { get; } = 60;

        protected override string[] ExpectedMessageText { get; } =
        [
            "MarketOnOpen orders are not supported for futures and future options.",
        ];

        protected override List<string> Solutions(Language _) =>
        [
            "Use a regular market order or a limit order instead.",
        ];
    }
}
