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
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects the "exceeded maximum orders" error.
    /// Error code: OrderResponseErrorCode.EXCEEDED_MAXIMUM_ORDERS (-20)
    /// </summary>
    public class ExceededMaximumOrdersOrderResponseErrorAnalysis : BaseBacktestAnalysis
    {
        private static readonly string[] MessageText =
        [
            "You have exceeded maximum number of orders (",
            "), for unlimited orders upgrade your account.",
        ];

        public IReadOnlyList<BacktestAnalysisResult> Run(List<string> logs)
        {
            var result = logs.Where(l => MessageText.All(t => l.Contains(t))).ToList();
            var potentialSolutions = result.Count > 0 ? PotentialSolutions() : [];
            return SingleResponse(result.Count > 0 ? (object)result : null, potentialSolutions);
        }

        private static List<string> PotentialSolutions() =>
        [
            "Switch to an organization on the Team tier or higher.",

            "Reduce the number of order in the algorithm. " +
            "To accomplish this, you could try the following: \n" +
            " - Reduce the universe size\n" +
            " - Trading less frequently\n" +
            " - Make the trading signal more strict\n" +
            " - Shorten the backtest period",
        ];
    }
}
