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
    public class BruteForceOptimizer : IOptimizationManager
    {
        private IEnumerable<OptimizationParameter> _args;
        public IOptimizationStrategy SearchStrategy { get; }

        public BruteForceOptimizer(IOptimizationStrategy searchStrategy)
        {
            SearchStrategy = searchStrategy;
        }

        public event EventHandler NewSuggestion;

        public void Initialize()
        {
            _args = new List<OptimizationParameter>
            {
                new OptimizationParameter() {Name = "ema-fast", MinValue = 10, MaxValue = 100, Step = 1},
                new OptimizationParameter() {Name = "ema-slow", MinValue = 20, MaxValue = 200, Step = 1}
            };
        }

        public void PushNewResults(OptimizationResult result)
        {
            if (result != null)
            {
                Console.WriteLine($"{string.Join(";", result.ParameterSet.Arguments.Select(s => $"{s.Key}={s.Value}"))}: {result.Profit}");
                return;
            }

            foreach (var suggestion in SearchStrategy.Step(null, _args))
            {
                NewSuggestion?.Invoke(this, new OptimizationEventArgs(suggestion));
            }

            NewSuggestion?.Invoke(this, null);
        }
    }
}
