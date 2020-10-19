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

namespace QuantConnect.Optimizer
{
    public class EulerSearchOptimizationStrategy : IOptimizationStrategy
    {
        public Extremum Extremum => throw new NotImplementedException();

        public OptimizationResult Solution => throw new NotImplementedException();

        public event EventHandler NewParameterSet;

        public void Initialize(Extremum extremum, HashSet<OptimizationParameter> parameters)
        {
            throw new NotImplementedException();
        }

        public void PushNewResults(OptimizationResult result)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ParameterSet> Step(ParameterSet seed, HashSet<OptimizationParameter> args)
        {
            throw new System.NotImplementedException("EulerSearch isn't implemented yet");
        }
    }
}
