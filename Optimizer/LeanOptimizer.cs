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
using System.Runtime;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Optimizer
{
    public abstract class LeanOptimizer
    {
        private readonly ConcurrentDictionary<string, ParameterSet> _parameterSetForBacktest;

        protected IOptimizationStrategy Strategy;
        protected OptimizationNodePacket NodePacket;

        public LeanOptimizer(OptimizationNodePacket nodePacket)
        {
            if (nodePacket.OptimizationParameters.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Cannot start an optimization job with no parameter to optimize");
            }

            NodePacket = nodePacket;
            Strategy = Composer.Instance.GetExportedValueByTypeName<IOptimizationStrategy>(NodePacket.OptimizationStrategy);

            _parameterSetForBacktest = new ConcurrentDictionary<string, ParameterSet>();

            Strategy.Initialize(
                Composer.Instance.GetExportedValueByTypeName<IOptimizationParameterSetGenerator>(NodePacket.ParameterSetGenerator),
                NodePacket.Criterion["extremum"] == "max"
                    ? new Maximization() as Extremum
                    : new Minimization(),
                NodePacket.OptimizationParameters);

            Strategy.NewParameterSet += async (s, e) =>
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

            var empty = OptimizationResult.Empty;
            Strategy.PushNewResults(empty);
        }

        public virtual void OnComplete() { }

        public virtual void Abort() { }

        protected abstract Task<string> RunLean(ParameterSet parameterSet);

        protected virtual void NewResult(string jsonString, string backtestId)
        {
            OptimizationResult result;
            lock (_parameterSetForBacktest)
            {
                ParameterSet parameterSet;
                if (!_parameterSetForBacktest.TryRemove(backtestId, out parameterSet))
                {
                    Log.Error($"Optimization compute job with id '{backtestId}' was not found");
                    return;
                }
                var value = JObject.Parse(jsonString).SelectToken(NodePacket.Criterion["name"]).Value<decimal>();

                result = new OptimizationResult(value, parameterSet);
            }

            Strategy.PushNewResults(result);

            if (!_parameterSetForBacktest.Any())
            {
                OnComplete();
            }
        }

        private async Task<string> EnqueueComputing(ParameterSet parameterSet)
        {
            var result = await RunLean(parameterSet);

            return await Task.FromResult(result);
        }
    }
}
