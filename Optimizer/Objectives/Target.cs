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
using System;

namespace QuantConnect.Optimizer.Objectives
{
    /// <summary>
    /// The optimization statistical target
    /// </summary>
    public class Target: Objective
    {
        /// <summary>
        /// Defines the direction of optimization, i.e. maximization or minimization
        /// </summary>
        [JsonProperty("extremum")]
        public Extremum Extremum { get; }

        /// <summary>
        /// Current value
        /// </summary>
        [JsonIgnore]
        public decimal? Current { get; private set; }

        /// <summary>
        /// Fires when target complies specified value
        /// </summary>
        public event EventHandler Reached;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public Target(string target, Extremum extremum, decimal? targetValue): base(target, targetValue)
        {
            Extremum = extremum;
        }

        /// <summary>
        /// Pretty representation of this optimization target
        /// </summary>
        public override string ToString()
        {
            if (TargetValue.HasValue)
            {
                return $"Target: {Target} TargetValue: {TargetValue.Value} at: {Current}";
            }
            return $"Target: {Target} at: {Current}";
        }

        /// <summary>
        /// Check backtest result
        /// </summary>
        /// <param name="jsonBacktestResult">Backtest result json</param>
        /// <returns>true if found a better solution; otherwise false</returns>
        public bool MoveAhead(string jsonBacktestResult)
        {
            if (string.IsNullOrEmpty(jsonBacktestResult))
            {
                throw new ArgumentNullException(nameof(jsonBacktestResult), "Target.MoveAhead: backtest result can not be null or empty.");
            }

            var token = JObject.Parse(jsonBacktestResult).SelectToken(Target);
            if (token == null)
            {
                return false;
            }
            var computedValue = token.Value<string>().ToNormalizedDecimal();
            if (!Current.HasValue || Extremum.Better(Current.Value, computedValue))
            {
                Current = computedValue;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Try comply target value
        /// </summary>
        public void CheckCompliance()
        {
            if (IsComplied())
            {
                Reached?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool IsComplied() => TargetValue.HasValue && Current.HasValue && (TargetValue.Value == Current.Value || Extremum.Better(TargetValue.Value, Current.Value));
    }
}
