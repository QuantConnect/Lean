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
using QuantConnect.Util;

namespace QuantConnect.Optimizer
{
    public class GridSearch : IOptimizationStrategy
    {
        public IEnumerable<ParameterSet> Step(ParameterSet seed, HashSet<OptimizationParameter> args)
        {
            foreach (var step in Recursive(seed?.Arguments, args))
            {
                yield return new ParameterSet(step);
            }
        }

        public IEnumerable<Dictionary<string, decimal>> Recursive(IEnumerable<KeyValuePair<string, decimal>> seed, HashSet<OptimizationParameter> args)
        {
            if (args.Count == 1)
            {
                var d = args.First();
                for (var value = d.MinValue; value <= d.MaxValue; value += d.Step)
                {
                    yield return new Dictionary<string, decimal>()
                    {
                        {d.Name, value}
                    };
                }
                yield break;
            }

            var d2 = args.First();
            for (var value = d2.MinValue; value <= d2.MaxValue; value += d2.Step)
            {
                foreach (var inner in Recursive(seed, args.Where(s => s.Name != d2.Name).ToHashSet()))
                {
                    inner.Add(d2.Name, value);

                    yield return inner;
                }
            }
        }
    }
}
