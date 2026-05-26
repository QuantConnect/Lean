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

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Lightweight per-trial record extracted at <c>LeanOptimizer.NewResult</c> time so we do
    /// not retain the full <c>JsonBacktestResult</c> payload for every backtest. A whole
    /// optimization can run thousands of backtests, each carrying many KB of JSON; pulling
    /// out just the fields the optimization-level analyzer needs (parameters, Sharpe, total
    /// orders, diagnostic analysis names) keeps memory bounded.
    /// </summary>
    public class OptimizationTrialMetrics
    {
        /// <summary>The backtest id that produced this trial.</summary>
        public string BacktestId { get; }

        /// <summary>Parameter values (parsed to double) the trial was run with.</summary>
        public IReadOnlyDictionary<string, double> Parameters { get; }

        /// <summary>The trial's Sharpe ratio (0 if <see cref="HasSharpe"/> is false).</summary>
        public double Sharpe { get; }

        /// <summary>True if the trial's Sharpe ratio was parsed from the backtest statistics.</summary>
        public bool HasSharpe { get; }

        /// <summary>The trial's Total Orders count (0 if absent).</summary>
        public int TotalOrders { get; }

        /// <summary>
        /// Names of the diagnostic <see cref="QuantConnect.Analysis"/> entries the backtest's
        /// result handler attached. Used by the zero-order failure breakdown.
        /// </summary>
        public IReadOnlyList<string> AnalysisNames { get; }

        private OptimizationTrialMetrics(
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

        /// <summary>
        /// Extracts the fields the optimization-level analyzer needs from a backtest result
        /// JSON payload. Returns null when the parameter set is missing or the payload cannot
        /// be parsed; callers can drop the trial in that case.
        /// </summary>
        /// <param name="backtestId">The backtest id that produced this trial.</param>
        /// <param name="parameterSet">The parameter set the trial was run with.</param>
        /// <param name="jsonBacktestResult">The serialized backtest result JSON.</param>
        public static OptimizationTrialMetrics ExtractFrom(string backtestId, ParameterSet parameterSet, string jsonBacktestResult)
        {
            if (parameterSet == null)
            {
                return null;
            }

            var parameters = ParseParameterSet(parameterSet);
            if (parameters.Count == 0)
            {
                return null;
            }

            ParsedBacktest parsed = null;
            if (!string.IsNullOrEmpty(jsonBacktestResult))
            {
                try
                {
                    parsed = JsonConvert.DeserializeObject<ParsedBacktest>(jsonBacktestResult);
                }
                catch (JsonException ex)
                {
                    Log.Error(ex, $"OptimizationTrialMetrics.ExtractFrom(): failed to parse backtest result for '{backtestId}'");
                }
            }

            var hasSharpe = TryReadDouble(parsed?.Statistics, "Sharpe Ratio", out var sharpe);
            TryReadInt(parsed?.Statistics, "Total Orders", out var totalOrders);

            var analysisNames = parsed?.Analysis == null
                ? (IReadOnlyList<string>)System.Array.Empty<string>()
                : parsed.Analysis
                    .Where(a => !string.IsNullOrEmpty(a?.Name))
                    .Select(a => a.Name)
                    .ToList();

            return new OptimizationTrialMetrics(backtestId, parameters, sharpe, hasSharpe, totalOrders, analysisNames);
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

        // Minimal-shape DTO for JSON deserialization. Binds only the two backtest-result
        // fields the analyzer reads, avoiding a dependency on the full Result type.
        private sealed class ParsedBacktest
        {
            [JsonProperty("Statistics")]
            public IDictionary<string, string> Statistics { get; set; }

            [JsonProperty("Analysis")]
            public List<QuantConnect.Analysis> Analysis { get; set; }
        }
    }
}
