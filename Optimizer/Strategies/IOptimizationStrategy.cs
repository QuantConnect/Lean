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
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;

namespace QuantConnect.Optimizer.Strategies
{
    /// <summary>
    /// Defines the optimization settings, direction, solution and exit, i.e. optimization strategy
    /// </summary>
    public interface IOptimizationStrategy
    {
        /// <summary>
        /// Fires when new parameter set is retrieved
        /// </summary>
        event EventHandler<ParameterSet> NewParameterSet;

        /// <summary>
        /// Best found solution, its value and parameter set
        /// </summary>
        OptimizationResult Solution { get; }

        /// <summary>
        /// Initializes the strategy using generator, extremum settings and optimization parameters
        /// </summary>
        /// <param name="target">The optimization target</param>
        /// <param name="constraints">The optimization constraints to apply on backtest results</param>
        /// <param name="parameters">optimization parameters</param>
        /// <param name="settings">optimization strategy advanced settings</param>
        void Initialize(Target target, IReadOnlyList<Constraint> constraints, HashSet<OptimizationParameter> parameters, OptimizationStrategySettings settings);
        
        /// <summary>
        /// Callback when lean compute job completed.
        /// </summary>
        /// <param name="result">Lean compute job result and corresponding parameter set</param>
        void PushNewResults(OptimizationResult result);

        /// <summary>
        /// Estimates amount of parameter sets that can be run
        /// </summary>
        int GetTotalBacktestEstimate();
    }
}
