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
using System.Linq;

namespace QuantConnect.Optimizer
{
    public class Constraint
    {
        [JsonProperty("target")]
        public string Objective { get; }

        [JsonProperty("operator")]
        public ComparisonOperatorTypes Operator { get; }

        [JsonProperty("target-value")]
        public decimal TargetValue { get; }

        public Constraint(string target, ComparisonOperatorTypes op, decimal? targetValue)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException("target", $"Constraint object can't be null or empty");
            }

            if (!targetValue.HasValue)
            {
                throw new ArgumentNullException("targetValue", $"Constraint target is not specified");
            }

            var _objective = target;
            if (!_objective.Contains("."))
            {
                // default path
                _objective = $"Statistics.{_objective}";
            }
            // escape empty space in json path
            Objective = string.Join(".", _objective.Split('.').Select(s => $"['{s}']"));

            Operator = op;
            TargetValue = targetValue.Value;
        }

        public bool IsMet(string jsonBacktestResult) => Operator.Compare(
            JObject.Parse(jsonBacktestResult).SelectToken(Objective).Value<string>().ToDecimal(),
            TargetValue);
    }
}
