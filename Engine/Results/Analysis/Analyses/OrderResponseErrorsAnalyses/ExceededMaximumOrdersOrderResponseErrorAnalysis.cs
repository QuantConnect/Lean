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
using QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects the "exceeded maximum orders" error.
    /// </summary>
    public class ExceededMaximumOrdersOrderResponseErrorAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm hit the organization's quota for the number of orders allowed in a backtest.";

        public override int Weight { get; } = 87;

        protected override string[] ExpectedMessageText { get; } =
        [
            "You have exceeded maximum number of orders (",
            "), for unlimited orders upgrade your account.",
        ];

        protected override List<string> Solutions(Language language) =>
        [
            "Switch to an organization on the Team tier or higher.",

            "Reduce the number of orders in the algorithm. " +
            "To accomplish this, you could try the following: \n" +
            " - Reduce the universe size\n" +
            " - Trade less frequently\n" +
            " - Make the trading signal more strict\n" +
            " - Shorten the backtest period",
        ];
    }
}
