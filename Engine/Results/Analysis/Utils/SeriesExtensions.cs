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
using Deedle;

namespace QuantConnect.Lean.Engine.Results.Analysis.Utils
{

    /// <summary>
    /// Extension methods on <see cref="Series{TKey,TValue}"/> that replicate the
    /// pandas operations used throughout the test suite.
    /// </summary>
    public static class SeriesExtensions
    {
        // ── Percent change ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns a new series of (v[i] / v[i-1] - 1) values, analogous to
        /// pandas <c>Series.pct_change()</c>.  The first key is dropped.
        /// </summary>
        public static Series<DateTime, double> PctChange(this Series<DateTime, double> s)
        {
            var keys = s.Keys.ToArray();
            var vals = s.Values.ToArray();
            var result = new Dictionary<DateTime, double>(keys.Length - 1);
            for (int i = 1; i < vals.Length; i++)
                result[keys[i]] = vals[i] / vals[i - 1] - 1.0;
            return result.ToSeries();
        }

        // ── Descriptive statistics ───────────────────────────────────────────────

        public static double Mean(this Series<DateTime, double> s)
            => s.Values.Average();

        public static double StdDev(this Series<DateTime, double> s)
        {
            var vals = s.Values.ToArray();
            if (vals.Length < 2) return 0.0;
            double mean = vals.Average();
            double variance = vals.Sum(v => (v - mean) * (v - mean)) / (vals.Length - 1);
            return Math.Sqrt(variance);
        }

        // ── Rolling statistics ───────────────────────────────────────────────────

        /// <summary>Rolling (simple) mean, analogous to <c>Series.rolling(n).mean()</c>.</summary>
        public static Series<DateTime, double> RollingMean(this Series<DateTime, double> s, int window)
        {
            var keys = s.Keys.ToArray();
            var vals = s.Values.ToArray();
            var result = new Dictionary<DateTime, double>();
            for (int i = window - 1; i < vals.Length; i++)
            {
                double sum = 0;
                for (int j = i - window + 1; j <= i; j++) sum += vals[j];
                result[keys[i]] = sum / window;
            }
            return result.ToSeries();
        }

        // ── Frame helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Sums all columns in <paramref name="frame"/> row-by-row, returning a
        /// <c>Series&lt;DateTime, double&gt;</c>. Analogous to
        /// <c>DataFrame.fillna(0).sum(axis=1)</c>.
        /// </summary>
        public static Series<DateTime, double> SumRows(this Frame<DateTime, string> frame)
        {
            return frame.Rows.Observations
                .Select(kvp => KeyValuePair.Create(
                    kvp.Key,
                    kvp.Value.As<double>().Values.Sum()))
                .ToSeries();
        }

        // ── Filtering ────────────────────────────────────────────────────────────

        /// <summary>
        /// Keeps only keys in [<paramref name="from"/>, <paramref name="to"/>].
        /// Analogous to boolean indexing: <c>s[(s.index &gt;= from) &amp; (s.index &lt;= to)]</c>.
        /// </summary>
        public static Series<DateTime, double> FilterByDate(
            this Series<DateTime, double> s, DateTime from, DateTime to)
            => s.Where(kvp => kvp.Key >= from && kvp.Key <= to);

        // ── Index intersection ───────────────────────────────────────────────────

        /// <summary>
        /// Restricts both series to their common keys (inner join on the index).
        /// Analogous to pandas <c>index.intersection</c> + <c>.loc</c>.
        /// </summary>
        public static (Series<DateTime, decimal> Left, Series<DateTime, decimal> Right)
            IntersectIndex(
                this Series<DateTime, decimal> left,
                Series<DateTime, decimal> right)
        {
            var common = left.Keys.Intersect(right.Keys).OrderBy(k => k).ToList();
            return (left.GetItems(common), right.GetItems(common));
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Converts a <see cref="Dictionary{DateTime,decimal}"/> to a Deedle series.</summary>
        public static Series<DateTime, decimal> ToSeries(this Dictionary<DateTime, decimal> dict)
        {
            var sorted = dict.OrderBy(kv => kv.Key).ToList();
            return new Series<DateTime, decimal>(
                sorted.Select(kv => kv.Key),
                sorted.Select(kv => kv.Value));
        }

        /// <summary>Converts an <see cref="IEnumerable{KeyValuePair}"/> to a Deedle series.</summary>
        public static Series<DateTime, decimal> ToSeries(
            this IEnumerable<KeyValuePair<DateTime, decimal>> pairs)
        {
            var sorted = pairs.OrderBy(kv => kv.Key).ToList();
            return new Series<DateTime, decimal>(
                sorted.Select(kv => kv.Key),
                sorted.Select(kv => kv.Value));
        }

        /// <summary>Converts a <see cref="Dictionary{DateTime,double}"/> to a Deedle series.</summary>
        public static Series<DateTime, double> ToSeries(this Dictionary<DateTime, double> dict)
        {
            var sorted = dict.OrderBy(kv => kv.Key).ToList();
            return new Series<DateTime, double>(
                sorted.Select(kv => kv.Key),
                sorted.Select(kv => kv.Value));
        }

        /// <summary>Converts an <see cref="IEnumerable{KeyValuePair}"/> to a Deedle series.</summary>
        public static Series<DateTime, double> ToSeries(
            this IEnumerable<KeyValuePair<DateTime, double>> pairs)
        {
            var sorted = pairs.OrderBy(kv => kv.Key).ToList();
            return new Series<DateTime, double>(
                sorted.Select(kv => kv.Key),
                sorted.Select(kv => kv.Value));
        }
    }
}
