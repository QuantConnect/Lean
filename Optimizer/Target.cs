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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Optimizer
{
    public class Target
    {
        [JsonProperty("objective")]
        public string Objective { get; }

        /// <summary>
        /// Defines the direction of optimization, i.e. maximization or minimization
        /// </summary>
        public Extremum Extremum { get; }

        /// <summary>
        /// Current value
        /// </summary>
        public decimal? Current { get; private set; }

        /// <summary>
        /// Target value; if defined and backtest complies with the targets then finish
        /// </summary>
        public decimal? TargetValue { get;}

        public Target(string objective, Extremum extremum, decimal? targetValue)
        {
            Objective = objective;
            Extremum = extremum;
        }

        public bool MoveAhead(string jsonBacktestResult)
        {
            var computedValue = JObject.Parse(jsonBacktestResult).SelectToken(Objective).Value<decimal>();
            if (!Current.HasValue || Extremum.Better(Current.Value, computedValue))
            {
                Current = computedValue;
                return true;
            }

            return false;
        }

        public bool IsComplied() => TargetValue.HasValue && Current.HasValue && Extremum.Better(Current.Value, TargetValue.Value);
    }
}
