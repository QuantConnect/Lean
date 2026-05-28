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
using System.Linq;

namespace QuantConnect.Optimizer.Analysis
{
    /// <summary>
    /// K-means clustering of backtests in standardized parameter space with k chosen by an elbow heuristic.
    /// </summary>
    internal static class OptimizationClustering
    {
        private const int KMin = 2;
        private const int KMaxAbsolute = 5;
        private const int Seed = 42;
        private const int MaxIterations = 100;
        private const double PlateauThreshold = 0.7;

        public static IReadOnlyList<Cluster> Build(
            IReadOnlyList<OptimizationBacktestMetrics> backtests,
            IReadOnlyCollection<OptimizationParameter> parameters)
        {
            var output = new List<Cluster>();
            if (backtests == null || parameters == null) return output;
            if (backtests.Count < KMin + 1 || parameters.Count == 0) return output;

            var paramNames = parameters.Select(p => p.Name).ToArray();

            var usable = backtests
                .Where(b => paramNames.All(b.Parameters.ContainsKey))
                .ToList();
            if (usable.Count < KMin + 1) return output;

            // Cap k_max at ceil(sqrt(N)) so small N doesn't get carved into too many clusters.
            var sqrtCap = (int)Math.Ceiling(Math.Sqrt(usable.Count));
            var kMaxEffective = Math.Min(KMaxAbsolute, sqrtCap);
            var maxK = Math.Min(kMaxEffective, usable.Count - 1);
            if (maxK < KMin) return output;

            // K-means math runs in double so we can use Math.Sqrt / distance comparisons;
            // we convert back to decimal at the boundary for the centroid output.
            var raw = usable
                .Select(b => paramNames.Select(n => (double)b.Parameters[n]).ToArray())
                .ToArray();
            var (normalized, means, stds) = Standardize(raw);

            var byK = new Dictionary<int, KMeansResult>();
            for (var k = KMin; k <= maxK; k++)
            {
                byK[k] = KMeans(normalized, k);
            }
            var bestK = SelectKByElbow(byK);
            var pick = byK[bestK];

            var centroidsOriginal = pick.Centroids
                .Select(c => Denormalize(c, means, stds))
                .ToArray();

            for (var c = 0; c < bestK; c++)
            {
                var memberIndices = Enumerable.Range(0, usable.Count)
                    .Where(i => pick.Labels[i] == c)
                    .ToList();
                if (memberIndices.Count == 0) continue;
                var sharpes = memberIndices.Select(i => usable[i].SharpeRatio).ToList();

                var centroidDict = new Dictionary<string, decimal>(paramNames.Length);
                for (var d = 0; d < paramNames.Length; d++)
                {
                    centroidDict[paramNames[d]] = (decimal)centroidsOriginal[c][d];
                }

                output.Add(new Cluster
                {
                    Centroid = centroidDict,
                    MemberCount = memberIndices.Count,
                    SharpeMean = sharpes.Average(),
                    SharpeStdDev = StdDev(sharpes),
                    SharpeMin = sharpes.Min(),
                    SharpeMax = sharpes.Max()
                });
            }

            // Order by mean Sharpe descending so the best-performing region is first.
            return output.OrderByDescending(x => x.SharpeMean).ToList();
        }

        private static int SelectKByElbow(Dictionary<int, KMeansResult> results)
        {
            var ks = results.Keys.OrderBy(k => k).ToList();
            if (ks.Count == 1) return ks[0];
            for (var i = 1; i < ks.Count; i++)
            {
                var prev = results[ks[i - 1]].Wcss;
                var curr = results[ks[i]].Wcss;
                if (prev > 0 && curr / prev > PlateauThreshold) return ks[i - 1];
            }
            return ks[^1];
        }

        private sealed class KMeansResult
        {
            public int[] Labels { get; }
            public double[][] Centroids { get; }
            public double Wcss { get; }

            public KMeansResult(int[] labels, double[][] centroids, double wcss)
            {
                Labels = labels;
                Centroids = centroids;
                Wcss = wcss;
            }
        }

