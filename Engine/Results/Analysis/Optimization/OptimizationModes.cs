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
using QuantConnect.Optimizer.Parameters;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis.Optimization
{
    /// <summary>
    /// Detects local maxima of the Sharpe surface on the parameter grid: trials whose
    /// Sharpe is strictly greater than every face-neighbor's Sharpe (face-neighbors differ
    /// from the candidate in exactly one parameter by one grid step). Works in 1, 2, 3+
    /// dimensions. Isolated trials with no neighbors are not flagged — there's no
    /// distribution to find a mode of.
    /// </summary>
    internal static class OptimizationModes
    {
        public static IReadOnlyList<Mode> Find(
            IReadOnlyList<OptimizationTrialMetrics> trials,
            IReadOnlyCollection<OptimizationParameter> parameters)
        {
            var modes = new List<Mode>();
            if (trials == null || parameters == null) return modes;
            if (parameters.Count == 0 || trials.Count == 0) return modes;

            var paramNames = parameters.Select(p => p.Name).ToArray();

            // Sorted distinct values per parameter — these define the grid axes.
            var axisValues = new Dictionary<string, List<double>>();
            foreach (var name in paramNames)
            {
                axisValues[name] = trials
                    .Where(t => t.Parameters.ContainsKey(name))
                    .Select(t => t.Parameters[name])
                    .Distinct()
                    .OrderBy(v => v)
                    .ToList();
            }

            // Map each trial to its grid position (one index per parameter).
            var indexed = new List<(OptimizationTrialMetrics Trial, int[] Indices)>();
            foreach (var t in trials)
            {
                if (!paramNames.All(t.Parameters.ContainsKey)) continue;
                var idx = new int[paramNames.Length];
                var ok = true;
                for (var d = 0; d < paramNames.Length; d++)
                {
                    idx[d] = axisValues[paramNames[d]].IndexOf(t.Parameters[paramNames[d]]);
                    if (idx[d] < 0) { ok = false; break; }
                }
                if (ok) indexed.Add((t, idx));
            }

            // O(1) neighbor lookup by index tuple.
            var byTuple = indexed.ToDictionary(p => TupleKey(p.Indices), p => p.Trial);

            foreach (var (trial, idx) in indexed)
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
                        if (neighbor.Sharpe >= trial.Sharpe) { dominatesAll = false; break; }
                    }
                }

                if (dominatesAll && totalNeighbors > 0)
                {
                    modes.Add(new Mode
                    {
                        BacktestId = trial.BacktestId,
                        Parameters = new Dictionary<string, double>(trial.Parameters),
                        SharpeRatio = trial.Sharpe,
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
