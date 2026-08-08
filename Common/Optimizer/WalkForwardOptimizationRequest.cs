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
using QuantConnect.Interfaces;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Defines a scheduled walk-forward optimization request.
    /// </summary>
    public class WalkForwardOptimizationRequest
    {
        /// <summary>
        /// The parent algorithm requesting the optimization.
        /// </summary>
        public IAlgorithm Algorithm { get; }

        /// <summary>
        /// The UTC time that triggered the scheduled optimization.
        /// </summary>
        public DateTime TriggerTimeUtc { get; }

        /// <summary>
        /// The optimization target used by provider-selected optimizations.
        /// </summary>
        public Target Target { get; }

        /// <summary>
        /// The optional selector used to choose a winning parameter set from optimization backtests.
        /// </summary>
        public Func<IReadOnlyList<OptimizationBacktest>, ParameterSet> TargetSelector { get; }

        /// <summary>
        /// The optimization parameters to evaluate.
        /// </summary>
        public HashSet<OptimizationParameter> Parameters { get; }

        /// <summary>
        /// The constraints to apply to optimization results.
        /// </summary>
        public IReadOnlyList<Constraint> Constraints { get; }

        /// <summary>
        /// Creates a new instance for provider-selected target optimization.
        /// </summary>
        public WalkForwardOptimizationRequest(
            IAlgorithm algorithm,
            DateTime triggerTimeUtc,
            Target target,
            HashSet<OptimizationParameter> parameters,
            IReadOnlyList<Constraint> constraints = null)
            : this(algorithm, triggerTimeUtc, target, null, parameters, constraints)
        {
        }

        /// <summary>
        /// Creates a new instance for selector-based optimization.
        /// </summary>
        public WalkForwardOptimizationRequest(
            IAlgorithm algorithm,
            DateTime triggerTimeUtc,
            Func<IReadOnlyList<OptimizationBacktest>, ParameterSet> targetSelector,
            HashSet<OptimizationParameter> parameters,
            IReadOnlyList<Constraint> constraints = null)
            : this(algorithm, triggerTimeUtc, null, targetSelector, parameters, constraints)
        {
        }

        private WalkForwardOptimizationRequest(
            IAlgorithm algorithm,
            DateTime triggerTimeUtc,
            Target target,
            Func<IReadOnlyList<OptimizationBacktest>, ParameterSet> targetSelector,
            HashSet<OptimizationParameter> parameters,
            IReadOnlyList<Constraint> constraints)
        {
            Algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
            TriggerTimeUtc = triggerTimeUtc;
            Target = target;
            TargetSelector = targetSelector;
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            Constraints = constraints ?? Array.Empty<Constraint>();
        }
    }
}
