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
using System.Threading.Tasks;
using QuantConnect.Util;

namespace QuantConnect.Optimizer
{
    public class LeanOptimizer
    {
        protected IOptimizationManager Optimizer;
        protected OptimizationNodePacket NodePacket;

        public LeanOptimizer(OptimizationNodePacket nodePacket)
        {
            if (nodePacket.OptimizationParameters.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Cannot start an optimization job with no parameter to optimize");
            }

            NodePacket = nodePacket;
            Optimizer = Composer.Instance.GetExportedValueByTypeName<IOptimizationManager>(NodePacket.OptimizationManager);

            Optimizer.Initialize(
                Composer.Instance.GetExportedValueByTypeName<IOptimizationStrategy>(NodePacket.OptimizationStrategy),
                NodePacket.Criterion["extremum"] == "max"
                    ? new Maximization() as Extremum
                    : new Minimization(),
                NodePacket.OptimizationParameters);

            Optimizer.NewSuggestion += (s, e) =>
            {
                if ((e as OptimizationEventArgs)?.ParameterSet == null) return;

                var result = RunLean((e as OptimizationEventArgs)?.ParameterSet);
                Optimizer.PushNewResults(new OptimizationResult(result, (e as OptimizationEventArgs)?.ParameterSet));
            };

            Optimizer.PushNewResults(null);
        }

        public virtual void Abort() { }

        protected virtual decimal RunLean(ParameterSet suggestion) => suggestion.Arguments.Sum(s => s.Value);
    }
}
