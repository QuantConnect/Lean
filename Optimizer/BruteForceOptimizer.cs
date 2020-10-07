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
        private HashSet<OptimizationParameter> _args;
        public IOptimizationStrategy SearchStrategy { get; private set; }
        public Extremum Extremum { get; private set; }
        public event EventHandler NewSuggestion;
        public OptimizationResult Solution { get; private set; }

        public void Initialize(IOptimizationStrategy searchStrategy, Extremum extremum, HashSet<OptimizationParameter> parameters)
        {
            SearchStrategy = searchStrategy;
            Extremum = extremum;
            _args = parameters;
        }

        public void PushNewResults(OptimizationResult result)
        {
            if (result != null)
            {
                if (Solution == null || Extremum.Better(Solution.Profit, result.Profit))
                {
                    Solution = result;
                }

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
