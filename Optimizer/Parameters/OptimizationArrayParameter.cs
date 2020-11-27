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
using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Optimizer.Parameters
{
    /// <summary>
    /// Defines the array based optimization parameter
    /// </summary>
    public class OptimizationArrayParameter : OptimizationParameter
    {
        /// <summary>
        /// The discrete set of optimization parameters to use
        /// </summary>
        [JsonProperty("values")]
        public IList<string> Values { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public OptimizationArrayParameter(string name, IList<string> values) : base(name)
        {
            Values = values;
            if (Values == null || Values.Count == 0)
            {
                throw new ArgumentException("Array based optimization parameter cannot be null or empty", nameof(values));
            }
        }

        public override IEnumerator<string> GetEnumerator() => new OptimizationArrayParameterEnumerator(this);

        /// <summary>
        /// Calculates number od data points for step based optimization parameter based on min/max and step values
        /// </summary>
        /// <returns></returns>
        public override int Estimate()
        {
            return Values.Count;
        }
    }
}
