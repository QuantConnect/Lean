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
using QuantConnect.Logging;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;

namespace QuantConnect.Optimizer.Strategies
{
    /// <summary>
    /// Advanced brute-force strategy with search in-depth for best solution on previous step
    /// </summary>
    public class EulerSearchOptimizationStrategy : StepBaseOptimizationStrategy
    {
        private object _locker = new object();
        private readonly HashSet<ParameterSet> _runningParameterSet = new HashSet<ParameterSet>();
        private int _segmentsAmount = 4;

        public override void Initialize(Target target, IReadOnlyList<Constraint> constraints, HashSet<OptimizationParameter> parameters, OptimizationStrategySettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(
                    "EulerSearchOptimizationStrategy.Initialize: Optimizations Strategy settings are required for this strategy");
            }
            _segmentsAmount = settings.DefaultSegmentAmount;
            base.Initialize(target, constraints, parameters, settings);
        }

        /// <summary>
        /// Checks whether new lean compute job better than previous and run new iteration if necessary.
        /// </summary>
        /// <param name="result">Lean compute job result and corresponding parameter set</param>
        public override void PushNewResults(OptimizationResult result)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException($"EulerSearchOptimizationStrategy.PushNewResults: strategy has not been initialized yet.");
            }

            lock (_locker)
            {
                if (!ReferenceEquals(result, OptimizationResult.Initial) && string.IsNullOrEmpty(result?.JsonBacktestResult))
                {
                    // one of the requested backtests failed
                    _runningParameterSet.Remove(result.ParameterSet);
                    return;
                }

                // check if the incoming result is not the initial seed
                if (result.Id > 0)
                {
                    _runningParameterSet.Remove(result.ParameterSet);
                    ProcessNewResult(result);
                }

                if (_runningParameterSet.Count > 0)
                {
                    return;
                }

                if (Target.Current.HasValue && OptimizationParameters.OfType<OptimizationStepParameter>().Any(s => s.Step > s.MinStep))
                {
                    var boundaries = new HashSet<OptimizationParameter>();
                    var parameterSet = Solution.ParameterSet;
                    foreach (var optimizationParameter in OptimizationParameters)
                    {
                        var optimizationStepParameter = optimizationParameter as OptimizationStepParameter;
                        if (optimizationStepParameter != null && optimizationStepParameter.Step > optimizationStepParameter.MinStep)
                        {
                            var newStep = Math.Max(optimizationStepParameter.MinStep.Value, optimizationStepParameter.Step.Value / _segmentsAmount);
                            var fractal = newStep * ((decimal)_segmentsAmount/ 2);
                            var parameter = parameterSet.Value.First(s => s.Key == optimizationParameter.Name);
                            boundaries.Add(new OptimizationStepParameter(
                                optimizationParameter.Name,
                                Math.Max(optimizationStepParameter.MinValue, parameter.Value.ToDecimal() - fractal),
                                Math.Min(optimizationStepParameter.MaxValue, parameter.Value.ToDecimal() + fractal),
                                newStep,
                                optimizationStepParameter.MinStep.Value));
                        }
                        else
                        {
                            boundaries.Add(optimizationParameter);
                        }
                    }

                    OptimizationParameters = boundaries;
                }
                else if (!ReferenceEquals(result, OptimizationResult.Initial))
                {
                    return;
                }

                foreach (var parameterSet in Step(result.ParameterSet, OptimizationParameters))
                {
                    OnNewParameterSet(parameterSet);
                }
            }
        }

        /// <summary>
        /// Handles new parameter set
        /// </summary>
        /// <param name="parameterSet">New parameter set</param>
        protected override void OnNewParameterSet(ParameterSet parameterSet)
        {
            _runningParameterSet.Add(parameterSet);
            base.OnNewParameterSet(parameterSet);
        }
    }
}
