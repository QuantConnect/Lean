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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Optimizer
{
    public class LeanOptimizer
    {
        private readonly ConcurrentDictionary<string, ParameterSet> _parameterSetForBacktest;

        protected IOptimizationStrategy Optimizer;
        protected OptimizationNodePacket NodePacket;

        public LeanOptimizer(OptimizationNodePacket nodePacket)
        {
            if (nodePacket.OptimizationParameters.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Cannot start an optimization job with no parameter to optimize");
            }

            NodePacket = nodePacket;
            Optimizer = Composer.Instance.GetExportedValueByTypeName<IOptimizationStrategy>(NodePacket.OptimizationStrategy);

            _parameterSetForBacktest = new ConcurrentDictionary<string, ParameterSet>();
            Optimizer.Initialize(
                Composer.Instance.GetExportedValueByTypeName<IOptimizationParameterSetGenerator>(NodePacket.ParameterSetGenerator),
                NodePacket.Criterion["extremum"] == "max"
                    ? new Maximization() as Extremum
                    : new Minimization(),
                NodePacket.OptimizationParameters);

            Optimizer.NewSuggestion += async (s, e) =>
            {
                var paramSet = (e as OptimizationEventArgs)?.ParameterSet;
                if (paramSet == null) return;

                var backtestId = await EnqueueComputing(paramSet);

                if (!string.IsNullOrEmpty(backtestId))
                {
                    _parameterSetForBacktest.TryAdd(backtestId, paramSet);
                }
                else
                {
                    Log.Error($"Optimization compute job could not be placed into the queue");
                }
            };

            Optimizer.PushNewResults(null);
        }

        public virtual void Abort() { }

        protected virtual async Task<string> RunLean(ParameterSet parameterSet)
        {
            throw new NotImplementedException();
        }

        protected virtual void NewResult(string jsonString, string backtestId)
        {
            ParameterSet parameterSet;
            if (_parameterSetForBacktest.TryGetValue(backtestId, out parameterSet))
            {
                var value = JObject.Parse(jsonString).SelectToken(NodePacket.Criterion["name"]).Value<decimal>();
                Optimizer.PushNewResults(new OptimizationResult(value, parameterSet));
            }
            else
            {
                Log.Error($"Optimization compute job with id '{backtestId}' was not found");
            }
        }

        private async Task<string> EnqueueComputing(ParameterSet parameterSet)
        {
            var result = await RunLean(parameterSet);

            return await Task.FromResult(result);
        }
    }
}
