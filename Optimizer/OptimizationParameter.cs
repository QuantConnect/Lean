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

using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace QuantConnect.Optimizer
{
    public class OptimizationParameter
    {
        public string Name { get; }
        [JsonProperty("min")]
        public decimal MinValue { get; }
        [JsonProperty("max")]
        public decimal MaxValue { get; }
        public decimal Step { get; }

        public OptimizationParameter(string name, decimal min, decimal max, decimal step)
        {
            Name = name;
            MinValue = min;
            MaxValue = max;
            Step = step;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(OptimizationParameter other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other?.Name);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as OptimizationParameter);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode() => this.Name.GetHashCode();
    }
}
