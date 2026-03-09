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

namespace QuantConnect.Optimizer.Strategies
{
    /// <summary>
    /// Find the best solution in first generation
    /// </summary>
    public class GridSearchOptimizationStrategy : StepBaseOptimizationStrategy
    {
        private object _locker = new object();

        /// <summary>
        /// Checks whether new lean compute job better than previous and run new iteration if necessary.
        /// </summary>
        /// <param name="result">Lean compute job result and corresponding parameter set</param>
        public override void PushNewResults(OptimizationResult result)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException($"GridSearchOptimizationStrategy.PushNewResults: strategy has not been initialized yet.");
            }

            lock (_locker)
            {
                if (!ReferenceEquals(result, OptimizationResult.Initial) && string.IsNullOrEmpty(result?.JsonBacktestResult))
                {
                    // one of the requested backtests failed
                    return;
                }

                // check if the incoming result is not the initial seed
                if (result.Id > 0)
                {
                    ProcessNewResult(result);
                    return;
                }

                foreach (var parameterSet in Step(OptimizationParameters))
                {
                    OnNewParameterSet(parameterSet);
                }
            }
        }
    }
}
