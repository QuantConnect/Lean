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

using QuantConnect.Optimizer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis.Optimization
{
    /// <summary>
    /// Aggregates failure-mode signals across backtests that produced zero orders. For up to
    /// <see cref="MaxBacktestsToInspect"/> zero-order trials, counts how many carry each
    /// distinct backtest-level analysis tag (e.g. "FlatEquityCurveAnalysis"). Returns null
    /// when no trials in the optimization produced zero orders.
    /// </summary>
    internal static class OptimizationFailedBacktests
    {
        // Cap on how many zero-order trials we look at. We only need a rough tally of the
        // common failure modes, not an exhaustive census.
        private const int MaxBacktestsToInspect = 10;

        public static FailedBacktestSummary Build(IReadOnlyList<OptimizationTrialMetrics> trials)
        {
            if (trials == null) return null;

            var zeroOrder = trials.Where(t => t.TotalOrders == 0).ToList();
            if (zeroOrder.Count == 0) return null;

            var sample = zeroOrder.Take(MaxBacktestsToInspect).ToList();
            var nameCount = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var trial in sample)
            {
                // De-dupe per-trial so the counts answer "in how many trials did this tag
                // appear", not "how many total occurrences across trials".
                var seen = new HashSet<string>(StringComparer.Ordinal);
                if (trial.AnalysisNames != null)
                {
                    foreach (var name in trial.AnalysisNames)
                    {
                        if (string.IsNullOrEmpty(name)) continue;
                        if (!seen.Add(name)) continue;
                        nameCount[name] = nameCount.GetValueOrDefault(name, 0) + 1;
                    }
                }
            }

            return new FailedBacktestSummary
            {
                ZeroOrderCount = zeroOrder.Count,
                InspectedCount = sample.Count,
                AnalysisNameCounts = nameCount
            };
        }
    }
}
