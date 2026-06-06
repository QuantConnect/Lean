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

using QuantConnect.Optimizer.Parameters;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QuantConnect.Optimizer.Analysis
{
    /// <summary>
    /// Detects local maxima of the Sharpe surface; backtests strictly greater than every face-neighbor on the parameter grid.
    /// </summary>
    internal static class OptimizationModes
    {
        public static IReadOnlyList<Mode> Find(
            IReadOnlyList<OptimizationBacktestMetrics> backtests,
            IReadOnlyCollection<OptimizationParameter> parameters)
        {
            var modes = new List<Mode>();
            if (backtests == null || parameters == null) return modes;
            if (parameters.Count == 0 || backtests.Count == 0) return modes;

            var paramNames = parameters.Select(p => p.Name).ToArray();

            // Sorted distinct values per parameter define the grid axes.
            var axisValues = new Dictionary<string, List<decimal>>();
            foreach (var name in paramNames)
            {
                axisValues[name] = backtests
                    .Where(b => b.Parameters.ContainsKey(name))
                    .Select(b => b.Parameters[name])
                    .Distinct()
                    .OrderBy(v => v)
                    .ToList();
            }

            // Map each backtest to its grid position.
            var indexed = new List<(OptimizationBacktestMetrics Backtest, int[] Indices)>();
            foreach (var b in backtests)
            {
                if (!paramNames.All(b.Parameters.ContainsKey)) continue;
                var idx = new int[paramNames.Length];
                var ok = true;
                for (var d = 0; d < paramNames.Length; d++)
                {
                    idx[d] = axisValues[paramNames[d]].IndexOf(b.Parameters[paramNames[d]]);
                    if (idx[d] < 0) { ok = false; break; }
                }
                if (ok) indexed.Add((b, idx));
            }

            var byTuple = indexed.ToDictionary(p => TupleKey(p.Indices), p => p.Backtest);

            foreach (var (backtest, idx) in indexed)
            {
                var totalNeighbors = 0;
                var dominatesAll = true;

                for (var d = 0; d < paramNames.Length && dominatesAll; d++)
                {
                    var axisLen = axisValues[paramNames[d]].Count;
                    foreach (var delta in new[] { -1, 1 })
                    {
                        var ni = idx[d] + delta;
                        if (ni < 0 || ni >= axisLen) continue;

                        var neighborIdx = (int[])idx.Clone();
                        neighborIdx[d] = ni;
                        if (!byTuple.TryGetValue(TupleKey(neighborIdx), out var neighbor)) continue;

                        totalNeighbors++;
                        if (neighbor.SharpeRatio >= backtest.SharpeRatio) { dominatesAll = false; break; }
                    }
                }

                if (dominatesAll && totalNeighbors > 0)
                {
                    modes.Add(new Mode
                    {
                        BacktestId = backtest.BacktestId,
                        Parameters = new Dictionary<string, decimal>(backtest.Parameters),
                        SharpeRatio = backtest.SharpeRatio,
                        NeighborCount = totalNeighbors
                    });
                }
            }

            return modes.OrderByDescending(m => m.SharpeRatio).ToList();
        }

        private static string TupleKey(int[] indices)
            => string.Join(",", indices.Select(i => i.ToString(CultureInfo.InvariantCulture)));
    }
}
