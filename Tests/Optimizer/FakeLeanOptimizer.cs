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
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Parameters;

namespace QuantConnect.Tests.Optimizer
{
    public class FakeLeanOptimizer : LeanOptimizer
    {
        private readonly HashSet<string> _backtests = new HashSet<string>();

        public event EventHandler Update;

        public FakeLeanOptimizer(OptimizationNodePacket nodePacket)
            : base(nodePacket)
        {
        }

        protected override string RunLean(ParameterSet parameterSet)
        {
            var id = Guid.NewGuid().ToString();
            _backtests.Add(id);

            Task.Delay(100).ContinueWith(task =>
            {
                try
                {
                    var sum = parameterSet.Value.Where(pair => pair.Key != "skipFromResultSum").Sum(s => s.Value.ToDecimal());
                    if (sum != 29)
                    {
                        NewResult(BacktestResult.Create(sum, sum / 100).ToJson(), id);
                    }
                    else
                    {
                        // fail some backtests by passing empty json
                        NewResult(string.Empty, id);
                    }
                }
                catch
                {
                }
            });

            return id;
        }

        protected override void AbortLean(string backtestId)
        {
            _backtests.Remove(backtestId);
        }

        protected override void SendUpdate()
        {
            OnUpdate();
        }

        private void OnUpdate()
        {
            Update?.Invoke(this, EventArgs.Empty);
        }
    }
}
