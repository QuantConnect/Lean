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
    /// Detects brokerage model rejections where no data is available for the ordered security.
    /// </summary>
    public class NoDataForSymbolAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to place an order for a security but there is no data for the security.";

        public override int Weight { get; } = 80;

        protected override string[] ExpectedMessageText { get; } =
        [
            "There is no data for the provided symbol ",
        ];

        protected override List<string> Solutions(Language _) =>
        [
            "Check if you've subscribed to the correct security and if the data is available.",
        ];
    }
}
