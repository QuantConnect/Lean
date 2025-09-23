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
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Statistics;
using QuantConnect.Util;

namespace QuantConnect.Api
{
    /// <summary>
    /// Json converter for <see cref="OptimizationBacktest"/> which creates a light weight easy to consume serialized version
    /// </summary>
    public class OptimizationBacktestJsonConverter : JsonConverter
    {
        private static Regex _customIndexStatisticRegex = new Regex(@"^(\d+)_(-C)+$");

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(OptimizationBacktest);
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var optimizationBacktest = value as OptimizationBacktest;
            if (ReferenceEquals(optimizationBacktest, null)) return;

            writer.WriteStartObject();

            if (!string.IsNullOrEmpty(optimizationBacktest.Name))
            {
                writer.WritePropertyName("name");
                writer.WriteValue(optimizationBacktest.Name);
            }

            if (!string.IsNullOrEmpty(optimizationBacktest.BacktestId))
            {
                writer.WritePropertyName("id");
                writer.WriteValue(optimizationBacktest.BacktestId);

                writer.WritePropertyName("progress");
                writer.WriteValue(optimizationBacktest.Progress);

                writer.WritePropertyName("exitCode");
                writer.WriteValue(optimizationBacktest.ExitCode);
            }

            if (optimizationBacktest.StartDate != default)
            {
                writer.WritePropertyName("startDate");
                writer.WriteValue(optimizationBacktest.StartDate.ToStringInvariant(DateFormat.ISOShort));
            }

            if (optimizationBacktest.EndDate != default)
            {
                writer.WritePropertyName("endDate");
                writer.WriteValue(optimizationBacktest.EndDate.ToStringInvariant(DateFormat.ISOShort));
            }

            if (optimizationBacktest.OutOfSampleMaxEndDate != null)
            {
                writer.WritePropertyName("outOfSampleMaxEndDate");
                writer.WriteValue(optimizationBacktest.OutOfSampleMaxEndDate.ToStringInvariant(DateFormat.ISOShort));

                writer.WritePropertyName("outOfSampleDays");
                writer.WriteValue(optimizationBacktest.OutOfSampleDays);
            }

            if (!optimizationBacktest.Statistics.IsNullOrEmpty())
            {
                writer.WritePropertyName("statistics");
                writer.WriteStartObject();

                var customStatisticsNames = new HashSet<string>();

                foreach (var (name, statisticValue, index) in optimizationBacktest.Statistics
                    .Select(kvp => (Name: kvp.Key, kvp.Value, Index: StatisticsIndices.TryGetValue(kvp.Key, out var index) ? index : int.MaxValue))
                    .OrderBy(t => t.Index)
                    .ThenByDescending(t => t.Name))
                {
                    var statistic = statisticValue.Replace("%", string.Empty, StringComparison.InvariantCulture);
                    if (Currencies.TryParse(statistic, out var result))
                    {
                        string key;
                        if (index < StatisticsIndices.Count)
                        {
                            key = index.ToStringInvariant();
                        }
                        else
                        {
                            // Custom statistic, write out the name
                            if (IsLeanStatisticIndex(name))
                            {
                                // This is a custom statistic with a name that collides with a Lean statistic index (e.g. "0")
                                key = name + "_";
                                do
                                {
                                    key += "-C";
                                }
                                while (customStatisticsNames.Contains(key));
                            }
                            else
                            {
                                key = name;
                            }
                            customStatisticsNames.Add(key);
                        }

                        writer.WritePropertyName(key);
                        writer.WriteValue(result);
                    }
                }
                writer.WriteEndObject();
            }

            if (optimizationBacktest.ParameterSet != null)
            {
                writer.WritePropertyName("parameterSet");
                serializer.Serialize(writer, optimizationBacktest.ParameterSet.Value);
            }

