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
        private const decimal SmallestPositiveStep = 0.0000000000000000000000000001m;

        private int _i;

        /// <summary>
        /// Indicates was strategy initialized or no
        /// </summary>
        protected bool Initialized { get; set; }

        /// <summary>
        /// Optimization parameters
        /// </summary>
        protected HashSet<OptimizationParameter> OptimizationParameters { get; set; }

        /// <summary>
        /// Optimization target, i.e. maximize or minimize
        /// </summary>
        protected Target Target { get; set; }

        /// <summary>
        /// Optimization constraints; if it doesn't comply just drop the backtest
        /// </summary>
        protected IEnumerable<Constraint> Constraints { get; set; }

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
            // both factors are at most int.MaxValue so their product fits in a long, the total is capped since it can't fit in an int
            long total = 1;
            foreach (var arg in OptimizationParameters)
            {
                total = Math.Min(total * Estimate(arg), int.MaxValue);
            }

            return (int)total;
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

            // the step can be set to zero after initialization, the grid would never be exhausted
            if (stepParameter.Step.Value <= 0)
            {
                throw new InvalidOperationException($"Optimization parameter cannot be estimated due to step value is not positive, but was {stepParameter.Step.Value}");
            }

            // dividing the range by the step can round across a grid boundary, so search for the last index meeting the
            // enumerator's own 'MinValue + index * Step <= MaxValue' condition, which is monotonic in the index
            bool IsInGrid(long index)
            {
                try
                {
                    return stepParameter.MinValue + index * stepParameter.Step.Value <= stepParameter.MaxValue;
                }
                catch (OverflowException)
                {
                    // the step is positive, so an overflowing value is above any max value
                    return false;
                }
            }

            long lastInGrid = 0;
            long firstOutOfGrid = 1;
            while (IsInGrid(firstOutOfGrid))
            {
                if (firstOutOfGrid >= int.MaxValue - 1)
                {
                    // the count doesn't fit in an int, the estimate is capped
                    return int.MaxValue;
                }
                lastInGrid = firstOutOfGrid;
                firstOutOfGrid = Math.Min(firstOutOfGrid * 2, int.MaxValue - 1);
            }

            while (firstOutOfGrid - lastInGrid > 1)
            {
                var middle = lastInGrid + (firstOutOfGrid - lastInGrid) / 2;
                if (IsInGrid(middle))
                {
                    lastInGrid = middle;
                }
                else
                {
                    firstOutOfGrid = middle;
                }
            }

            return (int)lastInGrid + 1;
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

            // a zero step never exhausts the grid: any positive step enumerates a single value once, without changing its precision,
            // and a range too narrow for its segments gets the finest step decimal can represent
            if (parameter.MinValue == parameter.MaxValue)
            {
                parameter.Step = parameter.MinStep = 1;
                return;
            }

            parameter.Step = Math.Max(SmallestPositiveStep, Math.Abs(parameter.MaxValue - parameter.MinValue) / defaultSegmentAmount);
            parameter.MinStep = Math.Max(SmallestPositiveStep, parameter.Step.Value / 10);
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