        private static KMeansResult KMeans(double[][] points, int k)
        {
            var n = points.Length;
            var d = points[0].Length;
            var rng = new Random(Seed);

            // k-means++ initialization.
            var centroids = new double[k][];
            centroids[0] = (double[])points[rng.Next(n)].Clone();
            for (var c = 1; c < k; c++)
            {
                var dists = new double[n];
                for (var i = 0; i < n; i++)
                {
                    var min = double.MaxValue;
                    for (var j = 0; j < c; j++)
                    {
                        var dd = SquaredDistance(points[i], centroids[j]);
                        if (dd < min) min = dd;
                    }
                    dists[i] = min;
                }
                var sum = dists.Sum();
                var pick = rng.NextDouble() * sum;
                double acc = 0;
                var chosen = n - 1;
                for (var i = 0; i < n; i++)
                {
                    acc += dists[i];
                    if (acc >= pick) { chosen = i; break; }
                }
                centroids[c] = (double[])points[chosen].Clone();
            }

            // Lloyd's iteration.
            var labels = new int[n];
            for (var iter = 0; iter < MaxIterations; iter++)
            {
                var changed = false;
                for (var i = 0; i < n; i++)
                {
                    var best = 0;
                    var bestDist = double.MaxValue;
                    for (var c = 0; c < k; c++)
                    {
                        var dd = SquaredDistance(points[i], centroids[c]);
                        if (dd < bestDist) { bestDist = dd; best = c; }
                    }
                    if (labels[i] != best) { labels[i] = best; changed = true; }
                }
                if (!changed && iter > 0) break;

                var sums = new double[k][];
                var counts = new int[k];
                for (var c = 0; c < k; c++) sums[c] = new double[d];
                for (var i = 0; i < n; i++)
                {
                    var c = labels[i];
                    counts[c]++;
                    for (var j = 0; j < d; j++) sums[c][j] += points[i][j];
                }
                for (var c = 0; c < k; c++)
                {
                    if (counts[c] == 0) continue;
                    for (var j = 0; j < d; j++) centroids[c][j] = sums[c][j] / counts[c];
                }
            }

            double wcss = 0;
            for (var i = 0; i < n; i++) wcss += SquaredDistance(points[i], centroids[labels[i]]);
            return new KMeansResult(labels, centroids, wcss);
        }

        private static (double[][] Normalized, double[] Means, double[] Stds) Standardize(double[][] points)
        {
            var n = points.Length;
            var d = points[0].Length;
            var means = new double[d];
            var stds = new double[d];
            for (var j = 0; j < d; j++)
            {
                double s = 0;
                for (var i = 0; i < n; i++) s += points[i][j];
                means[j] = s / n;
            }
            for (var j = 0; j < d; j++)
            {
                double s = 0;
                for (var i = 0; i < n; i++)
                {
                    var t = points[i][j] - means[j];
                    s += t * t;
                }
                stds[j] = n > 1 ? Math.Sqrt(s / (n - 1)) : 1.0;
                if (stds[j] < 1e-12) stds[j] = 1.0;
            }
            var normalized = new double[n][];
            for (var i = 0; i < n; i++)
            {
                normalized[i] = new double[d];
                for (var j = 0; j < d; j++) normalized[i][j] = (points[i][j] - means[j]) / stds[j];
            }
            return (normalized, means, stds);
        }

        private static double[] Denormalize(double[] standardized, double[] means, double[] stds)
        {
            var d = standardized.Length;
            var result = new double[d];
            for (var j = 0; j < d; j++) result[j] = standardized[j] * stds[j] + means[j];
            return result;
        }

        private static double SquaredDistance(double[] a, double[] b)
        {
            double s = 0;
            for (var i = 0; i < a.Length; i++) { var d = a[i] - b[i]; s += d * d; }
            return s;
        }

        private static decimal StdDev(IReadOnlyCollection<decimal> values)
        {
            if (values.Count < 2) return 0m;
            var mean = values.Average();
            var s = values.Sum(v => (v - mean) * (v - mean));
            return (decimal)Math.Sqrt((double)(s / (values.Count - 1)));
        }
    }
}
