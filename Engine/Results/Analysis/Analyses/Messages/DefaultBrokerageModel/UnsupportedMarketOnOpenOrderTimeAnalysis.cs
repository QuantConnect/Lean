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
    /// Detects brokerage model rejections where a MarketOnOpen order was placed without the required
    /// minimum time gap before the intended fill bar.
    /// </summary>
    public class UnsupportedMarketOnOpenOrderTimeAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "Market on open order placed at wrong time";

        public override int Weight { get; } = 0;

        protected override string[] ExpectedMessageText { get; } =
        [
            "MarketOnOpen orders must be placed with at least a ",
            " bar between order and intended fill.",
        ];

        protected override List<string> Solutions(Language _) =>
        [
            "MarketOnOpen orders require a minimum time gap between submission and the intended fill. " +
            "Place the order at least one bar before the market open.",
        ];
    }
}
