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
using QuantConnect.Lean.Engine.Results.Analysis.Utils;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects orders placed when the security price is zero.
    /// Error code: OrderResponseErrorCode.SECURITY_PRICE_ZERO (-16)
    /// </summary>
    public class SecurityPriceZeroOrderResponseErrorAnalysis : BaseBacktestAnalysis
    {
        public IReadOnlyList<BacktestAnalysisResult> Run(List<string> logs, Language language)
        {
            var result = logs
                .Where(l => l.Contains("The security does not have an accurate price as it has not yet received a bar of data."))
                .ToList();
            var potentialSolutions = result.Count > 0 ? PotentialSolutions(language) : [];
            return SingleResponse(new BacktestAnalysysRepeatedContext(result), potentialSolutions);
        }

        private static List<string> PotentialSolutions(Language language) =>
        [
            "This occurs when you place an order or exercise an Option contract while the security price is $0. " +
            "The security price can be $0 if the algorithm hasn't received data for the security yet. " +
            "If you subscribe to a security and place an order for the security in the same time step, you'll get this error. " +
            $"To avoid this order response error, enable the `{CodeByLanguage.SeedInitialPrices[language]}` setting to seed new assets with their last known price.\n" +
            (language == Language.Python
                ? "```\nself.settings.seed_initial_prices = True\n```"
                : "```\nSettings.SeedInitialPrices = true;\n```"),

            "This error also occurs if the data is missing. Investigate if it's a data issue.",
        ];
    }
}
