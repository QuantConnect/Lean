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
    /// Detects Tradier brokerage model rejections where the security type is not supported (only Equity and Option are supported).
    /// </summary>
    public class TradierUnsupportedSecurityTypeAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to trade a security type that is not supported by the Tradier brokerage model.";

        public override int Weight { get; } = 65;

        protected override string[] ExpectedMessageText { get; } =
        [
            "Tradier does not support ",
            " security type.",
        ];

        protected override List<string> Solutions(Language _) =>
        [
            "Only trade security types that Tradier supports (Equity and Option).",
        ];
    }
}
