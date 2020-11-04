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
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;

namespace QuantConnect.Optimizer.Strategies
{
    /// <summary>
    /// Base class for any optimization built on top of brute force optimization method
    /// </summary>
    public abstract class StepBaseOptimizationStrategy : IOptimizationStrategy
    {
        private int _i;

        /// <summary>
        /// Indicates was strategy initialized or no
        /// </summary>
        protected bool Initialized = false;

        /// <summary>
        /// Optimization parameters
        /// </summary>
        protected HashSet<OptimizationParameter> OptimizationParameters;

        // Optimization target, i.e. maximize or minimize
        protected Target Target;

        // Optimization constraints; if it doesn't comply just drop the backtest
        protected IEnumerable<Constraint> Constraints;

        /// <summary>
        /// Keep the best found solution - lean computed job result and corresponding  parameter set 
        /// </summary>
        public OptimizationResult Solution { get; protected set; }

        /// <summary>
        /// Advanced strategy settings
        /// </summary>
        public OptimizationStrategySettings Settings { get; protected set; }

        /// <summary>
        /// Fires when new parameter set is generated
        /// </summary>
        public event EventHandler NewParameterSet;

        /// <summary>
        /// Initializes the strategy using generator, extremum settings and optimization parameters
        /// </summary>
        /// <param name="target">The optimization target</param>
        /// <param name="constraints">The optimization constraints to apply on backtest results</param>
        /// <param name="parameters">Optimization parameters</param>
        /// <param name="settings">Optimization strategy settings</param>
        public virtual void Initialize(Target target, IReadOnlyList<Constraint> constraints, HashSet<OptimizationParameter> parameters, OptimizationStrategySettings settings)
        {
            if (Initialized)
            {
                throw new InvalidOperationException($"GridSearchOptimizationStrategy.Initialize: can not be re-initialized.");
            }

            Target = target;
            Constraints = constraints;
            OptimizationParameters = parameters;
            Settings = settings;

            foreach (var optimizationParameter in OptimizationParameters.OfType<OptimizationStepParameter>())
            {
                if (!optimizationParameter.Step.HasValue)
                {
                    optimizationParameter.CalculateStep(Settings.DefaultSegmentAmount);
                }
            }

            Initialized = true;
        }

        /// <summary>
        /// Checks whether new lean compute job better than previous and run new iteration if necessary.
        /// </summary>
        /// <param name="result">Lean compute job result and corresponding parameter set</param>
        public abstract void PushNewResults(OptimizationResult result);

        /// <summary>
        /// Calculate number of parameter sets within grid
        /// </summary>
        /// <returns>Number of parameter sets for given optimization parameters</returns>
        public int GetTotalBacktestEstimate()
        {
            int total = 1;
            foreach (var arg in OptimizationParameters)
            {
                total *= arg.Estimate();
            }

            return total;
        }

        /// <summary>
        /// Handles new parameter set
        /// </summary>
        /// <param name="parameterSet">New parameter set</param>
        protected virtual void OnNewParameterSet(ParameterSet parameterSet)
        {
            NewParameterSet?.Invoke(this, new OptimizationEventArgs(parameterSet));
        }

        protected virtual void ProcessNewResult(OptimizationResult result)
        {
            // check if the incoming result is not the initial seed
            if (result.Id > 0)
            {
                if (Constraints?.All(constraint => constraint.IsMet(result.JsonBacktestResult)) != false)
                {
                    if (Target.MoveAhead(result.JsonBacktestResult))
                    {
                        Solution = result;
                        Target.CheckCompliance();
                    }
                }
            }
        }

        /// <summary>
        /// Enumerate all possible arrangements
        /// </summary>
        /// <param name="seed">Seeding</param>
        /// <param name="args"></param>
        /// <returns>Collection of possible combinations for given optimization parameters settings</returns>
        protected IEnumerable<ParameterSet> Step(ParameterSet seed, HashSet<OptimizationParameter> args)
        {
            foreach (var step in Recursive(seed, new Queue<OptimizationParameter>(args)))
            {
                yield return new ParameterSet(
                    ++_i,
                    step.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }
        }

        private IEnumerable<Dictionary<string, string>> Recursive(ParameterSet seed, Queue<OptimizationParameter> args)
        {
            if (args.Count == 1)
            {
                var optimizationParameterLast = args.Dequeue();
                foreach (var value in optimizationParameterLast)
                {
                    yield return new Dictionary<string, string>()
                    {
                        {optimizationParameterLast.Name, value}
                    };
                }
                yield break;
            }

            var optimizationParameter = args.Dequeue();
            foreach (var value in optimizationParameter)
            {
                foreach (var inner in Recursive(seed, new Queue<OptimizationParameter>(args)))
                {
                    inner.Add(optimizationParameter.Name, value);

                    yield return inner;
                }
            }
        }

    }
}
