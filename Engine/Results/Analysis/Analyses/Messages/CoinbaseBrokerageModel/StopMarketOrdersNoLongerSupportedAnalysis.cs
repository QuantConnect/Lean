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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.CoinbaseBrokerageModel
{
    /// <summary>
    /// Detects Coinbase brokerage model rejections due to Stop Market orders, which are no longer supported.
    /// </summary>
    public class StopMarketOrdersNoLongerSupportedAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to place a stop-market order, but the Coinbase brokerage model doesn't support these orders after 2019-03-23.";

        public override int Weight { get; } = 0;

        protected override string[] ExpectedMessageText { get; } =
        [
            "Stop Market orders are no longer supported since ",
        ];

        protected override List<string> Solutions(Language _) =>
        [
            "Use a Stop Limit order instead.",
        ];
    }
}
