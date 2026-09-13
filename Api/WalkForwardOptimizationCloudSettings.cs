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
using QuantConnect.Configuration;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Statistics;
using QuantConnect.Util;

namespace QuantConnect.Api
{
    /// <summary>
    /// Configuration used to run walk-forward cloud optimizations.
    /// </summary>
    internal sealed class WalkForwardOptimizationCloudSettings
    {
        private const string DefaultOptimizationStrategy = "QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy";

        /// <summary>
        /// Optimization name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optimization target.
        /// </summary>
        public Target Target { get; set; }

        /// <summary>
        /// Optimization direction used by the API.
        /// </summary>
        public string TargetTo { get; set; }

        /// <summary>
        /// Optimization strategy type name.
        /// </summary>
        public string Strategy { get; set; }

        /// <summary>
        /// Compile id to optimize.
        /// </summary>
        public string CompileId { get; set; }

        /// <summary>
        /// User-confirmed estimated cost.
        /// </summary>
        public decimal EstimatedCost { get; set; }

        /// <summary>
        /// Cloud optimization node type.
        /// </summary>
        public string NodeType { get; set; }

        /// <summary>
        /// Number of parallel nodes.
        /// </summary>
        public int ParallelNodes { get; set; }

        /// <summary>
        /// Poll interval in seconds.
        /// </summary>
        public int PollIntervalSeconds { get; set; }

        /// <summary>
        /// Timeout in minutes.
        /// </summary>
        public int TimeoutMinutes { get; set; }

        /// <summary>
        /// Reads and validates cloud optimization settings for a request.
        /// </summary>
        public static WalkForwardOptimizationCloudSettings Read(WalkForwardOptimizationRequest request)
        {
            return new WalkForwardOptimizationCloudSettings
            {
                Name = Config.Get("walk-forward-optimization-name", $"Walk Forward Optimization {request.TriggerTimeUtc:O}"),
                Target = request.Target ?? new Target(Config.Get("walk-forward-optimization-selector-target", PerformanceMetrics.NetProfit), new Maximization(), null),
                TargetTo = request.Target?.Extremum is Minimization ? "min" : "max",
                Strategy = Config.Get("walk-forward-optimization-strategy", DefaultOptimizationStrategy),
                CompileId = GetRequiredConfig("walk-forward-optimization-compile-id"),
                EstimatedCost = GetRequiredConfig("walk-forward-optimization-estimated-cost").ToDecimal(),
                NodeType = GetRequiredConfig("walk-forward-optimization-node-type"),
                ParallelNodes = GetRequiredConfig("walk-forward-optimization-parallel-nodes").ToInt32(),
                PollIntervalSeconds = Math.Max(0, Config.GetInt("walk-forward-optimization-api-poll-interval-seconds", 10)),
                TimeoutMinutes = Config.GetInt("walk-forward-optimization-api-timeout-minutes", 60)
            };
        }

        private static string GetRequiredConfig(string key)
        {
            var value = Config.Get(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{key} must be configured before running walk-forward cloud optimization.");
            }
            return value;
        }
    }
}
