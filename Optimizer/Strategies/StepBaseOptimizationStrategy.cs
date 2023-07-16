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

        /// <summary>
        /// Optimization target, i.e. maximize or minimize
        /// </summary>
        protected Target Target;

        /// <summary>
        /// Optimization constraints; if it doesn't comply just drop the backtest
        /// </summary>
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
        public event EventHandler<ParameterSet> NewParameterSet;

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
                // if the Step optimization parameter does not provide a step to use, we calculate one based on settings
                if (!optimizationParameter.Step.HasValue)
                {
                    var stepSettings = Settings as StepBaseOptimizationStrategySettings;
                    if (stepSettings == null)
                    {
                        throw new ArgumentException($"OptimizationStrategySettings is not of {nameof(StepBaseOptimizationStrategySettings)} type", nameof(settings));
                    }
                    CalculateStep(optimizationParameter, stepSettings.DefaultSegmentAmount);
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
            var total = 1;
            foreach (var arg in OptimizationParameters)
            {
                total *= Estimate(arg);
            }

            return total;
        }

        /// <summary>
        /// Calculates number od data points for step based optimization parameter based on min/max and step values
        /// </summary>
        private int Estimate(OptimizationParameter parameter)
        {
            if (parameter is StaticOptimizationParameter)
            {
                return 1;
            }

            var stepParameter = parameter as OptimizationStepParameter;
            if (stepParameter == null)
            {
                throw new InvalidOperationException($"Cannot estimate parameter of type {parameter.GetType().FullName}");
            }

            if (!stepParameter.Step.HasValue)
            {
                throw new InvalidOperationException("Optimization parameter cannot be estimated due to step value is not initialized");
            }

            return (int)Math.Floor((stepParameter.MaxValue - stepParameter.MinValue) / stepParameter.Step.Value) + 1;
        }

        /// <summary>
        /// Handles new parameter set
        /// </summary>
        /// <param name="parameterSet">New parameter set</param>
        protected virtual void OnNewParameterSet(ParameterSet parameterSet)
        {
            NewParameterSet?.Invoke(this, parameterSet);
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
        /// <param name="args"></param>
        /// <returns>Collection of possible combinations for given optimization parameters settings</returns>
        protected IEnumerable<ParameterSet> Step(HashSet<OptimizationParameter> args)
        {
            foreach (var step in Recursive(new Queue<OptimizationParameter>(args)))
            {
                yield return new ParameterSet(
                    ++_i,
                    step.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }
        }

        /// <summary>
        /// Calculate step and min step values based on default number of fragments
        /// </summary>
        private void CalculateStep(OptimizationStepParameter parameter, int defaultSegmentAmount)
        {
            if (defaultSegmentAmount < 1)
            {
                throw new ArgumentException($"Number of segments should be positive number, but specified '{defaultSegmentAmount}'", nameof(defaultSegmentAmount));
            }

            parameter.Step = Math.Abs(parameter.MaxValue - parameter.MinValue) / defaultSegmentAmount;
            parameter.MinStep = parameter.Step / 10;
        }

        private IEnumerable<Dictionary<string, string>> Recursive(Queue<OptimizationParameter> args)
        {
            if (args.Count == 1)
            {
                var optimizationParameterLast = args.Dequeue();
                using (var optimizationParameterLastEnumerator = GetEnumerator(optimizationParameterLast))
                {
                    while (optimizationParameterLastEnumerator.MoveNext())
                    {
                        yield return new Dictionary<string, string>()
                        {
                            {optimizationParameterLast.Name, optimizationParameterLastEnumerator.Current}
                        };
                    }
                }

                yield break;
            }

            var optimizationParameter = args.Dequeue();
            using (var optimizationParameterEnumerator = GetEnumerator(optimizationParameter))
            {
                while (optimizationParameterEnumerator.MoveNext())
                {
                    foreach (var inner in Recursive(new Queue<OptimizationParameter>(args)))
                    {
                        inner.Add(optimizationParameter.Name, optimizationParameterEnumerator.Current);

                        yield return inner;
                    }
                }
            }
        }

        private IEnumerator<string> GetEnumerator(OptimizationParameter parameter)
        {
            var staticOptimizationParameter = parameter as StaticOptimizationParameter;
            if (staticOptimizationParameter != null)
            {
                return new List<string> { staticOptimizationParameter.Value }.GetEnumerator();
            }

            var stepParameter = parameter as OptimizationStepParameter;
            if (stepParameter == null)
            {
                throw new InvalidOperationException("");
            }

            return new OptimizationStepParameterEnumerator(stepParameter);
        }
    }
}
