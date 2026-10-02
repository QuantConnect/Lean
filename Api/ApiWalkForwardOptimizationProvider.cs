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
using System.Threading;
using QuantConnect.Interfaces;
using QuantConnect.Optimizer;
using QuantConnect.Util;

namespace QuantConnect.Api
{
    /// <summary>
    /// Runs scheduled walk-forward optimization using the QuantConnect cloud API optimization endpoints.
    /// </summary>
    public class ApiWalkForwardOptimizationProvider : IWalkForwardOptimizationProvider
    {
        private readonly IApi _api;

        /// <summary>
        /// Creates a new instance using the configured API client.
        /// </summary>
        public ApiWalkForwardOptimizationProvider()
        {
        }

        /// <summary>
        /// Creates a new instance using the specified API client.
        /// </summary>
        /// <param name="api">The API client</param>
        public ApiWalkForwardOptimizationProvider(IApi api)
        {
            _api = api;
        }

        /// <summary>
        /// Runs a cloud optimization for the specified walk-forward request.
        /// </summary>
        /// <param name="request">The walk-forward optimization request</param>
        /// <returns>The walk-forward optimization result</returns>
        public WalkForwardOptimizationResult Optimize(WalkForwardOptimizationRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.Algorithm.ProjectId <= 0)
            {
                throw new InvalidOperationException("Walk-forward cloud optimization requires a configured project id.");
            }

            var settings = WalkForwardOptimizationCloudSettings.Read(request);
            var optimization = CreateAndWaitForOptimization(request, settings);
            var backtests = optimization.Backtests?.Values.ToList() ?? new List<OptimizationBacktest>();
            var parameterSet = request.TargetSelector == null
                ? WalkForwardOptimizationBacktestSelector.SelectParameterSet(settings.Target, backtests)
                : null;

            return new WalkForwardOptimizationResult(parameterSet, backtests);
        }

        /// <summary>
        /// Gets the API client used to submit and poll cloud optimizations.
        /// </summary>
        protected virtual IApi ApiClient => _api ?? Composer.Instance.GetPart<IApi>();

        private Optimization CreateAndWaitForOptimization(WalkForwardOptimizationRequest request, WalkForwardOptimizationCloudSettings settings)
        {
            var summary = ApiClient.CreateOptimization(
                request.Algorithm.ProjectId,
                settings.Name,
                settings.Target.Target,
                settings.TargetTo,
                settings.Target.TargetValue,
                settings.Strategy,
                settings.CompileId,
                request.Parameters,
                request.Constraints,
                settings.EstimatedCost,
                settings.NodeType,
                settings.ParallelNodes);

            if (string.IsNullOrWhiteSpace(summary?.OptimizationId))
            {
                throw new InvalidOperationException("Walk-forward cloud optimization did not return an optimization id.");
            }

            var timeoutUtc = DateTime.UtcNow.AddMinutes(settings.TimeoutMinutes);
            while (true)
            {
                var optimization = ApiClient.ReadOptimization(summary.OptimizationId);
                if (optimization?.Status == OptimizationStatus.Completed)
                {
                    return optimization;
                }
                if (optimization?.Status == OptimizationStatus.Aborted)
                {
                    throw new InvalidOperationException($"Walk-forward cloud optimization '{summary.OptimizationId}' was aborted.");
                }
                if (settings.TimeoutMinutes >= 0 && DateTime.UtcNow >= timeoutUtc)
                {
                    throw new TimeoutException($"Walk-forward cloud optimization '{summary.OptimizationId}' timed out after {settings.TimeoutMinutes} minutes.");
                }
                if (settings.PollIntervalSeconds > 0)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(settings.PollIntervalSeconds));
                }
            }
        }
    }
}
