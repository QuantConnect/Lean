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
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;

namespace QuantConnect.Api
{
    /// <summary>
    /// Selects the best parameter set from cloud optimization backtests.
    /// </summary>
    internal static class WalkForwardOptimizationBacktestSelector
    {
        /// <summary>
        /// Selects the best parameter set according to the specified target.
        /// </summary>
        public static ParameterSet SelectParameterSet(Target target, IReadOnlyList<OptimizationBacktest> backtests)
        {
            OptimizationBacktest bestBacktest = null;
            decimal bestValue = 0;

            foreach (var backtest in backtests.Where(backtest => backtest?.ParameterSet != null))
            {
                if (!TryGetTargetValue(backtest.Statistics, target.Target, out var targetValue))
                {
                    continue;
                }
                if (bestBacktest == null || target.Extremum.Better(bestValue, targetValue))
                {
                    bestBacktest = backtest;
                    bestValue = targetValue;
                }
            }

            return bestBacktest?.ParameterSet;
        }

        private static bool TryGetTargetValue(IDictionary<string, string> statistics, string targetPath, out decimal targetValue)
        {
            targetValue = 0;
            if (statistics == null)
            {
                return false;
            }

            foreach (var targetKey in GetCandidateStatisticKeys(targetPath))
            {
                var statistic = statistics.FirstOrDefault(kvp => string.Equals(kvp.Key, targetKey, StringComparison.OrdinalIgnoreCase));
                if (statistic.Key != null && TryParseStatistic(statistic.Value, out targetValue))
                {
                    return true;
                }

                statistic = statistics.FirstOrDefault(kvp => string.Equals(RemoveWhitespace(kvp.Key), RemoveWhitespace(targetKey), StringComparison.OrdinalIgnoreCase));
                if (statistic.Key != null && TryParseStatistic(statistic.Value, out targetValue))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> GetCandidateStatisticKeys(string targetPath)
        {
            yield return targetPath;

            var path = targetPath.Replace("[", string.Empty, StringComparison.Ordinal)
                .Replace("]", string.Empty, StringComparison.Ordinal)
                .Replace("'", string.Empty, StringComparison.Ordinal)
                .Split(".");
            var lastPart = path.LastOrDefault();
            if (!string.IsNullOrWhiteSpace(lastPart))
            {
                yield return lastPart;
                yield return Regex.Replace(lastPart, "([a-z])([A-Z])", "$1 $2");
            }
        }

        private static bool TryParseStatistic(string value, out decimal parsed)
        {
            value = value?.Trim();
            var percentage = value?.EndsWith('%') == true;
            if (percentage)
            {
                value = value.TrimEnd('%');
            }
            var success = decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed);
            if (success && percentage)
            {
                parsed /= 100;
            }
            return success;
        }

        private static string RemoveWhitespace(string value)
        {
            return value == null ? string.Empty : Regex.Replace(value, @"\s+", string.Empty);
        }
    }
}
