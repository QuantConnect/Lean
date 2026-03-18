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
using QuantConnect.Orders;
using System;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects orders filled at stale (outdated) prices.
    /// </summary>
    public class StaleOrderFillsAnalysis : BaseResultsAnalysis
    {
        public override string Issue { get; } = "Some orders filled at stale prices.";

        public override int Weight { get; } = 65;
        public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.Result.OrderEvents, parameters.Language);

        /// <summary>
        /// Searches order events for fill messages that contain a stale-price warning.
        /// </summary>
        /// <param name="orderEvents">The list of order events from the backtest result.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <returns>Analysis results when stale fill events are detected.</returns>
        public IReadOnlyList<AnalysisResult> Run(IReadOnlyList<OrderEvent> orderEvents, Language language)
        {
            var result = orderEvents
                .Where(e => e.Message != null && e.Message.Contains("Warning: fill at stale price", StringComparison.InvariantCultureIgnoreCase))
                //.Select(OrdersReader.ParseOrderEvent)
                .ToList();

            var potentialSolutions = result.Count > 0 ? Solutions(language) : [];
            return SingleResponse(new ResultsAnalysisRepeatedContext(result), potentialSolutions);
        }

        private static List<string> Solutions(Language language) =>
        [
            "Stale fills occur when you fill an order with price data that is timestamped an hour or more into the past. " +
            "Stale fills usually only occur if you trade illiquid assets or if your algorithm uses daily data but you trade intraday with Scheduled Events. " +
            "If your order is filled with stale data, the fill price may not be realistic. " +
            "The pre-built fill models can only fill market orders with stale data. " +
            $"To adjust the length of time that needs to pass before an order is considered stale, set the `{FormatCode(nameof(AlgorithmSettings.StalePriceTimeSpan), language)}` setting.\n" +
            (language == Language.Python
                ? "```\ndef initialize(self) -> None:\n    # Adjust the stale price time span to be 10 minutes.\n    self.settings.stale_price_time_span = timedelta(minutes=10)\n```"
                : "```\npublic override void Initialize()\n{\n    // Adjust the stale price time span to be 10 minutes.\n    Settings.StalePriceTimeSpan = TimeSpan.FromMinutes(10);\n}\n```"),

            "Investigate if there is a data issue.",
        ];
    }
}
