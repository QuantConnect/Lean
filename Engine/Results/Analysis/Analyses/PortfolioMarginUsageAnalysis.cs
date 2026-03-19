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
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects periods where the portfolio under-utilises available margin
    /// (3-day SMA of margin usage drops below 50 %).
    /// </summary>
    public class PortfolioMarginUsageAnalysis : BaseResultsAnalysis
    {
        public override string Issue { get; } = "The algorithm sometimes only utilizes a small proportion of the margin available.";

        public override int Weight { get; } = 74;
        public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.Result);

        /// <summary>
        /// Reads the "Portfolio Margin" chart from the backtest result and counts trading days
        /// where the 3-day SMA of total margin usage drops below 50%.
        /// </summary>
        /// <param name="backtestResult">The backtest result whose charts are inspected.</param>
        /// <returns>Analysis results when any such days are detected.</returns>
        public IReadOnlyList<AnalysisResult> Run(Result backtestResult)
        {
            // 1 – Get the Portfolio Margin chart.
            if (!backtestResult.Charts.TryGetValue("Portfolio Margin", out var chart))
            {
                return SingleResponse(new ResultsAnalysisContext(null));
            }

            // 2 - Load each series from the Portfolio Margin plot.
            //     Each series maps DateTime -> margin value.
            var marginPerAsset = chart.Series
                .Select(kvp => kvp.Value.Values.Cast<ChartPoint>()
                    .ToDictionary(pt => pt.Time, pt => pt.Y))
                .ToList();

            // 3 - Collect all distinct timestamps across all series (the "index" union).
            var allTimestamps = marginPerAsset
                .SelectMany(s => s.Keys)
                .Distinct()
                .Order()
                .ToArray();

            // 4 - Sum series together -> total portfolio margin per timestamp.
            var portfolioMargin = allTimestamps
                .Select(t => marginPerAsset.Sum(s => s.TryGetValue(t, out var margin) && margin != null ? (double)margin.Value : 0.0))
                .ToArray();

            // 5 - 3-day SMA, then count days below 50%.
            var countBelow50 = MathNet.Numerics.Statistics.Statistics.MovingAverage(portfolioMargin, 3).Count(x => x < 50);

            var result = countBelow50 > 0
                ? $"Number of days when the 3-day SMA of the margin usage drops below 50%: {countBelow50}"
                : null;
            var potentialSolutions = result != null ? Solutions() : [];
            return SingleResponse(new ResultsAnalysisContext(result), potentialSolutions);
        }

        private static List<string> Solutions() =>
        [
            "Adjust the strategy logic or position sizing to utilize more margin.",

            "If the algorithm logic leads to periods of time when the portfolio sits in cash, " +
            "consider holding a \"risk-free\" asset during these periods.",
        ];
    }
}
