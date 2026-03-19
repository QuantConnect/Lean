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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Flags backtests whose ending equity is zero or negative.
    /// </summary>
    public class PortfolioValueIsNotPositiveAnalysis : BaseResultsAnalysis
    {
        /// <summary>
        /// Gets the description of the non-positive portfolio equity issue.
        /// </summary>
        public override string Issue { get; } = "The portfolio equity dropped to zero or below.";

        /// <summary>
        /// Gets the severity weight for this portfolio value analysis.
        /// </summary>
        public override int Weight { get; } = 98;

        /// <summary>
        /// Runs the portfolio value positivity analysis against the provided backtest parameters.
        /// </summary>
        public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.Result);

        /// <summary>
        /// Checks whether the backtest's ending equity is positive.
        /// </summary>
        /// <param name="result">The backtest result containing portfolio statistics.</param>
        /// <returns>Analysis results flagging the issue when ending equity is zero or negative.</returns>
        public IReadOnlyList<AnalysisResult> Run(Result result)
        {
            var hasEquity = result.TotalPerformance.PortfolioStatistics.EndEquity > 0;
            var potentialSolutions = hasEquity ? [] : Solutions();
            return SingleResponse(new ResultsAnalysisContext(!hasEquity), potentialSolutions);
        }

        /// <summary>
        /// Returns suggested solutions for recovering from non-positive equity.
        /// </summary>
        private static List<string> Solutions() =>
        [
            "Add extended market hours or reduce the data resolution to potentially reduce the impact of gaps between bars.",

            "Reduce position sizes.",

            "If you disabled margin calls, re-enable them. " +
            "Margin calls may reduce your holdings enough to avoid the error.",

            "Investigate if it's a data issue with one of the portfolio holdings that's cause the portfolio value to drop to <= 0.",
        ];
    }
}
