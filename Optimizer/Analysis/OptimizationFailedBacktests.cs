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

namespace QuantConnect.Optimizer.Analysis
{
    /// <summary>
    /// Counts how many zero-order backtests carry each backtest-level analysis tag; returns null when no zero-order backtests exist.
    /// </summary>
    internal static class OptimizationFailedBacktests
    {
        // Cap on inspected backtests; a rough tally is enough.
        private const int MaxBacktestsToInspect = 10;

        public static FailedBacktestSummary Build(IReadOnlyList<OptimizationBacktestMetrics> backtests)
        {
            if (backtests == null) return null;

            var zeroOrder = backtests.Where(b => b.TotalOrders == 0).ToList();
            if (zeroOrder.Count == 0) return null;

            var sample = zeroOrder.Take(MaxBacktestsToInspect).ToList();
            var nameCount = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var backtest in sample)
            {
                // De-dupe per backtest so counts are "backtests carrying the tag", not raw occurrences.
                var seen = new HashSet<string>(StringComparer.Ordinal);
                if (backtest.AnalysisNames != null)
                {
                    foreach (var name in backtest.AnalysisNames)
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
