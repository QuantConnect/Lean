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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QuantConnect.Optimizer.Analysis
{
    /// <summary>
    /// Per-parameter sensitivity analysis via 1-D slices through the backtest cloud with a piecewise linear fit.
    /// </summary>
    internal static class OptimizationSlicing
    {
        public static ParameterReport AnalyzeParameter(
            OptimizationParameter parameter,
            IReadOnlyList<OptimizationBacktestMetrics> backtests,
            OptimizationBacktestMetrics best)
        {
            var name = parameter.Name;
            var owning = backtests.Where(b => b.Parameters.ContainsKey(name)).ToList();

            var otherParamNames = owning
                .SelectMany(b => b.Parameters.Keys)
                .Where(k => k != name)
                .Distinct()
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToList();

            // Group backtests by other-parameter values; each group is one 1-D slice.
            IEnumerable<IGrouping<string, OptimizationBacktestMetrics>> grouped = otherParamNames.Count == 0
                ? new[] { owning.GroupBy(_ => "").FirstOrDefault() }
                      .Where(g => g != null)
                      .Cast<IGrouping<string, OptimizationBacktestMetrics>>()
                : owning.GroupBy(b => SliceKey(b, otherParamNames));

            var slices = new List<SliceFit>();
            foreach (var group in grouped)
            {
                var slice = BuildSlice(group.ToList(), name, otherParamNames);
                if (slice != null) slices.Add(slice);
            }

            var hasBest = best.Parameters.TryGetValue(name, out var bestValue);
            var (searchedMin, searchedMax, step) = ExtractGridSpec(parameter, owning, name);
            var bestAtEdge = hasBest && IsAtSearchedEdge(bestValue, searchedMin, searchedMax, step);

            var meanRange = slices.Count > 0 ? slices.Average(s => s.SharpeRange) : 0m;
            var maxRange = slices.Count > 0 ? slices.Max(s => s.SharpeRange) : 0m;
            var maxDerivPerStep = slices.Count > 0
                ? slices.Max(s => s.MaxAbsDerivative) * (step ?? 1m)
                : 0m;

            return new ParameterReport
            {
                Name = name,
                SearchedMin = searchedMin,
                SearchedMax = searchedMax,
                Step = step,
                MeanWithinSliceSharpeRange = meanRange,
                MaxWithinSliceSharpeRange = maxRange,
                MaxAbsDerivativePerStep = maxDerivPerStep,
                BestValue = bestValue,
                BestAtSearchedEdge = bestAtEdge,
                Slices = slices
            };
        }

        private static SliceFit BuildSlice(
            List<OptimizationBacktestMetrics> backtests,
            string varyingParamName,
            IReadOnlyList<string> otherParamNames)
        {
            // Defensively collapse duplicate parameter values by averaging Sharpes.
            var points = backtests
                .GroupBy(b => b.Parameters[varyingParamName])
                .Select(g => (X: g.Key, Y: g.Average(b => b.SharpeRatio)))
                .OrderBy(p => p.X)
                .ToList();

            if (points.Count == 0) return null;

            var xs = points.Select(p => p.X).ToList();
            var ys = points.Select(p => p.Y).ToList();
            var sharpeRange = ys.Count >= 2 ? ys.Max() - ys.Min() : 0m;

            // Piecewise linear: one segment per adjacent pair; slope is sensitivity per parameter unit.
            var segments = new List<LinearSegment>();
            decimal maxAbsDerivative = 0m;
            for (var i = 0; i < points.Count - 1; i++)
            {
                var dx = xs[i + 1] - xs[i];
                var slope = (ys[i + 1] - ys[i]) / dx;
                segments.Add(new LinearSegment
                {
                    XLo = xs[i],
                    XHi = xs[i + 1],
                    A = ys[i],
                    B = slope
                });
                var absSlope = Math.Abs(slope);
                if (absSlope > maxAbsDerivative) maxAbsDerivative = absSlope;
            }

            var fixedParams = new Dictionary<string, decimal>();
            if (otherParamNames.Count > 0)
            {
                var first = backtests[0];
                foreach (var p in otherParamNames)
                {
                    if (first.Parameters.TryGetValue(p, out var v)) fixedParams[p] = v;
                }
            }

            return new SliceFit
            {
                FixedParameters = fixedParams,
                SharpeRange = sharpeRange,
                MaxAbsDerivative = maxAbsDerivative,
                Segments = segments
            };
        }

        private static (decimal Min, decimal Max, decimal? Step) ExtractGridSpec(
            OptimizationParameter parameter,
            IReadOnlyList<OptimizationBacktestMetrics> owning,
            string name)
        {
            if (parameter is OptimizationStepParameter step)
            {
                return (step.MinValue, step.MaxValue, step.Step);
            }

            // Fallback for non-step parameters: infer min/max/step from measured values.
            var values = owning.Select(b => b.Parameters[name]).Distinct().OrderBy(v => v).ToList();
            if (values.Count == 0) return (0m, 0m, null);
            if (values.Count == 1) return (values[0], values[0], null);

            var min = values[0];
            var max = values[^1];
            var gaps = new List<decimal>();
            for (var i = 1; i < values.Count; i++) gaps.Add(values[i] - values[i - 1]);
            return (min, max, gaps.Min());
        }

        private static bool IsAtSearchedEdge(decimal value, decimal min, decimal max, decimal? step)
        {
            var tol = ((step ?? 1m) / 2m) + 1e-9m;
            return Math.Abs(value - min) <= tol || Math.Abs(value - max) <= tol;
        }

        private static string SliceKey(OptimizationBacktestMetrics backtest, IReadOnlyList<string> otherParamNames)
        {
            return string.Join("|", otherParamNames.Select(p =>
                (backtest.Parameters.TryGetValue(p, out var v) ? v.ToString(CultureInfo.InvariantCulture) : "NaN")));
        }
    }
}
