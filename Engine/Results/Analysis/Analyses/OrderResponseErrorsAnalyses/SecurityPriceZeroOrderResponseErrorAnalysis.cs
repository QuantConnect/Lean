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
    /// Detects orders placed when the security price is zero.
    /// </summary>
    public class SecurityPriceZeroOrderResponseErrorAnalysis : MessageAnalysis
    {
        /// <summary>
        /// Gets a description of the zero security price ordering issue.
        /// </summary>
        public override string Issue { get; } = "The algorithm tried to place an order or exercise an Option contract while the security price was $0.";

        /// <summary>
        /// Gets the priority weight for this analysis.
        /// </summary>
        public override int Weight { get; } = 91;

        /// <summary>
        /// Gets the message fragment that identifies a zero security price error.
        /// </summary>
        protected override string[] ExpectedMessageText { get; } =
        [
            "The security does not have an accurate price as it has not yet received a bar of data.",
        ];

        /// <summary>
        /// Gets solutions for seeding initial prices or investigating missing data.
        /// </summary>
        protected override List<string> Solutions(Language language) =>
        [
            "The security price can be $0 if the algorithm hasn't received data for the security yet. " +
            "If you subscribe to a security and place an order for the security in the same time step, you'll get this error. " +
            $"To avoid this order response error, enable the `{FormatCode(nameof(AlgorithmSettings.SeedInitialPrices), language)}` setting to seed new assets with their last known price.\n" +
            (language == Language.Python
                ? "```\nself.settings.seed_initial_prices = True\n```"
                : "```\nSettings.SeedInitialPrices = true;\n```"),

            "This error also occurs if the data is missing. Investigate if it's a data issue.",
        ];
    }
}
