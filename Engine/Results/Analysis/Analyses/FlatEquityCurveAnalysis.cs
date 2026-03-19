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
    /// Detects prolonged flat (zero-change) segments in the equity curve.
    /// </summary>
    public class FlatEquityCurveAnalysis : BaseResultsAnalysis
    {
        /// <summary>
        /// Gets the description of the flat equity curve issue.
        /// </summary>
        public override string Issue { get; } = "The equity curve is flat for several days in a row.";

        /// <summary>
        /// Gets the severity weight for the flat equity curve analysis.
        /// </summary>
        public override int Weight { get; } = 99;

        /// <summary>
        /// Runs the flat equity curve analysis against the provided backtest parameters.
        /// </summary>
        public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.EquityCurve);

        /// <summary>
        /// Scans the equity curve for consecutive flat (unchanged) segments.
        /// </summary>
        /// <param name="equityCurve">Daily equity values from the backtest, keyed by date.</param>
        /// <returns>Analysis results describing any detected flat segments.</returns>
        public IReadOnlyList<AnalysisResult> Run(SortedList<DateTime, decimal> equityCurve)
        {
            // Find consecutive runs of identical equity values.
            var keys = equityCurve.Keys.ToArray();
            var vals = equityCurve.Values.ToArray();

            var segments = new List<object>();
            var i = 0;
            while (i < vals.Length)
            {
                var v = vals[i];
                var j = i + 1;
                while (j < vals.Length && vals[j] == v) j++;

                var tradingDays = j - i;
                if (tradingDays > 1)
                {
                    segments.Add(new
                    {
                        start = keys[i],
                        end = keys[j - 1],
                        trading_days = tradingDays,
                    });
                }
                i = j;
            }

            var potentialSolutions = segments.Count > 0 ? Solutions() : [];
            return SingleResponse(new ResultsAnalysisContext(segments.Count > 0 ? segments : null), potentialSolutions);
        }

        /// <summary>
        /// Returns suggested solutions for resolving flat equity curve segments.
        /// </summary>
        private static List<string> Solutions() =>
        [
            "Check if you need to warm-up some data structures, including indicators, RollingWindow objects, and training data.",

            "Check if the algorithm subscribes to any assets. " +
            "Is the universe selection actually selecting anything?",

            "Check if the trading logic ever leads to a trade.",

            "Check if there is enough cash to satisify the minimum order sizes.",
        ];
    }
}
