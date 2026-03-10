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
    /// Detects zero Forex conversion rate errors.
    /// Error code: OrderResponseErrorCode.FOREX_CONVERSION_RATE_ZERO (-18)
    /// </summary>
    public class ForexConversionRateZeroOrderResponseErrorAnalysis : BaseBacktestAnalysis
    {
        private static readonly string[] MessageText =
        [
            ": requires ",
            " and ",
            " to have non-zero conversion rates. This can be caused by lack of data.",
        ];

        public IReadOnlyList<BacktestAnalysisResult> Run(List<string> logs)
        {
            var result = logs.Where(l => MessageText.All(t => l.Contains(t))).ToList();
            var potentialSolutions = result.Count > 0 ? PotentialSolutions() : [];
            return SingleResponse(result.Count > 0 ? (object)result : null, potentialSolutions);
        }

        private static List<string> PotentialSolutions() =>
        [
            "This error occurs when you place a trade for a Forex or Crypto pair and LEAN can't convert the value of the base currency to your account currency. " +
            "This error usually indicates a lack of data. Check if there is some data missing.",
        ];
    }
}
