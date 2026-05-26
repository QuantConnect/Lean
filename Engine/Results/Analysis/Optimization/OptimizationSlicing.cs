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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis.Optimization
{
    /// <summary>
    /// Per-parameter sensitivity analysis. For each optimized parameter, builds a set of
    /// 1-D "slices" through the trial cloud (this parameter varies; every other is held
    /// constant), fits a piecewise linear interpolant to each slice, and aggregates
    /// sensitivity metrics across slices.
    /// </summary>
    internal static class OptimizationSlicing
    {
        public static ParameterReport AnalyzeParameter(
            OptimizationParameter parameter,
            IReadOnlyList<OptimizationTrialMetrics> trials,
            OptimizationTrialMetrics best)
        {
            var name = parameter.Name;
            var owning = trials.Where(t => t.Parameters.ContainsKey(name)).ToList();

            var otherParamNames = owning
                .SelectMany(t => t.Parameters.Keys)
                .Where(k => k != name)
                .Distinct()
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToList();

            // Group trials by the values held constant in other parameters — each group is one 1-D slice.
            IEnumerable<IGrouping<string, OptimizationTrialMetrics>> grouped = otherParamNames.Count == 0
                ? new[] { owning.GroupBy(_ => "").FirstOrDefault() }
                      .Where(g => g != null)
                      .Cast<IGrouping<string, OptimizationTrialMetrics>>()
                : owning.GroupBy(t => SliceKey(t, otherParamNames));

            var primaryKey = otherParamNames.Count == 0 ? "" : SliceKey(best, otherParamNames);

            var slices = new List<SliceFit>();
            foreach (var group in grouped)
            {
                var isPrimary = group.Key == primaryKey;
                var slice = BuildSlice(group.ToList(), name, otherParamNames, isPrimary);
                if (slice != null) slices.Add(slice);
            }

            var distinctValueCount = owning.Select(t => t.Parameters[name]).Distinct().Count();
            var bestValue = best.Parameters.TryGetValue(name, out var bv) ? bv : double.NaN;
            var (searchedMin, searchedMax, step) = ExtractGridSpec(parameter, owning, name);
            var bestAtEdge = IsAtSearchedEdge(bestValue, searchedMin, searchedMax, step);

            var meanRange = slices.Count > 0 ? slices.Average(s => s.SharpeRange) : 0;
            var maxRange = slices.Count > 0 ? slices.Max(s => s.SharpeRange) : 0;
            var maxDerivPerStep = slices.Count > 0
                ? slices.Max(s => s.MaxAbsDerivative) * (step ?? 1.0)
                : 0;

            return new ParameterReport
            {
                Name = name,
                SearchedMin = searchedMin,
                SearchedMax = searchedMax,
                Step = step,
                DistinctValueCount = distinctValueCount,
                MeanWithinSliceSharpeRange = meanRange,
                MaxWithinSliceSharpeRange = maxRange,
                MaxAbsDerivativePerStep = maxDerivPerStep,
                BestValue = bestValue,
                BestAtSearchedEdge = bestAtEdge,
                Slices = slices
            };
        }

        private static SliceFit BuildSlice(
            List<OptimizationTrialMetrics> trials,
            string varyingParamName,
            IReadOnlyList<string> otherParamNames,
            bool isPrimary)
        {
            // Collapse duplicate parameter values within a slice (shouldn't happen in a true grid
            // sweep, but be defensive — average their Sharpes).
            var points = trials
                .GroupBy(t => t.Parameters[varyingParamName])
                .Select(g => (X: g.Key, Y: g.Average(t => t.Sharpe)))
                .OrderBy(p => p.X)
                .ToList();

            if (points.Count == 0) return null;

            var xs = points.Select(p => p.X).ToList();
            var ys = points.Select(p => p.Y).ToList();
            var sharpeRange = ys.Count >= 2 ? ys.Max() - ys.Min() : 0;

            // Piecewise linear: one segment per adjacent pair; slope IS the sensitivity
            // per unit of the parameter for this slice.
            var segments = new List<LinearSegment>();
            double maxAbsDerivative = 0;
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

            var fixedParams = new Dictionary<string, double>();
            if (otherParamNames.Count > 0)
            {
                var first = trials[0];
                foreach (var p in otherParamNames)
                {
                    if (first.Parameters.TryGetValue(p, out var v)) fixedParams[p] = v;
                }
            }

            return new SliceFit
            {
                FixedParameters = fixedParams,
                ParameterValues = xs,
                SharpeValues = ys,
                SharpeRange = sharpeRange,
                MaxAbsDerivative = maxAbsDerivative,
                IsPrimary = isPrimary,
                Segments = segments
            };
        }

        private static (double Min, double Max, double? Step) ExtractGridSpec(
            OptimizationParameter parameter,
            IReadOnlyList<OptimizationTrialMetrics> owning,
            string name)
        {
            if (parameter is OptimizationStepParameter step)
            {
                return ((double)step.MinValue, (double)step.MaxValue,
                    step.Step.HasValue ? (double)step.Step.Value : (double?)null);
            }

            // Fallback when the parameter isn't a step-based one: infer min/max/step from the
            // measured distinct values across trials. Step is the smallest gap.
            var values = owning.Select(t => t.Parameters[name]).Distinct().OrderBy(v => v).ToList();
            if (values.Count == 0) return (0, 0, null);
            if (values.Count == 1) return (values[0], values[0], null);

            var min = values[0];
            var max = values[^1];
            var gaps = new List<double>();
            for (var i = 1; i < values.Count; i++) gaps.Add(values[i] - values[i - 1]);
            return (min, max, gaps.Min());
        }

        private static bool IsAtSearchedEdge(double value, double min, double max, double? step)
        {
            if (double.IsNaN(value)) return false;
            var tol = ((step ?? 1.0) / 2) + 1e-9;
            return Math.Abs(value - min) <= tol || Math.Abs(value - max) <= tol;
        }

        private static string SliceKey(OptimizationTrialMetrics t, IReadOnlyList<string> otherParamNames)
        {
            return string.Join("|", otherParamNames.Select(p =>
                (t.Parameters.TryGetValue(p, out var v) ? v : double.NaN)
                    .ToString("R", CultureInfo.InvariantCulture)));
        }
    }
}
