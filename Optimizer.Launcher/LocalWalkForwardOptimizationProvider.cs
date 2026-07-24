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
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Strategies;
using QuantConnect.Statistics;
using QuantConnect.Util;

namespace QuantConnect.Optimizer.Launcher
{
    /// <summary>
    /// Runs scheduled walk-forward optimization using the local LEAN optimizer launcher.
    /// </summary>
    public class LocalWalkForwardOptimizationProvider : IWalkForwardOptimizationProvider
    {
        /// <summary>
        /// Runs a local optimization for the specified walk-forward request.
        /// </summary>
        /// <param name="request">The walk-forward optimization request</param>
        /// <returns>The walk-forward optimization result</returns>
        public WalkForwardOptimizationResult Optimize(WalkForwardOptimizationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using var endedEvent = new ManualResetEvent(false);
            using var optimizer = CreateOptimizer(CreatePacket(request));

            OptimizationResult result = null;
            optimizer.Ended += (sender, optimizationResult) =>
            {
                result = optimizationResult;
                endedEvent.Set();
            };

            optimizer.Start();

            var timeoutMinutes = Config.GetInt("walk-forward-optimization-timeout-minutes");
            if (timeoutMinutes > 0)
            {
                if (!endedEvent.WaitOne(TimeSpan.FromMinutes(timeoutMinutes)))
                {
                    throw new TimeoutException($"Walk-forward optimization timed out after {timeoutMinutes} minutes.");
                }
            }
            else
            {
                endedEvent.WaitOne();
            }

            var backtests = result?.Backtests ?? optimizer.CompletedOptimizationBacktests;
            var parameterSet = request.TargetSelector == null ? result?.ParameterSet : null;
            return new WalkForwardOptimizationResult(parameterSet, backtests);
        }

        /// <summary>
        /// Creates the optimizer instance for the optimization packet.
        /// </summary>
        /// <param name="packet">The optimization packet</param>
        /// <returns>The optimizer instance</returns>
        protected virtual LeanOptimizer CreateOptimizer(OptimizationNodePacket packet)
        {
            var optimizerType = Config.Get(
                "walk-forward-optimization-launcher",
                Config.Get("optimization-launcher", nameof(ConsoleLeanOptimizer)));

            return (LeanOptimizer)Activator.CreateInstance(Composer.Instance
                .GetExportedTypes<LeanOptimizer>()
                .Single(type => type.Name == optimizerType || type.FullName == optimizerType), packet);
        }

        private static OptimizationNodePacket CreatePacket(WalkForwardOptimizationRequest request)
        {
            return new OptimizationNodePacket
            {
                Name = Config.Get("walk-forward-optimization-name", $"Walk Forward Optimization {request.TriggerTimeUtc:O}"),
                Created = request.TriggerTimeUtc,
                ProjectId = request.Algorithm.ProjectId,
                OptimizationId = Config.Get("walk-forward-optimization-id", Guid.NewGuid().ToString()),
                OptimizationStrategy = Config.Get(
                    "walk-forward-optimization-strategy",
                    Config.Get("optimization-strategy", typeof(GridSearchOptimizationStrategy).FullName)),
                OptimizationStrategySettings = GetStrategySettings(),
                Criterion = request.Target ?? GetSelectorTarget(),
                Constraints = request.Constraints,
                OptimizationParameters = request.Parameters,
                MaximumConcurrentBacktests = Config.GetInt(
                    "walk-forward-optimization-maximum-concurrent-backtests",
                    Config.GetInt("maximum-concurrent-backtests", Math.Max(1, Environment.ProcessorCount / 2))),
                Channel = Config.Get("data-channel")
            };
        }

        private static OptimizationStrategySettings GetStrategySettings()
        {
            var settings = Config.Get(
                "walk-forward-optimization-strategy-settings",
                Config.Get("optimization-strategy-settings",
                    "{\"$type\":\"QuantConnect.Optimizer.Strategies.StepBaseOptimizationStrategySettings, QuantConnect.Optimizer\"}"));
            return (OptimizationStrategySettings)JsonConvert.DeserializeObject(
                settings,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
        }

        private static Target GetSelectorTarget()
        {
            return new Target(
                Config.Get("walk-forward-optimization-selector-target", PerformanceMetrics.NetProfit),
                new Maximization(),
                null);
        }
    }
}
