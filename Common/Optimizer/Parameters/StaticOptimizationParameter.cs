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

namespace QuantConnect.Optimizer.Parameters
{
    /// <summary>
    /// Defines the step based optimization parameter
    /// </summary>
    public class StaticOptimizationParameter : OptimizationParameter
    {
        /// <summary>
        /// Minimum value of optimization parameter, applicable for boundary conditions
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="value">The fixed value of this parameter</param>
        public StaticOptimizationParameter(string name, string value) : base(name)
        {
            Value = value;
        }
    }
}
