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
using QuantConnect.Util;
using System;

namespace QuantConnect.Optimizer
{
    public class Constraint
    {
        [JsonProperty("objective")]
        public string Objective { get; set; }

        [JsonProperty("operator")]
        public ComparisonOperatorTypes Operator { get; set; }

        [JsonProperty("value")]
        public decimal TargetValue { get; set; }

        public Constraint(string objective, ComparisonOperatorTypes op, decimal? targetValue)
        {
            if (string.IsNullOrEmpty(objective))
            {
                throw new ArgumentNullException("objective", $"Constraint object can't be null or empty");
            }

            if (!targetValue.HasValue)
            {
                throw new ArgumentNullException("targetValue", $"Constraint target is not specified");
            }

            Objective = objective;
            Operator = op;
            TargetValue = targetValue.Value;
        }

        public bool IsMet(string jsonBacktestResult) => Operator.Compare(
            TargetValue, 
            JObject.Parse(jsonBacktestResult).SelectToken(Objective).Value<decimal>());
    }
}
