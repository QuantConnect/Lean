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
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace QuantConnect.Optimizer.Objectives
{
    /// <summary>
    /// A backtest optimization constraint.
    /// Allows specifying statistical constraints for the optimization, eg. a backtest can't have a DrawDown less than 10%
    /// </summary>
    public class Constraint : Objective
    {
        /// <summary>
        /// The target comparison operation, eg. 'Greater'
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ComparisonOperatorTypes Operator { get; set; }

        public Constraint()
        {

        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public Constraint(string target, ComparisonOperatorTypes @operator, decimal? targetValue) : base(target, targetValue)
        {
            Operator = @operator;

            if (!TargetValue.HasValue)
            {
                throw new ArgumentNullException(nameof(targetValue), Messages.Constraint.ConstraintTargetValueNotSpecified);
            }
        }

        /// <summary>
        /// Asserts the constraint is met
        /// </summary>
        public bool IsMet(string jsonBacktestResult)
        {
            if (string.IsNullOrEmpty(jsonBacktestResult))
            {
                throw new ArgumentNullException(nameof(jsonBacktestResult), $"Constraint.IsMet(): {Messages.OptimizerObjectivesCommon.NullOrEmptyBacktestResult}");
            }

            var token = JObject.Parse(jsonBacktestResult).SelectToken(Target);
            if (token == null)
            {
                return false;
            }

            return Operator.Compare(
                token.Value<string>().ToNormalizedDecimal(),
                TargetValue.Value);
        }

        /// <summary>
        /// Pretty representation of a constraint
        /// </summary>
        public override string ToString()
        {
            return $"{Target} '{Operator}' {TargetValue.Value}";
        }
    }
}
