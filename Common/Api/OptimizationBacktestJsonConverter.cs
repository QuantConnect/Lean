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

                // TODO: Handle special case where custom statistics names are integers from 0 -> StatisticIndices.Length

                foreach (var (name, statisticValue, index) in optimizationBacktest.Statistics
                    .Select(kvp => (Name: kvp.Key, kvp.Value, Index: TryGetStatisticIndex(kvp.Key, out var index) ? index : int.MaxValue))
                    .OrderBy(t => t.Index)
                    .ThenBy(t => t.Name))
                {
                    switch (name)
                    {
                        case PerformanceMetrics.PortfolioTurnover:
                        case PerformanceMetrics.SortinoRatio:
                        case PerformanceMetrics.StartEquity:
                        case PerformanceMetrics.EndEquity:
                        case PerformanceMetrics.DrawdownRecovery:
                            continue;
                    }
                    var statistic = statisticValue.Replace("%", string.Empty, StringComparison.InvariantCulture);
                    if (Currencies.TryParse(statistic, out var result))
                    {
                        writer.WritePropertyName(index < StatisticsIndices.Length ? index.ToStringInvariant() : name);
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
                statistics = new Dictionary<string, string>(
                    StatisticsIndices.Select(kvp => KeyValuePair.Create(kvp.Key, jStatistics[GetStatisticDeserializationIndex(kvp.Value, isArray)].Value<string>())));

                // We can deserialize custom statistics from the object format
                if (!isArray)
                {
                    foreach (var statistic in jStatistics.Children<JProperty>())
                    {
                        if (int.TryParse(statistic.Name, out var index) && index >= 0 && index < StatisticsIndices.Length)
                        {
                            // Already deserialized
                            continue;
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
        private static object GetStatisticDeserializationIndex(int index, bool isArray) => isArray ? index : index.ToStringInvariant();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetStatisticIndex(string statistic, out int index)
        {
            for (var i = 0; i < StatisticsIndices.Length; i++)
            {
                if (StatisticsIndices[i].Key == statistic)
                {
                    index = StatisticsIndices[i].Value;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        private static KeyValuePair<string, int>[] StatisticsIndices =
        [
            KeyValuePair.Create(PerformanceMetrics.Alpha, 0),
            KeyValuePair.Create(PerformanceMetrics.AnnualStandardDeviation, 1),
            KeyValuePair.Create(PerformanceMetrics.AnnualVariance, 2),
            KeyValuePair.Create(PerformanceMetrics.AverageLoss, 3),
            KeyValuePair.Create(PerformanceMetrics.AverageWin, 4),
            KeyValuePair.Create(PerformanceMetrics.Beta, 5),
            KeyValuePair.Create(PerformanceMetrics.CompoundingAnnualReturn, 6),
            KeyValuePair.Create(PerformanceMetrics.Drawdown, 7),
            KeyValuePair.Create(PerformanceMetrics.EstimatedStrategyCapacity, 8),
            KeyValuePair.Create(PerformanceMetrics.Expectancy, 9),
            KeyValuePair.Create(PerformanceMetrics.InformationRatio, 10),
            KeyValuePair.Create(PerformanceMetrics.LossRate, 11),
            KeyValuePair.Create(PerformanceMetrics.NetProfit, 12),
            KeyValuePair.Create(PerformanceMetrics.ProbabilisticSharpeRatio, 13),
            KeyValuePair.Create(PerformanceMetrics.ProfitLossRatio, 14),
            KeyValuePair.Create(PerformanceMetrics.SharpeRatio, 15),
            // TODO: Add SortinoRatio
            // TODO: Add StartingEquity
            // TODO: Add EndingEquity
            // TODO: Add DrawdownRecovery
            KeyValuePair.Create(PerformanceMetrics.TotalFees, 16),
            KeyValuePair.Create(PerformanceMetrics.TotalOrders, 17),
            KeyValuePair.Create(PerformanceMetrics.TrackingError, 18),
            KeyValuePair.Create(PerformanceMetrics.TreynorRatio, 19),
            KeyValuePair.Create(PerformanceMetrics.WinRate, 20),
        ];
    }
}
