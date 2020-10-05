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
        public IEnumerable<ParameterSet> Step(ParameterSet seed, IReadOnlyDictionary<string, OptimizationParameter> args)
        {
            foreach (var step in Recursive(seed?.Arguments, args))
            {
                yield return new ParameterSet(step);
            }
        }

        public IEnumerable<Dictionary<string, decimal>> Recursive(IEnumerable<KeyValuePair<string, decimal>> seed, IReadOnlyDictionary<string, OptimizationParameter> args)
        {
            if (args.Count == 1)
            {
                var d = args.First();
                for (var value = d.Value.MinValue; value <= d.Value.MaxValue; value += d.Value.Step)
                {
                    yield return new Dictionary<string, decimal>()
                    {
                        {d.Key, value}
                    };
                }
                yield break;
            }

            var d2 = args.First();
            for (var value = d2.Value.MinValue; value <= d2.Value.MaxValue; value += d2.Value.Step)
            {
                foreach (var inner in Recursive(seed, args.Where(s => s.Key != d2.Key).ToReadOnlyDictionary()))
                {
                    inner.Add(d2.Key, value);

                    yield return inner;
                }
            }
        }
    }
}
