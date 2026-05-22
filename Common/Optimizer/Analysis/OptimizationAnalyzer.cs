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

using Newtonsoft.Json;
using QuantConnect.Logging;
using QuantConnect.Optimizer.Parameters;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QuantConnect.Optimizer.Analysis
{
    // Types like OptimizationAnalysis / SharpeSummary / Cluster / Mode / etc. live in
    // QuantConnect.Optimizer (the parent namespace); they're referenced unqualified
    // because the file is inside QuantConnect.Optimizer.Analysis and the C# compiler
    // walks outward through parent namespaces when resolving simple names.
    /// <summary>
    /// Builds an aggregate diagnostic (<see cref="OptimizationAnalysis"/>) from a completed
    /// optimization's compute-job results. Computes the Sharpe distribution, the best trial,
    /// per-parameter sensitivity slices, k-means clusters in parameter space, local maxima,
    /// and a zero-order failure breakdown. The optimization-side analogue of
    /// <see cref="ResultsAnalyzer"/>; invoked from <c>LeanOptimizer.TriggerOnEndEvent</c>.
    /// </summary>
    public class OptimizationAnalyzer
    {
        private readonly OptimizationAnalysisRunParameters _parameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizationAnalyzer"/> class.
        /// </summary>
        /// <param name="parameters">The inputs to analyze.</param>
        public OptimizationAnalyzer(OptimizationAnalysisRunParameters parameters)
        {
            _parameters = parameters;
        }

        /// <summary>
        /// Runs the full analysis pipeline and returns the aggregate diagnostic.
        /// </summary>
        /// <returns>The populated <see cref="OptimizationAnalysis"/>, or <c>null</c> if no usable trials remain.</returns>
        public OptimizationAnalysis Run()
        {
            var allTrials = ExtractTrials(_parameters.CompletedTrials);
            var trials = allTrials.Where(t => t.HasSharpe).ToList();
            if (trials.Count == 0)
            {
                Log.Trace("OptimizationAnalyzer.Run(): no completed backtests with parsable Sharpe ratios; skipping analysis");
                return null;
            }

            var sharpes = trials.Select(t => t.Sharpe).ToList();
            var overall = new SharpeSummary
            {
                Mean = sharpes.Average(),
                StdDev = StdDev(sharpes),
                Min = sharpes.Min(),
                Max = sharpes.Max(),
                Median = Median(sharpes)
            };

            // Always maximize Sharpe. The optimization's chosen Criterion may be something else
            // but the analyzer uses Sharpe as the universal yardstick for the analysis surface.
            var best = trials.OrderByDescending(t => t.Sharpe).First();
            var bestSummary = new BestTrialSummary
            {
                BacktestId = best.BacktestId,
                Parameters = new Dictionary<string, double>(best.Parameters),
                SharpeRatio = best.Sharpe
            };

            var paramReports = _parameters.OptimizationParameters
                .Select(p => OptimizationSlicing.AnalyzeParameter(p, trials, best))
                .ToList();

            var clusters = OptimizationClustering.Build(trials, _parameters.OptimizationParameters);
            var modes = OptimizationModes.Find(trials, _parameters.OptimizationParameters);
            var failed = OptimizationFailedBacktests.Build(allTrials);

            return new OptimizationAnalysis
            {
                TrialCountTotal = allTrials.Count,
                TrialCountUsed = trials.Count,
                OverallSharpe = overall,
                Best = bestSummary,
                Parameters = paramReports,
                Clusters = clusters,
                Modes = modes,
                FailedBacktests = failed
            };
        }

        // ── Trial extraction ─────────────────────────────────────────────────────

        /// <summary>
        /// Parses each completed trial's JSON backtest payload into a typed
        /// <see cref="TrialRecord"/>: parameter values + Sharpe + total orders + the backtest's
        /// own diagnostic Analysis tags.
        /// </summary>
        private static List<TrialRecord> ExtractTrials(IReadOnlyList<OptimizationTrial> trialInputs)
        {
            var trials = new List<TrialRecord>();
            if (trialInputs == null) return trials;

            foreach (var t in trialInputs)
            {
                if (t == null || string.IsNullOrEmpty(t.JsonBacktestResult) || t.ParameterSet == null)
                {
                    continue;
                }

                Dictionary<string, double> paramValues;
                try
                {
                    paramValues = ParseParameterSet(t.ParameterSet);
                }
                catch
                {
                    continue;
                }
                if (paramValues.Count == 0)
                {
                    continue;
                }

                ParsedBacktest parsed;
                try
                {
                    parsed = JsonConvert.DeserializeObject<ParsedBacktest>(t.JsonBacktestResult);
                }
                catch
                {
                    continue;
                }
                if (parsed == null)
                {
                    continue;
                }

                var hasSharpe = TryReadDouble(parsed.Statistics, "Sharpe Ratio", out var sharpe);
                TryReadInt(parsed.Statistics, "Total Orders", out var totalOrders);

                var analysisNames = parsed.Analysis == null
                    ? new List<string>()
                    : parsed.Analysis
                        .Where(a => !string.IsNullOrEmpty(a?.Name))
                        .Select(a => a.Name)
                        .ToList();

                trials.Add(new TrialRecord(
                    backtestId: t.BacktestId,
                    parameters: paramValues,
                    sharpe: sharpe,
                    hasSharpe: hasSharpe,
                    totalOrders: totalOrders,
                    analysisNames: analysisNames));
            }
            return trials;
        }

        private static Dictionary<string, double> ParseParameterSet(ParameterSet parameterSet)
        {
            var result = new Dictionary<string, double>();
            if (parameterSet?.Value == null) return result;
            foreach (var kv in parameterSet.Value)
            {
                if (double.TryParse(kv.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                {
                    result[kv.Key] = d;
                }
            }
            return result;
        }

        private static bool TryReadDouble(IDictionary<string, string> statistics, string key, out double value)
        {
            value = 0;
            if (statistics == null) return false;
            if (!statistics.TryGetValue(key, out var raw) || string.IsNullOrEmpty(raw)) return false;
            // QC statistics often carry trailing units like "%" — strip them before parsing.
            var trimmed = raw.TrimEnd('%').Trim();
            return double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryReadInt(IDictionary<string, string> statistics, string key, out int value)
        {
            value = 0;
            if (!TryReadDouble(statistics, key, out var d)) return false;
            value = (int)d;
            return true;
        }

        // ── Aggregate helpers ────────────────────────────────────────────────────

        private static double StdDev(IReadOnlyCollection<double> values)
        {
            if (values.Count < 2) return 0;
            var mean = values.Average();
            var s = values.Sum(v => (v - mean) * (v - mean));
            return System.Math.Sqrt(s / (values.Count - 1));
        }

        private static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0) return 0;
            return sorted.Count % 2 == 1
                ? sorted[sorted.Count / 2]
                : 0.5 * (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]);
        }

        // ── Minimal-shape DTO for JSON deserialization ───────────────────────────
        //
        // The JsonBacktestResult string carried by OptimizationResult is the Newtonsoft.Json
        // serialization of the in-process backtest Result. We only need a few fields; rather
        // than depend on the full Result type (with its many nested dependencies), bind a
        // minimal shape with the two properties we read.

        private sealed class ParsedBacktest
        {
            [JsonProperty("Statistics")]
            public IDictionary<string, string> Statistics { get; set; }

            [JsonProperty("Analysis")]
            public List<QuantConnect.Analysis> Analysis { get; set; }
        }
    }

    /// <summary>
    /// Internal per-trial record used across the optimization-analysis helpers
    /// (<see cref="OptimizationClustering"/>, <see cref="OptimizationModes"/>,
    /// <see cref="OptimizationSlicing"/>, <see cref="OptimizationFailedBacktests"/>).
    /// Public visibility kept low: only the helpers in this namespace need it.
    /// </summary>
    internal sealed class TrialRecord
    {
        public string BacktestId { get; }
        public IReadOnlyDictionary<string, double> Parameters { get; }
        public double Sharpe { get; }
        public bool HasSharpe { get; }
        public int TotalOrders { get; }
        public IReadOnlyList<string> AnalysisNames { get; }

        public TrialRecord(
            string backtestId,
            IReadOnlyDictionary<string, double> parameters,
            double sharpe,
            bool hasSharpe,
            int totalOrders,
            IReadOnlyList<string> analysisNames)
        {
            BacktestId = backtestId;
            Parameters = parameters;
            Sharpe = sharpe;
            HasSharpe = hasSharpe;
            TotalOrders = totalOrders;
            AnalysisNames = analysisNames;
        }
    }
}
