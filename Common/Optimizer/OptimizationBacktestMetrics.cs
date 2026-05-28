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
using QuantConnect.Packets;
using QuantConnect.Statistics;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Lightweight per-backtest record extracted at <see cref="LeanOptimizer"/> time to avoid retaining the full backtest JSON.
    /// </summary>
    public class OptimizationBacktestMetrics : BacktestSummary
    {
        /// <summary>
        /// The backtest's total performance (wraps <see cref="QuantConnect.Statistics.TradeStatistics"/>,
        /// <see cref="QuantConnect.Statistics.PortfolioStatistics"/>, and <see cref="QuantConnect.Statistics.Trade"/> list); null when absent from the backtest result.
        /// </summary>
        public AlgorithmPerformance TotalPerformance { get; set; }

        /// <summary>
        /// Number of orders the backtest produced.
        /// </summary>
        public int TotalOrders { get; set; }

        /// <summary>
        /// Names of the diagnostic <see cref="QuantConnect.Analysis"/> entries the backtest attached.
        /// </summary>
        public IReadOnlyList<string> AnalysisNames { get; set; }

        /// <summary>
        /// Extracts the fields the analyzer needs from a backtest result JSON; returns null when the parameter set is invalid.
        /// </summary>
        /// <param name="backtestId">The backtest id.</param>
        /// <param name="parameterSet">The parameter set the backtest was run with.</param>
        /// <param name="jsonBacktestResult">The serialized backtest result JSON.</param>
        public static OptimizationBacktestMetrics ExtractFrom(string backtestId, ParameterSet parameterSet, string jsonBacktestResult)
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

            BacktestResult parsed = null;
            if (!string.IsNullOrEmpty(jsonBacktestResult))
            {
                try
                {
                    // DeserializeJson uses the LEAN-wide JsonSerializer (CamelCaseNamingStrategy + OrderJsonConverter),
                    // so polymorphic Orders and camelCase JSON both work without extra configuration.
                    parsed = jsonBacktestResult.DeserializeJson<BacktestResult>();
                }
                catch (JsonException ex)
                {
                    Log.Error(ex, $"OptimizationBacktestMetrics.ExtractFrom(): failed to parse backtest result for '{backtestId}'");
                }
            }

            var analysisNames = parsed?.Analysis == null
                ? (IReadOnlyList<string>)System.Array.Empty<string>()
                : parsed.Analysis
                    .Where(a => !string.IsNullOrEmpty(a?.Name))
                    .Select(a => a.Name)
                    .ToList();

            return new OptimizationBacktestMetrics
            {
                BacktestId = backtestId,
                Parameters = parameters,
                SharpeRatio = parsed?.TotalPerformance?.PortfolioStatistics?.SharpeRatio ?? 0m,
                TotalPerformance = parsed?.TotalPerformance,
                TotalOrders = parsed?.Orders?.Count ?? 0,
                AnalysisNames = analysisNames
            };
        }

        private static Dictionary<string, decimal> ParseParameterSet(ParameterSet parameterSet)
        {
            var result = new Dictionary<string, decimal>();
            if (parameterSet?.Value == null) return result;
            foreach (var kv in parameterSet.Value)
            {
                if (decimal.TryParse(kv.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                {
                    result[kv.Key] = d;
                }
            }
            return result;
        }
    }
}
