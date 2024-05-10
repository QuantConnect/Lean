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
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace QuantConnect.Optimizer.Objectives
{
    /// <summary>
    /// Base class for optimization <see cref="Objectives.Target"/> and <see cref="Constraint"/>
    /// </summary>
    public abstract class Objective
    {
        private readonly Regex _targetTemplate = new Regex("['(.+)']");
        private string _target;

        /// <summary>
        /// Target; property of json file we want to track
        /// </summary>
        public string Target
        {
            get => _target;
            set
            {
                _target = value != null ? string.Join(".", value.Split('.').Select(s => _targetTemplate.Match(s).Success ? s : $"['{s}']")) : value;
            }
        }

        /// <summary>
        /// Target value
        /// </summary>
        /// <remarks>For <see cref="Objectives.Target"/> if defined and backtest complies with the targets then finish optimization</remarks>
        /// <remarks>For <see cref="Constraint"/> non optional, the value of the target constraint</remarks>
        public decimal? TargetValue { get; set; }

        protected Objective()
        {

        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        protected Objective(string target, decimal? targetValue)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException(nameof(target), Messages.Objective.NullOrEmptyObjective);
            }

            var objective = target;
            if (!objective.Contains("."))
            {
                // default path
                objective = $"Statistics.{objective}";
            }
            // escape empty space in json path
            Target = objective;
            TargetValue = targetValue;
        }

        #region Backwards Compatibility
        /// <summary>
        /// Target value
        /// </summary>
        /// <remarks>For <see cref="Objectives.Target"/> if defined and backtest complies with the targets then finish optimization</remarks>
        /// <remarks>For <see cref="Constraint"/> non optional, the value of the target constraint</remarks>
        [JsonProperty("target-value")]
        private decimal? OldTargetValue
        {
            set
            {
                TargetValue = value;
            }
        }
        #endregion
    }
}
