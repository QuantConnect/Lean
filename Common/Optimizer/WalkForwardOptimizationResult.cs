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
using QuantConnect.Api;
using QuantConnect.Optimizer.Parameters;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Defines the result of a walk-forward optimization request.
    /// </summary>
    public class WalkForwardOptimizationResult
    {
        /// <summary>
        /// Empty result used when no optimization was run.
        /// </summary>
        public static WalkForwardOptimizationResult Empty { get; } = new WalkForwardOptimizationResult(
            null,
            Array.Empty<OptimizationBacktest>());

        /// <summary>
        /// The selected parameter set.
        /// </summary>
        public ParameterSet ParameterSet { get; }

        /// <summary>
        /// The optimization backtests produced by the provider.
        /// </summary>
        public IReadOnlyList<OptimizationBacktest> Backtests { get; }

        /// <summary>
        /// Creates a new result with the selected parameter set.
        /// </summary>
        public WalkForwardOptimizationResult(ParameterSet parameterSet)
            : this(parameterSet, Array.Empty<OptimizationBacktest>())
        {
        }

        /// <summary>
        /// Creates a new result with candidate backtests for selector-based optimization.
        /// </summary>
        public WalkForwardOptimizationResult(IReadOnlyList<OptimizationBacktest> backtests)
            : this(null, backtests)
        {
        }

        /// <summary>
        /// Creates a new result with a selected parameter set and candidate backtests.
        /// </summary>
        public WalkForwardOptimizationResult(ParameterSet parameterSet, IReadOnlyList<OptimizationBacktest> backtests)
        {
            ParameterSet = parameterSet;
            Backtests = backtests ?? Array.Empty<OptimizationBacktest>();
        }
    }
}
