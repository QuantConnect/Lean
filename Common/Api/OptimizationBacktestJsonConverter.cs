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
using System.Linq;
using System.Runtime.CompilerServices;
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

        private static string[] StatisticNames { get; } = StatisticsIndices
            .OrderBy(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToArray();

        // Only 21 Lean statistics where supported when the serialized statistics where a json array
        private static int ArrayStatisticsCount = 21;

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
                        writer.WritePropertyName(index < StatisticsIndices.Count ? index.ToStringInvariant() : name);
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
                if (jStatistics.Type == JTokenType.Array)
                {
                    var statsCount = Math.Min(ArrayStatisticsCount, (jStatistics as JArray).Count);
                    statistics = new Dictionary<string, string>(StatisticsIndices
                        .Where(kvp => kvp.Value < statsCount)
                        .Select(kvp => KeyValuePair.Create(kvp.Key, jStatistics[kvp.Value].Value<string>()))
                        .Where(kvp => kvp.Value != null));
                }
                else
                {
                    statistics = new();
                    foreach (var statistic in jStatistics.Children<JProperty>())
                    {
                        var statisticName = TryConvertToLeanStatisticIndex(statistic.Name, out var index)
                            ? StatisticNames[index]
                            : statistic.Name;
                        statistics[statisticName] = statistic.Value.Value<string>();
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
        private static bool TryConvertToLeanStatisticIndex(string statistic, out int index)
        {
            return int.TryParse(statistic, out index) && index >= 0 && index < StatisticsIndices.Count;
        }
    }
}
