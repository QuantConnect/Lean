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

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Find the best solution in first generation
    /// </summary>
    public class GridSearchOptimizationStrategy : IOptimizationStrategy
    {
        private HashSet<OptimizationParameter> _args;
        private object _locker = new object();
        private bool _initialized = false;

        /// <summary>
        /// Global parameter step Id
        /// </summary>
        private int _i;

        // Optimization target, i.e. maximize or minimize
        private Target _target;

        // Optimization constraints; if it doesn't comply just drop the backtest
        private IEnumerable<Constraint> _constraints;

        /// <summary>
        /// Keep the best found solution - lean computed job result and corresponding  parameter set 
        /// </summary>
        public OptimizationResult Solution { get; private set; }

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
        public void Initialize(Target target, IReadOnlyList<Constraint> constraints, HashSet<OptimizationParameter> parameters)
        {
            if (_initialized)
            {
                throw new InvalidOperationException($"GridSearchOptimizationStrategy.Initialize: can not be re-initialized.");
            }

            _target = target;
            _constraints = constraints;
            _args = parameters;
            _initialized = true;
        }

        /// <summary>
        /// Checks whether new lean compute job better than previous and run new iteration if necessary.
        /// </summary>
        /// <param name="result">Lean compute job result and corresponding parameter set</param>
        public void PushNewResults(OptimizationResult result)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException($"GridSearchOptimizationStrategy.PushNewResults: strategy has not been initialized yet.");
            }

            lock (_locker)
            {
                if (!ReferenceEquals(result, OptimizationResult.Empty) && string.IsNullOrEmpty(result?.JsonBacktestResult))
                {
                    // one of the requested backtests failed
                    return;
                }

                // check if the incoming result is not the initial seed
                if (result.Id > 0)
                {
                    if (_constraints?.All(constraint => constraint.IsMet(result.JsonBacktestResult)) != false)
                    {
                        if (_target.MoveAhead(result.JsonBacktestResult))
                        {
                            Solution = result;
                            _target.CheckComplience();
                        }
                    }

                    return;
                }

                foreach (var parameterSet in Step(result.ParameterSet, _args))
                {
                    NewParameterSet?.Invoke(this, new OptimizationEventArgs(parameterSet));
                }
            }
        }

        /// <summary>
        /// Calculate number of parameter sets within grid
        /// </summary>
        /// <returns>Number of parameter sets for given optimization parameters</returns>
        public int GetTotalBacktestEstimate()
        {
            int total = 1;
            foreach (var arg in _args)
            {
                total *= (int)Math.Floor((arg.MaxValue - arg.MinValue) / arg.Step) + 1;
            }

            return total;
        }

        /// <summary>
        /// Enumerate all possible arrangements
        /// </summary>
        /// <param name="seed">Seeding</param>
        /// <param name="args"></param>
        /// <returns>Collection of possible combinations for given optimization parameters settings</returns>
        private IEnumerable<ParameterSet> Step(ParameterSet seed, HashSet<OptimizationParameter> args)
        {
            foreach (var step in Recursive(seed, new Queue<OptimizationParameter>(args)))
            {
                yield return new ParameterSet(
                    ++_i,
                    step.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToStringInvariant()));
            }
        }

        private IEnumerable<Dictionary<string, decimal>> Recursive(ParameterSet seed, Queue<OptimizationParameter> args)
        {
            if (args.Count == 1)
            {
                var d = args.Dequeue();
                for (var value = d.MinValue; value <= d.MaxValue; value += d.Step)
                {
                    yield return new Dictionary<string, decimal>()
                    {
                        {d.Name, value}
                    };
                }
                yield break;
            }

            var d2 = args.Dequeue();
            for (var value = d2.MinValue; value <= d2.MaxValue; value += d2.Step)
            {
                foreach (var inner in Recursive(seed, new Queue<OptimizationParameter>(args)))
                {
                    inner.Add(d2.Name, value);

                    yield return inner;
                }
            }
        }

    }
}
