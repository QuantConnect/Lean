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
        /// Maximum number of flat segments included in the sample. When more are found,
        /// only the longest ones are kept and the total count is reported in the result.
        /// </summary>
        private const int MaxReportedSegments = 5;

        /// <summary>
        /// Runs the flat equity curve analysis against the provided backtest parameters.
        /// </summary>
        public override IReadOnlyList<QuantConnect.Analysis> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.EquityCurve);

        /// <summary>
        /// Scans the equity curve for consecutive flat (unchanged) segments.
        /// </summary>
        /// <param name="equityCurve">Daily equity values from the backtest, keyed by date.</param>
        /// <returns>Analysis results describing any detected flat segments.</returns>
        public IReadOnlyList<QuantConnect.Analysis> Run(SortedList<DateTime, decimal> equityCurve)
        {
            // Find consecutive runs of identical equity values.
            var keys = equityCurve.Keys.ToArray();
            var vals = equityCurve.Values.ToArray();

            var segments = new List<(DateTime start, DateTime end, int tradingDays)>();
            var i = 0;
            while (i < vals.Length)
            {
                var v = vals[i];
                var j = i + 1;
                while (j < vals.Length && vals[j] == v) j++;

                var tradingDays = j - i;
                if (tradingDays > 1)
                {
                    segments.Add((keys[i], keys[j - 1], tradingDays));
                }
                i = j;
            }

            if (segments.Count == 0)
            {
                return SingleResponse(null);
            }

            // The number of flat segments is unbounded, so only keep the longest ones
            // and report the total count when there are more than we display.
            var biggestSegments = segments
                .OrderByDescending(s => s.tradingDays)
                .Take(MaxReportedSegments)
                .Select(s => new
                {
                    start = s.start,
                    end = s.end,
                    trading_days = s.tradingDays,
                })
                .ToList();

            var totalCount = segments.Count > MaxReportedSegments ? segments.Count : (int?)null;
            return SingleResponse(biggestSegments, totalCount, Solutions());
        }

        /// <summary>
        /// Returns suggested solutions for resolving flat equity curve segments.
        /// </summary>
        private static List<string> Solutions() =>
        [
            "Log how often each entry condition passes individually: when several conditions must align " +
            "(trend, volume, indicator thresholds), it is common that they are never all true at the same time. " +
            "Relax the single most restrictive condition and re-run, changing one condition per backtest " +
            "instead of redesigning the strategy.",

            "Check if you need to warm-up some data structures, including indicators, RollingWindow objects, and training data. " +
            "Log how many assets pass IsReady or warm-up gates: they can silently exclude most of the universe.",

            "Check if the algorithm subscribes to any assets. " +
            "Is the universe selection actually selecting anything? Log the selection count on each rebalance.",

            "Check custom data sources actually deliver data by logging the first points received: " +
            "a reader or date-format error can silently produce no data and therefore no signals.",

            "Check minimum-history or training-window requirements against the history the data source actually provides: " +
            "if a model requires more history than exists, the trading logic may never activate.",

            "Check if there is enough cash to satisfy the minimum order sizes.",
        ];
    }
}