            if (optimizationBacktest.Equity != null)
            {
                writer.WritePropertyName("equity");

                var equity = JsonConvert.SerializeObject(optimizationBacktest.Equity.Values);
                writer.WriteRawValue(equity);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            var name = jObject["name"].Value<string>();
            var hostName = jObject["hostName"]?.Value<string>();
            var backtestId = jObject["id"].Value<string>();
            var progress = jObject["progress"].Value<decimal>();
            var exitCode = jObject["exitCode"].Value<int>();

            var outOfSampleDays = jObject["outOfSampleDays"]?.Value<int>() ?? default;
            var startDate = jObject["startDate"]?.Value<DateTime?>() ?? default;
            var endDate = jObject["endDate"]?.Value<DateTime?>() ?? default;
            var outOfSampleMaxEndDate = jObject["outOfSampleMaxEndDate"]?.Value<DateTime>();

            var jStatistics = jObject["statistics"];
            Dictionary<string, string> statistics = default;
            if (jStatistics != null)
            {
                var isArray = jStatistics.Type == JTokenType.Array;
                statistics = new Dictionary<string, string>(isArray
                    ? StatisticsIndices
                        .Take(ArrayStatisticsCount)
                        .Select(kvp => KeyValuePair.Create(kvp.Key, jStatistics[kvp.Value].Value<string>()))
                    : StatisticsIndices
                        .Select(kvp => KeyValuePair.Create(kvp.Key, jStatistics[kvp.Value.ToStringInvariant()]?.Value<string>()))
                        .Where(kvp => kvp.Value != null)
                    );

                // We can deserialize custom statistics from the object format
                if (!isArray)
                {
                    var indicesWithCustomStats = new HashSet<string>();
                    foreach (var statistic in jStatistics.Children<JProperty>()
                        .Where(x => !IsLeanStatisticIndex(x.Name))
                        .OrderByDescending(x => x.Name))
                    {
                        var match = _customIndexStatisticRegex.Match(statistic.Name);
                        if (match.Success)
                        {
                            var indexStr = match.Groups[1].Value;
                            if (indicesWithCustomStats.Add(indexStr))
                            {
                                var index = int.Parse(indexStr, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                // This is a custom statistic with a name that collides with a Lean statistic index
                                if (index >= 0 && index < StatisticsIndices.Count)
                                {
                                    statistics[indexStr] = statistic.Value.Value<string>();
                                    continue;
                                }
                            }
                            // else, already processed a custom statistic for this index
                        }

                        statistics[statistic.Name] = statistic.Value.Value<string>();
                    }
                }
            }

            var parameterSet = serializer.Deserialize<ParameterSet>(jObject["parameterSet"].CreateReader());

            var equity = new CandlestickSeries();
            if (jObject["equity"] != null)
            {
                foreach (var point in JsonConvert.DeserializeObject<List<Candlestick>>(jObject["equity"].ToString()))
                {
                    equity.AddPoint(point);
                }
            }

            var optimizationBacktest = new OptimizationBacktest(parameterSet, backtestId, name)
            {
                HostName = hostName,
                Progress = progress,
                ExitCode = exitCode,
                Statistics = statistics,
                Equity = equity,
                EndDate = endDate,
                StartDate = startDate,
                OutOfSampleDays = outOfSampleDays,
                OutOfSampleMaxEndDate = outOfSampleMaxEndDate,
            };

            return optimizationBacktest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLeanStatisticIndex(string statistic)
        {
            return int.TryParse(statistic, out var index) && index >= 0 && index < StatisticsIndices.Count;
        }

        private static Dictionary<string, int> StatisticsIndices = new()
        {
            { PerformanceMetrics.Alpha, 0 },
            { PerformanceMetrics.AnnualStandardDeviation, 1 },
            { PerformanceMetrics.AnnualVariance, 2 },
            { PerformanceMetrics.AverageLoss, 3 },
            { PerformanceMetrics.AverageWin, 4 },
            { PerformanceMetrics.Beta, 5 },
            { PerformanceMetrics.CompoundingAnnualReturn, 6 },
            { PerformanceMetrics.Drawdown, 7 },
            { PerformanceMetrics.EstimatedStrategyCapacity, 8 },
            { PerformanceMetrics.Expectancy, 9 },
            { PerformanceMetrics.InformationRatio, 10 },
            { PerformanceMetrics.LossRate, 11 },
            { PerformanceMetrics.NetProfit, 12 },
            { PerformanceMetrics.ProbabilisticSharpeRatio, 13 },
            { PerformanceMetrics.ProfitLossRatio, 14 },
            { PerformanceMetrics.SharpeRatio, 15 },
            { PerformanceMetrics.TotalFees, 16 },
            { PerformanceMetrics.TotalOrders, 17 },
            { PerformanceMetrics.TrackingError, 18 },
            { PerformanceMetrics.TreynorRatio, 19 },
            { PerformanceMetrics.WinRate, 20 },
            { PerformanceMetrics.SortinoRatio, 21 },
            { PerformanceMetrics.StartEquity, 22 },
            { PerformanceMetrics.EndEquity, 23 },
            { PerformanceMetrics.DrawdownRecovery, 24 },
        };

        // Only 21 Lean statistics where supported when the serialized statistics where a json array
        private static int ArrayStatisticsCount = 21;
    }
}
